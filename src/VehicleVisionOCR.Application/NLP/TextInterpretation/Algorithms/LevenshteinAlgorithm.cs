using System;
using VehicleVisionOCR.Domain.NLP.Interfaces;

namespace VehicleVisionOCR.Application.NLP.TextInterpretation.Algorithms
{
    public class LevenshteinAlgorithm : ISimilarityAlgorithm
    {
        public string Name => "Damerau-Levenshtein";

        public double CalculateSimilarity(string source, string target)
        {
            if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target)) return 1.0;
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0.0;
            if (source == target) return 1.0;

            int sourceLength = source.Length;
            int targetLength = target.Length;

            // Stack-allocated matrix to avoid garbage collection for typical short words (< 50 chars)
            // If words are longer, we fall back to heap allocation.
            if (sourceLength < 50 && targetLength < 50)
            {
                Span<int> matrix = stackalloc int[(sourceLength + 1) * (targetLength + 1)];
                return Calculate(source, target, matrix, sourceLength, targetLength);
            }
            else
            {
                var matrix = new int[(sourceLength + 1) * (targetLength + 1)];
                return Calculate(source, target, matrix, sourceLength, targetLength);
            }
        }

        private double Calculate(ReadOnlySpan<char> source, ReadOnlySpan<char> target, Span<int> matrix, int sourceLength, int targetLength)
        {
            int cols = targetLength + 1;

            for (int i = 0; i <= sourceLength; i++) matrix[i * cols] = i;
            for (int j = 0; j <= targetLength; j++) matrix[j] = j;

            for (int i = 1; i <= sourceLength; i++)
            {
                for (int j = 1; j <= targetLength; j++)
                {
                    int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;

                    int deletion = matrix[(i - 1) * cols + j] + 1;
                    int insertion = matrix[i * cols + (j - 1)] + 1;
                    int substitution = matrix[(i - 1) * cols + (j - 1)] + cost;

                    int min = System.Math.Min(System.Math.Min(deletion, insertion), substitution);

                    // Damerau transposition check
                    if (i > 1 && j > 1 && source[i - 1] == target[j - 2] && source[i - 2] == target[j - 1])
                    {
                        min = System.Math.Min(min, matrix[(i - 2) * cols + (j - 2)] + cost);
                    }

                    matrix[i * cols + j] = min;
                }
            }

            int maxLen = System.Math.Max(sourceLength, targetLength);
            int distance = matrix[sourceLength * cols + targetLength];
            
            return 1.0 - ((double)distance / maxLen);
        }
    }
}
