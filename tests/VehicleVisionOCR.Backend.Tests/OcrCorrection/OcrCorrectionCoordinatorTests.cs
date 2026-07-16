using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VehicleVisionOCR.Backend.Services.OcrCorrection;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Enums;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Interfaces;
using VehicleVisionOCR.Backend.Services.OcrCorrection.Models;
using Xunit;

namespace VehicleVisionOCR.Backend.Tests.OcrCorrection
{
    public class OcrCorrectionCoordinatorTests
    {
        private readonly Mock<ILogger<OcrCorrectionCoordinator>> _mockLogger;
        private readonly Mock<IOcrCorrectionStrategy> _mockVinStrategy;
        private readonly OcrCorrectionCoordinator _sut;

        public OcrCorrectionCoordinatorTests()
        {
            _mockLogger = new Mock<ILogger<OcrCorrectionCoordinator>>();
            _mockVinStrategy = new Mock<IOcrCorrectionStrategy>();
            
            _mockVinStrategy.Setup(s => s.FieldType).Returns(TargetFieldType.VIN);

            var strategies = new List<IOcrCorrectionStrategy> { _mockVinStrategy.Object };

            _sut = new OcrCorrectionCoordinator(strategies, _mockLogger.Object);
        }

        [Fact]
        public async Task ProcessFieldAsync_ShouldRouteToCorrectStrategy()
        {
            // Arrange
            var expectedResult = new CorrectionResult { IsValid = true, CorrectedText = "TEST" };
            _mockVinStrategy.Setup(s => s.CorrectAsync("RAW", 90.0)).ReturnsAsync(expectedResult);

            // Act
            var result = await _sut.ProcessFieldAsync(TargetFieldType.VIN, "RAW", 90.0);

            // Assert
            result.Should().Be(expectedResult);
            _mockVinStrategy.Verify(s => s.CorrectAsync("RAW", 90.0), Times.Once);
        }

        [Fact]
        public async Task ProcessFieldAsync_ShouldReturnPassthroughResult_WhenStrategyIsMissing()
        {
            // Act
            var result = await _sut.ProcessFieldAsync(TargetFieldType.Color, "BLUE", 90.0);

            // Assert
            result.IsValid.Should().BeTrue();
            result.CorrectedText.Should().Be("BLUE");
            result.WasCorrected.Should().BeFalse();
            result.StrategyName.Should().Contain("Passthrough");
        }
    }
}
