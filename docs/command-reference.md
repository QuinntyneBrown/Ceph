# Command Reference

Complete reference for all `ceph-cli` commands, options, and behaviors.

## Global Options

These options are available on all commands:

| Option | Description |
|--------|-------------|
| `--version` | Show version information |
| `-?, -h, --help` | Show help and usage information |

---

## `init`

Generate docker-compose files and configuration for running a Ceph cluster in Docker on Windows.

### Synopsis

```
ceph-cli init [options]
```

### Options

| Option | Alias | Type | Default | Description |
|--------|-------|------|---------|-------------|
| `--output` | `-o` | string | Current directory | Directory where files will be generated |
| `--monitors` | `-m` | int | `1` | Number of MON daemons |
| `--osds` | `-s` | int | `3` | Number of OSD daemons |
| `--managers` | | int | `1` | Number of MGR daemons |
| `--image` | | string | `quay.io/ceph/ceph:v18` | Ceph container image to use |
| `--rgw` | | bool | `false` | Include a RADOS Gateway (S3/Swift API) service |
| `--mds` | | bool | `false` | Include a Metadata Server (CephFS) service |

### Generated Files

| File | Description |
|------|-------------|
| `docker-compose.yml` | Docker Compose service definitions, volumes, and network |
| `ceph.conf` | Ceph cluster configuration (FSID, monitors, auth, pool defaults) |
| `entrypoint.sh` | Bootstrap script that initializes each daemon type on first start |
| `.env` | Shared environment variables (demo credentials, network auto-detect) |
| `wslconfig.recommended` | Suggested `~/.wslconfig` settings for WSL2 |
| `README.md` | Quick-reference documentation for the generated cluster |

### Examples

```powershell
# Default cluster: 1 MON, 1 MGR, 3 OSDs
ceph-cli init

# Specify output directory
ceph-cli init --output C:\my-ceph-cluster

# High-availability cluster with all optional services
ceph-cli init -o C:\ceph -m 3 -s 5 --managers 2 --rgw --mds

# Use a specific Ceph version
ceph-cli init --image quay.io/ceph/ceph:v17
```

### Notes

- Running `init` into an existing directory will overwrite existing files.
- A new random FSID (cluster UUID) is generated each time.
- All container-mounted files are written with Unix LF line endings.
- `osd_pool_default_size` is set to `min(osd_count, 3)` automatically.

---

## `up`

Start the Ceph cluster using Docker Compose.

### Synopsis

```
ceph-cli up [options]
```

### Options

| Option | Alias | Type | Default | Description |
|--------|-------|------|---------|-------------|
| `--dir` | `-d` | string | Current directory | Directory containing the generated `docker-compose.yml` |

### Behavior

1. Validates that `docker-compose.yml` exists in the specified directory
2. Runs `docker compose up -d` with the compose file
3. On success, prompts to run `ceph-cli status` after ~60 seconds
4. On failure, suggests running `ceph-cli diagnose`

### Startup Order

The MON container starts first with a healthcheck. All other services use `depends_on` with `condition: service_healthy` and will not start until the MON healthcheck passes. The first startup takes approximately 60 seconds for the MON to bootstrap.

### Examples

```powershell
# Start from the current directory
ceph-cli up

# Start from a specific directory
ceph-cli up --dir C:\ceph-cluster
```

---

## `down`

Stop the Ceph cluster and remove containers.

### Synopsis

```
ceph-cli down [options]
```

### Options

| Option | Alias | Type | Default | Description |
|--------|-------|------|---------|-------------|
| `--dir` | `-d` | string | Current directory | Directory containing the generated `docker-compose.yml` |
| `--volumes` | | bool | `false` | Also remove persistent data volumes (destroys all cluster data) |

### Behavior

- Without `--volumes`: containers and network are removed but named volumes are preserved. Running `up` again will resume the existing cluster with all data intact.
- With `--volumes`: containers, network, and all named volumes are removed. All Ceph data is permanently destroyed.

### Examples

```powershell
# Stop cluster, preserve data
ceph-cli down --dir C:\ceph-cluster

# Stop cluster and destroy all data
ceph-cli down --dir C:\ceph-cluster --volumes
```

---

## `status`

Show container status and Ceph cluster health.

### Synopsis

```
ceph-cli status [options]
```

### Options

| Option | Alias | Type | Default | Description |
|--------|-------|------|---------|-------------|
| `--dir` | `-d` | string | Current directory | Directory containing the generated `docker-compose.yml` |

### Output

The command displays two sections:

1. **Container Status** -- output of `docker compose ps` showing each container's name, image, state, and ports
2. **Ceph Cluster Health** -- output of `docker exec ceph-mon1 ceph status` showing cluster ID, health, services, and data usage

### Examples

```powershell
ceph-cli status --dir C:\ceph-cluster
```

Sample output:

