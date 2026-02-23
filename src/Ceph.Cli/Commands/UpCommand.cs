using System.CommandLine;
using Ceph.Cli.Services;

namespace Ceph.Cli.Commands;

/// <summary>
/// <c>ceph-cli up</c> – starts the Ceph cluster using docker compose.
/// </summary>
public class UpCommand : Command
{
    public UpCommand() : base("up", "Start the Ceph cluster with docker compose")
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

        Console.WriteLine($"Starting Ceph cluster from {Path.GetFullPath(dir)} ...");
        var (exitCode, output) = EnvironmentChecker.RunProcess(
            "docker", $"compose -f \"{composePath}\" up -d", captureOutput: true);

        Console.WriteLine(output);

        if (exitCode == 0)
        {
            Console.WriteLine("Cluster started. Run 'ceph-cli status' to check health (allow ~60 s for bootstrap).");
        }
        else
        {
            Console.Error.WriteLine($"docker compose up failed (exit code {exitCode}).");
            Console.Error.WriteLine("Run 'ceph-cli diagnose' to check your environment.");
        }
    }
}
