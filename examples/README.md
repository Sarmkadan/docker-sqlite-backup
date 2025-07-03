# Docker SQLite Backup Examples

Practical examples demonstrating various deployment and usage scenarios for Docker SQLite Backup.

## Quick Start Examples

### 1. Basic Setup (`01-basic-setup.sh`)

The simplest way to get started with local backups.

```bash
chmod +x examples/01-basic-setup.sh
./examples/01-basic-setup.sh /path/to/database.db ./backups daily
```

**What it does:**
- Creates backup directory
- Generates default `appsettings.json`
- Builds the project
- Starts the application

**Output:**
- Running application with daily backups at 2 AM
- API available at http://localhost:5000

---

### 2. Docker Deployment (`02-docker-deployment.sh`)

Deploy backup service in a Docker container.

```bash
chmod +x examples/02-docker-deployment.sh
./examples/02-docker-deployment.sh sqlite-backup /var/lib/app.db /var/backups/sqlite us-east-1 company-backups
```

**What it does:**
- Builds Docker image
- Creates and starts container
- Mounts database and backup volumes
- Verifies container health

**Output:**
- Running Docker container with persistent storage
- Port 5000 exposed for API access

---

### 3. Kubernetes Deployment (`03-kubernetes-deployment.yaml`)

Production-grade Kubernetes deployment with persistent volumes and RBAC.

```bash
# Create namespace and deploy
kubectl apply -f examples/03-kubernetes-deployment.yaml

# Monitor deployment
kubectl get pods -n sqlite-backup
kubectl logs -f deployment/sqlite-backup -n sqlite-backup

# Port forward for testing
kubectl port-forward svc/sqlite-backup 5000:80 -n sqlite-backup
```

**Features:**
- StatefulSet with PersistentVolumes
- Health checks (liveness & readiness probes)
- Resource limits and requests
- Security context configuration
- RBAC with ServiceAccount

---

### 4. Backup Monitoring (`04-backup-monitoring.sh`)

Monitor backup health and get automated alerts.

```bash
chmod +x examples/04-backup-monitoring.sh

# Basic monitoring
./examples/04-backup-monitoring.sh http://localhost:5000 daily-backup

# With Slack notifications
./examples/04-backup-monitoring.sh \
  http://localhost:5000 \
  daily-backup \
  https://hooks.slack.com/services/YOUR/WEBHOOK/URL
```

**Checks:**
- System health status
- Last backup completion
- Verification results
- Backup freshness
- Success rate metrics

**Output:**
- Color-coded status report
- Optional Slack alerts for failures

---

### 5. Restore from Backup (`05-restore-from-backup.sh`)

Restore a backup to test recoverability.

```bash
chmod +x examples/05-restore-from-backup.sh

# List available backups
./examples/05-restore-from-backup.sh http://localhost:5000

# Restore specific backup
./examples/05-restore-from-backup.sh \
  http://localhost:5000 \
  backup-20260504-020000 \
  ./restored.db \
  true
```

**Steps:**
1. Lists available backups
2. Retrieves backup metadata
3. Executes restore operation
4. Verifies restored database integrity
5. Shows table listing and statistics

**Output:**
- Restored database file
- Verification report
- Ready-to-use recovered database

---

### 6. AWS S3 Setup (`06-s3-backup-setup.sh`)

Configure cloud storage with encryption and versioning.

```bash
chmod +x examples/06-s3-backup-setup.sh

# Quick setup (creates new bucket)
./examples/06-s3-backup-setup.sh default

# Use existing bucket
./examples/06-s3-backup-setup.sh default my-company-backups us-east-1
```

**Configuration:**
- AWS credential validation
- S3 bucket creation (or validation)
- Versioning enabled
- Server-side encryption (AES256)
- Public access blocked
- Lifecycle policies configured

**Output:**
- `appsettings.production.json` with S3 settings
- `.env.s3` with AWS credentials
- `run-s3-backup.sh` Docker run script

---

### 7. Compliance Audit (`07-compliance-audit.sh`)

Audit backup compliance with retention policies and regulatory requirements.

