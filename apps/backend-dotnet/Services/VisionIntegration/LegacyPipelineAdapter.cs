using System;
using System.Diagnostics;
using System.Threading.Tasks;
using VehicleVisionOCR.Backend.Services.VisionIntegration.Models;

namespace VehicleVisionOCR.Backend.Services.VisionIntegration
{
    public class LegacyPipelineAdapter
    {
        // Inject legacy engine here
        // private readonly ILegacyOcrEngine _legacyEngine;

        public async Task<PipelineExecutionResult> ExecuteAsync(byte[] imageBytes, CancellationToken cancellationToken = default)
        {
            var sw = Stopwatch.StartNew();
            var result = new PipelineExecutionResult { SourcePipeline = "Legacy" };

            try
            {
                // Simulate legacy execution
                await Task.Delay(250, cancellationToken); 
                
                result.ExtractedVin = "ME4MC77HGTA667788"; // MOCK Legacy result
                result.IsSuccessful = true;
                result.ConfidenceScore = 85.0;
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                sw.Stop();
                result.TotalExecutionTime = sw.Elapsed;
            }

            return result;
        }
    }
}
