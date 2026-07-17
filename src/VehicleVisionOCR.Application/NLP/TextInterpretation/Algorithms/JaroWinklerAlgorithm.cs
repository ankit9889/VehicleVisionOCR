using System;
using VehicleVisionOCR.Domain.NLP.Interfaces;

namespace VehicleVisionOCR.Application.NLP.TextInterpretation.Algorithms
{
    public class JaroWinklerAlgorithm : ISimilarityAlgorithm
    {
        public string Name => "Jaro-Winkler";
        
        private const double WeightThreshold = 0.7;
        private const int NumPrefixChars = 4;

        public double CalculateSimilarity(string source, string target)
        {
            if (string.IsNullOrEmpty(source) && string.IsNullOrEmpty(target)) return 1.0;
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0.0;
            if (source == target) return 1.0;

            int sLen = source.Length;
            int tLen = target.Length;
            int matchDistance = (System.Math.Max(sLen, tLen) / 2) - 1;

            bool[] sMatches = new bool[sLen];
            bool[] tMatches = new bool[tLen];

            int matches = 0;
            for (int i = 0; i < sLen; i++)
            {
                int start = System.Math.Max(0, i - matchDistance);
                int end = System.Math.Min(i + matchDistance + 1, tLen);

                for (int j = start; j < end; j++)
                {
                    if (tMatches[j]) continue;
                    if (source[i] != target[j]) continue;
                    
                    sMatches[i] = true;
                    tMatches[j] = true;
                    matches++;
                    break;
                }
            }

            if (matches == 0) return 0.0;

            int transpositions = 0;
            int k = 0;
            for (int i = 0; i < sLen; i++)
            {
                if (!sMatches[i]) continue;
                while (!tMatches[k]) k++;
                if (source[i] != target[k]) transpositions++;
                k++;
            }

            transpositions /= 2;

            double m = matches;
            double jaro = ((m / sLen) + (m / tLen) + ((m - transpositions) / m)) / 3.0;

            if (jaro < WeightThreshold) return jaro;

            int prefix = 0;
            for (int i = 0; i < System.Math.Min(NumPrefixChars, System.Math.Min(sLen, tLen)); i++)
            {
                if (source[i] == target[i]) prefix++;
                else break;
            }

            return jaro + (prefix * 0.1 * (1.0 - jaro));
        }
    }
}
