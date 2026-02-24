using System.CommandLine;
using System.Text;

namespace Ceph.Cli.Commands;

/// <summary>
/// <c>ceph-cli install-skill</c> – generates a CLAUDE.md file that teaches
/// AI coding agents how to use ceph-cli.
/// </summary>
public class InstallSkillCommand : Command
{
    public InstallSkillCommand() : base("install-skill", "Generate a CLAUDE.md file that teaches AI coding agents how to use ceph-cli")
    {
        var outputOption = new Option<string>(
            aliases: ["--output", "-o"],
            description: "Directory where CLAUDE.md will be written (default: current directory)",
            getDefaultValue: () => Directory.GetCurrentDirectory());

        AddOption(outputOption);

        this.SetHandler(Handle, outputOption);
    }

    private static void Handle(string output)
    {
        string dir = Path.GetFullPath(output);
        Directory.CreateDirectory(dir);

        string filePath = Path.Combine(dir, "CLAUDE.md");

        var sb = new StringBuilder();

        sb.AppendLine("# ceph-cli – Ceph Cluster Management for Windows/Docker");
        sb.AppendLine();
        sb.AppendLine("`ceph-cli` is a .NET CLI tool that scaffolds and manages a Ceph storage cluster running in Docker on Windows.");
        sb.AppendLine("It generates docker-compose files, starts/stops the cluster, checks health, diagnoses environment issues, and applies fixes.");
        sb.AppendLine();
        sb.AppendLine("## Installation");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("dotnet tool install --global ceph-cli");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("Or run from source:");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("dotnet run --project src/Ceph.Cli -- <command> [options]");
        sb.AppendLine("```");
        sb.AppendLine();

        // --- Commands ---
        sb.AppendLine("## Commands");
        sb.AppendLine();

        // init
        sb.AppendLine("### `ceph-cli init`");
        sb.AppendLine();
        sb.AppendLine("Generate docker-compose files and configuration for running Ceph in Docker on Windows.");
        sb.AppendLine();
        sb.AppendLine("| Option | Alias | Default | Description |");
        sb.AppendLine("|--------|-------|---------|-------------|");
        sb.AppendLine("| `--output` | `-o` | current directory | Directory where files will be generated |");
        sb.AppendLine("| `--monitors` | `-m` | `1` | Number of monitor (MON) daemons |");
        sb.AppendLine("| `--osds` | `-s` | `3` | Number of OSD daemons |");
        sb.AppendLine("| `--managers` | | `1` | Number of manager (MGR) daemons |");
        sb.AppendLine("| `--image` | | `quay.io/ceph/ceph:v18` | Ceph container image to use |");
        sb.AppendLine("| `--rgw` | | `false` | Include a RADOS Gateway (S3/Swift API) service |");
        sb.AppendLine("| `--mds` | | `false` | Include a Metadata Server (CephFS) service |");
        sb.AppendLine();
        sb.AppendLine("**Example:**");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("ceph-cli init --output ./my-cluster --monitors 3 --osds 3 --rgw");
        sb.AppendLine("```");
        sb.AppendLine();

        // up
        sb.AppendLine("### `ceph-cli up`");
        sb.AppendLine();
        sb.AppendLine("Start the Ceph cluster with docker compose.");
        sb.AppendLine();
        sb.AppendLine("| Option | Alias | Default | Description |");
        sb.AppendLine("|--------|-------|---------|-------------|");
        sb.AppendLine("| `--dir` | `-d` | current directory | Directory containing the generated docker-compose.yml |");
        sb.AppendLine();
        sb.AppendLine("**Example:**");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("ceph-cli up --dir ./my-cluster");
        sb.AppendLine("```");
        sb.AppendLine();

        // down
        sb.AppendLine("### `ceph-cli down`");
        sb.AppendLine();
        sb.AppendLine("Stop the Ceph cluster and remove containers.");
        sb.AppendLine();
        sb.AppendLine("| Option | Alias | Default | Description |");
        sb.AppendLine("|--------|-------|---------|-------------|");
        sb.AppendLine("| `--dir` | `-d` | current directory | Directory containing the generated docker-compose.yml |");
        sb.AppendLine("| `--volumes` | | `false` | Also remove persistent data volumes (destroys cluster data) |");
        sb.AppendLine();
        sb.AppendLine("**Example:**");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# Stop cluster, keep data");
        sb.AppendLine("ceph-cli down --dir ./my-cluster");
        sb.AppendLine();
        sb.AppendLine("# Stop cluster and destroy all data");
        sb.AppendLine("ceph-cli down --dir ./my-cluster --volumes");
        sb.AppendLine("```");
        sb.AppendLine();

        // status
        sb.AppendLine("### `ceph-cli status`");
        sb.AppendLine();
        sb.AppendLine("Check the Ceph cluster health and container status.");
        sb.AppendLine();
        sb.AppendLine("| Option | Alias | Default | Description |");
        sb.AppendLine("|--------|-------|---------|-------------|");
        sb.AppendLine("| `--dir` | `-d` | current directory | Directory containing the generated docker-compose.yml |");
        sb.AppendLine();
        sb.AppendLine("**Example:**");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("ceph-cli status");
        sb.AppendLine("```");
        sb.AppendLine();

        // diagnose
        sb.AppendLine("### `ceph-cli diagnose`");
        sb.AppendLine();
        sb.AppendLine("Detect common Windows/Docker issues that affect running Ceph.");
        sb.AppendLine();
        sb.AppendLine("| Option | Default | Description |");
        sb.AppendLine("|--------|---------|-------------|");
        sb.AppendLine("| `--json` | `false` | Output results as JSON |");
        sb.AppendLine();
        sb.AppendLine("**Checks performed:**");
        sb.AppendLine();
        sb.AppendLine("- WSL2 default version");
        sb.AppendLine("- WSL2 memory configuration");
        sb.AppendLine("- Docker daemon reachable");
        sb.AppendLine("- Docker WSL2 backend");
        sb.AppendLine("- Docker network conflict (172.20.x.x subnet)");
        sb.AppendLine("- Disk space");
        sb.AppendLine();
        sb.AppendLine("**Example:**");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("ceph-cli diagnose");
        sb.AppendLine("ceph-cli diagnose --json");
        sb.AppendLine("```");
        sb.AppendLine();

        // fix
        sb.AppendLine("### `ceph-cli fix`");
        sb.AppendLine();
        sb.AppendLine("Attempt to automatically remediate common Windows/Docker issues for running Ceph.");
        sb.AppendLine();
        sb.AppendLine("| Option | Default | Description |");
        sb.AppendLine("|--------|---------|-------------|");
        sb.AppendLine("| `--dry-run` | `false` | Show what would be fixed without making any changes |");
        sb.AppendLine("| `--wsl-memory` | `6` | WSL2 memory limit in GB to configure in ~/.wslconfig |");
        sb.AppendLine();
        sb.AppendLine("**Example:**");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("ceph-cli fix --dry-run");
        sb.AppendLine("ceph-cli fix --wsl-memory 8");
        sb.AppendLine("```");
        sb.AppendLine();

        // --- Workflows ---
        sb.AppendLine("## Common Workflows");
        sb.AppendLine();

        sb.AppendLine("### First-time setup (init → up → status)");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# 1. Scaffold the cluster files");
        sb.AppendLine("ceph-cli init --output ./my-cluster");
        sb.AppendLine();
        sb.AppendLine("# 2. Start the cluster");
        sb.AppendLine("ceph-cli up --dir ./my-cluster");
        sb.AppendLine();
        sb.AppendLine("# 3. Wait ~60 seconds for bootstrap, then check health");
        sb.AppendLine("ceph-cli status --dir ./my-cluster");
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("### Diagnose and fix environment issues");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# 1. Run diagnostics");
        sb.AppendLine("ceph-cli diagnose");
        sb.AppendLine();
        sb.AppendLine("# 2. Preview fixes");
        sb.AppendLine("ceph-cli fix --dry-run");
        sb.AppendLine();
        sb.AppendLine("# 3. Apply fixes");
        sb.AppendLine("ceph-cli fix");
        sb.AppendLine();
        sb.AppendLine("# 4. Verify");
        sb.AppendLine("ceph-cli diagnose");
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("### Reset cluster (destroy and recreate)");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# 1. Tear down and remove all data");
        sb.AppendLine("ceph-cli down --dir ./my-cluster --volumes");
        sb.AppendLine();
        sb.AppendLine("# 2. Re-scaffold (if options need to change)");
        sb.AppendLine("ceph-cli init --output ./my-cluster");
        sb.AppendLine();
        sb.AppendLine("# 3. Start fresh");
        sb.AppendLine("ceph-cli up --dir ./my-cluster");
        sb.AppendLine("```");
        sb.AppendLine();

        // --- Tips ---
        sb.AppendLine("## Tips for AI Agents");
        sb.AppendLine();
        sb.AppendLine("- **Always run `diagnose` before `up`** if the user reports issues. Most problems are environment-related (WSL2, Docker, disk space).");
        sb.AppendLine("- **Wait ~60 seconds after `up`** before checking `status`. The Ceph monitors need time to form a quorum and bootstrap.");
        sb.AppendLine("- **Use `fix --dry-run` first** to preview what changes will be made before applying them.");
        sb.AppendLine("- **The `--volumes` flag on `down` is destructive** — it deletes all cluster data. Only use it when the user explicitly wants to reset.");
        sb.AppendLine("- **The default `init` settings** (1 MON, 3 OSDs, 1 MGR) are suitable for local development. Production-like setups need 3+ monitors.");
        sb.AppendLine("- **If `status` can't reach ceph-mon1**, the cluster is likely still bootstrapping or failed to start — check `docker ps` and container logs.");
        sb.AppendLine("- **The `--dir` option** on `up`, `down`, and `status` must point to the directory containing the generated `docker-compose.yml`.");

        File.WriteAllText(filePath, sb.ToString());

        Console.WriteLine($"Created: {filePath}");
        Console.WriteLine();
        Console.WriteLine("The CLAUDE.md file describes all ceph-cli commands, options, and workflows.");
        Console.WriteLine("AI coding agents will use it to understand how to manage your Ceph cluster.");
    }
}
