using System.Collections.Generic;
using OpenCvSharp;
using VehicleVisionOCR.Domain.Vision.Interfaces;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Infrastructure.Vision.OpenCV
{
    public class RegionDetector : IRegionDetector
    {
        public List<RegionCandidate> DetectRegions(byte[] normalizedImage)
        {
            var candidates = new List<RegionCandidate>();

            using var src = Cv2.ImDecode(normalizedImage, ImreadModes.Grayscale);
            if (src.Empty()) return candidates;

            int imgWidth = src.Cols;
            int imgHeight = src.Rows;

            // Step 1: Detect Barcode (Anchor Point)
            var barcodeRect = FindBarcode(src);
            if (barcodeRect != new Rect(0,0,0,0))
            {
                candidates.Add(new RegionCandidate
                {
                    X = barcodeRect.X,
                    Y = barcodeRect.Y,
                    Width = barcodeRect.Width,
                    Height = barcodeRect.Height,
                    Features = new RegionFeatures
                    {
                        IsBarcode = true,
                        Area = barcodeRect.Width * barcodeRect.Height,
                        AspectRatio = (double)barcodeRect.Width / barcodeRect.Height,
                        Width = barcodeRect.Width,
                        Height = barcodeRect.Height,
                        DistanceToBarcode = 0,
                        RelativeHorizontalPosition = (double)barcodeRect.X / imgWidth,
                        RelativeVerticalPosition = (double)barcodeRect.Y / imgHeight
                    }
                });
            }

            // Step 2: Detect Text Blocks
            var textRects = FindTextBlocks(src);
            foreach (var rect in textRects)
            {
                // Avoid adding the barcode again if the text detector picked it up
                if (barcodeRect != new Rect(0,0,0,0) && rect.IntersectsWith(barcodeRect))
                {
                    continue; // Skip overlapping boxes
                }

                int distanceToBc = barcodeRect != new Rect(0,0,0,0) ? CalculateDistance(rect, barcodeRect) : 9999;

                candidates.Add(new RegionCandidate
                {
                    X = rect.X,
                    Y = rect.Y,
                    Width = rect.Width,
                    Height = rect.Height,
                    Features = new RegionFeatures
                    {
                        IsBarcode = false,
                        Area = rect.Width * rect.Height,
                        AspectRatio = (double)rect.Width / rect.Height,
                        Width = rect.Width,
                        Height = rect.Height,
                        DistanceToBarcode = distanceToBc,
                        RelativeHorizontalPosition = (double)rect.X / imgWidth,
                        RelativeVerticalPosition = (double)rect.Y / imgHeight
                    }
                });
            }

            return candidates;
        }

        private Rect FindBarcode(Mat gray)
        {
            using var gradX = new Mat();
            using var gradY = new Mat();
            using var gradient = new Mat();

            // Scharr edge detection focuses heavily on vertical lines (barcodes)
            Cv2.Scharr(gray, gradX, MatType.CV_32F, 1, 0);
            Cv2.Scharr(gray, gradY, MatType.CV_32F, 0, 1);
            Cv2.Subtract(gradX, gradY, gradient);
            Cv2.ConvertScaleAbs(gradient, gradient);

            using var blurred = new Mat();
            Cv2.Blur(gradient, blurred, new Size(9, 9));

            using var thresh = new Mat();
            Cv2.Threshold(blurred, thresh, 225, 255, ThresholdTypes.Binary);

            // Morphological closing to group the vertical barcode lines into a solid block
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(21, 7));
            using var closed = new Mat();
            Cv2.MorphologyEx(thresh, closed, MorphTypes.Close, kernel);
            Cv2.Erode(closed, closed, new Mat(), null, 4);
            Cv2.Dilate(closed, closed, new Mat(), null, 4);

            Cv2.FindContours(closed, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            if (contours.Length == 0) return new Rect(0,0,0,0);

            // Sort by area, the largest block with high aspect ratio is usually the barcode
            Rect bestRect = new Rect(0,0,0,0);
            double maxArea = 0;

            foreach (var contour in contours)
            {
                var rect = Cv2.BoundingRect(contour);
                double area = rect.Width * rect.Height;
                double aspect = (double)rect.Width / rect.Height;

                if (area > maxArea && aspect > 2.0 && aspect < 10.0) // Barcodes are usually long horizontally
                {
                    maxArea = area;
                    bestRect = rect;
                }
            }

            return bestRect;
        }

        private List<Rect> FindTextBlocks(Mat gray)
        {
            var textBlocks = new List<Rect>();

            using var grad = new Mat();
            using var kernel3 = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(3, 3));
            Cv2.MorphologyEx(gray, grad, MorphTypes.Gradient, kernel3);

            using var thresh = new Mat();
            Cv2.Threshold(grad, thresh, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);

            // Dilate horizontally to group characters into words/lines
            using var kernelLine = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(30, 1));
            using var connected = new Mat();
            Cv2.MorphologyEx(thresh, connected, MorphTypes.Close, kernelLine);

            Cv2.FindContours(connected, out var contours, out _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

            foreach (var contour in contours)
            {
                var rect = Cv2.BoundingRect(contour);
                
                // Filter out tiny noise and massive blocks
                if (rect.Height > 10 && rect.Width > 20 && rect.Height < gray.Rows / 2)
                {
                    textBlocks.Add(rect);
                }
            }

            return textBlocks;
        }

        private int CalculateDistance(Rect a, Rect b)
        {
            // Calculate center points
            int aCenterX = a.X + (a.Width / 2);
            int aCenterY = a.Y + (a.Height / 2);
            
            int bCenterX = b.X + (b.Width / 2);
            int bCenterY = b.Y + (b.Height / 2);

            return (int)System.Math.Sqrt(System.Math.Pow(aCenterX - bCenterX, 2) + System.Math.Pow(aCenterY - bCenterY, 2));
        }
    }
}
