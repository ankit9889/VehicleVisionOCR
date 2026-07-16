using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using VehicleVisionOCR.Backend.Services.OcrCorrection;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Models;
using VehicleVisionOCR.Backend.Services.OcrCorrection.VinServices;
using Xunit;

namespace VehicleVisionOCR.Backend.Tests.OcrCorrection
{
    public class VinCandidateGeneratorTests
    {
        private readonly Mock<IWmiRepository> _mockWmiRepo;
        private readonly IMemoryCache _cache;
        private readonly OcrCorrectionOptions _options;
        private readonly VinCandidateGenerator _sut;

        public VinCandidateGeneratorTests()
        {
            _mockWmiRepo = new Mock<IWmiRepository>();
            _mockWmiRepo.Setup(r => r.GetActiveWmiPrefixesAsync())
                        .ReturnsAsync(new List<string> { "LB8", "1HG", "JHM", "WBA" });

            _cache = new MemoryCache(new MemoryCacheOptions());
            _options = new OcrCorrectionOptions { CacheExpirationMinutes = 60 };
            
            var optionsWrapper = Options.Create(_options);
            _sut = new VinCandidateGenerator(_mockWmiRepo.Object, _cache, optionsWrapper);
        }

        [Fact]
        public async Task GenerateCandidatesAsync_ShouldReturnBaseCandidate_WhenMatchIsExact()
        {
            // Arrange
            string input = "LB8TC33FMNP881270"; // Exact WMI 'LB8'

            // Act
            var candidates = await _sut.GenerateCandidatesAsync(input);

            // Assert
            candidates.Should().HaveCount(1);
            candidates[0].Candidate.Should().Be(input);
        }

        [Fact]
        public async Task GenerateCandidatesAsync_ShouldGenerateFuzzyMatch_WhenWmiIsOffByOne()
        {
            // Arrange
            string input = "LBBTC33FMNP881270"; // Typo in WMI 'LBB' instead of 'LB8'

            // Act
            var candidates = await _sut.GenerateCandidatesAsync(input);

            // Assert
            candidates.Should().HaveCount(2); // Base + Fuzzy
            candidates.Select(c => c.Candidate).Should().Contain("LBBTC33FMNP881270"); // Base
            candidates.Select(c => c.Candidate).Should().Contain("LB8TC33FMNP881270"); // Corrected
        }

        [Fact]
        public async Task GenerateCandidatesAsync_ShouldNotGenerateFuzzyMatch_WhenWmiIsOffByMoreThanOne()
        {
            // Arrange
            string input = "LXXTC33FMNP881270"; // Way off WMI

            // Act
            var candidates = await _sut.GenerateCandidatesAsync(input);

            // Assert
            candidates.Should().HaveCount(1);
            candidates[0].Candidate.Should().Be(input);
        }
    }
}
