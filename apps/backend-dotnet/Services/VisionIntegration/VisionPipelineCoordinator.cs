using System.Threading.Tasks;
using VehicleVisionOCR.Backend.Services.VisionIntegration.Interfaces;
using VehicleVisionOCR.Backend.Services.VisionIntegration.Models;

namespace VehicleVisionOCR.Backend.Services.VisionIntegration
{
    public class VisionPipelineCoordinator : IVisionPipelineCoordinator
    {
        private readonly LegacyPipelineAdapter _legacyAdapter;
        private readonly ModernVisionPipeline _modernPipeline;
        private readonly PipelineComparisonService _comparisonService;
        private readonly ResultDecisionService _decisionService;
        private readonly MigrationStatisticsService _statisticsService;

        public VisionPipelineCoordinator(
            LegacyPipelineAdapter legacyAdapter,
            ModernVisionPipeline modernPipeline,
            PipelineComparisonService comparisonService,
            ResultDecisionService decisionService,
            MigrationStatisticsService statisticsService)
        {
            _legacyAdapter = legacyAdapter;
            _modernPipeline = modernPipeline;
            _comparisonService = comparisonService;
            _decisionService = decisionService;
            _statisticsService = statisticsService;
        }

        public async Task<PipelineExecutionResult> ProcessImageAsync(byte[] imageBytes, PipelineMode mode, System.Threading.CancellationToken cancellationToken = default)
        {
            // If in LegacyOnly, completely bypass modern execution to save resources
            if (mode == PipelineMode.LegacyOnly)
            {
                return await _legacyAdapter.ExecuteAsync(imageBytes, cancellationToken);
            }

            // Execute BOTH concurrently for ShadowMode, AutomaticSwitch, or ModernOnly (as a fallback)
            var legacyTask = _legacyAdapter.ExecuteAsync(imageBytes, cancellationToken);
            var modernTask = _modernPipeline.ExecuteAsync(imageBytes, cancellationToken);

            await Task.WhenAll(legacyTask, modernTask);

            var legacyResult = legacyTask.Result;
            var modernResult = modernTask.Result;

            // Perform Shadow Comparison
            var comparison = _comparisonService.Compare(legacyResult, modernResult);
            
            // Fire and forget logging (do not await so UI doesn't block on DB inserts)
            _ = _statisticsService.LogComparisonAsync(comparison);

            // Decide which result to return to the UI
            return _decisionService.Decide(legacyResult, modernResult, mode);
        }
    }
}
