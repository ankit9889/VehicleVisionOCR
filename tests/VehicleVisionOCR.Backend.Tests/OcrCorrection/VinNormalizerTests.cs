using FluentAssertions;
using VehicleVisionOCR.Backend.Services.OcrCorrection.VinServices;
using Xunit;

namespace VehicleVisionOCR.Backend.Tests.OcrCorrection
{
    public class VinNormalizerTests
    {
        private readonly VinNormalizer _sut;

        public VinNormalizerTests()
        {
            _sut = new VinNormalizer();
        }

        [Fact]
        public void NormalizeUniversalRules_ShouldRemoveSpacesAndDashes()
        {
            // Arrange
            string input = "LB8TC 33F-MN P88127";

            // Act
            var result = _sut.NormalizeUniversalRules(input);

            // Assert
            result.Normalized.Should().Be("LB8TC33FMNP88127");
            result.AppliedRules.Should().BeEmpty();
        }

        [Theory]
        [InlineData("LIOQ", "L100")]
        [InlineData("lIoQ", "L100")]
        public void NormalizeUniversalRules_ShouldConvertInvalidLettersToNumbers(string input, string expected)
        {
            // Act
            var result = _sut.NormalizeUniversalRules(input);

            // Assert
            result.Normalized.Should().Be(expected);
            result.AppliedRules.Should().ContainSingle().Which.Contains("Universal ISO-3779 Map");
        }

        [Theory]
        [InlineData("LB8TC33FMNP88127S", "LB8TC33FMNP881275")]
        [InlineData("LB8TC33FMNP88127Z", "LB8TC33FMNP881272")]
        [InlineData("LB8TC33FMNP88127B", "LB8TC33FMNP881278")]
        [InlineData("LB8TC33FMNP88127G", "LB8TC33FMNP881276")]
        [InlineData("LB8TC33FMNP88127T", "LB8TC33FMNP881277")]
        [InlineData("LB8TC33FMNP88127D", "LB8TC33FMNP881270")]
        public void NormalizeStructuralRules_ShouldConvertVisLettersToNumbers(string input, string expected)
        {
            // Act
            var result = _sut.NormalizeStructuralRules(input);

            // Assert
            result.Normalized.Should().Be(expected);
            result.AppliedRules.Should().ContainSingle().Which.Contains("VIS Positional Numeric Map");
        }

        [Fact]
        public void NormalizeStructuralRules_ShouldNotModifyValidVin()
        {
            // Arrange
            string validVin = "LB8TC33FMNP881270"; // Assuming 17 chars

            // Act
            var result = _sut.NormalizeStructuralRules(validVin);

            // Assert
            result.Normalized.Should().Be(validVin);
            result.AppliedRules.Should().BeEmpty();
        }

        [Fact]
        public void NormalizeStructuralRules_ShouldIgnoreShortStrings()
        {
            // Arrange
            string shortString = "LB8TC33F";

            // Act
            var result = _sut.NormalizeStructuralRules(shortString);

            // Assert
            result.Normalized.Should().Be(shortString);
            result.AppliedRules.Should().BeEmpty();
        }
    }
}
