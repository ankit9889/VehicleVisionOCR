using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VehicleVisionOCR.OCR.Core.Interfaces;
using VehicleVisionOCR.OCR.Core.Models;

namespace VehicleVisionOCR.OCR.Core.Pipeline
{
    public class ImagePipeline : IImagePipeline
    {
        private readonly List<IImagePreprocessor> _processors;
        private readonly ILogger<ImagePipeline> _logger;

        public ImagePipeline(ILogger<ImagePipeline> logger)
        {
            _processors = new List<IImagePreprocessor>();
            _logger = logger;
        }

        public void AddProcessor(IImagePreprocessor processor)
        {
            if (processor != null)
            {
                _processors.Add(processor);
            }
        }

        public async Task<byte[]> ExecuteAsync(byte[] image, ProcessingProfile profile)
        {
            if (image == null || image.Length == 0)
                throw new ArgumentException("Image data cannot be null or empty", nameof(image));

            byte[] currentImage = image;

            foreach (var processor in _processors)
            {
                try
                {
                    _logger.LogInformation($"Executing preprocessor: {processor.GetType().Name}");
                    currentImage = await processor.ProcessAsync(currentImage, profile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error executing preprocessor {processor.GetType().Name}");
                    // Fallback to original image if preprocessing fails, or throw depending on strictness
                    // For now, continue with the previous state of the image
                }
            }

            return currentImage;
        }
    }
}
