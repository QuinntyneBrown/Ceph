using System.CommandLine;
using System.Text;

namespace Ceph.Cli.Commands;

/// <summary>
/// <c>ceph-cli generate-guide</c> – generates a comprehensive markdown reference
/// designed for small language models to fully understand ceph-cli usage and
/// troubleshoot any issues.
/// </summary>
public class GenerateGuideCommand : Command
{
    public GenerateGuideCommand() : base("generate-guide", "Generate a comprehensive markdown guide for LLMs to understand and troubleshoot ceph-cli")
    {
        var outputOption = new Option<string>(
            aliases: ["--output", "-o"],
            description: "Directory where the guide will be written (default: current directory)",
            getDefaultValue: () => Directory.GetCurrentDirectory());

        var fileNameOption = new Option<string>(
            aliases: ["--filename", "-f"],
            description: "Name of the generated file",
            getDefaultValue: () => "ceph-cli-guide.md");

        AddOption(outputOption);
        AddOption(fileNameOption);

        this.SetHandler(Handle, outputOption, fileNameOption);
    }

    private static void Handle(string output, string fileName)
    {
        string dir = Path.GetFullPath(output);
        Directory.CreateDirectory(dir);
        string filePath = Path.Combine(dir, fileName);

        var sb = new StringBuilder();

        WriteHeader(sb);
        WriteQuickReference(sb);
        WriteArchitecture(sb);
        WriteCommandReference(sb);
        WriteGeneratedFiles(sb);
        WriteWorkflows(sb);
        WriteTroubleshooting(sb);
        WriteEnvironmentChecks(sb);
        WriteCephHealthReference(sb);
        WriteDockerReference(sb);
        WriteDecisionTree(sb);

        File.WriteAllText(filePath, sb.ToString());

        Console.WriteLine($"Created: {filePath}");
        Console.WriteLine($"Size: {new FileInfo(filePath).Length / 1024} KB");
        Console.WriteLine();
        Console.WriteLine("This guide covers:");
        Console.WriteLine("  - Complete command reference with all options");
        Console.WriteLine("  - Cluster architecture and how services relate");
        Console.WriteLine("  - Step-by-step workflows for every scenario");
        Console.WriteLine("  - Troubleshooting decision tree (symptom → cause → fix)");
        Console.WriteLine("  - Ceph health status reference");
        Console.WriteLine("  - Docker and WSL2 diagnostics");
    }

    // -------------------------------------------------------------------------
    // Sections
    // -------------------------------------------------------------------------

    private static void WriteHeader(StringBuilder sb)
    {
        sb.AppendLine("# ceph-cli Complete Guide");
        sb.AppendLine();
        sb.AppendLine("This document is a self-contained reference for `ceph-cli`, a .NET CLI tool that");
        sb.AppendLine("scaffolds and manages a Ceph distributed storage cluster running in Docker on Windows");
        sb.AppendLine("(via WSL2 and Docker Desktop). It is written for language models and automated agents");
        sb.AppendLine("that need to operate ceph-cli, interpret its output, and resolve problems.");
        sb.AppendLine();
        sb.AppendLine("## Key Facts");
        sb.AppendLine();
        sb.AppendLine("- **Runtime**: .NET 8+ CLI tool");
        sb.AppendLine("- **Platform**: Windows 10/11 with WSL2 and Docker Desktop");
        sb.AppendLine("- **Default Ceph version**: Quincy (v17) via `quay.io/ceph/ceph:v17`");
        sb.AppendLine("- **Container orchestration**: Docker Compose v2");
        sb.AppendLine("- **Network subnet**: `172.20.0.0/16` (configurable)");
        sb.AppendLine("- **Install**: `dotnet tool install -g QuinntyneBrown.Ceph.Cli`");
        sb.AppendLine("- **Run from source**: `dotnet run --project src/Ceph.Cli -- <command> [options]`");
        sb.AppendLine();
    }

    private static void WriteQuickReference(StringBuilder sb)
    {
        sb.AppendLine("## Quick Reference Card");
        sb.AppendLine();
        sb.AppendLine("| Command | Purpose | Key Flags |");
        sb.AppendLine("|---------|---------|-----------|");
        sb.AppendLine("| `ceph-cli init` | Generate cluster files | `-o <dir>`, `-m <mons>`, `-s <osds>`, `--rgw`, `--mds` |");
        sb.AppendLine("| `ceph-cli up` | Start the cluster | `-d <dir>` |");
        sb.AppendLine("| `ceph-cli down` | Stop the cluster | `-d <dir>`, `--volumes` (destroys data) |");
        sb.AppendLine("| `ceph-cli status` | Check cluster health | `-d <dir>` |");
        sb.AppendLine("| `ceph-cli logs` | View container logs | `-d <dir>`, `-s <service>`, `-f` (follow), `-n <lines>` |");
        sb.AppendLine("| `ceph-cli diagnose` | Run environment checks | `--json` |");
        sb.AppendLine("| `ceph-cli fix` | Auto-fix environment issues | `--dry-run`, `--wsl-memory <GB>` |");
        sb.AppendLine();
        sb.AppendLine("**Minimum viable workflow:**");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("ceph-cli diagnose              # check environment");
        sb.AppendLine("ceph-cli fix                   # fix any issues");
        sb.AppendLine("ceph-cli init -o ./cluster     # generate files");
        sb.AppendLine("ceph-cli up -d ./cluster       # start cluster");
        sb.AppendLine("# wait 60 seconds");
        sb.AppendLine("ceph-cli status -d ./cluster   # verify health");
        sb.AppendLine("```");
        sb.AppendLine();
    }

