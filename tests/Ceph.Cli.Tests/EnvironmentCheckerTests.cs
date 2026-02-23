using Ceph.Cli.Services;

namespace Ceph.Cli.Tests;

public class EnvironmentCheckerTests
{
    [Fact]
    public void RunAll_ReturnsNonEmptyResults()
    {
        var checker = new EnvironmentChecker();
        var results = checker.RunAll();
        Assert.NotEmpty(results);
    }

    [Fact]
    public void CheckOperatingSystem_AlwaysReturnsResult()
    {
        var checker = new EnvironmentChecker();
        var result = checker.CheckOperatingSystem();
        Assert.NotNull(result);
        Assert.NotEmpty(result.Name);
        Assert.NotEmpty(result.Message);
    }

    [Fact]
    public void CheckDockerDaemon_ReturnsResult()
    {
        var checker = new EnvironmentChecker();
        var result = checker.CheckDockerDaemon();
        Assert.NotNull(result);
        // We cannot guarantee Docker is running in CI, but we should get a result.
        Assert.Equal("Docker daemon reachable", result.Name);
    }

    [Fact]
    public void CheckDockerComposeInstalled_ReturnsResult()
    {
        var checker = new EnvironmentChecker();
        var result = checker.CheckDockerComposeInstalled();
        Assert.NotNull(result);
        Assert.Equal("Docker Compose", result.Name);
    }

    [Fact]
    public void CheckResult_FailedWithHint_HasRemediationHint()
    {
        // Build a failing check result directly to verify the record works as expected.
        var result = new EnvironmentChecker.CheckResult(
            "Test check",
            false,
            "This is a failure message.",
            "Run this to fix it.");

        Assert.False(result.Passed);
        Assert.NotNull(result.RemediationHint);
        Assert.Equal("Run this to fix it.", result.RemediationHint);
    }

    [Fact]
    public void CheckResult_PassedWithoutHint_HasNullRemediationHint()
    {
        var result = new EnvironmentChecker.CheckResult("Test check", true, "All good.");
        Assert.True(result.Passed);
        Assert.Null(result.RemediationHint);
    }

    [Fact]
    public void CheckDiskSpace_ReturnsResult()
    {
        var checker = new EnvironmentChecker();
        var result = checker.CheckDiskSpace();
        Assert.NotNull(result);
        Assert.Equal("Disk space", result.Name);
        Assert.NotEmpty(result.Message);
    }

    [Fact]
    public void CheckDockerNetworkConflict_ReturnsResult()
    {
        var checker = new EnvironmentChecker();
        var result = checker.CheckDockerNetworkConflict();
        Assert.NotNull(result);
        Assert.Equal("Docker network conflict", result.Name);
    }

    [Fact]
    public void CheckDockerWsl2Backend_ReturnsResult()
    {
        var checker = new EnvironmentChecker();
        var result = checker.CheckDockerWsl2Backend();
        Assert.NotNull(result);
        Assert.Equal("Docker WSL2 backend", result.Name);
    }

    [Fact]
    public void RunAll_IncludesNewPortabilityChecks()
    {
        var checker = new EnvironmentChecker();
        var results = checker.RunAll();
        var names = results.Select(r => r.Name).ToList();
        Assert.Contains("Disk space", names);
        Assert.Contains("Docker network conflict", names);
        Assert.Contains("Docker WSL2 backend", names);
    }
}
