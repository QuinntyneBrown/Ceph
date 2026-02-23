namespace Ceph.Cli.Services;

/// <summary>
/// Detects and reports on the Windows/Docker environment required to run Ceph.
/// </summary>
public class EnvironmentChecker
{
    public record CheckResult(string Name, bool Passed, string Message, string? RemediationHint = null);

    /// <summary>Runs all environment checks and returns results.</summary>
    public IReadOnlyList<CheckResult> RunAll()
    {
        var results = new List<CheckResult>
        {
            CheckOperatingSystem(),
            CheckWsl2(),
            CheckDockerDesktop(),
            CheckDockerDaemon(),
            CheckDockerComposeInstalled(),
            CheckWsl2MemoryConfiguration(),
            CheckDiskSpace(),
            CheckDockerNetworkConflict(),
            CheckDockerWsl2Backend(),
        };
        return results;
    }

    // -------------------------------------------------------------------------
    // Individual checks
    // -------------------------------------------------------------------------

    public CheckResult CheckOperatingSystem()
    {
        bool isWindows = OperatingSystem.IsWindows();
        return new CheckResult(
            "Operating system",
            isWindows,
            isWindows ? "Running on Windows – OK." : "Not running on Windows. Some checks may not apply.",
            isWindows ? null : "This tool is designed for Windows. You can still run it on other OSes for file generation."
        );
    }

    public CheckResult CheckWsl2()
    {
        if (!OperatingSystem.IsWindows())
            return new CheckResult("WSL2", true, "Skipped – not running on Windows.");

        try
        {
            var result = RunProcess("wsl", "--status", captureOutput: true);
            bool passed = result.exitCode == 0 && result.output.Contains("Default Version: 2", StringComparison.OrdinalIgnoreCase);
            return new CheckResult(
                "WSL2 default version",
                passed,
                passed ? "WSL2 is set as the default version – OK." : "WSL2 is not set as the default version.",
                passed ? null : "Run: wsl --set-default-version 2"
            );
        }
        catch
        {
            return new CheckResult("WSL2", false, "Could not run 'wsl --status'. WSL may not be installed.", "Enable WSL2 in Windows Features or run: wsl --install");
        }
    }

    public CheckResult CheckDockerDesktop()
    {
        if (!OperatingSystem.IsWindows())
            return new CheckResult("Docker Desktop", true, "Skipped – not running on Windows.");

        string[] possiblePaths =
        [
            @"C:\Program Files\Docker\Docker\Docker Desktop.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Docker", "Docker", "Docker Desktop.exe"),
        ];

