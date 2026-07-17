using System;
using System.Collections.Generic;

namespace DebugScoring
{
    class Program
    {
        static void Main(string[] args)
        {
            var cand1 = "ME6R73LHPB112244";
            var cand2 = "MEG6R73LHPB112244";
            var knownWmis = new List<string> { "MEG", "ME6" };

            Console.WriteLine($"Score cand1: {ScoreCandidate(cand1, knownWmis)}");
            Console.WriteLine($"Score cand2: {ScoreCandidate(cand2, knownWmis)}");
        }

        static double ScoreCandidate(string candidate, List<string> knownWmis)
        {
            double score = 0.0;
            score += 99.0 * 0.40; // OCR confidence

            if (candidate.Length >= 14 && candidate.Length <= 20)
                score += 30.0;

            if (candidate.Length >= 3 && knownWmis != null && knownWmis.Contains(candidate.Substring(0, 3)))
                score += 5.0;

            if (candidate.Length >= 14 && candidate.Length <= 20)
            {
                bool isCheckDigitValid = ValidateCheckDigit(candidate);
                if (isCheckDigitValid)
                    score += 30.0;
                else if (candidate.Length == 17)
                    score -= 20.0;
            }

            int visStartIndex = candidate.Length == 17 ? 11 : 10;
            for (int i = visStartIndex; i < candidate.Length; i++)
            {
                if (char.IsLetter(candidate[i]))
                    score -= (i >= visStartIndex + 2) ? 10.0 : 5.0;
            }

            char wmiRegion = candidate.Length > 0 ? candidate[0] : ' ';
            bool isCheckDigitMandatory = (wmiRegion == '1' || wmiRegion == '2' || wmiRegion == '3' || 
                                          wmiRegion == '4' || wmiRegion == '5' || wmiRegion == 'L') && candidate.Length == 17;

            if (isCheckDigitMandatory && candidate.Length >= 9)
            {
                if (char.IsLetter(candidate[8]) && candidate[8] != 'X')
                    score -= 15.0;
            }

            string invalidChars = "IOQ";
            foreach (char c in candidate)
            {
                if (invalidChars.Contains(c))
                {
                    score -= 50.0;
                }
            }

            return score;
        }

        static bool ValidateCheckDigit(string vin)
        {
            if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
                return false;

            var values = new Dictionary<char, int>
            {
                {'A', 1}, {'B', 2}, {'C', 3}, {'D', 4}, {'E', 5}, {'F', 6}, {'G', 7}, {'H', 8},
                {'J', 1}, {'K', 2}, {'L', 3}, {'M', 4}, {'N', 5}, {'P', 7}, {'R', 9},
                {'S', 2}, {'T', 3}, {'U', 4}, {'V', 5}, {'W', 6}, {'X', 7}, {'Y', 8}, {'Z', 9},
                {'1', 1}, {'2', 2}, {'3', 3}, {'4', 4}, {'5', 5}, {'6', 6}, {'7', 7}, {'8', 8}, {'9', 9}, {'0', 0}
            };

            int[] weights = { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 };

            int sum = 0;
            for (int i = 0; i < 17; i++)
            {
                if (!values.TryGetValue(vin[i], out int val))
                    return false;
                sum += val * weights[i];
            }

            int remainder = sum % 11;
            char expectedCheckDigit = remainder == 10 ? 'X' : (char)('0' + remainder);

            return vin[8] == expectedCheckDigit;
        }
    }
}
