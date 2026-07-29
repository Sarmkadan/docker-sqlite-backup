using Xunit;
using System;
using DockerSqliteBackup.Configuration;

namespace DockerSqliteBackup.Tests
{
    public class AppSettingsExtensionsTests
    {
        [Fact]
        public void IsVerificationEnabled_ReturnsCorrectValue()
        {
            var settings = new AppSettings();
            settings.EnableVerificationByDefault = true;
            Assert.True(settings.IsVerificationEnabled());
            Assert.False(settings.IsVerificationEnabled(false));
        }

        [Fact]
        public void IsS3StorageEnabled_ReturnsCorrectValue()
        {
            var settings = new AppSettings();
            settings.EnableS3StorageByDefault = false;
            Assert.False(settings.IsS3StorageEnabled());
            Assert.True(settings.IsS3StorageEnabled(true));
        }

        [Fact]
        public void GetNotificationEmailsAsString_ReturnsCorrectFormatOrNull()
        {
            var settings = new AppSettings();
            Assert.Null(settings.GetNotificationEmailsAsString());
            settings.NotificationEmails = new[] { "a@b.com", "c@d.com" };
            Assert.Equal("a@b.com, c@d.com", settings.GetNotificationEmailsAsString());
        }

        [Fact]
        public void IsEncryptionConfigured_ReturnsCorrectValue()
        {
            var settings = new AppSettings();
            Assert.False(settings.IsEncryptionConfigured());
            settings.EnableEncryption = true;
            settings.EncryptionKey = "some-key";
            Assert.True(settings.IsEncryptionConfigured());
        }

        [Fact]
        public void ShouldCompressBackups_ReturnsCorrectValue()
        {
            var settings = new AppSettings();
            settings.CompressBackups = false;
            Assert.False(settings.ShouldCompressBackups());
            Assert.True(settings.ShouldCompressBackups(true));
        }

        [Fact]
        public void Validate_ReturnsTrue_WhenValid()
        {
            var settings = new AppSettings();
            Assert.True(AppSettingsExtensions.Validate(settings));
        }

        [Fact]
        public void Validate_ReturnsFalse_WhenInvalidEncryption()
        {
            var settings = new AppSettings { EnableEncryption = true, EncryptionKey = "" };
            Assert.False(AppSettingsExtensions.Validate(settings));
        }
        
        [Fact]
        public void Validate_ThrowsException_WhenInvalidEncryptionAndThrowOnError()
        {
            var settings = new AppSettings { EnableEncryption = true, EncryptionKey = "" };
            Assert.Throws<InvalidOperationException>(() => AppSettingsExtensions.Validate(settings, true));
        }
    }
}
