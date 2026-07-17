using System;

namespace VehicleVisionOCR.Backend.Helpers
{
    public static class FuzzyMatcher
    {
        public static int ComputeLevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
            if (string.IsNullOrEmpty(target)) return source.Length;

            int n = source.Length;
            int m = target.Length;

            var pool = System.Buffers.ArrayPool<int>.Shared;
            int[] v0 = pool.Rent(m + 1);
            int[] v1 = pool.Rent(m + 1);

            try
            {
                for (int i = 0; i <= m; i++)
                    v0[i] = i;

                for (int i = 0; i < n; i++)
                {
                    v1[0] = i + 1;

                    for (int j = 0; j < m; j++)
                    {
                        int substitutionCost = (source[i] == target[j]) ? 0 : 1;
                        v1[j + 1] = Math.Min(
                            Math.Min(v1[j] + 1, v0[j + 1] + 1),
                            v0[j] + substitutionCost);
                    }

                    // Swap arrays
                    var temp = v0;
                    v0 = v1;
                    v1 = temp;
                }

                return v0[m];
            }
            finally
            {
                pool.Return(v0);
                pool.Return(v1);
            }
        }

        public static double ComputeJaroWinkler(string source, string target)
        {
            if (source == target) return 1.0;
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0.0;

            int len1 = source.Length;
            int len2 = target.Length;
            int matchDistance = Math.Max(len1, len2) / 2 - 1;

            bool[] sourceMatches = new bool[len1];
            bool[] targetMatches = new bool[len2];

            int matches = 0;
            for (int i = 0; i < len1; i++)
            {
                int start = Math.Max(0, i - matchDistance);
                int end = Math.Min(i + matchDistance + 1, len2);

                for (int j = start; j < end; j++)
                {
                    if (targetMatches[j]) continue;
                    if (source[i] != target[j]) continue;
                    sourceMatches[i] = true;
                    targetMatches[j] = true;
                    matches++;
                    break;
                }
            }

            if (matches == 0) return 0.0;

            int t = 0;
            int k = 0;
            for (int i = 0; i < len1; i++)
            {
                if (!sourceMatches[i]) continue;
                while (!targetMatches[k]) k++;
                if (source[i] != target[k]) t++;
                k++;
            }

            double m = matches;
            double jaro = (m / len1 + m / len2 + (m - t / 2.0) / m) / 3.0;

            int prefix = 0;
            for (int i = 0; i < Math.Min(4, Math.Min(len1, len2)); i++)
            {
                if (source[i] == target[i]) prefix++;
                else break;
            }

            return jaro + prefix * 0.1 * (1.0 - jaro);
        }

        public static bool IsFuzzyMatch(string rawText, string dbColor, int thresholdDistance = 3)
        {
            // First check exact substring match
            if (rawText.Contains(dbColor)) return true;

            // If not exact match, let's break the raw text into windows of words and compare
            // For example, if dbColor is "LUNAR SILVER METALLIC" (3 words)
            // we check all 3-word windows in raw text.
            var rawWords = rawText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var colorWords = dbColor.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (colorWords.Length == 0 || rawWords.Length < colorWords.Length) return false;

            for (int i = 0; i <= rawWords.Length - colorWords.Length; i++)
            {
                string windowString = string.Join(" ", rawWords, i, colorWords.Length);
                int distance = ComputeLevenshteinDistance(windowString, dbColor);
                
                // Allow 1 typo per 5 characters, but max threshold
                int allowedDistance = Math.Max(1, Math.Min(thresholdDistance, dbColor.Length / 5));
                if (distance <= allowedDistance)
                {
                    return true;
                }
            }
            
            // Also try comparing against the entire raw text if it's short
            if (rawText.Length < dbColor.Length + 10)
            {
                int distance = ComputeLevenshteinDistance(rawText, dbColor);
                int allowedDistance = Math.Max(1, Math.Min(thresholdDistance, dbColor.Length / 4));
                if (distance <= allowedDistance) return true;
            }

            return false;
        }
    }
}