    private static void WriteArchitecture(StringBuilder sb)
    {
        sb.AppendLine("## Cluster Architecture");
        sb.AppendLine();
        sb.AppendLine("### Services and Their Roles");
        sb.AppendLine();
        sb.AppendLine("| Service | Container Name | Role | Required | Count |");
        sb.AppendLine("|---------|---------------|------|----------|-------|");
        sb.AppendLine("| MON (Monitor) | `ceph-mon1` ... `ceph-monN` | Maintains cluster maps, manages consensus | Yes | 1+ (odd numbers recommended, 3 for HA) |");
        sb.AppendLine("| MGR (Manager) | `ceph-mgr1` ... `ceph-mgrN` | Provides metrics, dashboard, module host | Yes | 1+ |");
        sb.AppendLine("| OSD (Object Store Daemon) | `ceph-osd1` ... `ceph-osdN` | Stores actual data on block devices | Yes | 1+ (3 recommended for replication) |");
        sb.AppendLine("| RGW (RADOS Gateway) | `ceph-rgw` | S3/Swift compatible HTTP API | No | 0 or 1 |");
        sb.AppendLine("| MDS (Metadata Server) | `ceph-mds` | Manages CephFS filesystem metadata | No | 0 or 1 |");
        sb.AppendLine();
        sb.AppendLine("### Startup Order and Dependencies");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("MON starts first (no dependencies)");
        sb.AppendLine(" └─ MON becomes healthy (healthcheck: ceph health)");
        sb.AppendLine("     ├─ MGR starts (depends_on: MON healthy)");
        sb.AppendLine("     ├─ OSD starts (depends_on: MON healthy) ← runs privileged");
        sb.AppendLine("     ├─ RGW starts (depends_on: MON healthy) ← optional, port 7480");
        sb.AppendLine("     └─ MDS starts (depends_on: MON healthy) ← optional");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("**Important timing**: After `ceph-cli up`, the cluster needs ~60 seconds to fully");
        sb.AppendLine("bootstrap. During this time, `ceph-cli status` may report the cluster as unreachable.");
        sb.AppendLine("This is normal.");
        sb.AppendLine();
        sb.AppendLine("### Network Layout");
        sb.AppendLine();
        sb.AppendLine("All containers are on a Docker bridge network `ceph-net` with subnet `172.20.0.0/16`.");
        sb.AppendLine();
        sb.AppendLine("| Service | IP Range |");
        sb.AppendLine("|---------|----------|");
        sb.AppendLine("| MON | `172.20.1.1` – `172.20.1.N` |");
        sb.AppendLine("| MGR | `172.20.2.1` – `172.20.2.N` |");
        sb.AppendLine("| OSD | `172.20.3.1` – `172.20.3.N` |");
        sb.AppendLine("| RGW | `172.20.4.1` |");
        sb.AppendLine("| MDS | `172.20.4.2` |");
        sb.AppendLine();
        sb.AppendLine("### Data Volumes");
        sb.AppendLine();
        sb.AppendLine("Each service has a named Docker volume for persistent data:");
        sb.AppendLine();
        sb.AppendLine("- `ceph-etc` — shared `/etc/ceph` (keyrings, ceph.conf) across all containers");
        sb.AppendLine("- `ceph-mon1-data` — MON data (`/var/lib/ceph`)");
        sb.AppendLine("- `ceph-mgr1-data` — MGR data");
        sb.AppendLine("- `ceph-osd1-data` through `ceph-osdN-data` — OSD data (block storage)");
        sb.AppendLine();
        sb.AppendLine("**WARNING**: `ceph-cli down --volumes` deletes ALL these volumes. Data is unrecoverable.");
        sb.AppendLine();
    }

