using System.CommandLine;
using Ceph.Cli.Services;

namespace Ceph.Cli.Commands;

/// <summary>
/// <c>ceph-cli down</c> – stops the Ceph cluster and optionally removes volumes.
/// </summary>
public class DownCommand : Command
{
    public DownCommand() : base("down", "Stop the Ceph cluster and remove containers")
    {
        var dirOption = new Option<string>(
            aliases: ["--dir", "-d"],
            description: "Directory containing the generated docker-compose.yml",
            getDefaultValue: () => Directory.GetCurrentDirectory());

        var volumesOption = new Option<bool>(
            "--volumes",
            description: "Also remove persistent data volumes (destroys cluster data)",
            getDefaultValue: () => false);

        AddOption(dirOption);
        AddOption(volumesOption);

        this.SetHandler(Handle, dirOption, volumesOption);
    }

    private static void Handle(string dir, bool volumes)
    {
        string composePath = Path.Combine(Path.GetFullPath(dir), "docker-compose.yml");
        if (!File.Exists(composePath))
        {
            Console.Error.WriteLine($"docker-compose.yml not found in {Path.GetFullPath(dir)}");
            return;
        }

        string args = volumes
            ? $"compose -f \"{composePath}\" down --volumes"
            : $"compose -f \"{composePath}\" down";

        Console.WriteLine(volumes
            ? "Stopping Ceph cluster and removing volumes ..."
            : "Stopping Ceph cluster ...");

        var (exitCode, output) = EnvironmentChecker.RunProcess("docker", args, captureOutput: true);
        Console.WriteLine(output);

        if (exitCode == 0)
            Console.WriteLine("Cluster stopped.");
        else
            Console.Error.WriteLine($"docker compose down failed (exit code {exitCode}).");
    }
}
