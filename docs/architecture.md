# Architecture

This document explains how the Ceph CLI tool generates and manages a Docker-based Ceph cluster on Windows, covering the container architecture, networking, bootstrap process, volume strategy, and generated file details.

## Overview

The CLI generates a Docker Compose configuration that runs a multi-container Ceph cluster. Each Ceph daemon (MON, MGR, OSD, and optionally RGW/MDS) runs in its own container, sharing keyrings through a common Docker volume.

```
+---------------------------------------------------+
|  Docker Desktop (WSL2 backend)                     |
|                                                    |
|  ceph-net (172.20.0.0/16)                          |
|  +-------+  +-------+  +-------+-------+-------+  |
|  | MON 1 |  | MGR 1 |  | OSD 1 | OSD 2 | OSD 3 |  |
|  | .1.1  |  | .2.1  |  | .3.1  | .3.2  | .3.3  |  |
|  +---+---+  +---+---+  +---+---+---+---+---+---+  |
|      |          |           |       |       |      |
|      +----------+-----------+-------+-------+      |
|                     |                              |
|              [ ceph-etc volume ]                   |
|              (shared /etc/ceph)                    |
+---------------------------------------------------+
```

## Ceph Daemon Roles

### MON (Monitor)

Monitors maintain the cluster map, including the monitor map, OSD map, placement group map, and CRUSH map. They are the authority on cluster state and membership.

- **Container name:** `ceph-mon1`, `ceph-mon2`, ...
- **IP range:** `172.20.1.x`
- **Listens on:** Port 6789
- **Bootstraps:** First daemon to start; creates admin keyring, monitor keyring, and bootstrap keyrings for other daemons
- **Health check:** `ceph --connect-timeout 5 health` every 30 seconds (with 60-second start period)

### MGR (Manager)

Managers run alongside monitors and provide additional monitoring and interface capabilities (dashboard, Prometheus endpoint, etc.).

- **Container name:** `ceph-mgr1`, `ceph-mgr2`, ...
- **IP range:** `172.20.2.x`
- **Depends on:** MON (waits for `service_healthy` condition)
- **Bootstraps:** Creates its own keyring via `ceph auth get-or-create`

### OSD (Object Storage Daemon)

OSDs store the actual data. Each OSD manages one storage device (in this Docker setup, backed by a named volume).

- **Container name:** `ceph-osd1`, `ceph-osd2`, ...
- **IP range:** `172.20.3.x`
- **Depends on:** MON (waits for `service_healthy` condition)
- **Runs as:** `privileged: true` (required for OSD operations)
- **Bootstraps:** Creates a new OSD ID via `ceph osd new`, generates a keyring, initializes the filesystem with `ceph-osd --mkfs`

### RGW (RADOS Gateway) -- Optional

Provides an S3/Swift-compatible REST API for object storage.

- **Container name:** `ceph-rgw`
- **IP:** `172.20.4.1`
- **Exposed port:** `7480` (mapped to host)
- **Depends on:** MON

### MDS (Metadata Server) -- Optional

Manages metadata for the CephFS distributed filesystem.

- **Container name:** `ceph-mds`
- **IP:** `172.20.4.2`
- **Depends on:** MON

## Networking

All containers are connected to a single Docker bridge network:

```yaml
networks:
  ceph-net:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16
```

Each service type is assigned a subnet range for its static IPs:

| Service | Subnet range | Example IPs |
|---------|-------------|-------------|
| MON     | `172.20.1.x` | `172.20.1.1`, `172.20.1.2`, `172.20.1.3` |
| MGR     | `172.20.2.x` | `172.20.2.1`, `172.20.2.2` |
| OSD     | `172.20.3.x` | `172.20.3.1`, `172.20.3.2`, `172.20.3.3` |
| RGW     | `172.20.4.1` | Fixed |
| MDS     | `172.20.4.2` | Fixed |

The `CEPH_PUBLIC_NETWORK` environment variable is set to `172.20.0.0/16` on all containers so Ceph daemons bind to the correct interface.

### Network Conflict Detection

The `diagnose` command checks all existing Docker networks for subnet conflicts with `172.20.0.0/16`. If a conflict is found, the `fix` command can remove the conflicting network.

## Volume Strategy

The cluster uses Docker named volumes for persistence:

| Volume | Mount point | Purpose |
|--------|-------------|---------|
| `ceph-etc` | `/etc/ceph` | **Shared** across all containers -- stores `ceph.conf`, admin keyring, and bootstrap keyrings |
| `ceph-mon{i}-data` | `/var/lib/ceph` | MON data (monmap, store.db) |
| `ceph-mgr{i}-data` | `/var/lib/ceph` | MGR data and keyring |
| `ceph-osd{i}-data` | `/var/lib/ceph` | OSD data, keyring, and journal |
| `ceph-rgw-data` | `/var/lib/ceph` | RGW data (if enabled) |
| `ceph-mds-data` | `/var/lib/ceph` | MDS data (if enabled) |

### Why `ceph-etc` is shared

Ceph uses `cephx` authentication. During bootstrap, the MON generates:

1. **Admin keyring** (`/etc/ceph/ceph.client.admin.keyring`) -- used by all daemons and `ceph` CLI commands
2. **Bootstrap keyrings** (`/var/lib/ceph/bootstrap-{osd,mgr,mds,rgw}/ceph.keyring`) -- used by daemons during their initial registration with the MON

