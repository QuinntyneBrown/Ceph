# Ceph CLI

A .NET CLI tool that scaffolds and manages a [Ceph](https://ceph.io/) distributed storage cluster running inside **Docker on Windows** (via WSL2 and Docker Desktop).

## Features

- **One-command cluster setup** -- generate a fully configured, multi-container Ceph cluster
- **Customizable topology** -- configure monitor, OSD, and manager counts
- **Optional services** -- RADOS Gateway (S3/Swift API) and Metadata Server (CephFS)
- **Full lifecycle management** -- init, start, stop, and monitor your cluster
- **Environment diagnostics** -- 9 automated checks for WSL2, Docker, disk, and networking
- **Auto-remediation** -- fix common Windows/Docker issues with a single command

## Installation

```powershell
dotnet tool install -g QuinntyneBrown.Ceph.Cli
```

This installs the `ceph-cli` command globally.

## Quick Start

```powershell
# 1. Check your environment
ceph-cli diagnose

# 2. Auto-fix any issues
ceph-cli fix

# 3. Generate cluster files
ceph-cli init --output C:\ceph-cluster

# 4. Start the cluster
ceph-cli up --dir C:\ceph-cluster

# 5. Check cluster health (wait ~60 s for bootstrap)
ceph-cli status --dir C:\ceph-cluster
```

## Prerequisites

| Requirement      | Minimum              |
|------------------|----------------------|
| .NET SDK         | 8.0                  |
| Windows          | 10/11 21H2 or later  |
| WSL2             | Default version 2    |
| Docker Desktop   | 4.x (WSL2 backend)   |
| Free disk space  | 10 GB                |

## Commands

| Command    | Description |
|------------|-------------|
| `init`     | Generate `docker-compose.yml`, `ceph.conf`, `.env`, `entrypoint.sh`, and supporting files |
| `up`       | Start the Ceph cluster via `docker compose up -d` |
| `down`     | Stop and remove containers; `--volumes` removes persistent data |
| `status`   | Show container status and Ceph cluster health |
| `diagnose` | Run 9 environment checks (WSL2, Docker, disk, network, backend) |
| `fix`      | Auto-remediate detected issues (WSL2 version, memory config, Docker, network conflicts) |

## Command Reference

### `init`

Generate all files needed to run a Ceph cluster in Docker.

```powershell
ceph-cli init [options]
```

| Option           | Default                      | Description |
|------------------|------------------------------|-------------|
| `--output, -o`   | `.` (current directory)      | Output directory for generated files |
| `--monitors, -m` | `1`                          | Number of MON daemons |
| `--osds, -s`     | `3`                          | Number of OSD daemons |
| `--managers`      | `1`                          | Number of MGR daemons |
| `--image`         | `quay.io/ceph/ceph:v18`     | Ceph container image |
| `--rgw`           | `false`                      | Include RADOS Gateway (S3/Swift API) |
| `--mds`           | `false`                      | Include Metadata Server (CephFS) |

### `up`

```powershell
ceph-cli up [--dir <path>]
```

### `down`

```powershell
ceph-cli down [--dir <path>] [--volumes]
```

### `status`

```powershell
ceph-cli status [--dir <path>]
```

### `diagnose`

```powershell
ceph-cli diagnose [--json]
```

### `fix`

```powershell
ceph-cli fix [--dry-run] [--wsl-memory <GB>]
```

| Option           | Default | Description |
|------------------|---------|-------------|
| `--dry-run`      | `false` | Preview fixes without applying |
| `--wsl-memory`   | `6`     | WSL2 memory limit in GB for `~/.wslconfig` |

## Documentation

| Guide | Description |
|-------|-------------|
| [Getting Started](docs/getting-started.md) | Step-by-step setup from scratch |
| [Rancher Desktop](docs/rancher-desktop.md) | Using Rancher Desktop instead of Docker Desktop |
| [Architecture](docs/architecture.md) | How the cluster, networking, and bootstrap work |
| [Troubleshooting](docs/troubleshooting.md) | Common issues and how to resolve them |
| [Command Reference](docs/command-reference.md) | Detailed reference for every command and option |

## Project Structure

```
Ceph/
├── src/
│   └── Ceph.Cli/
│       ├── Program.cs                    # Entry point
│       ├── Commands/
│       │   ├── InitCommand.cs            # Scaffold cluster files
│       │   ├── UpCommand.cs              # Start cluster
│       │   ├── DownCommand.cs            # Stop cluster
│       │   ├── StatusCommand.cs          # Check health
│       │   ├── DiagnoseCommand.cs        # Environment checks
│       │   └── FixCommand.cs             # Auto-remediation
│       └── Services/
│           ├── DockerComposeGenerator.cs  # File generation
│           ├── EnvironmentChecker.cs      # 9 diagnostic checks
│           └── IssueFixer.cs              # Automated fixes
├── tests/
│   └── Ceph.Cli.Tests/
│       ├── DockerComposeGeneratorTests.cs
│       └── EnvironmentCheckerTests.cs
└── docs/
    ├── getting-started.md
    ├── architecture.md
    ├── troubleshooting.md
    └── command-reference.md
```

## Run Tests

```powershell
dotnet test
```

## License

This project is licensed under the [MIT License](LICENSE).
