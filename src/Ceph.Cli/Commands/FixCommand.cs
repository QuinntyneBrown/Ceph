using System.CommandLine;
using Ceph.Cli.Services;

namespace Ceph.Cli.Commands;

/// <summary>
/// <c>ceph-cli fix</c> – attempts to automatically remediate common
/// Windows/Docker issues detected by the diagnose command.
/// </summary>
public class FixCommand : Command
{
    public FixCommand() : base("fix", "Attempt to automatically remediate common Windows/Docker issues for running Ceph")
    {
        var dryRunOption = new Option<bool>(
            "--dry-run",
            description: "Show what would be fixed without making any changes",
            getDefaultValue: () => false);

        var memoryOption = new Option<int>(
            "--wsl-memory",
            description: "WSL2 memory limit in GB to configure in ~/.wslconfig",
            getDefaultValue: () => 6);

        AddOption(dryRunOption);
        AddOption(memoryOption);

        this.SetHandler(Handle, dryRunOption, memoryOption);
    }

    private static void Handle(bool dryRun, int wslMemory)
    {
        if (dryRun)
        {
            Console.WriteLine("[DRY RUN] The following fixes would be applied:");
            Console.WriteLine("  • Set WSL2 as the default version (wsl --set-default-version 2)");
            Console.WriteLine("  • Update ~/.wslconfig with recommended memory/swap settings");
            Console.WriteLine("  • Start Docker Desktop if it is not running");
            Console.WriteLine();
            Console.WriteLine("Re-run without --dry-run to apply the fixes.");
            return;
        }

        Console.WriteLine("=== Ceph Automatic Remediation ===");
        Console.WriteLine();

        // Run diagnostics first so we only apply fixes that are needed.
        var checker = new EnvironmentChecker();
        var diagnostics = checker.RunAll().ToDictionary(r => r.Name, r => r);
        var fixer = new IssueFixer();

        // Fix WSL2 default version
        if (diagnostics.TryGetValue("WSL2 default version", out var wsl2Check) && !wsl2Check.Passed)
        {
            Console.Write("  Fixing WSL2 default version... ");
            var result = fixer.FixWsl2DefaultVersion();
            PrintFixResult(result);
        }
        else
        {
            Console.WriteLine("  ✔  WSL2 default version – already OK.");
        }

        // Fix WSL2 memory configuration
        if (diagnostics.TryGetValue("WSL2 memory configuration", out var memCheck) && !memCheck.Passed)
        {
            Console.Write("  Fixing WSL2 memory configuration... ");
            var result = fixer.FixWsl2MemoryConfiguration(memoryGb: wslMemory);
            PrintFixResult(result);
        }
        else
        {
            Console.WriteLine("  ✔  WSL2 memory configuration – already OK.");
        }

        // Attempt to start Docker Desktop if daemon is not reachable
        if (diagnostics.TryGetValue("Docker daemon reachable", out var dockerCheck) && !dockerCheck.Passed)
        {
            Console.Write("  Starting Docker Desktop... ");
            var result = fixer.StartDockerDesktop();
            PrintFixResult(result);
        }
        else
        {
            Console.WriteLine("  ✔  Docker daemon – already running.");
        }

        Console.WriteLine();
        Console.WriteLine("Fix pass complete. Run 'ceph-cli diagnose' to verify the environment.");
    }

    private static void PrintFixResult(IssueFixer.FixResult result)
    {
        string icon = result.Applied ? "✔" : "✖";
        string color = result.Applied ? "\x1b[32m" : "\x1b[31m";
        string reset = "\x1b[0m";
        Console.WriteLine($"{color}{icon}{reset}  {result.Message}");
    }
}