By sharing `/etc/ceph` via a named volume, all containers have access to the admin keyring without needing to copy files between containers.

### Why `ceph.conf` is staged as a seed

The generated `ceph.conf` is bind-mounted at `/ceph.conf.seed:ro` (not at `/etc/ceph/ceph.conf`) because:

1. `/etc/ceph` is backed by the shared `ceph-etc` volume
2. Bind-mounting a file inside a volume mount is unreliable across Docker versions
3. The entrypoint script copies the seed config on first boot: `cp /ceph.conf.seed /etc/ceph/ceph.conf`

## Bootstrap Sequence

The entrypoint script (`entrypoint.sh`) handles the complete bootstrap for each daemon type:

### MON Bootstrap (runs first)

```
1. Copy seed ceph.conf to /etc/ceph/ceph.conf (if not present)
2. Create admin keyring (/etc/ceph/ceph.client.admin.keyring)
3. Create MON keyring, import admin key
4. Create bootstrap keyrings for OSD, MGR, MDS, RGW
5. Import all bootstrap keys into MON keyring
6. Create monmap with MON name and IP
7. Initialize MON data directory (ceph-mon --mkfs)
8. Set ownership to ceph:ceph
9. Start ceph-mon in foreground (-f)
```

### MGR Bootstrap (after MON healthy)

```
1. Wait for MON to respond to `ceph -s`
2. Create MGR data directory
3. Create MGR keyring via `ceph auth get-or-create`
4. Start ceph-mgr in foreground (-f)
```

### OSD Bootstrap (after MON healthy)

```
1. Wait for MON to respond to `ceph -s`
2. Generate a UUID for the OSD
3. Register with MON via `ceph osd new <uuid>` (returns OSD ID)
4. Create OSD keyring via `ceph auth get-or-create`
5. Initialize OSD filesystem (ceph-osd --mkfs --no-mon-config)
6. Add OSD to CRUSH map
7. Start ceph-osd in foreground (-f)
```

### Service Startup Order

Docker Compose enforces this order using `depends_on` with health checks:

```
MON (starts first, has healthcheck)
 └── MGR (waits for MON healthy)
 └── OSD (waits for MON healthy)
 └── RGW (waits for MON healthy)
 └── MDS (waits for MON healthy)
```

## Generated Files

### `docker-compose.yml`

Defines all services, volumes, and the bridge network. Key design decisions:

- **`hostname:` directive** -- set to match the container name so Ceph daemon names match the monmap
- **`entrypoint: /entrypoint.sh`** -- overrides the image's default entrypoint
- **`privileged: true`** -- only on OSD containers (required for block device operations)
- **`start_period: 60s`** -- gives the MON time to bootstrap before health checks start

### `ceph.conf`

Minimal Ceph configuration:

```ini
[global]
fsid = <random-uuid>
mon_initial_members = ceph-mon1[,ceph-mon2,...]
mon_host = 172.20.1.1[,172.20.1.2,...]
auth_cluster_required = cephx
auth_service_required = cephx
auth_client_required = cephx
osd_pool_default_size = <min(osd_count, 3)>
osd_pool_default_min_size = 1
osd_journal_size = 1024

[osd]
osd_max_object_name_len = 256
osd_max_object_namespace_len = 64
```

The `osd_pool_default_size` is automatically set to `min(OSD count, 3)` so a cluster with fewer than 3 OSDs still works.

### `entrypoint.sh`

A Bash script that bootstraps each Ceph daemon type. See the Bootstrap Sequence section above for details. Written with Unix LF line endings to avoid "bad interpreter" errors in Linux containers.

### `.env`

Shared environment variables:

```
CEPH_DEMO_UID=ceph-demo
CEPH_DEMO_ACCESS_KEY=demo-access-key
CEPH_DEMO_SECRET_KEY=demo-secret-key
CEPH_DEMO_BUCKET=demo-bucket
NETWORK_AUTO_DETECT=4
```

These provide default credentials for the RADOS Gateway. Change them before using in production.

### `wslconfig.recommended`

Suggested WSL2 resource limits:

```ini
[wsl2]
memory=6GB
swap=2GB
processors=4
localhostForwarding=true
```

## Line Endings

All files mounted into Linux containers (`docker-compose.yml`, `ceph.conf`, `entrypoint.sh`, `.env`) are written with Unix LF (`\n`) line endings, even on Windows. This prevents "bad interpreter" and config parse errors that occur when files with Windows CRLF (`\r\n`) endings are used inside Linux containers.

## Environment Checks

The `diagnose` command performs 9 checks. See the [Command Reference](command-reference.md) for the full list.

## CLI Architecture

```
Program.cs
  └── RootCommand
       ├── InitCommand ──> DockerComposeGenerator.Generate()
       ├── UpCommand ────> docker compose up -d
       ├── DownCommand ──> docker compose down [--volumes]
       ├── StatusCommand > docker compose ps + docker exec ceph status
       ├── DiagnoseCommand > EnvironmentChecker.RunAll()
       └── FixCommand ───> EnvironmentChecker.RunAll() + IssueFixer.*()
```

All external process execution goes through `EnvironmentChecker.RunProcess()`, which handles:
- Redirected stdout/stderr capture
- Null-byte stripping for UTF-16LE output from Windows tools (e.g., `wsl --status`)
- Exit code propagation
