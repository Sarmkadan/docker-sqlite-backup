using System;
using System.Globalization;

namespace DockerSqliteBackup.Domain
{
    /// <summary>
    /// Extension methods for <see cref="AzureConfiguration"/>.
    /// </summary>
    public static class AzureConfigurationExtensions
    {
        /// <summary>
        /// Gets the full URI of the blob container combined with the optional prefix.
        /// </summary>
        /// <param name="configuration">The Azure configuration.</param>
        /// <returns>
        /// A <see cref="Uri"/> that points to the container/prefix, or <c>null</c> if either
        /// <see cref="AzureConfiguration.SasUri"/> or <see cref="AzureConfiguration.ContainerName"/> is <c>null</c> or whitespace.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when <paramref name="configuration"/> is <c>null</c>.
        /// </exception>
        public static Uri? GetBlobUri(this AzureConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            // Prefer a SAS URI if provided; otherwise we cannot construct a direct URI.
            if (string.IsNullOrWhiteSpace(configuration.SasUri) || string.IsNullOrWhiteSpace(configuration.ContainerName))
                return null;

            var baseUri = configuration.SasUri.TrimEnd('/');
            var container = configuration.ContainerName.Trim('/');
            var prefix = configuration.BlobPrefix?.Trim('/') ?? string.Empty;

            var combined = string.IsNullOrEmpty(prefix)
                ? $"{baseUri}/{container}"
                : $"{baseUri}/{container}/{prefix}";

            return new Uri(combined, UriKind.Absolute);
        }

        /// <summary>
        /// Returns the connection string that should be used to access Azure storage.
        /// If <see cref="AzureConfiguration.ConnectionString"/> is set it is returned unchanged;
        /// otherwise a connection string is built from the <see cref="AzureConfiguration.SasUri"/>.
        /// </summary>
        /// <param name="configuration">The Azure configuration.</param>
        /// <returns>The effective connection string, or <c>null</c> when neither source is available.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when <paramref name="configuration"/> is <c>null</c>.
        /// </exception>
        public static string? GetEffectiveConnectionString(this AzureConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            if (!string.IsNullOrWhiteSpace(configuration.ConnectionString))
                return configuration.ConnectionString;

            if (string.IsNullOrWhiteSpace(configuration.SasUri) || string.IsNullOrWhiteSpace(configuration.ContainerName))
                return null;

            // Build a minimal connection string using the SAS URI.
            // Example: "BlobEndpoint=https://account.blob.core.windows.net/;SharedAccessSignature=..."
            var uri = new Uri(configuration.SasUri, UriKind.Absolute);
            var endpoint = $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
            var query = uri.Query.TrimStart('?');

            return string.Create(CultureInfo.InvariantCulture, $"BlobEndpoint={endpoint};SharedAccessSignature={query}");
        }

        /// <summary>
        /// Enables or disables immutability on the configuration and returns the same instance for fluent usage.
        /// </summary>
        /// <param name="configuration">The Azure configuration.</param>
        /// <param name="enable">
        /// If <c>true</c>, immutability is enabled; otherwise it is disabled.
        /// </param>
        /// <returns>The modified <see cref="AzureConfiguration"/> instance.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when <paramref name="configuration"/> is <c>null</c>.
        /// </exception>
        public static AzureConfiguration WithImmutability(this AzureConfiguration configuration, bool enable)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            configuration.EnableImmutability = enable;
            return configuration;
        }
    }
}