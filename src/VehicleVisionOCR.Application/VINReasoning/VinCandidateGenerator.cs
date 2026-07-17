using System.Collections.Generic;
using VehicleVisionOCR.Domain.NLP.Models;
using VehicleVisionOCR.Domain.VIN.Models;

namespace VehicleVisionOCR.Application.VINReasoning
{
    public class VinCandidateGenerator
    {
        private static readonly Dictionary<char, char[]> CommonConfusions = new Dictionary<char, char[]>
        {
            {'B', new[] {'8'}},
            {'8', new[] {'B'}},
            {'S', new[] {'5'}},
            {'5', new[] {'S'}},
            {'Z', new[] {'2'}},
            {'2', new[] {'Z'}},
            {'G', new[] {'6'}},
            {'6', new[] {'G'}},
            {'O', new[] {'0'}}, // O is illegal in VINs
            {'I', new[] {'1'}}, // I is illegal
            {'Q', new[] {'0'}}  // Q is illegal
        };

        public List<VinCandidate> GenerateCandidates(InterpretationResult input)
        {
            var candidates = new List<VinCandidate>();
            
            if (input == null || string.IsNullOrWhiteSpace(input.NormalizedText))
                return candidates;

            string baseVin = input.NormalizedText;
            
            // 1. Add the primary OCR interpretation as the baseline candidate
            candidates.Add(new VinCandidate 
            { 
                CandidateString = baseVin,
                Score = new CandidateScore { InheritedOcrConfidence = input.Confidence }
            });

            // 2. Generate deterministic permutations based on known OCR confusions
            for (int i = 0; i < baseVin.Length; i++)
            {
                char c = baseVin[i];
                if (CommonConfusions.TryGetValue(c, out char[] alternatives))
                {
                    foreach (char alt in alternatives)
                    {
                        var chars = baseVin.ToCharArray();
                        chars[i] = alt;
                        string mutated = new string(chars);

                        var candidate = new VinCandidate
                        {
                            CandidateString = mutated,
                            Score = new CandidateScore { InheritedOcrConfidence = input.Confidence * 0.95 } // Slightly penalize mutated baseline
                        };

                        candidate.Repairs.Add(new CharacterRepair
                        {
                            PositionIndex = i,
                            OriginalCharacter = c,
                            RepairedCharacter = alt,
                            Justification = "Generated via OCR confusion matrix permutation."
                        });

                        candidates.Add(candidate);
                    }
                }
            }

            return candidates;
        }
    }
}
