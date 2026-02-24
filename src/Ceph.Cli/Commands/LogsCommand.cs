using System.CommandLine;
using Ceph.Cli.Services;

namespace Ceph.Cli.Commands;

/// <summary>
/// <c>ceph-cli logs</c> – streams or displays container logs for Ceph services.
/// </summary>
public class LogsCommand : Command
{
    public LogsCommand() : base("logs", "View logs from Ceph cluster containers")
    {
        var dirOption = new Option<string>(
            aliases: ["--dir", "-d"],
            description: "Directory containing the generated docker-compose.yml",
            getDefaultValue: () => Directory.GetCurrentDirectory());

        var serviceOption = new Option<string?>(
            aliases: ["--service", "-s"],
            description: "Specific service name (e.g. ceph-mon1, ceph-osd1). If omitted, shows logs for all services.");

        var followOption = new Option<bool>(
            aliases: ["--follow", "-f"],
            description: "Follow log output (stream in real-time)",
            getDefaultValue: () => false);

        var tailOption = new Option<int>(
            aliases: ["--tail", "-n"],
            description: "Number of lines to show from the end of the logs",
            getDefaultValue: () => 100);

        AddOption(dirOption);
        AddOption(serviceOption);
        AddOption(followOption);
        AddOption(tailOption);

        this.SetHandler(Handle, dirOption, serviceOption, followOption, tailOption);
    }

    private static void Handle(string dir, string? service, bool follow, int tail)
    {
        string composePath = Path.Combine(Path.GetFullPath(dir), "docker-compose.yml");
        if (!File.Exists(composePath))
        {
            Console.Error.WriteLine($"docker-compose.yml not found in {Path.GetFullPath(dir)}");
            Console.Error.WriteLine("Run 'ceph-cli init' first to generate cluster files.");
            return;
        }

        string args = $"compose -f \"{composePath}\" logs --tail {tail}";
        if (follow)
            args += " --follow";
        if (!string.IsNullOrWhiteSpace(service))
            args += $" {service}";

        if (follow)
        {
            // For follow mode, stream output directly to console (don't capture)
            Console.WriteLine($"Streaming logs (Ctrl+C to stop)...");
            var psi = new System.Diagnostics.ProcessStartInfo("docker", args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var process = System.Diagnostics.Process.Start(psi);
            process?.WaitForExit();
        }
        else
        {
            var (exitCode, output) = EnvironmentChecker.RunProcess("docker", args, captureOutput: true);
            if (exitCode == 0)
            {
                Console.WriteLine(output);
            }
            else
            {
                Console.Error.WriteLine($"Failed to retrieve logs (exit code {exitCode}).");
                if (!string.IsNullOrWhiteSpace(output))
                    Console.Error.WriteLine(output);
            }
        }
    }
}
