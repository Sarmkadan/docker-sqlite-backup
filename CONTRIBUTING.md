# Contributing to docker-sqlite-backup

Thank you for considering contributing to docker-sqlite-backup! Contributions of all kinds are welcome.

## Development Requirements

- **.NET SDK 10.0** — download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)
- **Docker** — optional, for container-related testing
- A Git client

## Building Locally

```bash
# Clone your fork
git clone https://github.com/your-username/docker-sqlite-backup.git
cd docker-sqlite-backup

# Restore dependencies
dotnet restore

# Build
dotnet build --configuration Release
```

## Running Tests

```bash
# Run all tests
dotnet test --verbosity normal

# Run with detailed output and save results
dotnet test --verbosity normal --logger "trx;LogFileName=test-results.trx"
```

Test results are written to `**/TestResults/*.trx`.

## Docker Build

```bash
docker build -t docker-sqlite-backup .
```

## How to Contribute

### 1. Fork and Branch

1. Fork the repository on GitHub.
2. Clone your fork: `git clone https://github.com/your-username/docker-sqlite-backup.git`
3. Create a branch: `git checkout -b feature/your-feature` or `git checkout -b fix/your-bug`

### 2. Make Changes

- Write or update tests for any changed behaviour.
- Ensure `dotnet build` and `dotnet test` pass without errors.
- Follow the code style described below.

### 3. Submit a Pull Request

Push your branch and open a Pull Request against `main`. Include a clear description of what the PR changes and why.

## Code Style

- Follow conventions in `.editorconfig` at the repository root.
- Provide XML documentation for public APIs.
- Prefer `var` only when the type is apparent from the right-hand side.
- Use Allman-style braces.
- Keep methods focused and small; extract helpers where appropriate.

## Reporting Issues

Use [GitHub Issues](https://github.com/sarmkadan/docker-sqlite-backup/issues). Include:
- Clear steps to reproduce
- Expected vs. actual behaviour
- .NET version and OS

## License

By contributing you agree that your contributions will be licensed under the [MIT License](LICENSE).
