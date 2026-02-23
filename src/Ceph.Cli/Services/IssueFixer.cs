namespace Ceph.Cli.Services;

/// <summary>
/// Applies automated fixes for common Windows/Docker issues detected by
/// <see cref="EnvironmentChecker"/>.
/// </summary>
public class IssueFixer
{
    public record FixResult(string Name, bool Applied, string Message);

    /// <summary>Attempts to fix the WSL2 default version.</summary>
    public FixResult FixWsl2DefaultVersion()
    {
        if (!OperatingSystem.IsWindows())
            return new FixResult("WSL2 default version", false, "Not on Windows – skipped.");

        try
        {
            var (exitCode, output) = EnvironmentChecker.RunProcess("wsl", "--set-default-version 2", captureOutput: true);
            bool ok = exitCode == 0;
            return new FixResult(
                "WSL2 default version",
                ok,
                ok ? "WSL2 set as default version successfully." : $"Failed to set WSL2 default version. Output: {output}"
            );
        }
        catch (Exception ex)
        {
            return new FixResult("WSL2 default version", false, $"Exception: {ex.Message}");
        }
    }

    /// <summary>Writes a sensible ~/.wslconfig if the memory entry is missing.</summary>
    public FixResult FixWsl2MemoryConfiguration(int memoryGb = 6, int swapGb = 2, int processors = 4)
    {
        if (!OperatingSystem.IsWindows())
            return new FixResult("WSL2 memory configuration", false, "Not on Windows – skipped.");

        string wslConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".wslconfig");

        try
        {
            string existing = File.Exists(wslConfigPath) ? File.ReadAllText(wslConfigPath) : string.Empty;

            if (existing.Contains("memory=", StringComparison.OrdinalIgnoreCase))
                return new FixResult("WSL2 memory configuration", true, $"{wslConfigPath} already has a memory setting – no changes made.");

            // Append or create [wsl2] section
            string addition = $"\n[wsl2]\nmemory={memoryGb}GB\nswap={swapGb}GB\nprocessors={processors}\n";
            if (existing.Contains("[wsl2]", StringComparison.OrdinalIgnoreCase))
            {
                // Insert after the [wsl2] line
                int idx = existing.IndexOf("[wsl2]", StringComparison.OrdinalIgnoreCase) + "[wsl2]".Length;
                existing = existing.Insert(idx, $"\nmemory={memoryGb}GB\nswap={swapGb}GB\nprocessors={processors}");
                File.WriteAllText(wslConfigPath, existing);
            }
            else
            {
                File.AppendAllText(wslConfigPath, addition);
            }

            return new FixResult(
                "WSL2 memory configuration",
                true,
                $"Updated {wslConfigPath}. Run 'wsl --shutdown' and restart Docker Desktop for the changes to take effect."
            );
        }
        catch (Exception ex)
        {
            return new FixResult("WSL2 memory configuration", false, $"Failed to update {wslConfigPath}: {ex.Message}");
        }
    }

    /// <summary>Removes a conflicting Docker network that uses the Ceph subnet.</summary>
    public FixResult FixDockerNetworkConflict()
    {
        try
        {
            var inspectResult = EnvironmentChecker.RunProcess("docker", "network ls -q", captureOutput: true);
            if (inspectResult.exitCode != 0 || string.IsNullOrWhiteSpace(inspectResult.output))
                return new FixResult("Docker network conflict", true, "No Docker networks to check.");

            var networkIds = inspectResult.output.Trim().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var id in networkIds)
            {
                var info = EnvironmentChecker.RunProcess("docker", $"network inspect {id.Trim()}", captureOutput: true);
                if (info.exitCode == 0 && info.output.Contains("172.20."))
                {
                    // Extract network name from JSON output
                    string netName = id.Trim();
                    int nameIdx = info.output.IndexOf("\"Name\":", StringComparison.Ordinal);
                    if (nameIdx >= 0)
                    {
                        int start = info.output.IndexOf('"', nameIdx + 7) + 1;
                        int end = info.output.IndexOf('"', start);
                        if (start > 0 && end > start)
                            netName = info.output[start..end];
                    }

                    var (exitCode, output) = EnvironmentChecker.RunProcess("docker", $"network rm {netName}", captureOutput: true);
                    if (exitCode == 0)
                        return new FixResult("Docker network conflict", true, $"Removed conflicting Docker network '{netName}'.");
                    else
                        return new FixResult("Docker network conflict", false, $"Could not remove network '{netName}': {output}. Stop containers using it first.");
                }
            }

            return new FixResult("Docker network conflict", true, "No conflicting networks found.");
        }
        catch (Exception ex)
        {
            return new FixResult("Docker network conflict", false, $"Exception: {ex.Message}");
        }
    }

    /// <summary>Attempts to start Docker Desktop on Windows.</summary>
    public FixResult StartDockerDesktop()
    {
        if (!OperatingSystem.IsWindows())
            return new FixResult("Start Docker Desktop", false, "Not on Windows – skipped.");

        string[] possiblePaths =
        [
            @"C:\Program Files\Docker\Docker\Docker Desktop.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Docker", "Docker", "Docker Desktop.exe"),
        ];

        string? exePath = possiblePaths.FirstOrDefault(File.Exists);
        if (exePath is null)
            return new FixResult("Start Docker Desktop", false, "Docker Desktop executable not found. Please install it from https://www.docker.com/products/docker-desktop/");

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exePath) { UseShellExecute = true });
            return new FixResult("Start Docker Desktop", true, "Docker Desktop launch command issued. Wait ~30 seconds for it to be ready.");
        }
        catch (Exception ex)
        {
            return new FixResult("Start Docker Desktop", false, $"Failed to start Docker Desktop: {ex.Message}");
        }
    }
}