    private static void WriteCommandReference(StringBuilder sb)
    {
        sb.AppendLine("## Complete Command Reference");
        sb.AppendLine();

        // init
        sb.AppendLine("### `ceph-cli init`");
        sb.AppendLine();
        sb.AppendLine("Generate docker-compose.yml, ceph.conf, entrypoint.sh, .env, README.md, and wslconfig.recommended.");
        sb.AppendLine();
        sb.AppendLine("| Option | Alias | Default | Description |");
        sb.AppendLine("|--------|-------|---------|-------------|");
        sb.AppendLine("| `--output` | `-o` | `.` (current dir) | Output directory for generated files |");
        sb.AppendLine("| `--monitors` | `-m` | `1` | Number of MON daemons (use odd numbers: 1, 3, 5) |");
        sb.AppendLine("| `--osds` | `-s` | `3` | Number of OSD daemons (minimum 1, 3 for replication) |");
        sb.AppendLine("| `--managers` | | `1` | Number of MGR daemons |");
        sb.AppendLine("| `--image` | | `quay.io/ceph/ceph:v17` | Ceph container image |");
        sb.AppendLine("| `--rgw` | | `false` | Include RADOS Gateway (S3/Swift API on port 7480) |");
        sb.AppendLine("| `--mds` | | `false` | Include Metadata Server (for CephFS) |");
        sb.AppendLine();
        sb.AppendLine("**Output**: Creates 6 files in the output directory. Overwrites existing files.");
        sb.AppendLine();
        sb.AppendLine("**Examples:**");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# Minimal dev cluster (1 MON, 3 OSDs, 1 MGR)");
        sb.AppendLine("ceph-cli init -o ./cluster");
        sb.AppendLine();
        sb.AppendLine("# HA cluster with S3 gateway");
        sb.AppendLine("ceph-cli init -o ./cluster -m 3 -s 5 --rgw");
        sb.AppendLine();
        sb.AppendLine("# Full-featured cluster");
        sb.AppendLine("ceph-cli init -o ./cluster -m 3 -s 5 --managers 2 --rgw --mds");
        sb.AppendLine("```");
        sb.AppendLine();

        // up
        sb.AppendLine("### `ceph-cli up`");
        sb.AppendLine();
        sb.AppendLine("Start the Ceph cluster using `docker compose up -d`.");
        sb.AppendLine();
        sb.AppendLine("| Option | Alias | Default | Description |");
        sb.AppendLine("|--------|-------|---------|-------------|");
        sb.AppendLine("| `--dir` | `-d` | `.` (current dir) | Directory containing docker-compose.yml |");
        sb.AppendLine();
        sb.AppendLine("**Behavior**: Pulls images if not cached, creates containers, starts in detached mode.");
        sb.AppendLine("Fails if docker-compose.yml is not found (tells user to run `init` first).");
        sb.AppendLine();
        sb.AppendLine("**Exit codes**: 0 = success, non-zero = docker compose failure.");
        sb.AppendLine();

        // down
        sb.AppendLine("### `ceph-cli down`");
        sb.AppendLine();
        sb.AppendLine("Stop the Ceph cluster and remove containers.");
        sb.AppendLine();
        sb.AppendLine("| Option | Alias | Default | Description |");
        sb.AppendLine("|--------|-------|---------|-------------|");
        sb.AppendLine("| `--dir` | `-d` | `.` (current dir) | Directory containing docker-compose.yml |");
        sb.AppendLine("| `--volumes` | | `false` | DESTRUCTIVE: Also removes all named data volumes |");
        sb.AppendLine();
        sb.AppendLine("**Without `--volumes`**: Containers are removed but volumes persist. Running `up` again");
        sb.AppendLine("resumes the cluster with existing data.");
        sb.AppendLine();
        sb.AppendLine("**With `--volumes`**: All cluster data is permanently deleted. Next `up` bootstraps from scratch.");
        sb.AppendLine();

        // status
        sb.AppendLine("### `ceph-cli status`");
        sb.AppendLine();
        sb.AppendLine("Display container status and Ceph cluster health.");
        sb.AppendLine();
        sb.AppendLine("| Option | Alias | Default | Description |");
        sb.AppendLine("|--------|-------|---------|-------------|");
        sb.AppendLine("| `--dir` | `-d` | `.` (current dir) | Directory containing docker-compose.yml |");
        sb.AppendLine();
        sb.AppendLine("**Output sections:**");
        sb.AppendLine();
        sb.AppendLine("1. **Container Status** — `docker compose ps` output showing each container's state");
        sb.AppendLine("2. **Ceph Cluster Health** — output of `docker exec ceph-mon1 ceph status` showing:");
        sb.AppendLine("   - Cluster ID and overall health (HEALTH_OK, HEALTH_WARN, HEALTH_ERR)");
        sb.AppendLine("   - Service counts (MON quorum, MGR active, OSD up/in counts)");
        sb.AppendLine("   - Data summary (pools, objects, usage, placement groups)");
        sb.AppendLine();
        sb.AppendLine("**If status fails**: The MON container may still be bootstrapping. Wait 60 seconds and retry.");
        sb.AppendLine();

        // logs
        sb.AppendLine("### `ceph-cli logs`");
        sb.AppendLine();
        sb.AppendLine("View logs from Ceph cluster containers.");
        sb.AppendLine();
        sb.AppendLine("| Option | Alias | Default | Description |");
        sb.AppendLine("|--------|-------|---------|-------------|");
        sb.AppendLine("| `--dir` | `-d` | `.` (current dir) | Directory containing docker-compose.yml |");
        sb.AppendLine("| `--service` | `-s` | (all) | Filter to a specific service (e.g. `ceph-mon1`, `ceph-osd1`) |");
        sb.AppendLine("| `--follow` | `-f` | `false` | Stream logs in real-time (Ctrl+C to stop) |");
        sb.AppendLine("| `--tail` | `-n` | `100` | Number of lines to show from end of logs |");
        sb.AppendLine();
        sb.AppendLine("**Examples:**");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# View last 100 lines from all services");
        sb.AppendLine("ceph-cli logs -d ./cluster");
        sb.AppendLine();
        sb.AppendLine("# View last 50 lines from a specific OSD");
        sb.AppendLine("ceph-cli logs -d ./cluster -s ceph-osd1 -n 50");
        sb.AppendLine();
        sb.AppendLine("# Stream MON logs in real-time");
        sb.AppendLine("ceph-cli logs -d ./cluster -s ceph-mon1 -f");
        sb.AppendLine("```");
        sb.AppendLine();

        // diagnose
        sb.AppendLine("### `ceph-cli diagnose`");
        sb.AppendLine();
        sb.AppendLine("Run 9 automated environment checks and report results.");
        sb.AppendLine();
        sb.AppendLine("| Option | Default | Description |");
        sb.AppendLine("|--------|---------|-------------|");
        sb.AppendLine("| `--json` | `false` | Output as JSON array |");
        sb.AppendLine();
        sb.AppendLine("**Checks performed (in order):**");
        sb.AppendLine();
        sb.AppendLine("| # | Check Name | What It Verifies | Failure Impact |");
        sb.AppendLine("|---|-----------|-------------------|----------------|");
        sb.AppendLine("| 1 | Operating system | Running on Windows | CLI works on other OSes but some checks are skipped |");
        sb.AppendLine("| 2 | WSL2 default version | `wsl --status` shows version 2 | Containers will fail to start |");
        sb.AppendLine("| 3 | Docker Desktop installed | Docker Desktop .exe exists on disk | Cannot run containers |");
        sb.AppendLine("| 4 | Docker daemon reachable | `docker info` succeeds | Cannot run any Docker commands |");
        sb.AppendLine("| 5 | Docker Compose | `docker compose version` succeeds | Cannot orchestrate multi-container cluster |");
        sb.AppendLine("| 6 | WSL2 memory configuration | `~/.wslconfig` has `memory=` setting | OOM kills, unstable cluster |");
        sb.AppendLine("| 7 | Disk space | System drive has >= 10 GB free | Image pull or OSD creation fails |");
        sb.AppendLine("| 8 | Docker network conflict | No existing network on 172.20.x.x | `up` fails with subnet conflict |");
        sb.AppendLine("| 9 | Docker WSL2 backend | Docker info shows WSL2, not Hyper-V | Performance issues, possible failures |");
        sb.AppendLine();
        sb.AppendLine("**JSON output schema:**");
        sb.AppendLine();
        sb.AppendLine("```json");
        sb.AppendLine("[");
        sb.AppendLine("  {");
        sb.AppendLine("    \"name\": \"Check name\",");
        sb.AppendLine("    \"passed\": true,");
        sb.AppendLine("    \"message\": \"Human-readable result\",");
        sb.AppendLine("    \"remediationHint\": \"How to fix (null if passed)\"");
        sb.AppendLine("  }");
        sb.AppendLine("]");
        sb.AppendLine("```");
        sb.AppendLine();

        // fix
        sb.AppendLine("### `ceph-cli fix`");
        sb.AppendLine();
        sb.AppendLine("Automatically remediate detected environment issues.");
        sb.AppendLine();
        sb.AppendLine("| Option | Default | Description |");
        sb.AppendLine("|--------|---------|-------------|");
        sb.AppendLine("| `--dry-run` | `false` | Preview fixes without applying them |");
        sb.AppendLine("| `--wsl-memory` | `6` | WSL2 memory limit in GB for `~/.wslconfig` |");
        sb.AppendLine();
        sb.AppendLine("**Fixes applied (only if the corresponding diagnose check fails):**");
        sb.AppendLine();
        sb.AppendLine("| Issue | Fix Applied | Side Effects |");
        sb.AppendLine("|-------|------------|--------------|");
        sb.AppendLine("| WSL2 not default | Runs `wsl --set-default-version 2` | None |");
        sb.AppendLine("| Missing memory config | Writes `memory=6GB` to `~/.wslconfig` | Requires `wsl --shutdown` to take effect |");
        sb.AppendLine("| Docker not running | Launches `Docker Desktop.exe` | ~30 second startup time |");
        sb.AppendLine("| Network conflict | Runs `docker network rm <name>` | Removes the conflicting network |");
        sb.AppendLine("| Low disk space | (informational only) | Cannot auto-fix |");
        sb.AppendLine("| Hyper-V backend | (informational only) | User must toggle in Docker Desktop settings |");
        sb.AppendLine();

        // generate-guide
        sb.AppendLine("### `ceph-cli generate-guide`");
        sb.AppendLine();
        sb.AppendLine("Generate this comprehensive markdown guide file.");
        sb.AppendLine();
        sb.AppendLine("| Option | Alias | Default | Description |");
        sb.AppendLine("|--------|-------|---------|-------------|");
        sb.AppendLine("| `--output` | `-o` | `.` (current dir) | Output directory |");
        sb.AppendLine("| `--filename` | `-f` | `ceph-cli-guide.md` | Output file name |");
        sb.AppendLine();

        // install-skill
        sb.AppendLine("### `ceph-cli install-skill`");
        sb.AppendLine();
        sb.AppendLine("Generate a concise CLAUDE.md file for AI coding agents.");
        sb.AppendLine();
        sb.AppendLine("| Option | Alias | Default | Description |");
        sb.AppendLine("|--------|-------|---------|-------------|");
        sb.AppendLine("| `--output` | `-o` | `.` (current dir) | Output directory for CLAUDE.md |");
        sb.AppendLine();
    }

