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
            var ocrManager = scope.ServiceProvider.GetRequiredService<IOcrManager>();
            var vehicleRepo = scope.ServiceProvider.GetRequiredService<IVehicleRepository>();
            var scanRepo = scope.ServiceProvider.GetRequiredService<IScanRepository>();
            var validationEngine = scope.ServiceProvider.GetRequiredService<IValidationEngine>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ScannerHub>>();

            var ocrCorrectionCoordinator = scope.ServiceProvider.GetRequiredService<VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces.IOcrCorrectionCoordinator>();

            try
            {
                _logger.LogInformation("Starting OCR processing on queued image.");
                
                var request = new VehicleVisionOCR.OCR.Core.Models.OcrRequest 
                { 
                    ImageData = queuedImage.ImageData
                };
                
                var response = await ocrManager.ProcessAsync(request, VehicleVisionOCR.OCR.Core.Enums.OcrEngineType.Tesseract);

                if (response.Status != VehicleVisionOCR.OCR.Core.Enums.OcrStatus.Success || response.Result == null)
                {
                    _logger.LogWarning("OCR failed to extract data.");
                    return;
                }

                _logger.LogInformation($"OCR completed. Confidence: {response.Result.OverallConfidence.Percentage}%");

                // Map to domain entity and validate
                var vehicle = new Vehicle
                {
                    Type = VehicleVisionOCR.Domain.Enums.VehicleType.Unknown,
                    SyncStatus = VehicleVisionOCR.Domain.Enums.SyncStatus.Pending
                };

                foreach (var field in response.Result.ExtractedFields)
                {
                    if (field.Value == "NULL" || string.IsNullOrWhiteSpace(field.Value))
                        continue;

                    if (field.Key == "VIN")
                    {
                        var vinResult = await ocrCorrectionCoordinator.ProcessFieldAsync(VehicleVisionOCR.Backend.Services.OcrCorrection.Enums.TargetFieldType.VIN, field.Value, response.Result.OverallConfidence.Percentage);
                        if (vinResult.IsValid)
                        {
                            vehicle.Vin = new VIN(vinResult.CorrectedText);
                        }
                    }
                    else if (field.Key == "LicensePlate" || field.Key == "Barcode") 
                    {
                        vehicle.RegistrationNumber = new RegistrationNumber(field.Value);
                    }
                    else if (field.Key == "Color")
                    {
                        var colorResult = await ocrCorrectionCoordinator.ProcessFieldAsync(VehicleVisionOCR.Backend.Services.OcrCorrection.Enums.TargetFieldType.Color, field.Value, response.Result.OverallConfidence.Percentage);
                        if (colorResult.IsValid)
                        {
                            vehicle.Color = colorResult.CorrectedText;
                        }
                    }
                }

                // If color was not found in specific ExtractedFields, attempt a fallback run against the entire RawText
                if (string.IsNullOrWhiteSpace(vehicle.Color) && !string.IsNullOrWhiteSpace(response.Result.RawText))
                {
                    var colorResult = await ocrCorrectionCoordinator.ProcessFieldAsync(VehicleVisionOCR.Backend.Services.OcrCorrection.Enums.TargetFieldType.Color, response.Result.RawText, response.Result.OverallConfidence.Percentage);
                    if (colorResult.IsValid && colorResult.WasCorrected)
                    {
                        vehicle.Color = colorResult.CorrectedText;
                    }
                }

                bool isValid = true;
                if (vehicle.Vin != null) isValid = isValid && validationEngine.ValidateVin(vehicle.Vin.Value);
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
                                RawText = response.Result.RawText,
                                ExtractedFields = response.Result.ExtractedFields,
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
                        RawExtractedText = response.Result.RawText
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
                        Confidence = response.Result.OverallConfidence.Percentage,
                        ImageId = scan.Images.FirstOrDefault()?.Id,
                        RawText = response.Result.RawText
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
                        RawText = response.Result.RawText,
                        ExtractedFields = response.Result.ExtractedFields
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
