# Contributing to Ceph CLI

Thanks for your interest in contributing! This guide will help you get started.

## Getting Started

1. Fork and clone the repository
2. Ensure you have the [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed
3. Build the project:
   ```powershell
   dotnet build
   ```
4. Run the tests:
   ```powershell
   dotnet test
   ```

## Development Workflow

1. Create a branch from `main` for your changes
2. Make your changes following the conventions below
3. Add or update tests as needed
4. Ensure all tests pass with `dotnet test`
5. Commit your changes with a clear, descriptive message
6. Open a pull request against `main`

## Project Layout

- **`src/Ceph.Cli/Commands/`** — Each CLI command has its own class (e.g., `InitCommand.cs`). Add new commands here.
- **`src/Ceph.Cli/Services/`** — Business logic lives in service classes. Commands should delegate to services rather than containing logic directly.
- **`tests/Ceph.Cli.Tests/`** — xUnit tests. Each service should have a corresponding test file.

## Coding Conventions

- Target **.NET 8.0**
- Use **nullable reference types** (`<Nullable>enable</Nullable>`)
- Follow standard C# naming conventions (PascalCase for public members, camelCase for locals)
- Container-mounted files (entrypoint scripts, config files) **must use Unix line endings (LF)** — CRLF will break inside Linux containers
- Keep commands thin: put logic in services so it can be unit tested independently

## Adding a New Command

1. Create a new class in `src/Ceph.Cli/Commands/` (see existing commands for the pattern)
2. Register the command in `Program.cs`
3. If the command requires non-trivial logic, add a service in `src/Ceph.Cli/Services/`
4. Add tests in `tests/Ceph.Cli.Tests/`

## Testing

- Tests use **xUnit**
- File-generation tests should create a temporary directory and clean up afterwards
- Tests should not require Docker or WSL2 to be running (mock or skip where necessary)
- Run the full suite before submitting:
  ```powershell
  dotnet test
  ```

## Reporting Issues

When reporting a bug, please include:

- The output of `ceph-cli diagnose --json`
- Your OS version and Docker Desktop version
- Steps to reproduce the issue
- Expected vs actual behavior

## Pull Request Guidelines

- Keep PRs focused — one feature or fix per PR
- Include tests for new functionality
- Update the README if you add or change commands/options
- Ensure `dotnet build` and `dotnet test` both pass