    private static void WriteGeneratedFiles(StringBuilder sb)
    {
        sb.AppendLine("## Generated Files Reference");
        sb.AppendLine();
        sb.AppendLine("When you run `ceph-cli init`, these 6 files are created:");
        sb.AppendLine();
        sb.AppendLine("| File | Purpose | Mounted Into Containers |");
        sb.AppendLine("|------|---------|------------------------|");
        sb.AppendLine("| `docker-compose.yml` | Defines all services, networks, volumes | N/A (used by Docker Compose) |");
        sb.AppendLine("| `ceph.conf` | Ceph cluster configuration (FSID, MON IPs, auth settings) | Yes, as `/ceph.conf.seed:ro` |");
        sb.AppendLine("| `entrypoint.sh` | Bootstrap script for each daemon type | Yes, as `/entrypoint.sh:ro` |");
        sb.AppendLine("| `.env` | Shared environment variables (demo keys) | Yes, via `env_file` |");
        sb.AppendLine("| `wslconfig.recommended` | Suggested `~/.wslconfig` for WSL2 | No |");
        sb.AppendLine("| `README.md` | Quick start guide for the generated cluster | No |");
        sb.AppendLine();
        sb.AppendLine("**Important**: `ceph.conf` and `entrypoint.sh` are written with Unix LF line endings.");
        sb.AppendLine("Windows CRLF line endings will break the container scripts. Do not edit these files");
        sb.AppendLine("with editors that convert line endings to CRLF.");
        sb.AppendLine();
        sb.AppendLine("**FSID**: Each `ceph-cli init` generates a new random FSID (cluster UUID) in `ceph.conf`.");
        sb.AppendLine("If you re-run `init` after the cluster has data, you must also run `down --volumes` first,");
        sb.AppendLine("because the stored data references the old FSID.");
        sb.AppendLine();
    }

