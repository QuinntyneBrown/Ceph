# Using Rancher Desktop Instead of Docker Desktop

[Rancher Desktop](https://rancherdesktop.io/) is a free, open-source alternative to Docker Desktop for running containers on Windows. This guide explains how to use `ceph-cli` with Rancher Desktop.

## Prerequisites

| Requirement        | Minimum              | How to check |
|--------------------|----------------------|--------------|
| .NET SDK           | 8.0                  | `dotnet --version` |
| Windows            | 10/11 21H2 or later  | `winver` |
| WSL2               | Default version 2    | `wsl --status` |
| Rancher Desktop    | 1.9+                 | Check in Rancher Desktop About dialog |
| Free disk space    | 10 GB                | Check in File Explorer |

## Install Rancher Desktop

1. Download from [rancherdesktop.io](https://rancherdesktop.io/) or use winget:

   ```powershell
   winget install suse.RancherDesktop
   ```

2. During setup (or in Preferences after install), configure these settings:

### Container Runtime: Use dockerd (moby)

**This is critical.** Rancher Desktop supports two container runtimes:

| Runtime | Compatible with ceph-cli? | Notes |
|---------|--------------------------|-------|
| **dockerd (moby)** | Yes | Provides the `docker` and `docker compose` CLI commands that ceph-cli requires |
| **containerd** | No | Does not provide `docker compose`; uses `nerdctl` instead |

To set the runtime:

1. Open Rancher Desktop
2. Go to **Preferences** (gear icon) > **Container Engine**
3. Select **dockerd (moby)**
4. Click **Apply**

Rancher Desktop will restart its WSL2 backend with the new runtime.

### Verify Docker CLI availability

After switching to dockerd, confirm the CLI tools are available:

```powershell
docker --version
docker compose version
```

Both commands must succeed before proceeding.

## WSL2 Setup

Rancher Desktop uses WSL2 just like Docker Desktop. The WSL2 configuration steps are identical:

```powershell
# Ensure WSL2 is the default
wsl --set-default-version 2
```

The `~/.wslconfig` recommendations also apply. You can use the CLI to configure it:

```powershell
ceph-cli fix --wsl-memory 6
wsl --shutdown
```

## Running the Diagnostics

Run `diagnose` as normal:

```powershell
ceph-cli diagnose
```

### Expected Differences

With Rancher Desktop, some checks behave differently:

| Check | Behavior with Rancher Desktop |
|-------|-------------------------------|
| **Docker Desktop installed** | Will show **FAIL** -- this is expected and safe to ignore |
| **Docker daemon reachable** | Should **PASS** if Rancher Desktop is running with dockerd |
| **Docker Compose** | Should **PASS** (Rancher Desktop bundles `docker compose` with dockerd) |
| **Docker WSL2 backend** | Should **PASS** (Rancher Desktop always uses WSL2) |
| All other checks | Behave identically |

The "Docker Desktop installed" failure is **informational only** -- it checks for the `Docker Desktop.exe` binary, which Rancher Desktop does not install. This does not affect cluster operation.

### Example Output

```
=== Ceph Environment Diagnostics ===

  OK  Operating system
     Running on Windows - OK.

  OK  WSL2 default version
     WSL2 is set as the default version - OK.

  FAIL  Docker Desktop installed
     Docker Desktop does not appear to be installed.
     Hint: Download and install Docker Desktop from https://www.docker.com/products/docker-desktop/

  OK  Docker daemon reachable
     Docker daemon is running - OK.

  OK  Docker Compose
     Docker Compose (v2 plugin) found - OK.

  ...

Results: 8 passed, 1 failed
```

The single failure for "Docker Desktop installed" is expected. All other checks should pass.

## Running the Fix Command

The `fix` command works with Rancher Desktop with one exception:

```powershell
ceph-cli fix
```

| Fix | Behavior with Rancher Desktop |
|-----|-------------------------------|
| **WSL2 default version** | Works identically |
| **WSL2 memory configuration** | Works identically |
| **Start Docker Desktop** | Will **fail** -- Rancher Desktop uses a different executable. Start Rancher Desktop manually instead. |
| **Docker network conflict** | Works identically |

If the Docker daemon is not running, start Rancher Desktop manually before running `fix` or other commands.

## Full Workflow

```powershell
# 1. Install the CLI
dotnet tool install -g QuinntyneBrown.Ceph.Cli

# 2. Start Rancher Desktop (if not already running)
#    Launch from the Start menu or taskbar

# 3. Run diagnostics (expect "Docker Desktop installed" to fail)
ceph-cli diagnose

# 4. Fix any real issues (ignore Docker Desktop warning)
ceph-cli fix
wsl --shutdown

# 5. Generate cluster files
ceph-cli init --output C:\ceph-cluster

# 6. Start the cluster
ceph-cli up --dir C:\ceph-cluster

# 7. Wait ~60 seconds, then check health
ceph-cli status --dir C:\ceph-cluster
```

## Troubleshooting

### `docker compose` command not found

**Cause:** Rancher Desktop is using the containerd runtime instead of dockerd.

**Fix:** Switch to dockerd in Preferences > Container Engine > dockerd (moby).

### Docker daemon not reachable

**Cause:** Rancher Desktop is not running or has not finished starting.

**Fix:** Start Rancher Desktop and wait for the status indicator (bottom-left of the Rancher Desktop window) to show a green dot or "Running" state. This can take 30-60 seconds after launch.

### Containers fail with permission errors

**Cause:** Rancher Desktop may have different default user namespace or security settings.

**Fix:** The generated `docker-compose.yml` already sets `privileged: true` on OSD containers. If other containers have permission issues, try running Rancher Desktop with administrative mode enabled in Preferences > Application > Administrative Access.

### Network conflicts

Rancher Desktop creates its own Docker networks. If you see a subnet conflict with `172.20.0.0/16`:

```powershell
# Detect conflicts
ceph-cli diagnose

# Auto-remove conflicting networks
ceph-cli fix
```

### Port forwarding differences

Rancher Desktop handles port forwarding to `localhost` automatically, similar to Docker Desktop. The RGW port (`7480`) and any other exposed ports should be accessible at `localhost:<port>` without additional configuration.

## Rancher Desktop vs Docker Desktop

| Feature | Docker Desktop | Rancher Desktop |
|---------|---------------|-----------------|
| License | Free for personal/small business; paid for enterprise | Free and open-source (Apache 2.0) |
| Container runtimes | dockerd only | dockerd (moby) or containerd |
| `docker compose` | Always available | Only with dockerd runtime |
| WSL2 integration | Yes | Yes |
| Kubernetes | Optional built-in | Built-in (K3s) |
| ceph-cli compatibility | Full | Full (with dockerd runtime) |
| `diagnose` results | All checks pass | "Docker Desktop installed" shows FAIL (safe to ignore) |
| `fix --start-docker` | Auto-starts Docker Desktop | Not supported; start Rancher Desktop manually |

## Next Steps

- [Getting Started](getting-started.md) -- full setup guide (applies to Rancher Desktop with the notes above)
- [Architecture](architecture.md) -- understand how the cluster works
- [Troubleshooting](troubleshooting.md) -- general troubleshooting (all steps apply to Rancher Desktop)
- [Command Reference](command-reference.md) -- full command details
