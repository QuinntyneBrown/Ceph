# Troubleshooting

This guide covers common problems when running a Ceph cluster with `ceph-cli` on Windows and how to resolve them.

## First Steps

Always start by running the built-in diagnostics:

```powershell
dotnet run --project src/Ceph.Cli -- diagnose
```

Then apply automatic fixes:

```powershell
dotnet run --project src/Ceph.Cli -- fix
```

## Docker & WSL2 Issues

### Docker daemon not reachable

**Symptom:** `diagnose` reports "Docker daemon is not reachable" or commands fail with "Cannot connect to the Docker daemon."

**Cause:** Docker Desktop is not running.

**Fix:**

```powershell
# Auto-fix via CLI
dotnet run --project src/Ceph.Cli -- fix

# Or start manually
Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe"
```

Wait about 30 seconds for Docker Desktop to fully initialize.

### WSL2 is not the default version

**Symptom:** `diagnose` reports "WSL2 is not set as the default version."

**Cause:** WSL defaults to version 1 on some systems.

**Fix:**

```powershell
wsl --set-default-version 2
```

### Docker Desktop using Hyper-V instead of WSL2

**Symptom:** `diagnose` reports "Docker Desktop appears to be using the Hyper-V backend."

**Cause:** Docker Desktop is configured to use the legacy Hyper-V backend.

**Fix:**

1. Open Docker Desktop
2. Go to Settings > General
3. Enable **"Use the WSL 2 based engine"**
4. Click Apply & Restart

### WSL2 memory not configured

**Symptom:** `diagnose` reports "~/.wslconfig not found" or "does not set 'memory'." The cluster runs out of memory or the OOM killer terminates containers.

**Fix:**

```powershell
# Auto-fix with 6 GB (default)
dotnet run --project src/Ceph.Cli -- fix

# Or specify a custom amount
dotnet run --project src/Ceph.Cli -- fix --wsl-memory 8
```

Then restart WSL:

```powershell
wsl --shutdown
```

And restart Docker Desktop.

## Cluster Startup Issues

### Network subnet conflict

**Symptom:** `docker compose up` fails with "Pool overlaps with other one on this address space."

**Cause:** Another Docker network already uses the `172.20.0.0/16` subnet.

**Fix:**

```powershell
# Automatic detection and removal
dotnet run --project src/Ceph.Cli -- diagnose   # shows the conflicting network name
dotnet run --project src/Ceph.Cli -- fix         # removes it

# Or remove manually
docker network ls
docker network rm <conflicting-network-name>
```

### MON container is unhealthy

**Symptom:** `docker compose up` reports "dependency failed to start: container ceph-mon1 is unhealthy." Other services fail to start.

**Possible causes:**

1. **CRLF line endings in entrypoint.sh** -- This was a bug in earlier versions. Regenerate files with the current CLI version.

2. **Config parse error** -- Check MON logs:
   ```powershell
   docker logs ceph-mon1
   ```
   If you see "parse error: expected '\<empty_line\>'" then regenerate the cluster files.

3. **Hostname mismatch** -- The container hostname must match `mon_initial_members` in `ceph.conf`. The CLI sets `hostname:` in docker-compose to ensure this.

**Fix:** Regenerate all files and restart from scratch:

```powershell
dotnet run --project src/Ceph.Cli -- down --dir C:\ceph-cluster --volumes
dotnet run --project src/Ceph.Cli -- init --output C:\ceph-cluster
dotnet run --project src/Ceph.Cli -- up --dir C:\ceph-cluster
```

### OSDs keep restarting

**Symptom:** `status` shows OSDs in "Restarting" state.

**Possible causes:**

1. **Auth failure** -- OSDs can't reach the MON to authenticate. Wait 60 seconds for the MON to fully bootstrap, then check OSD logs:
   ```powershell
   docker logs ceph-osd1
   ```

2. **Previous failed bootstrap** -- If the OSD partially bootstrapped and left stale state, remove volumes and restart:
   ```powershell
   dotnet run --project src/Ceph.Cli -- down --dir C:\ceph-cluster --volumes
   dotnet run --project src/Ceph.Cli -- up --dir C:\ceph-cluster
   ```

### Containers crash immediately

**Symptom:** Containers exit with code 1 right after starting.