    private static void WriteWorkflows(StringBuilder sb)
    {
        sb.AppendLine("## Workflows");
        sb.AppendLine();

        sb.AppendLine("### First-Time Setup");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# Step 1: Check prerequisites");
        sb.AppendLine("ceph-cli diagnose");
        sb.AppendLine();
        sb.AppendLine("# Step 2: Fix any failures");
        sb.AppendLine("ceph-cli fix");
        sb.AppendLine();
        sb.AppendLine("# Step 3: Generate cluster files");
        sb.AppendLine("ceph-cli init --output C:\\ceph-cluster");
        sb.AppendLine();
        sb.AppendLine("# Step 4: Start the cluster");
        sb.AppendLine("ceph-cli up --dir C:\\ceph-cluster");
        sb.AppendLine();
        sb.AppendLine("# Step 5: Wait ~60 seconds, then verify");
        sb.AppendLine("ceph-cli status --dir C:\\ceph-cluster");
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("### Stop and Resume (Preserving Data)");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# Stop cluster (data volumes persist)");
        sb.AppendLine("ceph-cli down --dir C:\\ceph-cluster");
        sb.AppendLine();
        sb.AppendLine("# Later, resume with existing data");
        sb.AppendLine("ceph-cli up --dir C:\\ceph-cluster");
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("### Full Reset (Destroy and Recreate)");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# Destroy everything");
        sb.AppendLine("ceph-cli down --dir C:\\ceph-cluster --volumes");
        sb.AppendLine();
        sb.AppendLine("# Regenerate files (new FSID)");
        sb.AppendLine("ceph-cli init --output C:\\ceph-cluster");
        sb.AppendLine();
        sb.AppendLine("# Start fresh");
        sb.AppendLine("ceph-cli up --dir C:\\ceph-cluster");
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("### Change Cluster Topology");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# Must destroy existing cluster first (different FSID)");
        sb.AppendLine("ceph-cli down --dir C:\\ceph-cluster --volumes");
        sb.AppendLine();
        sb.AppendLine("# Regenerate with new options");
        sb.AppendLine("ceph-cli init --output C:\\ceph-cluster -m 3 -s 5 --rgw");
        sb.AppendLine();
        sb.AppendLine("# Start new cluster");
        sb.AppendLine("ceph-cli up --dir C:\\ceph-cluster");
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("### Investigate a Problem");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# 1. Check environment first");
        sb.AppendLine("ceph-cli diagnose");
        sb.AppendLine();
        sb.AppendLine("# 2. Check container and cluster status");
        sb.AppendLine("ceph-cli status --dir C:\\ceph-cluster");
        sb.AppendLine();
        sb.AppendLine("# 3. Check logs for the failing service");
        sb.AppendLine("ceph-cli logs --dir C:\\ceph-cluster -s ceph-osd1 -n 200");
        sb.AppendLine();
        sb.AppendLine("# 4. If environment issue, fix it");
        sb.AppendLine("ceph-cli fix");
        sb.AppendLine();
        sb.AppendLine("# 5. If data corruption, reset");
        sb.AppendLine("ceph-cli down --dir C:\\ceph-cluster --volumes");
        sb.AppendLine("ceph-cli init --output C:\\ceph-cluster");
        sb.AppendLine("ceph-cli up --dir C:\\ceph-cluster");
        sb.AppendLine("```");
        sb.AppendLine();
    }

