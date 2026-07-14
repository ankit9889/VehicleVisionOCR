using System;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using VehicleVisionOCR.OCR.Core.Interfaces;
using VehicleVisionOCR.OCR.Core.Models;

namespace VehicleVisionOCR.OCR.Tesseract.Preprocessors
{
    public class ImageDeskew : OpenCvImagePreprocessor, IImageDeskew
    {
        public ImageDeskew(ILogger<ImageDeskew> logger) : base(logger) { }

        protected override bool ShouldProcess(ProcessingProfile profile) => profile.EnableDeskew;

        protected override void ProcessMat(Mat src, Mat dst, ProcessingProfile profile)
        {
            _logger.LogInformation("Applying Image Deskew.");

            using var gray = new Mat();
            
            if (src.Channels() == 3 || src.Channels() == 4)
                Cv2.CvtColor(src, gray, ColorConversionCodes.BGR2GRAY);
            else
                src.CopyTo(gray);

            // Binarize
            using var thresh = new Mat();
            Cv2.Threshold(gray, thresh, 0, 255, ThresholdTypes.BinaryInv | ThresholdTypes.Otsu);

            // Get coordinates of all non-zero pixels
            var points = new Mat();
            Cv2.FindNonZero(thresh, points);

            if (points.Rows > 0)
            {
                var box = Cv2.MinAreaRect(points);
                double angle = box.Angle;

                // Adjust angle
                if (angle < -45)
                    angle += 90;

                // Create rotation matrix
                var center = new Point2f(src.Cols / 2f, src.Rows / 2f);
                using var rotMat = Cv2.GetRotationMatrix2D(center, angle, 1.0);

                // Rotate
                Cv2.WarpAffine(src, dst, rotMat, src.Size(), InterpolationFlags.Cubic, BorderTypes.Replicate);
            }
            else
            {
                src.CopyTo(dst);
            }
        }
    }
}
