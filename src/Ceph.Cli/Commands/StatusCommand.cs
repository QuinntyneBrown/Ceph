using System.CommandLine;
using Ceph.Cli.Services;

namespace Ceph.Cli.Commands;

/// <summary>
/// <c>ceph-cli status</c> – checks Ceph cluster health via docker exec.
/// </summary>
public class StatusCommand : Command
{
    public StatusCommand() : base("status", "Check the Ceph cluster health and container status")
    {
        var dirOption = new Option<string>(
            aliases: ["--dir", "-d"],
            description: "Directory containing the generated docker-compose.yml",
            getDefaultValue: () => Directory.GetCurrentDirectory());

        AddOption(dirOption);

        this.SetHandler(Handle, dirOption);
    }

    private static void Handle(string dir)
    {
        string composePath = Path.Combine(Path.GetFullPath(dir), "docker-compose.yml");
        if (!File.Exists(composePath))
        {
            Console.Error.WriteLine($"docker-compose.yml not found in {Path.GetFullPath(dir)}");
            Console.Error.WriteLine("Run 'ceph-cli init' first to generate cluster files.");
            return;
        }

        // Show container status
        Console.WriteLine("=== Container Status ===");
        var (psExit, psOutput) = EnvironmentChecker.RunProcess(
            "docker", $"compose -f \"{composePath}\" ps", captureOutput: true);
        Console.WriteLine(psOutput);

        if (psExit != 0)
        {
            Console.Error.WriteLine("Could not query container status. Is Docker running?");
            return;
        }

        // Try to get Ceph cluster health from the first MON
        Console.WriteLine("=== Ceph Cluster Health ===");
        var (healthExit, healthOutput) = EnvironmentChecker.RunProcess(
            "docker", "exec ceph-mon1 ceph status", captureOutput: true);

        if (healthExit == 0)
        {
            Console.WriteLine(healthOutput);
        }
        else
        {
            Console.Error.WriteLine("Could not reach ceph-mon1. The cluster may still be bootstrapping.");
            Console.Error.WriteLine("Wait ~60 seconds after 'ceph-cli up' and try again.");
            if (!string.IsNullOrWhiteSpace(healthOutput))
                Console.Error.WriteLine(healthOutput);
        }
    }
}
