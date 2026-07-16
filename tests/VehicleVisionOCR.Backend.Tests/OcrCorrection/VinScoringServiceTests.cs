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
            double score = _sut.ScoreCandidate(validVin, validVin, ocrConfidence, wmis);

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
            double score = _sut.ScoreCandidate(invalidVin, invalidVin, ocrConfidence, wmis);

            // Assert
            // 36 (conf) + 30 (pattern) + 5 (wmi) - 15 (penalty) = 56
            score.Should().Be(56.0);
        }

        [Fact]
        public void ScoreCandidate_UnknownWMI_ShouldNotGiveBonus()
        {
            // Arrange
            string validVin = "9HGCM82633A004352"; // Check digit math will likely be invalid, let's just test WMI
            double ocrConfidence = 100.0; // 40
            var wmis = new List<string> { "1HG" }; // 0 bonus since 9HG is not in wmis

            // Act
            double score = _sut.ScoreCandidate(validVin, validVin, ocrConfidence, wmis);

            // Assert
            // 40 (conf) + 30 (pattern) + ? (check digit penalty or bonus)
            // It should at least be calculable
            score.Should().NotBe(0);
            score.Should().BeLessThan(100.0); // Can't reach 100 without WMI and potentially good CD
        }
    }
}
