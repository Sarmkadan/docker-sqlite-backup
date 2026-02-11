#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.RegularExpressions;
using DockerSqliteBackup.Api;

namespace DockerSqliteBackup.Validation;

/// <summary>
/// Validator for request objects. Provides fluent validation API
/// for creating reusable validation rules.
/// </summary>
public class RequestValidator
{
    private readonly List<ValidationError> _errors = [];

    /// <summary>
    /// Validates that a required string field has content.
    /// </summary>
    public RequestValidator Required(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            _errors.Add(new ValidationError(fieldName, $"{fieldName} is required"));

        return this;
    }

    /// <summary>
    /// Validates that a Guid is not empty.
    /// </summary>
    public RequestValidator RequiredGuid(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
            _errors.Add(new ValidationError(fieldName, $"{fieldName} is required"));

        return this;
    }

    /// <summary>
    /// Validates that a string matches a pattern.
    /// </summary>
    public RequestValidator Matches(string? value, string pattern, string fieldName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && !Regex.IsMatch(value, pattern))
            _errors.Add(new ValidationError(fieldName, message));

        return this;
    }

    /// <summary>
    /// Validates that a string is a valid file path.
    /// </summary>
    public RequestValidator ValidFilePath(string? filePath, string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            try
            {
                var path = Path.GetFullPath(filePath);
                // Additional checks could be done here
            }
            catch
            {
                _errors.Add(new ValidationError(fieldName, $"{fieldName} is not a valid file path"));
            }
        }

        return this;
    }

    /// <summary>
    /// Validates that a value is within a range.
    /// </summary>
    public RequestValidator InRange<T>(T value, T min, T max, string fieldName) where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            _errors.Add(new ValidationError(fieldName, $"{fieldName} must be between {min} and {max}"));

        return this;
    }

    /// <summary>
    /// Validates that a string has a minimum length.
    /// </summary>
    public RequestValidator MinLength(string? value, int minLength, string fieldName)
    {
        if (!string.IsNullOrEmpty(value) && value.Length < minLength)
            _errors.Add(new ValidationError(fieldName,
                $"{fieldName} must be at least {minLength} characters long"));

        return this;
    }

    /// <summary>
    /// Validates that a string has a maximum length.
    /// </summary>
    public RequestValidator MaxLength(string? value, int maxLength, string fieldName)
    {
        if (!string.IsNullOrEmpty(value) && value.Length > maxLength)
            _errors.Add(new ValidationError(fieldName,
                $"{fieldName} must not exceed {maxLength} characters"));

        return this;
    }

    /// <summary>
    /// Validates that a string is a valid email.
    /// </summary>
    public RequestValidator Email(string? value, string fieldName)
    {
        if (!string.IsNullOrEmpty(value))
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(value);
                if (addr.Address != value)
                    throw new();
            }
            catch
            {
                _errors.Add(new ValidationError(fieldName, $"{fieldName} is not a valid email address"));
            }
        }

        return this;
    }

    /// <summary>
    /// Adds a custom validation error.
    /// </summary>
    public RequestValidator AddError(string fieldName, string message)
    {
        _errors.Add(new ValidationError(fieldName, message));
        return this;
    }

    /// <summary>
    /// Checks if there are any validation errors.
    /// </summary>
    public bool IsValid => _errors.Count == 0;

    /// <summary>
    /// Gets all validation errors.
    /// </summary>
    public IEnumerable<ValidationError> GetErrors() => _errors;

    /// <summary>
    /// Throws an exception if there are validation errors.
    /// </summary>
    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            var message = string.Join("; ", _errors.Select(e => e.Message));
            throw new ValidationException(message, _errors);
        }
    }

    /// <summary>
    /// Gets a validation error response.
    /// </summary>
    public ApiResponse<object> ToErrorResponse()
    {
        var errorList = _errors.GroupBy(e => e.FieldName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Message).ToArray());

        return ApiResponse<object>.ErrorResponse(
            "VALIDATION_ERROR",
            "One or more validation errors occurred",
            null);
    }
}

/// <summary>
/// Represents a single validation error.
/// </summary>
public class ValidationError
{
    public string FieldName { get; set; }
    public string Message { get; set; }

    public ValidationError(string fieldName, string message)
    {
        FieldName = fieldName;
        Message = message;
    }
}

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class ValidationException : Exception
{
    public List<ValidationError> Errors { get; }

    public ValidationException(string message, IEnumerable<ValidationError> errors)
        : base(message)
    {
        Errors = errors.ToList();
    }
}
