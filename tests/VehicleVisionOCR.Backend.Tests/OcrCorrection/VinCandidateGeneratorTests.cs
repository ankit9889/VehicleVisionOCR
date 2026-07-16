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
        public async Task GenerateCandidatesAsync_ShouldReturnBaseCandidate_WhenNoAmbiguity()
        {
            // Arrange
            string input = "X"; // Minimal test string without confusion mapping

            // Act
            var candidates = await _sut.GenerateCandidatesAsync(input);

            // Assert
            candidates.Should().HaveCount(1);
            candidates[0].Candidate.Should().Be(input);
        }

        [Fact]
        public async Task GenerateCandidatesAsync_ShouldGenerateCombinations_ForAmbiguousCharacters()
        {
            // Arrange
            string input = "G7"; // G maps to 6, 7 maps to T and 1

            // Act
            var candidates = await _sut.GenerateCandidatesAsync(input);

            // Assert
            // 2 options for G (G, 6) * 3 options for 7 (7, T, 1) = 6 combinations
            candidates.Should().HaveCount(6);
            candidates.Select(c => c.Candidate).Should().Contain("G7");
            candidates.Select(c => c.Candidate).Should().Contain("67");
            candidates.Select(c => c.Candidate).Should().Contain("GT");
            candidates.Select(c => c.Candidate).Should().Contain("6T");
            candidates.Select(c => c.Candidate).Should().Contain("G1");
            candidates.Select(c => c.Candidate).Should().Contain("61");
        }

        [Fact]
        public async Task GenerateCandidatesAsync_ShouldLimitCombinations_ToPreventExplosion()
        {
            // Arrange
            string input = "0158627A"; // Highly ambiguous string (8 characters, all map to multiple options)

            // Act
            var candidates = await _sut.GenerateCandidatesAsync(input);

            // Assert
            // It should generate candidates but cap at max substitutions or max list size
            candidates.Should().NotBeEmpty();
            // Max substitutions is 4, so it shouldn't be the full 3^8 = 6561 combinations
            candidates.Count.Should().BeLessThan(2000);
        }
    }
}