    private static void WriteTroubleshooting(StringBuilder sb)
    {
        sb.AppendLine("## Troubleshooting");
        sb.AppendLine();
        sb.AppendLine("### Symptom → Cause → Fix Table");
        sb.AppendLine();
        sb.AppendLine("| Symptom | Likely Cause | Fix |");
        sb.AppendLine("|---------|-------------|-----|");
        sb.AppendLine("| `docker-compose.yml not found` | Wrong `--dir` path or `init` not run | Run `ceph-cli init -o <dir>` first |");
        sb.AppendLine("| `docker compose up failed` | Docker not running | Run `ceph-cli fix` or start Docker Desktop manually |");
        sb.AppendLine("| MON container restarts | ceph.conf has wrong FSID for existing data | Run `down --volumes`, then `init` + `up` |");
        sb.AppendLine("| OSD containers restart (exit 139) | ceph-osd binary segfaults (ARM64 + v18) | Use `--image quay.io/ceph/ceph:v17` with `init` |");
        sb.AppendLine("| OSD containers restart (exit 1) | MON not ready yet or keyring mismatch | Wait 60s; if persists, reset with `down --volumes` |");
        sb.AppendLine("| MGR never starts | MON healthcheck never passes | Check MON logs: `ceph-cli logs -s ceph-mon1` |");
        sb.AppendLine("| `status` says \"Could not reach ceph-mon1\" | Cluster still bootstrapping | Wait 60s after `up` and retry |");
        sb.AppendLine("| `status` says \"Could not reach ceph-mon1\" (after 60s) | MON crashed or network issue | Check `ceph-cli logs -s ceph-mon1` |");
        sb.AppendLine("| HEALTH_WARN: insecure global_id | Normal for dev clusters | Safe to ignore, or run: `docker exec ceph-mon1 ceph config set mon auth_allow_insecure_global_id_reclaim false` |");
        sb.AppendLine("| HEALTH_WARN: msgr2 not enabled | Single MON without msgr2 | Safe to ignore, or run: `docker exec ceph-mon1 ceph mon enable-msgr2` |");
        sb.AppendLine("| HEALTH_WARN: OSD count < 3 | Fewer than 3 OSDs deployed | Reinit with `-s 3` or more |");
        sb.AppendLine("| All OSDs show `0 up` | OSDs registered but crashed | Check OSD logs: `ceph-cli logs -s ceph-osd1 -n 200` |");
        sb.AppendLine("| Network conflict on 172.20.x.x | Another compose project used this subnet | Run `ceph-cli fix` or `docker network rm <name>` |");
        sb.AppendLine("| `Cannot connect to the Docker daemon` | Docker Desktop not running | Start Docker Desktop; run `ceph-cli fix` |");
        sb.AppendLine("| Image pull hangs or times out | Slow network or image not available for arch | Check `docker pull quay.io/ceph/ceph:v17` manually |");
        sb.AppendLine("| Containers OOMKilled | WSL2 memory limit too low | Run `ceph-cli fix --wsl-memory 8`, then `wsl --shutdown` |");
        sb.AppendLine("| Volume permission errors | OSD not running as privileged | Verify `privileged: true` in docker-compose.yml (set by `init`) |");
        sb.AppendLine("| `entrypoint.sh: line N: \\r: command not found` | File has CRLF line endings | Re-run `ceph-cli init` (generates LF); don't edit with Notepad |");
        sb.AppendLine("| Cluster data corrupted after re-init | New FSID doesn't match old volumes | Always run `down --volumes` before `init` if changing config |");
        sb.AppendLine();

        sb.AppendLine("### ARM64 / Apple Silicon / Snapdragon Specific Issues");
        sb.AppendLine();
        sb.AppendLine("The `quay.io/ceph/ceph:v18` (Reef) image has a known issue where the `ceph-osd` binary");
        sb.AppendLine("segfaults on ARM64 platforms (exit code 139). The `ceph-mon` and `ceph-mgr` binaries");
        sb.AppendLine("work fine in v18. **Use `quay.io/ceph/ceph:v17` (Quincy) on ARM64 systems.**");
        sb.AppendLine();
        sb.AppendLine("To check your architecture:");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("docker info --format '{{.Architecture}}'");
        sb.AppendLine("# If output is \"aarch64\" or \"arm64\", use v17");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("To verify the OSD binary works in a given image:");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("docker run --rm quay.io/ceph/ceph:v17 ceph-osd --version");
        sb.AppendLine("# Should print version without crashing");
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("### Entrypoint Bootstrap Stages");
        sb.AppendLine();
        sb.AppendLine("The `entrypoint.sh` script bootstraps each daemon. Understanding its stages helps");
        sb.AppendLine("interpret log output:");
        sb.AppendLine();
        sb.AppendLine("**MON bootstrap:**");
        sb.AppendLine();
        sb.AppendLine("1. Copy `/ceph.conf.seed` → `/etc/ceph/ceph.conf` (first run only)");
        sb.AppendLine("2. Create admin keyring (`/etc/ceph/ceph.client.admin.keyring`)");
        sb.AppendLine("3. Create MON keyring, import admin key");
        sb.AppendLine("4. Create bootstrap keyrings for OSD, MGR, MDS, RGW");
        sb.AppendLine("5. Create monmap with `monmaptool`");
        sb.AppendLine("6. Run `ceph-mon --mkfs` to initialize MON data store");
        sb.AppendLine("7. Start MON daemon: `ceph-mon -f`");
        sb.AppendLine();
        sb.AppendLine("**OSD bootstrap:**");
        sb.AppendLine();
        sb.AppendLine("1. Wait for MON to be ready (`ceph -s` succeeds)");
        sb.AppendLine("2. Register new OSD with `ceph osd new`");
        sb.AppendLine("3. Create file-backed block device for bluestore (5 GB sparse file)");
        sb.AppendLine("4. Create OSD keyring via `ceph auth get-or-create`");
        sb.AppendLine("5. Run `ceph-osd --mkfs` to initialize bluestore");
        sb.AppendLine("6. Add OSD to CRUSH map");
        sb.AppendLine("7. Start OSD daemon: `ceph-osd -f`");
        sb.AppendLine();
        sb.AppendLine("**MGR/RGW/MDS bootstrap:**");
        sb.AppendLine();
        sb.AppendLine("1. Wait for MON to be ready");
        sb.AppendLine("2. Create keyring via `ceph auth get-or-create`");
        sb.AppendLine("3. Start daemon in foreground");
        sb.AppendLine();
    }

