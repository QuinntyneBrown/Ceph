# Getting Started

This guide walks you through setting up a Ceph distributed storage cluster on Windows using Docker Desktop and WSL2, from initial prerequisites to a healthy running cluster.

## 1. Prerequisites

Before you begin, make sure you have the following installed:

| Requirement      | Minimum              | How to check |
|------------------|----------------------|--------------|
| .NET SDK         | 8.0                  | `dotnet --version` |
| Windows          | 10/11 21H2 or later  | `winver` |
| WSL2             | Default version 2    | `wsl --status` |
| Docker Desktop   | 4.x (WSL2 backend)   | `docker --version` |
| Free disk space  | 10 GB                | Check in File Explorer |

### Install .NET SDK

Download from [dot.net](https://dot.net/download) or use winget:

```powershell
winget install Microsoft.DotNet.SDK.8
```

### Install WSL2

```powershell
wsl --install
wsl --set-default-version 2
```

### Install Docker Desktop

Download from [docker.com](https://www.docker.com/products/docker-desktop/) and make sure **"Use the WSL 2 based engine"** is enabled in Settings > General.

## 2. Build the CLI

Clone the repository and build:

```powershell
git clone https://github.com/QuinntyneBrown/Ceph.git
cd Ceph
dotnet build
```

## 3. Check Your Environment

Run the built-in diagnostics to verify everything is configured correctly:

```powershell
dotnet run --project src/Ceph.Cli -- diagnose
```

You should see output like:

```
=== Ceph Environment Diagnostics ===

  [green]OK[/green]  Operating system
     Running on Windows - OK.

  [green]OK[/green]  WSL2 default version
     WSL2 is set as the default version - OK.

  [green]OK[/green]  Docker Desktop installed
     Docker Desktop installation found - OK.

  ...

Results: 9 passed, 0 failed
```

If any checks fail, proceed to step 4.

## 4. Fix Detected Issues

The CLI can automatically fix most issues it detects:

```powershell
# Preview what will be changed
dotnet run --project src/Ceph.Cli -- fix --dry-run

# Apply fixes
dotnet run --project src/Ceph.Cli -- fix
```

After fixing, run `diagnose` again to confirm all 9 checks pass.

**What `fix` can auto-remediate:**

- Set WSL2 as the default version (`wsl --set-default-version 2`)
- Create or update `~/.wslconfig` with recommended memory/swap/processor settings
- Start Docker Desktop if it is not running
- Remove Docker networks that conflict with the Ceph cluster subnet

**What requires manual action:**

- Installing Docker Desktop (download from docker.com)
- Freeing disk space
- Switching Docker Desktop from Hyper-V to WSL2 backend (toggle in Docker Desktop settings)

## 5. Generate Cluster Files

Generate a default cluster (1 MON, 1 MGR, 3 OSDs):

```powershell
dotnet run --project src/Ceph.Cli -- init --output C:\ceph-cluster
```

Or generate a larger cluster with optional services:

```powershell
dotnet run --project src/Ceph.Cli -- init `
    --output C:\ceph-cluster `
    --monitors 3 `
    --osds 5 `
    --managers 2 `
    --rgw `
    --mds
```

This creates the following files in the output directory:

| File | Purpose |
|------|---------|
| `docker-compose.yml` | Defines all Ceph services, volumes, and networking |
| `ceph.conf` | Ceph cluster configuration (FSID, auth, pools) |
| `entrypoint.sh` | Bootstrap script that initializes each daemon type |
| `.env` | Shared environment variables for all containers |
| `wslconfig.recommended` | Recommended `~/.wslconfig` for WSL2 tuning |
| `README.md` | Quick-reference docs for the generated cluster |

## 6. Start the Cluster

```powershell
dotnet run --project src/Ceph.Cli -- up --dir C:\ceph-cluster
```

The first start will:
1. Create a Docker bridge network (`172.20.0.0/16`)
2. Create named volumes for each daemon
3. Start the MON container and wait for it to become healthy (~60 seconds)
4. Start MGR, OSD, and any optional services after MON is healthy

## 7. Check Cluster Health

Wait about 60 seconds for the bootstrap to complete, then:

```powershell
dotnet run --project src/Ceph.Cli -- status --dir C:\ceph-cluster
```

A healthy cluster looks like:

```
=== Container Status ===
NAME        IMAGE                   COMMAND            SERVICE     STATUS
ceph-mon1   quay.io/ceph/ceph:v18   "/entrypoint.sh"   ceph-mon1   Up 2 minutes (healthy)
ceph-mgr1   quay.io/ceph/ceph:v18   "/entrypoint.sh"   ceph-mgr1   Up 90 seconds
ceph-osd1   quay.io/ceph/ceph:v18   "/entrypoint.sh"   ceph-osd1   Up 90 seconds
ceph-osd2   quay.io/ceph/ceph:v18   "/entrypoint.sh"   ceph-osd2   Up 90 seconds
ceph-osd3   quay.io/ceph/ceph:v18   "/entrypoint.sh"   ceph-osd3   Up 90 seconds

=== Ceph Cluster Health ===
  cluster:
    id:     50e64de6-21b4-42cb-9a56-c034ea1454b4
    health: HEALTH_WARN
            mon is allowing insecure global_id reclaim
            1 monitors have not enabled msgr2

  services:
    mon: 1 daemons, quorum ceph-mon1
    mgr: ceph-mgr1(active)
    osd: 3 osds: 3 up, 3 in
```

> **Note:** `HEALTH_WARN` with "insecure global_id reclaim" and "msgr2" messages are normal for a fresh dev/test cluster. See the [Troubleshooting Guide](troubleshooting.md) for how to suppress these warnings.

## 8. Use the Cluster

With the cluster running, you can interact with Ceph directly:

```powershell
# Run any ceph command inside the MON container
docker exec ceph-mon1 ceph status
docker exec ceph-mon1 ceph osd tree
docker exec ceph-mon1 ceph df

# Create a pool
docker exec ceph-mon1 ceph osd pool create mypool 32

# Use RADOS to store objects
docker exec ceph-mon1 rados -p mypool put myobject /etc/ceph/ceph.conf
docker exec ceph-mon1 rados -p mypool ls
```

If you included the RADOS Gateway (`--rgw`), the S3-compatible API is available at `http://localhost:7480`.

## 9. Stop the Cluster

```powershell
# Stop containers (data is preserved in Docker volumes)
dotnet run --project src/Ceph.Cli -- down --dir C:\ceph-cluster

# Stop and destroy all data
dotnet run --project src/Ceph.Cli -- down --dir C:\ceph-cluster --volumes
```

## Next Steps

- [Architecture Guide](architecture.md) -- understand how the cluster is structured
- [Troubleshooting Guide](troubleshooting.md) -- resolve common issues
- [Command Reference](command-reference.md) -- full details on every command and option
