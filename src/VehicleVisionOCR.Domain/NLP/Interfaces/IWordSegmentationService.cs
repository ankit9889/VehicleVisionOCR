using System.Collections.Generic;

namespace VehicleVisionOCR.Domain.NLP.Interfaces
{
    public interface IWordSegmentationService
    {
        List<string> Segment(string text);
    }
}
