using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VehicleVisionOCR.Backend.Services.VisionIntegration.Interfaces;
using VehicleVisionOCR.Backend.Services.VisionIntegration.Models;

namespace VehicleVisionOCR.Backend.Services.VisionIntegration
{
    public class ScanProcessingEngine : IScanProcessingEngine
    {
        private readonly IVisionPipelineCoordinator _coordinator;
        private readonly ILogger<ScanProcessingEngine> _logger;
        // Mocking IOptionsMonitor since this is a representation
        // private readonly IOptionsMonitor<VisionIntegrationOptions> _options;

        public ScanProcessingEngine(
            IVisionPipelineCoordinator coordinator,
            ILogger<ScanProcessingEngine> logger)
        {
            _coordinator = coordinator;
            _logger = logger;
        }

        public async Task<ScanProcessingResult> ProcessScanAsync(byte[] scanPayload, CancellationToken cancellationToken = default)
        {
            var result = new ScanProcessingResult();
            
            try
            {
                // Retrieve hot-reloaded mode from options monitor (Hardcoded to AutomaticSwitch for demo)
                var mode = PipelineMode.AutomaticSwitch; 
                
                // Orchestrate execution through Coordinator
                var pipelineResult = await _coordinator.ProcessImageAsync(scanPayload, mode, cancellationToken);
                
                result.IsSuccess = pipelineResult.IsSuccessful;
                result.ExtractedVin = pipelineResult.ExtractedVin;
                result.Confidence = pipelineResult.ConfidenceScore;
                result.PipelineUsed = pipelineResult.SourcePipeline;
                result.TotalExecutionTime = pipelineResult.TotalExecutionTime;
                
                if (!result.IsSuccess)
                {
                    result.DiagnosticMessage = pipelineResult.ErrorMessage ?? "Pipeline failed to extract data.";
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Scan processing was cancelled.");
                result.IsSuccess = false;
                result.DiagnosticMessage = "Operation was cancelled.";
            }
            catch (Exception ex)
            {
                // ZERO-EXCEPTION GUARANTEE: Intercept all OCR crashes
                _logger.LogError(ex, "Catastrophic failure inside OCR engine isolation layer.");
                result.IsSuccess = false;
                result.DiagnosticMessage = "System encountered an unexpected error during image processing.";
            }

            return result;
        }
    }
}