```bash
chmod +x examples/07-compliance-audit.sh

# Run compliance audit
./examples/07-compliance-audit.sh \
  http://localhost:5000 \
  daily-backup \
  30 \
  10 \
  audit-report.json
```

**Tests Performed:**
1. System health check
2. Schedule configuration
3. Retention policy compliance
4. Recent backup availability
5. Backup verification status
6. Storage accessibility
7. Backup integrity metrics
8. Success rate analysis

**Output:**
- Color-coded test results
- JSON compliance report
- Exit code indicates pass/fail

---

## Common Workflows

### Development Setup

```bash
# 1. Basic local backups
./examples/01-basic-setup.sh

# 2. Monitor backups
./examples/04-backup-monitoring.sh

# 3. Verify backups work
./examples/05-restore-from-backup.sh
```

### Production Deployment (Docker)

```bash
# 1. Build and deploy
./examples/02-docker-deployment.sh my-backup /data/app.db /backups

# 2. Configure S3 storage
./examples/06-s3-backup-setup.sh default company-backups us-west-2

# 3. Continuous monitoring
watch -n 300 './examples/04-backup-monitoring.sh'

# 4. Compliance audits (daily)
0 0 * * * /path/to/examples/07-compliance-audit.sh
```

### Kubernetes Deployment

```bash
# 1. Deploy
kubectl apply -f examples/03-kubernetes-deployment.yaml

# 2. Monitor
kubectl logs -f deployment/sqlite-backup -n sqlite-backup

# 3. Audit compliance
kubectl exec deployment/sqlite-backup -n sqlite-backup -- \
  /app/examples/07-compliance-audit.sh http://localhost:5000
```

### Disaster Recovery Testing

```bash
# Monthly restore test
0 2 1 * * /path/to/examples/05-restore-from-backup.sh && \
  sqlite3 ./test-restore.db "SELECT COUNT(*) FROM main_table;" && \
  rm ./test-restore.db
```

## Environment Requirements

### All Examples
- Bash 4.0+
- curl command-line tool
- jq JSON processor

### Docker Examples
- Docker installed and running
- Docker daemon access

### Kubernetes Examples
- kubectl configured
- Active Kubernetes cluster
- PersistentVolume provisioner

### S3 Setup
- AWS CLI installed
- AWS credentials configured
- S3 access permissions

### Database Examples
- sqlite3 CLI tool (for verification)

## Customization

### Modify Backup Schedule

Edit the cron expression in `appsettings.json`:

```json
{
  "Schedules": [{
    "CronExpression": "0 */4 * * *"  // Every 4 hours
  }]
}
```

### Change Retention Policy

```json
{
  "Schedules": [{
    "RotationStrategy": "Age",
    "RetentionDays": 90  // Keep 90 days
  }]
}
```

### Use Multiple Storage Backends

```json
{
  "Schedules": [
    {
      "Id": "daily-local",
      "StorageType": "Local",
      "CronExpression": "0 2 * * *"
    },
    {
      "Id": "weekly-s3",
      "StorageType": "S3",
      "CronExpression": "0 3 * * 0"
    }
  ]
}
```

## Troubleshooting

### Script Permission Errors

```bash
chmod +x examples/*.sh
```

### Docker Container Fails to Start

```bash
# Check logs
docker logs <container-name>

# Verify configuration
docker exec <container-name> cat /app/appsettings.json
```

### Kubernetes Pod Crashes

```bash
# Check pod status
kubectl describe pod <pod-name> -n sqlite-backup

# View logs
kubectl logs <pod-name> -n sqlite-backup
```

### S3 Access Denied

```bash
# Verify AWS credentials
aws --profile default sts get-caller-identity

# Check bucket policy
aws s3api get-bucket-policy --bucket <bucket-name>
```

## Further Reading

- [Getting Started Guide](../docs/getting-started.md)
- [Architecture Documentation](../docs/architecture.md)
- [API Reference](../docs/api-reference.md)
- [Deployment Guide](../docs/deployment.md)
- [FAQ](../docs/faq.md)