    private static void WriteEnvironmentChecks(StringBuilder sb)
    {
        sb.AppendLine("## Environment Requirements");
        sb.AppendLine();
        sb.AppendLine("| Requirement | Minimum | How to Check | How to Fix |");
        sb.AppendLine("|-------------|---------|-------------|------------|");
        sb.AppendLine("| Windows | 10/11 21H2+ | `winver` | Upgrade Windows |");
        sb.AppendLine("| .NET SDK | 8.0 | `dotnet --version` | Install from https://dot.net |");
        sb.AppendLine("| WSL2 | Version 2 default | `wsl --status` | `wsl --set-default-version 2` |");
        sb.AppendLine("| Docker Desktop | 4.x | `docker --version` | Install from https://docker.com |");
        sb.AppendLine("| Docker Compose | v2 plugin | `docker compose version` | Bundled with Docker Desktop |");
        sb.AppendLine("| Docker backend | WSL2 (not Hyper-V) | `docker info` | Docker Desktop Settings → General → WSL 2 engine |");
        sb.AppendLine("| WSL2 memory | >= 4 GB configured | Check `~/.wslconfig` | `ceph-cli fix --wsl-memory 6` |");
        sb.AppendLine("| Free disk | >= 10 GB | `Get-PSDrive C` | Free up space |");
        sb.AppendLine("| Network | 172.20.0.0/16 available | `docker network ls` | Remove conflicting network |");
        sb.AppendLine();
    }

    private static void WriteCephHealthReference(StringBuilder sb)
    {
        sb.AppendLine("## Ceph Health Status Reference");
        sb.AppendLine();
        sb.AppendLine("When you run `ceph-cli status`, the Ceph Cluster Health section shows one of:");
        sb.AppendLine();
        sb.AppendLine("### HEALTH_OK");
        sb.AppendLine();
        sb.AppendLine("Everything is working correctly. All OSDs are up, all PGs are active+clean.");
        sb.AppendLine();
        sb.AppendLine("### HEALTH_WARN");
        sb.AppendLine();
        sb.AppendLine("The cluster is functional but has warnings. Common warnings and what to do:");
        sb.AppendLine();
        sb.AppendLine("| Warning Message | Severity | Action |");
        sb.AppendLine("|----------------|----------|--------|");
        sb.AppendLine("| `mon is allowing insecure global_id reclaim` | Low | Safe to ignore in dev. Fix: `docker exec ceph-mon1 ceph config set mon auth_allow_insecure_global_id_reclaim false` |");
        sb.AppendLine("| `1 monitors have not enabled msgr2` | Low | Safe to ignore. Fix: `docker exec ceph-mon1 ceph mon enable-msgr2` |");
        sb.AppendLine("| `N osds: M up, K in` where M < N | Medium | Some OSDs crashed. Check OSD logs. May need reset. |");
        sb.AppendLine("| `pgs unknown` or `pgs not active` | Medium | Normal during first 2 minutes of bootstrap. If persists, check OSDs. |");
        sb.AppendLine("| `too few PGs per OSD` | Low | Normal for dev clusters with few pools. |");
        sb.AppendLine("| `pool X has no application enabled` | Low | Run: `docker exec ceph-mon1 ceph osd pool application enable <pool> <app>` |");
        sb.AppendLine("| `OSD near full` | High | Free disk space or add more OSDs. |");
        sb.AppendLine();
        sb.AppendLine("### HEALTH_ERR");
        sb.AppendLine();
        sb.AppendLine("The cluster has critical issues. Data may be at risk.");
        sb.AppendLine();
        sb.AppendLine("| Error Message | Action |");
        sb.AppendLine("|--------------|--------|");
        sb.AppendLine("| `no active mgr` | MGR container crashed. Check logs: `ceph-cli logs -s ceph-mgr1` |");
        sb.AppendLine("| `N osds: 0 up` | All OSDs are down. Check OSD logs. Likely needs full reset. |");
        sb.AppendLine("| `OSD full` | Cluster cannot write. Free space urgently. |");
        sb.AppendLine();
        sb.AppendLine("### Understanding Service Counts");
        sb.AppendLine();
        sb.AppendLine("The status output shows service counts like `osd: 3 osds: 3 up (since 5m), 3 in`.");
        sb.AppendLine();
        sb.AppendLine("- **N osds**: Total OSDs registered in the cluster map");
        sb.AppendLine("- **M up**: Currently running and reachable");
        sb.AppendLine("- **K in**: Marked as part of the data placement (can store data)");
        sb.AppendLine();
        sb.AppendLine("Healthy: N = M = K (all registered, all up, all in).");
        sb.AppendLine("Problem: M < N means some OSDs are down. K < N means some OSDs were removed from placement.");
        sb.AppendLine();
    }

    private static void WriteDockerReference(StringBuilder sb)
    {
        sb.AppendLine("## Direct Docker Commands");
        sb.AppendLine();
        sb.AppendLine("These Docker commands can be used alongside ceph-cli for deeper inspection:");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("# Check container status");
        sb.AppendLine("docker ps -a --filter name=ceph");
        sb.AppendLine();
        sb.AppendLine("# View logs for a specific container");
        sb.AppendLine("docker logs ceph-mon1");
        sb.AppendLine("docker logs ceph-osd1 --tail 50");
        sb.AppendLine();
        sb.AppendLine("# Execute commands inside the MON container");
        sb.AppendLine("docker exec ceph-mon1 ceph status");
        sb.AppendLine("docker exec ceph-mon1 ceph health detail");
        sb.AppendLine("docker exec ceph-mon1 ceph osd tree");
        sb.AppendLine("docker exec ceph-mon1 ceph osd pool ls");
        sb.AppendLine("docker exec ceph-mon1 ceph df");
        sb.AppendLine();
        sb.AppendLine("# Check OSD status");
        sb.AppendLine("docker exec ceph-mon1 ceph osd status");
        sb.AppendLine("docker exec ceph-mon1 ceph osd dump");
        sb.AppendLine();
        sb.AppendLine("# Enable msgr2 (fixes warning)");
        sb.AppendLine("docker exec ceph-mon1 ceph mon enable-msgr2");
        sb.AppendLine();
        sb.AppendLine("# Disable insecure global_id reclaim (fixes warning)");
        sb.AppendLine("docker exec ceph-mon1 ceph config set mon auth_allow_insecure_global_id_reclaim false");
        sb.AppendLine();
        sb.AppendLine("# Create a test pool");
        sb.AppendLine("docker exec ceph-mon1 ceph osd pool create testpool 32");
        sb.AppendLine();
        sb.AppendLine("# Check Docker networks");
        sb.AppendLine("docker network ls");
        sb.AppendLine("docker network inspect <network-id>");
        sb.AppendLine();
        sb.AppendLine("# Check Docker volumes");
        sb.AppendLine("docker volume ls --filter name=ceph");
        sb.AppendLine("```");
        sb.AppendLine();
    }

