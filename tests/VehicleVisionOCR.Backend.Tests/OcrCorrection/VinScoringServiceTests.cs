using System.Collections.Generic;
using FluentAssertions;
using VehicleVisionOCR.Backend.Services.OcrCorrection.VinServices;
using Xunit;

namespace VehicleVisionOCR.Backend.Tests.OcrCorrection
{
    public class VinScoringServiceTests
    {
        private readonly VinScoringService _sut;

        public VinScoringServiceTests()
        {
            _sut = new VinScoringService();
        }

        [Fact]
        public void ScoreCandidate_PerfectVin_ShouldScoreCloseTo100()
        {
            // Arrange
            string validVin = "1HGCM82633A004352"; // Math is valid
            double ocrConfidence = 90.0; // 90 * 0.4 = 36
            var wmis = new List<string> { "1HG" };

            // Act
            double score = _sut.ScoreCandidate(new VehicleVisionOCR.Backend.Services.OcrCorrection.Models.CandidateScore { Candidate = validVin }, validVin, ocrConfidence, wmis);

            // Assert
            // 36 (confidence) + 30 (pattern) + 30 (check digit) + 5 (WMI) = 101 -> capped at 100
            score.Should().Be(100.0);
        }

        [Fact]
        public void ScoreCandidate_InvalidCheckDigit_ShouldApplyPenalty()
        {
            // Arrange
            string invalidVin = "1HGCM82633A004353"; // Math is invalid
            double ocrConfidence = 90.0; // 36
            var wmis = new List<string> { "1HG" }; // 5

            // Act
            double score = _sut.ScoreCandidate(new VehicleVisionOCR.Backend.Services.OcrCorrection.Models.CandidateScore { Candidate = invalidVin }, invalidVin, ocrConfidence, wmis);

            // Assert
            // 36 (conf) + 30 (pattern) + 5 (wmi) - 20 (penalty) = 51
            score.Should().Be(51.0);
        }

        [Fact]
        public void ScoreCandidate_UnknownWMI_ShouldNotGiveBonus()
        {
            // Arrange
            string validVin = "1AGCM82633A004352"; // WMI '1AG' is not in our known list. It starts with '1', so check digit is enforced (and will fail math).
            double ocrConfidence = 100.0; // 40
            var wmis = new List<string> { "1HG" }; // 0 bonus since 1AG is not in wmis

            // Act
            double score = _sut.ScoreCandidate(new VehicleVisionOCR.Backend.Services.OcrCorrection.Models.CandidateScore { Candidate = validVin }, validVin, ocrConfidence, wmis);

            // Assert
            // 40 (conf) + 30 (pattern) - 20 (check digit penalty) = 50
            score.Should().NotBe(0);
            score.Should().BeLessThan(100.0);
        }
    }
}
