using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DockerSqliteBackup.Exceptions;
using ValidationException = DockerSqliteBackup.Exceptions.ValidationException;

namespace DockerSqliteBackup.Benchmarks;

[MemoryDiagnoser]
public class ValidationExceptionExtensionsTests
{
    private ValidationException _exceptionWithErrors;
    private ValidationException _exceptionWithoutErrors;
    private ValidationException _exceptionWithParameter;

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

    [Benchmark]
    public void HasError_WithExistingKey()
    {
        _exceptionWithErrors.HasError("username");
    }

    [Benchmark]
    public void HasError_WithNonExistingKey()
    {
        _exceptionWithErrors.HasError("nonexistent");
    }

    [Benchmark]
    public void HasError_WithEmptyErrors()
    {
        _exceptionWithoutErrors.HasError("anyKey");
    }

    [Benchmark]
    public void GetError_WithExistingKey()
    {
        _exceptionWithErrors.GetError("email");
    }

    [Benchmark]
    public void GetError_WithNonExistingKey()
    {
        _exceptionWithErrors.GetError("nonexistent");
    }

    [Benchmark]
    public void GetError_WithEmptyErrors()
    {
        _exceptionWithoutErrors.GetError("anyKey");
    }

    [Benchmark]
    public void ToDetailedString_WithErrors()
    {
        _exceptionWithErrors.ToDetailedString();
    }

    [Benchmark]
    public void ToDetailedString_WithoutErrors()
    {
        _exceptionWithoutErrors.ToDetailedString();
    }

    [Benchmark]
    public void ToDetailedString_WithParameterName()
    {
        _exceptionWithParameter.ToDetailedString();
    }
}