    private static void WriteDecisionTree(StringBuilder sb)
    {
        sb.AppendLine("## Troubleshooting Decision Tree");
        sb.AppendLine();
        sb.AppendLine("Use this decision tree to systematically diagnose any ceph-cli issue:");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("START: What is the problem?");
        sb.AppendLine("│");
        sb.AppendLine("├─ Cannot run ceph-cli at all");
        sb.AppendLine("│  ├─ \"command not found\" → Install: dotnet tool install -g QuinntyneBrown.Ceph.Cli");
        sb.AppendLine("│  └─ .NET errors → Install .NET 8 SDK from https://dot.net");
        sb.AppendLine("│");
        sb.AppendLine("├─ `ceph-cli init` fails");
        sb.AppendLine("│  └─ Permission error → Run as administrator or choose a writable directory");
        sb.AppendLine("│");
        sb.AppendLine("├─ `ceph-cli up` fails");
        sb.AppendLine("│  ├─ \"docker-compose.yml not found\" → Run `ceph-cli init` first");
        sb.AppendLine("│  ├─ \"Cannot connect to Docker\" → Start Docker Desktop, wait 30s");
        sb.AppendLine("│  ├─ Image pull fails → Check internet; try `docker pull quay.io/ceph/ceph:v17`");
        sb.AppendLine("│  └─ Network conflict → Run `ceph-cli fix` or `docker network rm <name>`");
        sb.AppendLine("│");
        sb.AppendLine("├─ Cluster starts but containers crash");
        sb.AppendLine("│  ├─ Check: Which containers are crashing?");
        sb.AppendLine("│  │  ├─ MON crashing → FSID mismatch. Run `down --volumes`, `init`, `up`");
        sb.AppendLine("│  │  ├─ OSD crashing (exit 139) → ARM64 + v18 bug. Use v17 image");
        sb.AppendLine("│  │  ├─ OSD crashing (exit 1) → Check logs: `ceph-cli logs -s ceph-osd1`");
        sb.AppendLine("│  │  │  ├─ \"waiting for MON\" loops → MON not healthy, check MON first");
        sb.AppendLine("│  │  │  └─ Keyring errors → Reset: `down --volumes`, `init`, `up`");
        sb.AppendLine("│  │  └─ MGR crashing → Check logs: `ceph-cli logs -s ceph-mgr1`");
        sb.AppendLine("│  └─ All containers crash → OOM? Run `ceph-cli fix --wsl-memory 8`");
        sb.AppendLine("│");
        sb.AppendLine("├─ `ceph-cli status` shows warnings");
        sb.AppendLine("│  ├─ HEALTH_WARN → Check the specific warning in the Health Reference above");
        sb.AppendLine("│  ├─ HEALTH_ERR → Critical issue, see Health Reference above");
        sb.AppendLine("│  ├─ \"0 up\" OSDs → OSDs crashed. Check OSD logs, likely needs reset");
        sb.AppendLine("│  └─ \"pgs unknown\" → Wait 2 minutes. If persists, check OSDs");
        sb.AppendLine("│");
        sb.AppendLine("├─ `ceph-cli status` cannot reach MON");
        sb.AppendLine("│  ├─ Just started cluster? → Wait 60 seconds and retry");
        sb.AppendLine("│  ├─ MON container running? → Check `docker ps --filter name=ceph-mon1`");
        sb.AppendLine("│  │  ├─ Not running → Check MON logs, likely needs reset");
        sb.AppendLine("│  │  └─ Running → MON may be stuck. Check logs, consider reset");
        sb.AppendLine("│  └─ Docker not running → Start Docker Desktop");
        sb.AppendLine("│");
        sb.AppendLine("└─ `ceph-cli diagnose` shows failures");
        sb.AppendLine("   └─ Run `ceph-cli fix` to auto-remediate what it can");
        sb.AppendLine("      └─ Re-run `ceph-cli diagnose` to verify");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("### The Nuclear Option (Full Reset)");
        sb.AppendLine();
        sb.AppendLine("If nothing else works, a full reset resolves nearly all cluster issues:");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("ceph-cli down --dir <cluster-dir> --volumes");
        sb.AppendLine("ceph-cli init --output <cluster-dir>");
        sb.AppendLine("ceph-cli up --dir <cluster-dir>");
        sb.AppendLine("# Wait 60 seconds");
        sb.AppendLine("ceph-cli status --dir <cluster-dir>");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("This destroys all data and creates a fresh cluster from scratch.");
        sb.AppendLine();
    }
}
