using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using VehicleVisionOCR.Scanner.Core.Interfaces;
using VehicleVisionOCR.OCR.Core.Interfaces;
using VehicleVisionOCR.Application.Interfaces.Repositories;
using VehicleVisionOCR.Domain.Entities;
using VehicleVisionOCR.Domain.ValueObjects;
using VehicleVisionOCR.Backend.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using VehicleVisionOCR.Backend.Hubs;

namespace VehicleVisionOCR.Backend.BackgroundServices
{
    public class ScanWorkflowService : BackgroundService
    {
        private readonly ILogger<ScanWorkflowService> _logger;
        private readonly IScannerManager _scannerManager;
        private readonly ImageProcessingQueue _queue;
        private readonly IServiceProvider _serviceProvider;

        public ScanWorkflowService(
            ILogger<ScanWorkflowService> logger,
            IScannerManager scannerManager,
            ImageProcessingQueue queue,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _scannerManager = scannerManager;
            _queue = queue;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scan Workflow Service is starting.");

            // Subscribe to scanner events
            _scannerManager.Events.OnImageCaptured += async (sender, args) =>
            {
                _logger.LogInformation($"Image captured from scanner {args.ScannerId}. Queuing for OCR.");
                await _queue.EnqueueAsync(args.ScannerId, args.Image.Data);
            };

            _scannerManager.Events.OnDisconnected += (sender, args) =>
            {
                _logger.LogWarning($"Scanner disconnected: {args.ScannerId}");
            };

            _scannerManager.Events.OnError += (sender, args) =>
            {
                _logger.LogError(args.Exception, $"Scanner Error [{args.ScannerId}]: {args.ErrorMessage}");
            };

            // Initialize scanners
            await _scannerManager.InitializeAsync();

            // Start consuming the queue
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var queuedImage = await _queue.DequeueAsync(stoppingToken);
                    await ProcessImageAsync(queuedImage, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Graceful shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing queued image.");
                }
            }
        }