        bool found = possiblePaths.Any(File.Exists);
        return new CheckResult(
            "Docker Desktop installed",
            found,
            found ? "Docker Desktop installation found – OK." : "Docker Desktop does not appear to be installed.",
            found ? null : "Download and install Docker Desktop from https://www.docker.com/products/docker-desktop/"
        );
    }

    public CheckResult CheckDockerDaemon()
    {
        try
        {
            var result = RunProcess("docker", "info", captureOutput: true);
            bool passed = result.exitCode == 0;
            return new CheckResult(
                "Docker daemon reachable",
                passed,
                passed ? "Docker daemon is running – OK." : "Docker daemon is not reachable.",
                passed ? null : "Start Docker Desktop or the Docker service, then retry."
            );
        }
        catch
        {
            return new CheckResult("Docker daemon reachable", false, "Could not run 'docker info'. Is Docker installed?", "Install Docker Desktop from https://www.docker.com/products/docker-desktop/");
        }
    }

    public CheckResult CheckDockerComposeInstalled()
    {
        try
        {
            var result = RunProcess("docker", "compose version", captureOutput: true);
            bool v2 = result.exitCode == 0;
            if (v2)
                return new CheckResult("Docker Compose", true, $"Docker Compose (v2 plugin) found – OK. {result.output.Trim()}");

            // Fall back to legacy docker-compose
            var legacyResult = RunProcess("docker-compose", "--version", captureOutput: true);
            bool legacy = legacyResult.exitCode == 0;
            return new CheckResult(
                "Docker Compose",
                legacy,
                legacy ? $"Docker Compose (legacy) found – OK. {legacyResult.output.Trim()}" : "Docker Compose is not installed.",
                legacy ? null : "Install Docker Desktop (includes Compose) or run: pip install docker-compose"
            );
        }
        catch
        {
            return new CheckResult("Docker Compose", false, "Could not detect Docker Compose.", "Install Docker Desktop which bundles Docker Compose.");
        }
    }

    public CheckResult CheckWsl2MemoryConfiguration()
    {
        if (!OperatingSystem.IsWindows())
            return new CheckResult("WSL2 memory configuration", true, "Skipped – not running on Windows.");

        string wslConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".wslconfig");

        if (!File.Exists(wslConfigPath))
        {
            return new CheckResult(
                "WSL2 memory configuration",
                false,
                $"~/.wslconfig not found. WSL2 may use too much memory.",
                $"Create {wslConfigPath} with a [wsl2] section setting 'memory=4GB' (or more) and 'swap=2GB'."
            );
        }

        string content = File.ReadAllText(wslConfigPath);
        bool hasMemorySetting = content.Contains("memory=", StringComparison.OrdinalIgnoreCase);
        return new CheckResult(
            "WSL2 memory configuration",
            hasMemorySetting,
            hasMemorySetting ? $"~/.wslconfig has a memory setting – OK." : $"~/.wslconfig exists but does not set 'memory'. Ceph may run out of memory.",
            hasMemorySetting ? null : $"Add 'memory=4GB' under the [wsl2] section in {wslConfigPath}"
        );
    }

    public CheckResult CheckDiskSpace()
    {
        try
        {
            string root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)) ?? "C:\\";
            var drive = new DriveInfo(root);
            long freeGb = drive.AvailableFreeSpace / (1024L * 1024 * 1024);
            // Ceph image is ~500 MB and each OSD needs space for data
            bool passed = freeGb >= 10;
            return new CheckResult(
                "Disk space",
                passed,
                passed ? $"Drive {drive.Name} has {freeGb} GB free – OK." : $"Drive {drive.Name} only has {freeGb} GB free. Ceph needs at least 10 GB.",
                passed ? null : "Free up disk space or move Docker's data directory to a larger drive."
            );
        }
        catch (Exception ex)
        {
            return new CheckResult("Disk space", false, $"Could not check disk space: {ex.Message}");
        }
    }

    public CheckResult CheckDockerNetworkConflict()
    {
        try
        {
            var result = RunProcess("docker", "network ls --format {{.Name}}", captureOutput: true);
            if (result.exitCode != 0)
                return new CheckResult("Docker network conflict", true, "Skipped – Docker not reachable.");

            // Check if any existing network uses the 172.20.0.0/16 subnet
            var inspectResult = RunProcess("docker", "network ls -q", captureOutput: true);
            if (inspectResult.exitCode != 0 || string.IsNullOrWhiteSpace(inspectResult.output))
                return new CheckResult("Docker network conflict", true, "No Docker networks found – OK.");

            var networkIds = inspectResult.output.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var id in networkIds)
            {
                var info = RunProcess("docker", $"network inspect {id.Trim()} --format {{{{range .IPAM.Config}}}}{{{{.Subnet}}}}{{{{end}}}}", captureOutput: true);
                if (info.exitCode == 0 && info.output.Contains("172.20."))
                {
                    var nameInfo = RunProcess("docker", $"network inspect {id.Trim()} --format {{{{.Name}}}}", captureOutput: true);
                    string netName = nameInfo.output.Trim();
                    return new CheckResult(
                        "Docker network conflict",
                        false,
                        $"Docker network '{netName}' already uses subnet 172.20.x.x which conflicts with Ceph.",
                        $"Remove the conflicting network: docker network rm {netName}  (or use 'ceph-cli init' with a different --network-subnet)"
                    );
                }
            }

            return new CheckResult("Docker network conflict", true, "No Docker network conflicts detected – OK.");
        }
        catch
        {
            return new CheckResult("Docker network conflict", true, "Skipped – could not inspect Docker networks.");
        }
    }

    public CheckResult CheckDockerWsl2Backend()
    {
        if (!OperatingSystem.IsWindows())
            return new CheckResult("Docker WSL2 backend", true, "Skipped – not running on Windows.");

        try
        {
            var result = RunProcess("docker", "info --format {{.Isolation}}", captureOutput: true);
            if (result.exitCode != 0)
                return new CheckResult("Docker WSL2 backend", true, "Skipped – Docker not reachable.");

            // Check docker info for WSL2 indicators
            var fullInfo = RunProcess("docker", "info", captureOutput: true);
            if (fullInfo.exitCode == 0)
            {
                bool usesWsl2 = fullInfo.output.Contains("WSL", StringComparison.OrdinalIgnoreCase)
                    || fullInfo.output.Contains("linux", StringComparison.OrdinalIgnoreCase);
                if (usesWsl2)
                    return new CheckResult("Docker WSL2 backend", true, "Docker is using the WSL2 backend – OK.");

                bool usesHyperV = fullInfo.output.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase);
                if (usesHyperV)
                    return new CheckResult(
                        "Docker WSL2 backend",
                        false,
                        "Docker Desktop appears to be using the Hyper-V backend instead of WSL2.",
                        "Open Docker Desktop Settings > General > enable 'Use the WSL 2 based engine', then restart Docker Desktop."
                    );
            }

            return new CheckResult("Docker WSL2 backend", true, "Could not determine Docker backend – assuming WSL2.");
        }
        catch
        {
            return new CheckResult("Docker WSL2 backend", true, "Skipped – could not check Docker backend.");
        }
    }

    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------

    internal static (int exitCode, string output) RunProcess(string fileName, string arguments, bool captureOutput = false)
    {
        var psi = new System.Diagnostics.ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = captureOutput,
            RedirectStandardError = captureOutput,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException($"Could not start process '{fileName}'.");

        string output = captureOutput ? process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd() : string.Empty;
        process.WaitForExit();
        return (process.ExitCode, output);
    }
}
