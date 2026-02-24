using System.CommandLine;
using Ceph.Cli.Services;

namespace Ceph.Cli.Commands;

/// <summary>
/// <c>ceph-cli init</c> – scaffolds docker-compose files and configuration for
/// running a Ceph cluster in Docker on Windows.
/// </summary>
public class InitCommand : Command
{
    public InitCommand() : base("init", "Generate docker-compose files and configuration for running Ceph in Docker on Windows")
    {
        var outputOption = new Option<string>(
            aliases: ["--output", "-o"],
            description: "Directory where files will be generated (default: current directory)",
            getDefaultValue: () => Directory.GetCurrentDirectory());

        var monCountOption = new Option<int>(
            aliases: ["--monitors", "-m"],
            description: "Number of monitor (MON) daemons",
            getDefaultValue: () => 1);

        var osdCountOption = new Option<int>(
            aliases: ["--osds", "-s"],
            description: "Number of OSD daemons",
            getDefaultValue: () => 3);

        var mgrCountOption = new Option<int>(
            "--managers",
            description: "Number of manager (MGR) daemons",
            getDefaultValue: () => 1);

        var imageOption = new Option<string>(
            "--image",
            description: "Ceph container image to use",
            getDefaultValue: () => "quay.io/ceph/ceph:v17");

        var rgwOption = new Option<bool>(
            "--rgw",
            description: "Include a RADOS Gateway (S3/Swift API) service",
            getDefaultValue: () => false);

        var mdsOption = new Option<bool>(
            "--mds",
            description: "Include a Metadata Server (CephFS) service",
            getDefaultValue: () => false);

        AddOption(outputOption);
        AddOption(monCountOption);
        AddOption(osdCountOption);
        AddOption(mgrCountOption);
        AddOption(imageOption);
        AddOption(rgwOption);
        AddOption(mdsOption);

        this.SetHandler(
            Handle,
            outputOption, monCountOption, osdCountOption, mgrCountOption, imageOption, rgwOption, mdsOption);
    }

    private static void Handle(
        string output,
        int monitors,
        int osds,
        int managers,
        string image,
        bool rgw,
        bool mds)
    {
        Console.WriteLine($"Generating Ceph docker-compose files in: {Path.GetFullPath(output)}");
        Console.WriteLine($"  Monitors : {monitors}");
        Console.WriteLine($"  OSDs     : {osds}");
        Console.WriteLine($"  Managers : {managers}");
        Console.WriteLine($"  Image    : {image}");
        Console.WriteLine($"  RGW      : {(rgw ? "yes" : "no")}");
        Console.WriteLine($"  MDS      : {(mds ? "yes" : "no")}");
        Console.WriteLine();

        var generator = new DockerComposeGenerator();
        var options = new DockerComposeGenerator.GenerateOptions(
            OutputDirectory: Path.GetFullPath(output),
            MonitorCount: monitors,
            OsdCount: osds,
            MgrCount: managers,
            CephImage: image,
            IncludeRgw: rgw,
            IncludeMds: mds);

        var files = generator.Generate(options);

        Console.WriteLine("Created files:");
        foreach (var file in files)
            Console.WriteLine($"  {file}");

        Console.WriteLine();
        Console.WriteLine("Next steps:");
        Console.WriteLine("  1. Review ceph.conf and .env");
        Console.WriteLine("  2. (Optional) Copy wslconfig.recommended to %USERPROFILE%\\.wslconfig and run: wsl --shutdown");
        Console.WriteLine("  3. Start the cluster: docker compose up -d");
        Console.WriteLine("  4. Check health    : docker exec ceph-mon1 ceph status");
    }
}
