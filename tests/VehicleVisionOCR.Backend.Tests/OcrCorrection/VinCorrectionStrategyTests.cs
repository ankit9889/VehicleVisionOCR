using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using VehicleVisionOCR.Backend.Services.OcrCorrection;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Enums;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Models;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Strategies;
using Xunit;

namespace VehicleVisionOCR.Backend.Tests.OcrCorrection
{
    public class VinCorrectionStrategyTests
    {
        private readonly Mock<IVinNormalizer> _mockNormalizer;
        private readonly Mock<IVinCandidateGenerator> _mockGenerator;
        private readonly Mock<IVinScoringService> _mockScorer;
        private readonly Mock<IWmiRepository> _mockWmiRepo;
        private readonly IMemoryCache _cache;
        private readonly OcrCorrectionOptions _options;
        private readonly VinCorrectionStrategy _sut;

        public VinCorrectionStrategyTests()
        {
            _mockNormalizer = new Mock<IVinNormalizer>();
            _mockGenerator = new Mock<IVinCandidateGenerator>();
            _mockScorer = new Mock<IVinScoringService>();
            _mockWmiRepo = new Mock<IWmiRepository>();
            _cache = new MemoryCache(new MemoryCacheOptions());
            
            _options = new OcrCorrectionOptions 
            { 
                MinVinScoreThreshold = 80.0,
                CacheExpirationMinutes = 60
            };
            
            _sut = new VinCorrectionStrategy(
                _mockNormalizer.Object,
                _mockGenerator.Object,
                _mockScorer.Object,
                _mockWmiRepo.Object,
                _cache,
                Options.Create(_options)
            );
        }

        [Fact]
        public async Task CorrectAsync_ShouldReturnValidResult_ForPerfectVin()
        {
            // Arrange
            string validVin = "1HGCM82633A004352";
            _mockNormalizer.Setup(n => n.NormalizeUniversalRules(validVin))
                           .Returns((validVin, new List<string>()));
            
            _mockGenerator.Setup(g => g.GenerateCandidatesAsync(validVin))
                          .ReturnsAsync(new List<CandidateScore> { new CandidateScore { Candidate = validVin } });

            _mockNormalizer.Setup(n => n.NormalizeStructuralRules(validVin))
                           .Returns((validVin, new List<string>()));

            _mockWmiRepo.Setup(r => r.GetActiveWmiPrefixesAsync()).ReturnsAsync(new List<string> { "1HG" });

            _mockScorer.Setup(s => s.ScoreCandidate(It.IsAny<VehicleVisionOCR.Backend.Services.OcrCorrection.Models.CandidateScore>(), validVin, 95.0, It.IsAny<List<string>>()))
                .Returns(100.0);

            // Act
            var result = await _sut.CorrectAsync(validVin, 95.0);

            // Assert
            result.IsValid.Should().BeTrue();
            result.CorrectedText.Should().Be(validVin);
            result.FinalScore.Should().Be(100.0);
            result.ConfidenceLevel.Should().Be(ConfidenceLevel.High);
        }

        [Fact]
        public async Task CorrectAsync_ShouldFail_WhenScoreIsBelowThreshold()
        {
            // Arrange
            string weakVin = "1HGCM82633A004352";
            _mockNormalizer.Setup(n => n.NormalizeUniversalRules(weakVin))
                           .Returns((weakVin, new List<string>()));
            
            _mockGenerator.Setup(g => g.GenerateCandidatesAsync(weakVin))
                          .ReturnsAsync(new List<CandidateScore> { new CandidateScore { Candidate = weakVin } });

            _mockNormalizer.Setup(n => n.NormalizeStructuralRules(weakVin))
                           .Returns((weakVin, new List<string>()));

            _mockScorer.Setup(s => s.ScoreCandidate(It.IsAny<VehicleVisionOCR.Backend.Services.OcrCorrection.Models.CandidateScore>(), weakVin, 50.0, It.IsAny<List<string>>()))
                .Returns(65.0); // Below 80.0

            // Act
            var result = await _sut.CorrectAsync(weakVin, 50.0);

            // Assert
            result.IsValid.Should().BeFalse();
            result.FailureReason.Should().Contain("below minimum threshold");
        }
    }
}