**Fix:** Check logs for the specific container:

```powershell
docker logs ceph-mon1
docker logs ceph-osd1
```

Common causes:
- **"bad interpreter"** -- CRLF line endings. Regenerate files.
- **"No such file or directory"** -- Wrong Ceph image version. The CLI is designed for `quay.io/ceph/ceph:v18`.

## Cluster Health Warnings

### "mon is allowing insecure global_id reclaim"

**Symptom:** `ceph status` shows `HEALTH_WARN: mon is allowing insecure global_id reclaim`.

**Cause:** A security setting that is relaxed by default in fresh clusters.

**Fix (optional for dev/test clusters):**

```powershell
docker exec ceph-mon1 ceph config set mon auth_allow_insecure_global_id_reclaim false
```

### "monitors have not enabled msgr2"

**Symptom:** `ceph status` shows `HEALTH_WARN: 1 monitors have not enabled msgr2`.

**Cause:** The monitor is using the v1 protocol on port 6789. The v2 protocol (port 3300) is not enabled.

**Fix (optional for dev/test clusters):**

```powershell
docker exec ceph-mon1 ceph mon enable-msgr2
```

### "no active mgr"

**Symptom:** `ceph status` shows `mgr: no daemons active`.

**Cause:** The MGR is still starting or failed to bootstrap.

**Fix:** Wait 30-60 seconds. If it persists:

```powershell
docker logs ceph-mgr1
```

### "0 osds: 0 up"

**Symptom:** `ceph status` shows `osd: 3 osds: 0 up, 3 in`.

**Cause:** OSDs are registered but not yet running.

**Fix:** Wait for OSDs to complete their bootstrap. Check logs:

```powershell
docker logs ceph-osd1
```

## Disk Space Issues

### Insufficient disk space

**Symptom:** `diagnose` reports the drive has less than 10 GB free.

**Cause:** The Ceph container image is approximately 500 MB, and each OSD needs space for data.

**Fix:**

- Free up disk space
- Move Docker's data directory to a larger drive (Docker Desktop Settings > Resources > Disk image location)
- Reduce the number of OSDs

### Docker disk image is full

**Symptom:** Containers fail to start with "no space left on device."

**Fix:**

```powershell
# Remove unused Docker resources
docker system prune -f

# Or increase Docker Desktop's virtual disk size
# Docker Desktop > Settings > Resources > Virtual disk limit
```

## Port Conflicts

### RGW port 7480 already in use

**Symptom:** `docker compose up` fails when RGW is enabled because port 7480 is in use.

**Fix:**

1. Find what is using port 7480:
   ```powershell
   netstat -ano | findstr :7480
   ```

2. Either stop that process or edit the generated `docker-compose.yml` to use a different host port:
   ```yaml
   ports:
     - "8480:7480"  # changed from 7480 to 8480 on host
   ```

## Data Recovery

### Accidentally ran `down --volumes`

**Symptom:** All cluster data is gone after running `down --volumes`.

**Cause:** The `--volumes` flag deletes all Docker named volumes, which destroys all Ceph data.

**Fix:** There is no recovery. The cluster must be re-initialized:

```powershell
dotnet run --project src/Ceph.Cli -- init --output C:\ceph-cluster
dotnet run --project src/Ceph.Cli -- up --dir C:\ceph-cluster
```

**Prevention:** Never use `--volumes` unless you intend to destroy all data. A plain `down` preserves data and allows `up` to resume the existing cluster.

## Interacting with the Cluster

### Running Ceph commands

All Ceph CLI commands should be run inside the MON container:

```powershell
docker exec ceph-mon1 ceph <command>
```

Examples:

```powershell
docker exec ceph-mon1 ceph status
docker exec ceph-mon1 ceph osd tree
docker exec ceph-mon1 ceph health detail
docker exec ceph-mon1 ceph osd pool ls
docker exec ceph-mon1 ceph df
```

### Getting a shell inside a container

```powershell
docker exec -it ceph-mon1 bash
```

## Getting Help

If the issue persists after trying the steps above:

1. Collect diagnostic output: `dotnet run --project src/Ceph.Cli -- diagnose --json`
2. Collect container logs: `docker logs ceph-mon1 > mon.log 2>&1`
3. Open an issue at the project repository with the diagnostic output and logs
