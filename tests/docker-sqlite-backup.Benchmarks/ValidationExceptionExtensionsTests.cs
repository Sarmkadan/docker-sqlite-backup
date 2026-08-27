using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DockerSqliteBackup.Exceptions;
using ValidationException = DockerSqliteBackup.Exceptions.ValidationException;

namespace DockerSqliteBackup.Benchmarks;

/// <summary>
/// Contains benchmarks for the ValidationExceptionExtensions methods.
/// </summary>
[MemoryDiagnoser]
public class ValidationExceptionExtensionsTests
{
    private ValidationException _exceptionWithErrors;
    private ValidationException _exceptionWithoutErrors;
    private ValidationException _exceptionWithParameter;

    /// <summary>
    /// Sets up the test fixtures by creating three ValidationException instances:
    /// one with errors dictionary, one without errors, and one with a parameter name.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        var errors = new Dictionary<string, string>
        {
            { "username", "Username is required" },
            { "email", "Email is invalid" },
            { "password", "Password must be at least 8 characters" }
        };
        _exceptionWithErrors = new ValidationException(errors);

        _exceptionWithoutErrors = new ValidationException("testParam", "Test message");

        _exceptionWithParameter = new ValidationException("connectionString", "Connection string cannot be empty");
    }

    /// <summary>
    /// Benchmarks the HasError method when the error dictionary contains the specified key.
    /// </summary>
    [Benchmark]
    public void HasError_WithExistingKey()
    {
        _exceptionWithErrors.HasError("username");
    }

    /// <summary>
    /// Benchmarks the HasError method when the error dictionary does not contain the specified key.
    /// </summary>
    [Benchmark]
    public void HasError_WithNonExistingKey()
    {
        _exceptionWithErrors.HasError("nonexistent");
    }

    /// <summary>
    /// Benchmarks the HasError method when the error dictionary is empty.
    /// </summary>
    [Benchmark]
    public void HasError_WithEmptyErrors()
    {
        _exceptionWithoutErrors.HasError("anyKey");
    }

    /// <summary>
    /// Benchmarks the GetError method when the error dictionary contains the specified key.
    /// </summary>
    [Benchmark]
    public void GetError_WithExistingKey()
    {
        _exceptionWithErrors.GetError("email");
    }

    /// <summary>
    /// Benchmarks the GetError method when the error dictionary does not contain the specified key.
    /// </summary>
    [Benchmark]
    public void GetError_WithNonExistingKey()
    {
        _exceptionWithErrors.GetError("nonexistent");
    }

    /// <summary>
    /// Benchmarks the GetError method when the error dictionary is empty.
    /// </summary>
    [Benchmark]
    public void GetError_WithEmptyErrors()
    {
        _exceptionWithoutErrors.GetError("anyKey");
    }

    /// <summary>
    /// Benchmarks the ToDetailedString method when the ValidationException contains errors.
    /// </summary>
    [Benchmark]
    public void ToDetailedString_WithErrors()
    {
        _exceptionWithErrors.ToDetailedString();
    }

    /// <summary>
    /// Benchmarks the ToDetailedString method when the ValidationException does not contain errors.
    /// </summary>
    [Benchmark]
    public void ToDetailedString_WithoutErrors()
    {
        _exceptionWithoutErrors.ToDetailedString();
    }

    /// <summary>
    /// Benchmarks the ToDetailedString method when the ValidationException has a parameter name but no errors.
    /// </summary>
    [Benchmark]
    public void ToDetailedString_WithParameterName()
    {
        _exceptionWithParameter.ToDetailedString();
    }
}
