using FluentAssertions;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Helpers;
using Xunit;

namespace VehicleVisionOCR.Backend.Tests.OcrCorrection
{
    public class VinCheckDigitCalculatorTests
    {
        [Theory]
        [InlineData("ME4MC56FGTA009533", false)] // Valid pattern, but depends if it's real check digit. Usually tests use real VINs. Let's use a real VIN.
        [InlineData("1M8GDM9A_KP042788", false)] // Invalid chars
        [InlineData("1HGCM82633A004352", true)]  // Real valid VIN (Honda Accord)
        [InlineData("1HGCM82633A004353", false)] // Real valid VIN with bad check digit
        [InlineData("1FMEU23RX3LA31114", false)] // Invalid
        [InlineData("JHMCS113X2C003290", false)] // Invalid
        [InlineData("JHMCS11302C003290", false)] // Invalid checksum
        [InlineData("SALJD3BG4AA896011", false)] // Invalid checksum
        public void Validate_ShouldReturnCorrectResultForRealVins(string vin, bool expected)
        {
            // Act
            bool result = VinCheckDigitCalculator.Validate(vin);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void Validate_ShouldReturnFalseForShortVin()
        {
            // Act
            bool result = VinCheckDigitCalculator.Validate("1HGCM82633A00435");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Validate_ShouldReturnFalseForNullOrEmpty()
        {
            // Act
            bool resultNull = VinCheckDigitCalculator.Validate(null);
            bool resultEmpty = VinCheckDigitCalculator.Validate(string.Empty);
            bool resultWhitespace = VinCheckDigitCalculator.Validate("   ");

            // Assert
            resultNull.Should().BeFalse();
            resultEmpty.Should().BeFalse();
            resultWhitespace.Should().BeFalse();
        }

        [Fact]
        public void Validate_ShouldReturnFalseForVinWithInvalidCharacters()
        {
            // The characters I, O, Q are not allowed
            bool result = VinCheckDigitCalculator.Validate("1HGCM82633A00435I");

            // Assert
            result.Should().BeFalse();
        }
    }
}
