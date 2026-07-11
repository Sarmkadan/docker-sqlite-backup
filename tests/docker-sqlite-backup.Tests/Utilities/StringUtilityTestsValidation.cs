using System;
using System.Collections.Generic;
using System.Linq;

namespace DockerSqliteBackup.Tests.Utilities
{
    /// <summary>
    /// Validation helpers for <see cref="StringUtilityTests"/>.
    /// </summary>
    public static class StringUtilityTestsValidation
    {
        /// <summary>
        /// Validates the <see cref="StringUtilityTests"/> instance and returns a list of human-readable problems.
        /// </summary>
        /// <param name="value">The test class instance to validate.</param>
        /// <returns>A read-only list of validation problem messages. The list is empty when the instance is considered valid.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        public static IReadOnlyList<string> Validate(this StringUtilityTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            // Using reflection we can ensure that the expected public test methods exist.
            // This guards against accidental renaming or removal of required test methods.
            var expectedMethodNames = new[]
            {
                nameof(StringUtilityTests.FormatBytes_ShouldReturnExpectedString),
                nameof(StringUtilityTests.ToKebabCase_ShouldConvertCorrectly),
                nameof(StringUtilityTests.ToSnakeCase_ShouldConvertCorrectly),
                nameof(StringUtilityTests.ToPascalCase_ShouldConvertCorrectly),
                nameof(StringUtilityTests.ToCamelCase_ShouldConvertCorrectly),
                nameof(StringUtilityTests.Truncate_ShouldTruncateCorrectly),
                nameof(StringUtilityTests.MaskSensitive_ShouldMaskCorrectly),
                nameof(StringUtilityTests.IsValidEmail_ShouldReturnExpectedResult),
                nameof(StringUtilityTests.IsValidGuid_ShouldReturnExpectedResult),
                nameof(StringUtilityTests.SplitLines_ShouldHandleDifferentLineEndings),
                nameof(StringUtilityTests.JoinReadable_ShouldFormatCorrectly),
                nameof(StringUtilityTests.RemoveWhitespace_ShouldRemoveAllWhitespace),
                nameof(StringUtilityTests.QuoteIfNeeded_ShouldQuoteWhenNecessary),
                nameof(StringUtilityTests.Repeat_ShouldRepeatCorrectly)
            };

            var actualMethods = value.GetType()
                .GetMethods(System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.DeclaredOnly)
                .Select(m => m.Name)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var expected in expectedMethodNames)
            {
                if (!actualMethods.Contains(expected))
                {
                    problems.Add($"Expected test method '{expected}' is missing.");
                }
            }

            return problems;
        }

        /// <summary>
        /// Determines whether the <see cref="StringUtilityTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The test class instance to check.</param>
        /// <returns><c>true</c> if no validation problems are found; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        public static bool IsValid(this StringUtilityTests value) => !value.Validate().Any();

        /// <summary>
        /// Ensures that the <see cref="StringUtilityTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The test class instance to validate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when validation problems are found.</exception>
        public static void EnsureValid(this StringUtilityTests value)
        {
            var problems = value.Validate();
            if (problems.Any())
            {
                throw new ArgumentException($"StringUtilityTests validation failed: {string.Join("; ", problems)}");
            }
        }
    }
}