```
=== Container Status ===
NAME        IMAGE                   COMMAND            SERVICE     STATUS
ceph-mon1   quay.io/ceph/ceph:v18   "/entrypoint.sh"   ceph-mon1   Up 2 minutes (healthy)
ceph-mgr1   quay.io/ceph/ceph:v18   "/entrypoint.sh"   ceph-mgr1   Up 90 seconds
ceph-osd1   quay.io/ceph/ceph:v18   "/entrypoint.sh"   ceph-osd1   Up 90 seconds

=== Ceph Cluster Health ===
  cluster:
    id:     50e64de6-21b4-42cb-9a56-c034ea1454b4
    health: HEALTH_OK

  services:
    mon: 1 daemons, quorum ceph-mon1
    mgr: ceph-mgr1(active)
    osd: 3 osds: 3 up, 3 in

  data:
    pools:   1 pools, 1 pgs
    objects: 0 objects, 0 B
    usage:   1.2 GiB used, 299 GiB / 300 GiB avail
```

---

## `diagnose`

Run environment checks and report any issues found in the Windows/Docker setup.

### Synopsis

```
ceph-cli diagnose [options]
```

### Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `--json` | bool | `false` | Output results as JSON |

### Checks Performed

The command runs 9 checks in order:

| # | Check | What it verifies | Remediation |
|---|-------|-----------------|-------------|
| 1 | **Operating system** | Running on Windows | Informational only |
| 2 | **WSL2 default version** | `wsl --status` reports "Default Version: 2" | `wsl --set-default-version 2` |
| 3 | **Docker Desktop installed** | `Docker Desktop.exe` exists in Program Files | Install from docker.com |
| 4 | **Docker daemon reachable** | `docker info` succeeds | Start Docker Desktop |
| 5 | **Docker Compose** | `docker compose version` (v2 plugin) or `docker-compose --version` (legacy) | Install Docker Desktop |
| 6 | **WSL2 memory configuration** | `~/.wslconfig` exists and contains `memory=` | Create/update `.wslconfig` |
| 7 | **Disk space** | System drive has at least 10 GB free | Free disk space |
| 8 | **Docker network conflict** | No Docker network uses the `172.20.x.x` subnet | Remove conflicting network |
| 9 | **Docker WSL2 backend** | `docker info` indicates WSL2/Linux (not Hyper-V) | Switch in Docker Desktop settings |

### Console Output

Each check displays with a colored icon:
- Green checkmark for passed checks
- Red X for failed checks with a yellow remediation hint

### JSON Output

With `--json`, results are printed as a JSON array:

```json
[
  {
    "name": "Operating system",
    "passed": true,
    "message": "Running on Windows - OK.",
    "remediationHint": null
  },
  ...
]
```

### Examples

```powershell
# Interactive console output
ceph-cli diagnose

# Machine-readable JSON
ceph-cli diagnose --json

# Pipe JSON to a file
ceph-cli diagnose --json > diagnostics.json
```

---

## `fix`

Attempt to automatically remediate common Windows/Docker issues detected by `diagnose`.

### Synopsis

```
ceph-cli fix [options]
```

### Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `--dry-run` | bool | `false` | Show what would be fixed without making any changes |
| `--wsl-memory` | int | `6` | WSL2 memory limit in GB to configure in `~/.wslconfig` |

### Fix Operations

The command first runs all diagnostic checks, then applies fixes only for failed checks:

| Issue | Fix applied | Details |
|-------|------------|---------|
| WSL2 not default version | `wsl --set-default-version 2` | Requires administrator privileges |
| WSL2 memory not configured | Creates/updates `~/.wslconfig` | Adds `memory`, `swap`, `processors` under `[wsl2]` section |
| Docker daemon not reachable | Launches Docker Desktop | Starts `Docker Desktop.exe` via shell execute |
| Docker network conflict | `docker network rm <name>` | Removes networks using `172.20.x.x` subnet |

### Informational-Only Issues

Some detected issues cannot be auto-fixed and display a warning instead:

- **Disk space** -- user must free space manually
- **Docker WSL2 backend** -- user must toggle in Docker Desktop settings

### Examples

```powershell
# Preview fixes
ceph-cli fix --dry-run

# Apply fixes with default settings
ceph-cli fix

# Apply fixes with 8 GB WSL2 memory
ceph-cli fix --wsl-memory 8
```

### Post-Fix Steps

After running `fix`, you may need to:

1. Restart WSL: `wsl --shutdown`
2. Restart Docker Desktop
3. Run `ceph-cli diagnose` to verify all checks pass

---

## Workflow Examples

### Full setup from scratch

```powershell
# Install the CLI
dotnet tool install -g QuinntyneBrown.Ceph.Cli

# Check and fix environment
ceph-cli diagnose
ceph-cli fix
wsl --shutdown

# Generate and start cluster
ceph-cli init --output C:\ceph
ceph-cli up --dir C:\ceph

# Wait ~60 seconds, then check
ceph-cli status --dir C:\ceph
```

### Reset a broken cluster

```powershell
ceph-cli down --dir C:\ceph --volumes
ceph-cli init --output C:\ceph
ceph-cli up --dir C:\ceph
```

### Scale up with S3 API

```powershell
ceph-cli down --dir C:\ceph --volumes
ceph-cli init --output C:\ceph -m 3 -s 5 --rgw
ceph-cli up --dir C:\ceph
```

### CI/CD diagnostic check

```powershell
ceph-cli diagnose --json | ConvertFrom-Json | Where-Object { -not $_.passed }
```
