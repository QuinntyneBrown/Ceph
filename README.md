# Ceph

A .NET CLI tool that scaffolds and manages a [Ceph](https://ceph.io/) distributed storage cluster running inside **Docker on Windows** (via WSL2 and Docker Desktop).

## Features

- Generate a fully configured Ceph cluster with a single command
- Customizable topology: configure monitor, OSD, and manager counts
- Optional RADOS Gateway (S3/Swift API) and Metadata Server (CephFS) support
- Built-in environment diagnostics for WSL2, Docker, disk space, and networking
- Auto-remediation of common Windows/Docker configuration issues
- Lifecycle management: start, stop, and monitor your cluster

## Prerequisites

| Requirement | Minimum |
|-------------|---------|
| .NET SDK | 8.0 |
| Windows 10/11 | 21H2 (for WSL2) |
| WSL2 | Default version 2 |
| Docker Desktop | 4.x |

## Build

```powershell
dotnet build
```

## Usage

```powershell
# Generate docker-compose files and config in the current directory
ceph-cli init

# Generate with custom options
ceph-cli init --output C:\ceph-cluster --monitors 3 --osds 5 --rgw --mds

# Start the cluster
ceph-cli up

# Check cluster health
ceph-cli status

# Stop the cluster
ceph-cli down

# Stop and remove persistent data volumes
ceph-cli down --volumes

# Diagnose common Windows/Docker issues
ceph-cli diagnose

# Output diagnostics as JSON
ceph-cli diagnose --json

# Attempt automatic fixes for detected issues
ceph-cli fix

# Preview what fix would do without making changes
ceph-cli fix --dry-run
```

## Commands

| Command | Description |
|---------|-------------|
| `init` | Generates `docker-compose.yml`, `ceph.conf`, `.env`, `entrypoint.sh`, `wslconfig.recommended`, and a `README.md` |
| `up` | Starts the Ceph cluster via `docker compose up -d` |
| `down` | Stops and removes containers; use `--volumes` to also remove persistent data |
| `status` | Shows container status and Ceph cluster health |
| `diagnose` | Checks WSL2, Docker Desktop, Docker daemon, Docker Compose, memory, disk space, and network configuration |
| `fix` | Automatically remediates detected issues (WSL2 default version, `~/.wslconfig`, Docker Desktop start, network conflicts) |

## Init Options

| Option | Default | Description |
|--------|---------|-------------|
| `--output, -o` | `.` | Output directory for generated files |
| `--monitors, -m` | `1` | Number of MON daemons |
| `--osds, -s` | `3` | Number of OSD daemons |
| `--managers` | `1` | Number of MGR daemons |
| `--image` | `quay.io/ceph/ceph:v18` | Ceph container image |
| `--rgw` | `false` | Include RADOS Gateway for S3/Swift API |
| `--mds` | `false` | Include Metadata Server for CephFS |

## Project Structure

```
Ceph/
├── src/
│   └── Ceph.Cli/
│       ├── Program.cs              # Entry point and command registration
│       ├── Commands/               # Command implementations
│       │   ├── InitCommand.cs
│       │   ├── UpCommand.cs
│       │   ├── DownCommand.cs
│       │   ├── StatusCommand.cs
│       │   ├── DiagnoseCommand.cs
│       │   └── FixCommand.cs
│       └── Services/               # Business logic
│           ├── DockerComposeGenerator.cs
│           ├── EnvironmentChecker.cs
│           └── IssueFixer.cs
└── tests/
    └── Ceph.Cli.Tests/            # xUnit tests
        ├── DockerComposeGeneratorTests.cs
        └── EnvironmentCheckerTests.cs
```

## Run Tests

```powershell
dotnet test
```

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on how to contribute to this project.

## License

This project is licensed under the [MIT License](LICENSE).
