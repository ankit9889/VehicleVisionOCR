using System.Collections.Generic;
using System.Linq;
using System.Text;
using VehicleVisionOCR.Domain.Vision.Interfaces;
using VehicleVisionOCR.Domain.Vision.Models;

namespace VehicleVisionOCR.Application.Vision.OcrFusion.Engines
{
    public class CharacterVotingEngine : ICharacterVotingEngine
    {
        private readonly IProbabilityEngine _probabilityEngine;
        
        // This should ideally come from configuration
        private readonly Dictionary<char, char[]> _confusionMatrix = new Dictionary<char, char[]>
        {
            { '8', new[] { 'B' } },
            { 'B', new[] { '8' } },
            { '5', new[] { 'S' } },
            { 'S', new[] { '5' } },
            { '2', new[] { 'Z' } },
            { 'Z', new[] { '2' } },
            { 'O', new[] { '0', 'D' } },
            { '0', new[] { 'O', 'D', 'Q' } },
            { 'G', new[] { '6' } },
            { '6', new[] { 'G' } },
            { 'T', new[] { '7' } },
            { '7', new[] { 'T' } }
        };

        public CharacterVotingEngine(IProbabilityEngine probabilityEngine)
        {
            _probabilityEngine = probabilityEngine;
        }

        public FusedStringCandidate VoteOnClusters(List<CharacterCluster> clusters)
        {
            var stringBuilder = new StringBuilder();
            var confidences = new List<ConfidenceScore>();
            double totalAggregateConfidence = 0;

            int assumedTotalPasses = 10; // This should ideally be passed in based on config

            foreach (var cluster in clusters)
            {
                var charScores = new Dictionary<char, double>();

                // 1. Calculate raw probability for each piece of evidence
                foreach (var evidence in cluster.EvidenceList)
                {
                    double prob = _probabilityEngine.CalculateCharacterProbability(evidence, cluster, assumedTotalPasses);
                    
                    if (!charScores.ContainsKey(evidence.Character))
                    {
                        charScores[evidence.Character] = 0;
                    }
                    
                    charScores[evidence.Character] += prob; // Accumulate probabilities for the same character

                    // Process optional ChoiceIterator alternatives if they were successfully collected
                    if (evidence.Alternatives != null && evidence.Alternatives.Count > 0)
                    {
                        foreach (var alt in evidence.Alternatives)
                        {
                            var altEvidence = new CharacterEvidence
                            {
                                Character = alt.Character,
                                Confidence = alt.Confidence,
                                X = evidence.X,
                                Y = evidence.Y,
                                Width = evidence.Width,
                                Height = evidence.Height,
                                SourcePassId = evidence.SourcePassId,
                                SourcePageSegmentationMode = evidence.SourcePageSegmentationMode,
                                SourceScale = evidence.SourceScale,
                                SourcePreprocessing = evidence.SourcePreprocessing,
                                LineIndex = evidence.LineIndex,
                                WordIndex = evidence.WordIndex
                            };

                            // The probability engine will naturally give this a consensus bonus if it matches 
                            // the primary character of another OCR pass in the same cluster.
                            double altProb = _probabilityEngine.CalculateCharacterProbability(altEvidence, cluster, assumedTotalPasses);
                            
                            // Apply a slight penalty because it was ranked as an alternative, not the primary
                            altProb *= 0.8; 

                            if (!charScores.ContainsKey(alt.Character))
                            {
                                charScores[alt.Character] = 0;
                            }
                            
                            charScores[alt.Character] += altProb;
                        }
                    }
                }

                // 2. Apply Confusion Matrix Penalty/Bonus
                ApplyConfusionMatrix(charScores);

                // 3. Find the winning character
                if (charScores.Any())
                {
                    var winner = charScores.OrderByDescending(kvp => kvp.Value).First();
                    
                    stringBuilder.Append(winner.Key);
                    totalAggregateConfidence += winner.Value;
                    
                    var cs = new ConfidenceScore
                    {
                        Value = winner.Key.ToString(),
                        Confidence = winner.Value,
                        Reasoning = "Mathematical probability winner."
                    };

                    // Add runner-ups to AlternativeCandidates
                    foreach(var kvp in charScores.Where(x => x.Key != winner.Key).OrderByDescending(x => x.Value).Take(2))
                    {
                        cs.AlternativeCandidates.Add(kvp.Key.ToString(), kvp.Value);
                    }
                    
                    confidences.Add(cs);
                }
            }

            return new FusedStringCandidate
            {
                Text = stringBuilder.ToString(),
                AggregateConfidence = clusters.Count > 0 ? totalAggregateConfidence / clusters.Count : 0,
                CharacterConfidences = confidences
            };
        }

        private void ApplyConfusionMatrix(Dictionary<char, double> charScores)
        {
            var keys = charScores.Keys.ToList();
            
            // If the cluster contains multiple highly confusable characters (e.g. 8 and B),
            // this implies ambiguity. We smooth their probabilities slightly.
            foreach (var key in keys)
            {
                if (_confusionMatrix.TryGetValue(key, out var confusables))
                {
                    foreach (var confusable in confusables)
                    {
                        if (charScores.ContainsKey(confusable))
                        {
                            // Shift 10% of probability from the winner to the loser to keep alternatives viable
                            double shift = charScores[key] * 0.10;
                            charScores[key] -= shift;
                            charScores[confusable] += shift;
                        }
                    }
                }
            }
        }
    }
}
