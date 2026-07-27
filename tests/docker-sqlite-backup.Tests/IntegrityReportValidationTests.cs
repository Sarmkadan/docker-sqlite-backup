using Xunit;
using DockerSqliteBackup.Domain;

namespace DockerSqliteBackup.Tests.IntegrityReportValidationTests
{
    public class IntegrityReportValidationTests
    {
        [Fact]
        public void Validate_HappyPath_ReturnsEmptyList()
        {
            // Arrange
            var report = new IntegrityReport();

            // Act
            var problems = IntegrityReportValidation.Validate(report);

            // Assert
            Assert.Empty(problems);
        }

        [Fact]
        public void Validate_NullReport_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => IntegrityReportValidation.Validate(null));
        }

        [Fact]
        public void Validate_EmptyId_ReportsProblem()
        {
            // Arrange
            var report = new IntegrityReport { Id = Guid.Empty };

            // Act
            var problems = IntegrityReportValidation.Validate(report);

            // Assert
            Assert.Single(problems);
        }

        [Fact]
        public void IsValid_HappyPath_ReturnsTrue()
        {
            // Arrange
            var report = new IntegrityReport();

            // Act
            var isValid = IntegrityReportValidation.IsValid(report);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void IsValid_NullReport_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => IntegrityReportValidation.IsValid(null));
        }

        [Fact]
        public void IsValid_InvalidReport_ReturnsFalse()
        {
            // Arrange
            var report = new IntegrityReport { Id = Guid.Empty };

            // Act
            var isValid = IntegrityReportValidation.IsValid(report);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public void EnsureValid_HappyPath_DoesNothing()
        {
            // Arrange
            var report = new IntegrityReport();

            // Act and Assert
            IntegrityReportValidation.EnsureValid(report);
        }

        [Fact]
        public void EnsureValid_NullReport_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => IntegrityReportValidation.EnsureValid(null));
        }

        [Fact]
        public void EnsureValid_InvalidReport_ThrowsArgumentException()
        {
            // Arrange
            var report = new IntegrityReport { Id = Guid.Empty };

            // Act and Assert
            Assert.Throws<ArgumentException>(() => IntegrityReportValidation.EnsureValid(report));
        }
    }
}
