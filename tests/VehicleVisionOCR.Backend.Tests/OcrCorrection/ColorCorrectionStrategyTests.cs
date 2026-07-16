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
    public class ColorCorrectionStrategyTests
    {
        private readonly Mock<IColorRepository> _mockColorRepo;
        private readonly IMemoryCache _cache;
        private readonly OcrCorrectionOptions _options;
        private readonly ColorCorrectionStrategy _sut;

        public ColorCorrectionStrategyTests()
        {
            _mockColorRepo = new Mock<IColorRepository>();
            _mockColorRepo.Setup(r => r.GetActiveColorsAsync())
                          .ReturnsAsync(new List<string> { "BLACK", "WHITE", "RED", "LUNAR SILVER METALLIC", "DEEP BLUE METALLIC" });

            _cache = new MemoryCache(new MemoryCacheOptions());
            _options = new OcrCorrectionOptions 
            { 
                MinColorScoreThreshold = 60.0,
                CacheExpirationMinutes = 60
            };

            _sut = new ColorCorrectionStrategy(_mockColorRepo.Object, _cache, Options.Create(_options));
        }

        [Fact]
        public async Task CorrectAsync_ShouldReturnExactMatch()
        {
            // Act
            var result = await _sut.CorrectAsync("BLACK", 95.0);

            // Assert
            result.IsValid.Should().BeTrue();
            result.CorrectedText.Should().Be("BLACK");
            result.AppliedRules.Should().Contain(r => r.Contains("Exact Match"));
            result.ConfidenceLevel.Should().Be(ConfidenceLevel.High);
        }

        [Fact]
        public async Task CorrectAsync_ShouldReturnContainsMatch()
        {
            // Act
            var result = await _sut.CorrectAsync("PAINT IS BLACK GLOSS", 90.0);

            // Assert
            result.IsValid.Should().BeTrue();
            result.CorrectedText.Should().Be("BLACK");
            result.AppliedRules.Should().Contain(r => r.Contains("Contains Match"));
        }

        [Fact]
        public async Task CorrectAsync_ShouldReturnTokenMatch()
        {
            // Act
            var result = await _sut.CorrectAsync("LUNAR METALLIC", 85.0);

            // Assert
            result.IsValid.Should().BeTrue();
            result.CorrectedText.Should().Be("LUNAR SILVER METALLIC");
            result.AppliedRules.Should().Contain(r => r.Contains("Token Match"));
        }

        [Fact]
        public async Task CorrectAsync_ShouldReturnFuzzyMatch_WhenTypoExists()
        {
            // Act
            var result = await _sut.CorrectAsync("WH1TE", 80.0);

            // Assert
            result.IsValid.Should().BeTrue();
            result.CorrectedText.Should().Be("WHITE");
            // Either Reversed OCR Confusion or Levenshtein Fuzzy Match
            result.WasCorrected.Should().BeTrue();
        }

        [Fact]
        public async Task CorrectAsync_ShouldReturnInvalid_ForUnknownColor()
        {
            // Act
            var result = await _sut.CorrectAsync("PURPLE RAIN", 90.0);

            // Assert
            result.IsValid.Should().BeFalse();
            result.CorrectedText.Should().Be("Unknown");
        }
    }
}
