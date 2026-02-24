# Frequently Asked Questions

Common questions and answers about Docker SQLite Backup.

## General Questions

### What is Docker SQLite Backup?

Docker SQLite Backup is an enterprise-grade automated backup tool for SQLite databases. It provides:

- **Scheduled backups** using cron expressions
- **Multiple storage backends** (local filesystem, AWS S3)
- **Automatic verification** to ensure backup integrity
- **Intelligent rotation** to manage backup retention
- **REST API and CLI** for integration and automation

### Why should I use this instead of regular SQLite backups?

Regular SQLite backups require manual scripting and lack important production features:

| Feature | SQLite BACKUP | Docker SQLite Backup |
|---------|---------------|----------------------|
| Scheduling | Manual cron | Built-in cron management |
| Verification | None | Automatic integrity checks |
| Multiple storage | No | S3, local, extensible |
| Rotation policies | Manual | Automatic by age/count |
| REST API | No | Full HTTP API |
| Monitoring | Manual | Health checks, metrics |
| Error handling | Basic | Comprehensive with retry |

### What databases does it support?

Currently supports **SQLite only**. The codebase is modular and could be extended to support other databases (MySQL, PostgreSQL, etc.) in the future.

### Is this suitable for large databases?

Yes! The tool has been tested with databases up to several GB. Key optimizations:

- Streaming uploads to avoid memory issues
- Configurable timeout protection
- Concurrent backup limiting
- Progress tracking for large operations

Recommended approach for very large databases (>10GB):
- Use hourly backups with count-based rotation
- Run during off-peak hours
- Configure adequate timeouts
- Use S3 transfer acceleration

### Does this support differential/incremental backups?

Not currently. The tool creates full backups each time. However, you can achieve similar benefits:

1. **Count-based rotation**: Keep many frequent backups
2. **S3 versioning**: S3 will only store actual changes
3. **Custom storage**: Implement your own incremental backend

## Configuration Questions

### How do I set the backup schedule?

Use cron expressions in the `CronExpression` field:

```json
{
  "Schedules": [
    {
      "Id": "backup",
      "CronExpression": "0 2 * * *"  // Daily at 2 AM
    }
  ]
}
```

**Common expressions:**
- `0 2 * * *` - Daily at 2 AM
- `0 */4 * * *` - Every 4 hours
- `0 0 * * 0` - Weekly on Sunday
- `0 0 1 * *` - Monthly on the 1st
- `*/30 * * * *` - Every 30 minutes

