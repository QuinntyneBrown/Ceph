# Ceph

A .NET CLI tool that scaffolds and manages a [Ceph](https://ceph.io/) distributed storage cluster running inside **Docker on Windows** (via WSL2 and Docker Desktop).

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
| `init`  | Generates `docker-compose.yml`, `ceph.conf`, `.env`, `entrypoint.sh`, `wslconfig.recommended`, and a `README.md` |
| `diagnose` | Checks WSL2, Docker Desktop, Docker daemon, Docker Compose, and memory configuration |
| `fix`   | Automatically remediates detected issues (WSL2 default version, `~/.wslconfig`, Docker Desktop start) |

## Run Tests

```powershell
dotnet test
```
