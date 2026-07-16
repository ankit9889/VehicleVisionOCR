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
            result.AppliedRules.Should().ContainSingle().Which.Contains("Stripped Whitespace/Hyphens");
        }

        [Theory]
        [InlineData("LIOQ", "LIOQ")] // No longer maps I->1, O->0 to preserve for confusion matrix
        [InlineData("lIoQ", "LIOQ")] // Still capitalizes
        public void NormalizeUniversalRules_ShouldNotConvertInvalidLettersToNumbers(string input, string expected)
        {
            // Act
            var result = _sut.NormalizeUniversalRules(input);

            // Assert
            result.Normalized.Should().Be(expected);
            result.AppliedRules.Should().BeEmpty();
        }

        [Fact]
        public void NormalizeStructuralRules_ShouldNotModifyValidVin()
        {
            // Arrange
            string validVin = "LB8TC33FMNP881270"; 

            // Act
            var result = _sut.NormalizeStructuralRules(validVin);

            // Assert
            result.Normalized.Should().Be(validVin);
            result.AppliedRules.Should().BeEmpty();
        }

        [Theory]
        [InlineData("LB8TC33FMNP88127S")]
        public void NormalizeStructuralRules_ShouldPreserveRawTextForConfusionMatrix(string input)
        {
            // Act
            var result = _sut.NormalizeStructuralRules(input);

            // Assert
            result.Normalized.Should().Be(input);
            result.AppliedRules.Should().BeEmpty();
        }
    }
}
