# Migration Guide: v1.0 to v2.0

This guide helps you migrate from Docker SQLite Backup v1.0.0 to v2.0.0.

## Overview

v2.0.0 introduces significant improvements to Docker integration, enhanced health checks, and updated dependencies for .NET 10 compatibility. All public APIs remain stable, but deployment configurations have been refined.

## Key Changes

### Docker & Deployment

- **Port Change**: Application now binds to port `8080` (previously `5000`)
- **Health Check Enhancement**: Improved HEALTHCHECK directive with wget fallback support
- **Alpine Optimization**: Updated base images to latest Alpine .NET 10 variants
- **Non-root User**: Application runs as `backup` user (UID 1000) for enhanced security

### Dependency Updates

- All NuGet packages pinned to `.NET 10.0.0` releases
- AWS SDK.S3 updated to `3.7.400.1` (latest stable)
- Cronos scheduler remains at `0.13.0` (stable)

### Configuration Changes

Environment variables remain compatible, but review the following:

```bash
# v2.0.0 docker-compose.yml now uses:
ASPNETCORE_URLS: "http://0.0.0.0:8080"

# Update your port mappings:
ports:
  - "8080:8080"  # Changed from 5000:5000
```

## Migration Steps

### 1. Update Docker Compose (if using local setup)

Replace port `5000` with `8080`:

```yaml
# Before (v1.0)
ports:
  - "5000:5000"

# After (v2.0)
ports:
  - "8080:8080"
```

Update health check endpoint:

```yaml
# Before (v1.0)
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:5000/health"]

# After (v2.0)
healthcheck:
  test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8080/health"]
```

### 2. Update Environment Variables

If you're using Kubernetes or custom Docker configurations, update ASPNETCORE_URLS:

```bash
# Before
ASPNETCORE_URLS=http://0.0.0.0:5000

# After
ASPNETCORE_URLS=http://0.0.0.0:8080
```

### 3. Update Reverse Proxy Configuration

If you're running behind Nginx, Caddy, or similar:

```nginx
# Nginx example (update upstream)
upstream sqlite_backup {
    server localhost:8080;  # Changed from 5000
}
```

### 4. Update Monitoring & Alerting

Update any monitoring rules that check the health endpoint:

```bash
# Old health check URL
curl http://localhost:5000/health

# New health check URL
curl http://localhost:8080/health
```

### 5. Firewall & Network Rules

If you have firewall rules for port `5000`, update them to use port `8080`:

```bash
# Allow port 8080
sudo ufw allow 8080/tcp
```

## Breaking Changes

**Port Migration Required**
- The default port has changed from `5000` to `8080`
- Applications connecting to this service must update their connection URLs
- If you have external DNS records pointing to port 5000, update them

## Non-Breaking Changes

- NuGet package names remain unchanged
- API endpoints remain the same (only port differs)
- Configuration schema is fully backward compatible
- S3 backup/restore functionality unchanged

## Rollback Procedure

If you need to revert to v1.0.0:

```bash
# Update docker-compose.yml to use old image
image: sqlite-backup:1.0.0

# Revert ports
ports:
  - "5000:5000"

# Revert environment
ASPNETCORE_URLS: "http://0.0.0.0:5000"

# Rebuild and restart
docker-compose up -d --build
```

## Troubleshooting

### Health Check Failures

If health checks are failing after upgrade:

```bash
# Check if application is listening on 8080
docker exec sqlite-backup netstat -tlnp | grep 8080

# Test health endpoint directly
docker exec sqlite-backup wget -O- http://localhost:8080/health
```

### Port Already in Use

If port 8080 is already in use:

```bash
# Find process using port 8080
sudo lsof -i :8080

# Or in docker-compose.yml, map to different port:
ports:
  - "9000:8080"  # Map container 8080 to host 9000
```

### Performance After Upgrade

v2.0.0 includes Alpine optimization that may reduce memory footprint:

```bash
# Check container resource usage
docker stats sqlite-backup
```

## Support

For migration issues or questions:
- Check [architecture.md](architecture.md) for deployment patterns
- Review [deployment.md](deployment.md) for Kubernetes examples
- Open an issue on GitHub with the `migration` label