        private async Task ProcessImageAsync(QueuedImage queuedImage, CancellationToken token)
        {
            using var scope = _serviceProvider.CreateScope();
            var processingEngine = scope.ServiceProvider.GetRequiredService<VehicleVisionOCR.Backend.Services.VisionIntegration.Interfaces.IScanProcessingEngine>();
            var vehicleRepo = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();
            var scanRepo = scope.ServiceProvider.GetRequiredService<IScanRepository>();
            var validationEngine = scope.ServiceProvider.GetRequiredService<IValidationEngine>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ScannerHub>>();

            try
            {
                _logger.LogInformation("Starting isolated OCR processing layer on queued image.");
                
                var processingResult = await processingEngine.ProcessScanAsync(queuedImage.ImageData, token);

                if (!processingResult.IsSuccess || string.IsNullOrEmpty(processingResult.ExtractedVin))
                {
                    _logger.LogWarning("Isolated OCR processing engine failed to extract valid data. Diagnostic: {Message}", processingResult.DiagnosticMessage);
                    await SendFeedbackAsync(queuedImage.ScannerId, VehicleVisionOCR.Scanner.Core.Enums.BeepPattern.Error);
                    
                    // Broadcast failure to React UI
                    await hubContext.Clients.All.SendAsync("OnScanProcessed", new 
                    {
                        Success = false,
                        Message = processingResult.DiagnosticMessage ?? "Validation failed. Please verify the data manually."
                    });
                    return;
                }

                _logger.LogInformation($"OCR completed via {processingResult.PipelineUsed}. Confidence: {processingResult.Confidence}%");

                // Map to domain entity and validate
                var vehicle = new Vehicle
                {
                    Type = VehicleVisionOCR.Domain.Enums.VehicleType.Unknown,
                    SyncStatus = VehicleVisionOCR.Domain.Enums.SyncStatus.Pending
                };

                vehicle.Vin = new VIN(processingResult.ExtractedVin);
                
                if (!string.IsNullOrWhiteSpace(processingResult.ExtractedRegistrationNumber))
                {
                    vehicle.RegistrationNumber = new RegistrationNumber(processingResult.ExtractedRegistrationNumber);
                }

                if (!string.IsNullOrWhiteSpace(processingResult.ExtractedColor))
                {
                    vehicle.Color = processingResult.ExtractedColor;
                }

                bool isValid = validationEngine.ValidateVin(vehicle.Vin.Value);
                if (vehicle.RegistrationNumber != null) isValid = isValid && validationEngine.ValidateLicensePlate(vehicle.RegistrationNumber.Value);
                
                if (isValid)
                {
                    // Check for duplicates
                    if (vehicle.RegistrationNumber != null)
                    {
                        var existingVehicle = await vehicleRepo.GetByRegistrationNumberAsync(vehicle.RegistrationNumber);
                        if (existingVehicle != null)
                        {
                            _logger.LogWarning($"Duplicate barcode detected: {vehicle.RegistrationNumber.Value}");
                            await SendFeedbackAsync(queuedImage.ScannerId, VehicleVisionOCR.Scanner.Core.Enums.BeepPattern.Warning);
                            
                            // Save image to disk so it can be linked if user chooses to "Update"
                            string? savedFileName = null;
                            if (queuedImage.ImageData != null && queuedImage.ImageData.Length > 0)
                            {
                                var imgDir = System.IO.Path.Combine(System.Environment.CurrentDirectory, "ScanData", "Images");
                                if (!System.IO.Directory.Exists(imgDir))
                                {
                                    System.IO.Directory.CreateDirectory(imgDir);
                                }
                                savedFileName = $"{System.Guid.NewGuid()}.jpg";
                                var filePath = System.IO.Path.Combine(imgDir, savedFileName);
                                await System.IO.File.WriteAllBytesAsync(filePath, queuedImage.ImageData, token);
                            }

                            await hubContext.Clients.All.SendAsync("OnScanProcessed", new 
                            {
                                Success = false,
                                IsDuplicate = true,
                                VehicleId = existingVehicle.Id,
                                ImageFileName = savedFileName,
                                Message = $"Duplicate Barcode Detected: {vehicle.RegistrationNumber.Value}.",
                                RawText = processingResult.RawText,
                                ExtractedFields = processingResult.ExtractedFields,
                                RegistrationNumber = vehicle.RegistrationNumber.Value,
                                Color = vehicle.Color
                            });
                            return;
                        }
                    }

                    await vehicleRepo.AddAsync(vehicle);
                    
                    var scan = new VehicleScan
                    {
                        VehicleId = vehicle.Id,
                        Status = VehicleVisionOCR.Domain.Enums.ScanStatus.Initiated, // Wait for user to confirm in UI
                        RawExtractedText = processingResult.RawText
                    };

                    // Save Image
                    if (queuedImage.ImageData != null && queuedImage.ImageData.Length > 0)
                    {
                        var imgDir = System.IO.Path.Combine(System.Environment.CurrentDirectory, "ScanData", "Images");
                        if (!System.IO.Directory.Exists(imgDir))
                        {
                            System.IO.Directory.CreateDirectory(imgDir);
                        }
                        var fileName = $"{System.Guid.NewGuid()}.jpg";
                        var filePath = System.IO.Path.Combine(imgDir, fileName);
                        await System.IO.File.WriteAllBytesAsync(filePath, queuedImage.ImageData, token);

                        scan.Images.Add(new ScanImage
                        {
                            LocalFilePath = filePath,
                            FileName = fileName,
                            ContentType = "image/jpeg",
                            FileSizeBytes = queuedImage.ImageData.Length
                        });
                    }

                    await scanRepo.AddAsync(scan);
                    
                    await unitOfWork.SaveChangesAsync(token);
                    
                    _logger.LogInformation($"Successfully processed and saved Vehicle {vehicle.Vin?.Value}");

                    await SendFeedbackAsync(queuedImage.ScannerId, VehicleVisionOCR.Scanner.Core.Enums.BeepPattern.Success);

                    // Broadcast to React UI
                    await hubContext.Clients.All.SendAsync("OnScanProcessed", new 
                    {
                        Success = true,
                        ScanId = scan.Id,
                        VehicleId = vehicle.Id,
                        Vin = vehicle.Vin?.Value,
                        RegistrationNumber = vehicle.RegistrationNumber?.Value,
                        Color = vehicle.Color,
                        Confidence = processingResult.Confidence,
                        ImageId = scan.Images.FirstOrDefault()?.Id,
                        RawText = processingResult.RawText
                    });
                }
                else
                {
                    _logger.LogWarning("Validation failed for extracted vehicle data.");
                    await SendFeedbackAsync(queuedImage.ScannerId, VehicleVisionOCR.Scanner.Core.Enums.BeepPattern.Error);

                    // Broadcast failure to React UI for manual correction
                    await hubContext.Clients.All.SendAsync("OnScanProcessed", new 
                    {
                        Success = false,
                        Message = "Validation failed. Please verify the data manually.",
                        RawText = processingResult.RawText,
                        ExtractedFields = processingResult.ExtractedFields
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error during OCR pipeline execution.");
                await SendFeedbackAsync(queuedImage.ScannerId, VehicleVisionOCR.Scanner.Core.Enums.BeepPattern.Error);
            }
        }

        private async Task SendFeedbackAsync(string scannerId, VehicleVisionOCR.Scanner.Core.Enums.BeepPattern pattern)
        {
            try
            {
                foreach (var scanner in _scannerManager.ActiveScanners)
                {
                    if (scanner.Info.Id == scannerId && scanner is IScannerFeedback feedbackScanner)
                    {
                        await feedbackScanner.SoundBeepAsync(pattern);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send hardware feedback to scanner.");
            }
        }
    }
}