Tool: [crontab.guru](https://crontab.guru)

### How long should I keep backups?

Depends on your use case:

| Use Case | Retention |
|----------|-----------|
| Development | 7 days |
| Production non-critical | 30 days |
| Production critical | 90 days |
| Compliance required | 1+ years |

Example for multiple retention tiers:

```json
{
  "Schedules": [
    {
      "Id": "daily-1week",
      "CronExpression": "0 2 * * *",
      "RetentionDays": 7
    },
    {
      "Id": "weekly-1year",
      "CronExpression": "0 3 * * 0",
      "RetentionDays": 365
    }
  ]
}
```

### Should I use retention by age or by count?

**By Age (`RetentionDays`):**
- Pros: Predictable storage costs, regulatory compliance
- Cons: May have varying number of backups
- Use when: You have strict compliance requirements

**By Count (`MaxBackupCount`):**
- Pros: Fixed number of backups, easier capacity planning
- Cons: Retention duration varies with backup frequency
- Use when: You want consistent storage footprint

**Hybrid (Both):**
- Applies both constraints
- Deletes oldest first when either limit exceeded
- Most flexible approach

### How do I configure AWS S3 storage?

```json
{
  "Schedules": [
    {
      "Id": "s3-backup",
      "StorageType": "S3",
      "S3Config": {
        "BucketName": "my-company-backups",
        "Region": "us-east-1",
        "AccessKeyId": "${AWS_KEY}",
        "SecretAccessKey": "${AWS_SECRET}",
        "EnableEncryption": true,
        "EnableVersioning": true,
        "StorageClass": "STANDARD_IA"
      }
    }
  ]
}
```

**Security tip:** Never hardcode credentials. Use:
- Environment variables
- IAM roles (if on EC2/ECS)
- AWS credential profiles
- Docker secrets (if on Swarm/K8s)

### What is the difference between "Local" and "S3" storage?

| Aspect | Local | S3 |
|--------|-------|-----|
| **Speed** | Fastest | Network latency |
| **Durability** | Depends on storage | 99.999999999% |
| **Cost** | Only disk space | Storage + API calls |
| **Scalability** | Limited by disk | Unlimited |
| **Accessibility** | Single machine | Accessible from anywhere |
| **Best for** | Dev/test, fast recovery | Production, long-term archive |

**Recommendation:** Use local for recent backups, S3 for long-term archive.

## Operation Questions

### How do I trigger an immediate backup?

**Via API:**
```bash
curl -X POST http://localhost:5000/api/backup/trigger \
  -H "Content-Type: application/json" \
  -d '{"scheduleId": "daily-backup"}'
```

**Via CLI:**
```bash
dotnet run -- trigger-backup --schedule daily-backup
```

### How do I verify a backup manually?

**Via API:**
```bash
curl -X POST http://localhost:5000/api/backup/verify/daily-backup \
  -H "Content-Type: application/json" \
  -d '{"backupId": "backup-20260504-020000"}'
```

**Via CLI:**
```bash
dotnet run -- verify-backup --backup backup-20260504-020000
```

### How do I restore a backup?

**Via API:**
```bash
curl -X POST http://localhost:5000/api/backup/restore \
  -H "Content-Type: application/json" \
  -d '{
    "backupId": "backup-20260504-020000",
    "outputPath": "/tmp/restored.db",
    "verifyAfterRestore": true
  }'
```

**Via CLI:**
```bash
dotnet run -- restore \
  --backup backup-20260504-020000 \
  --output ./restored.db \
  --verify
```

### Why did a backup fail?

Check the following in order:

1. **Is the database being modified?**
   - "database is locked" error
   - Solution: Stop writes, increase timeout, or schedule during off-peak

2. **Is storage available?**
   - Check disk space (local) or S3 access (AWS)
   - Solution: Free up space or check AWS credentials

3. **Are timeouts too short?**
   - Check `BackupTimeoutSeconds` in configuration
   - Solution: Increase timeout if backups are legitimately large

4. **Check logs for details:**
   ```bash
   docker logs sqlite-backup | grep -i error
   ```

### How do I monitor backup health?

Use the health endpoint:

```bash
# Quick check
curl http://localhost:5000/health

# Detailed health
curl http://localhost:5000/health/detailed | jq .

# List recent backups
curl "http://localhost:5000/api/backup/list?scheduleId=daily-backup&limit=5"
```

### Can I run multiple schedules simultaneously?

Yes! The `MaxConcurrentBackups` setting controls this:

```json
{
  "AppSettings": {
    "MaxConcurrentBackups": 3
  },
  "Schedules": [
    {"Id": "hourly", ...},
    {"Id": "daily", ...},
    {"Id": "weekly", ...}
  ]
}
```

Maximum 3 will run concurrently. Excess wait in queue.

## Performance Questions

### My backups are running slowly. How can I optimize?

**Check database size:**
```bash
ls -lh /path/to/database.db
```

**Optimization strategies:**

1. **Increase timeouts** (if backups legitimately take time):
   ```json
   {"AppSettings": {"BackupTimeoutSeconds": 7200}}
   ```

2. **Run during off-peak hours** (when database is less active):
   ```json
   {"Schedules": [{"CronExpression": "0 2 * * *"}]}
   ```

3. **Reduce concurrent backups** (less I/O contention):
   ```json
   {"AppSettings": {"MaxConcurrentBackups": 1}}
   ```

4. **Enable compression** (trade CPU for storage):
   ```json
   {"Schedules": [{"CompressionEnabled": true}]}
   ```

5. **Use S3 transfer acceleration** (if uploading to S3):
   ```json
   {"S3Config": {"UseTransferAcceleration": true}}
   ```

### How much disk space do I need?

Calculation: `(BackupSize × MaxBackupCount) × 2`

The "×2" accounts for temporary files during verification.

**Example:**
- Database: 100 MB
- Keeping 10 backups
- Space needed: (100 × 10) × 2 = **2 GB**

**On S3:** Only pay for actual storage (no temporary files).

### Why is memory usage high?

Large databases require more memory. Solutions:

1. **Reduce `MaxConcurrentBackups`** to run fewer in parallel
2. **Enable compression** to reduce in-memory buffers
3. **Stream large files** (built-in, no configuration needed)
4. **Use S3 instead of local** (reduces temporary files)

## Troubleshooting Questions

### "Database is locked" error

**Cause:** Database is being written while backup runs.

**Solutions:**
1. Stop application writes during backup
2. Increase `BackupTimeoutSeconds`
3. Run backups during off-peak hours
4. Lower `MaxConcurrentBackups` to reduce contention

```json
{
  "AppSettings": {
    "BackupTimeoutSeconds": 7200,
    "MaxConcurrentBackups": 1
  },
  "Schedules": [{
    "CronExpression": "0 2 * * *"  // 2 AM
  }]
}
```

### "Backup not found" when listing

**Cause:** Backup was deleted by rotation policy or doesn't exist.

**Solutions:**
1. Check `RetentionDays` or `MaxBackupCount`
2. List all backups to see what exists
3. Increase retention if you need older backups

```bash
curl "http://localhost:5000/api/backup/list?scheduleId=daily-backup&limit=100"
```

### Verification always fails

**Common causes:**

1. **Database too large** (verification timeout):
   ```json
   {"AppSettings": {"BackupTimeoutSeconds": 7200}}
   ```

2. **Not enough disk space** for temporary restore:
   - Free up space on backup storage device

3. **Schema changed** after backup:
   - Normal; verify still passes, just reports schema diff

4. **SQLite compatibility** issue:
   - Ensure backup database is readable: `sqlite3 backup.db ".tables"`

### Application won't start

**Check:**
```bash
# 1. Configuration is valid JSON
json /path/to/appsettings.json  # or use IDE

# 2. Database path exists
ls -la /path/to/database.db

# 3. Backup directory is writable
touch /path/to/backups/test && rm /path/to/backups/test

# 4. Check logs
docker logs sqlite-backup 2>&1 | head -50
```

### S3 upload fails with "Access Denied"

**Solutions:**

1. **Verify credentials:**
   ```bash
   aws sts get-caller-identity  # Should show your AWS account
   ```

2. **Check S3 bucket policy:**
   ```bash
   aws s3api get-bucket-policy --bucket my-backups
   ```

3. **Verify IAM permissions:**
   ```json
   {
     "Version": "2012-10-17",
     "Statement": [{
       "Effect": "Allow",
       "Action": ["s3:*"],
       "Resource": "arn:aws:s3:::my-backups/*"
     }]
   }
   ```

## Development Questions

### How do I contribute?

1. Fork the repository on GitHub
2. Create a feature branch
3. Make changes following code style
4. Write tests for new features
5. Submit a pull request

See `CONTRIBUTING.md` for detailed guidelines.

### How do I run tests locally?

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "NamespaceName.TestClassName"

# Run with verbose output
dotnet test -v d

# Run with coverage
dotnet test /p:CollectCoverage=true
```

### How do I build and publish a release?

```bash
# Build for production
dotnet build -c Release

# Create Docker image
docker build -t sqlite-backup:1.2.0 .

# Push to registry
docker push docker.io/myrepo/sqlite-backup:1.2.0

# Create GitHub release
gh release create v1.2.0 --title "Version 1.2.0" --notes "Release notes..."
```

### Where is the documentation?

- **README.md**: Project overview and quick start
- **docs/getting-started.md**: Step-by-step setup guide
- **docs/architecture.md**: System design and components
- **docs/api-reference.md**: Complete HTTP API reference
- **docs/deployment.md**: Production deployment strategies
- **docs/faq.md**: This file

## Support Questions

### How do I report a bug?

1. **Check existing issues**: https://github.com/Sarmkadan/docker-sqlite-backup/issues
2. **Create a new issue** with:
   - Clear title and description
   - Steps to reproduce
   - Expected vs. actual behavior
   - Logs and error messages
   - Version and environment details

### How do I request a feature?

Create a GitHub issue with:
- Clear use case and motivation
- Proposed solution (if you have one)
- Alternative approaches you considered

### How do I get help?

1. **Check this FAQ** for common questions
2. **Review existing issues** for similar problems
3. **Check documentation** in docs/ directory
4. **Create a GitHub issue** if stuck

### Where can I see the roadmap?

Check the GitHub project board and issues for planned features and known limitations.

### Is there a changelog?

Yes! See `CHANGELOG.md` for version history and breaking changes.
