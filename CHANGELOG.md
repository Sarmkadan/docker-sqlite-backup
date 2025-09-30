# Changelog

All notable changes to Docker SQLite Backup are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-05-04

### Added
- **Kubernetes Support**: Official Kubernetes manifests with StatefulSet, RBAC, and persistent volumes
- **S3 Transfer Acceleration**: Optional transfer acceleration for faster uploads to S3
- **Webhook Retry Logic**: Automatic retry with exponential backoff for failed webhook deliveries
- **Detailed Health Checks**: New `/health/detailed` endpoint with component-level status
- **Backup Compression**: Optional gzip compression for reduced storage footprint
- **Multi-format Output**: Support for JSON, CSV, and XML output in CLI and API responses
- **Event Listener Framework**: Pluggable event system for custom integrations
- **Metrics Endpoint**: Prometheus-compatible `/metrics` endpoint for monitoring
- **Compliance Audit Tooling**: Audit scripts for regulatory compliance verification

### Changed
- **Improved CLI**: Enhanced command-line interface with better help and error messages
- **Performance**: Optimized backup streaming for large databases (>5GB)
- **Logging**: More structured logging with request IDs for tracing
- **Configuration**: Hierarchical configuration from multiple sources (JSON, env vars, CLI args)
- **Error Handling**: Comprehensive error codes and detailed error messages

### Fixed
- **Race Condition**: Fixed concurrent backup scheduling race condition
- **Memory Leak**: Resolved event listener memory leak in long-running deployments
- **S3 Timeout**: Increased default S3 timeout for large database uploads
- **Verification**: Fixed verification failing on databases with custom collations

### Deprecated
- Direct credential configuration in appsettings.json (use environment variables instead)

## [1.1.0] - 2026-02-15

### Added
- **AWS S3 Storage**: Full support for storing backups on AWS S3 with encryption
- **Restore API**: New endpoints to restore backups programmatically
- **Schedule Management**: API endpoints to enable/disable schedules at runtime
- **Rate Limiting**: Built-in rate limiting middleware for API protection
- **Audit Logging**: Comprehensive audit trail of all backup operations
- **Health Checks**: Liveness and readiness probes for orchestration systems
- **Docker Support**: Official Dockerfile with multi-stage builds

### Changed
- **Database Schema**: Updated backup metadata schema to support more attributes
- **API Response**: Standardized response envelope for all API endpoints
- **Default Retention**: Changed default retention from 7 days to 30 days

### Fixed
- **Notification**: Fixed webhook notification payload serialization
- **Rotation**: Fixed rotation policy not cleaning up all expired backups
- **CLI**: Fixed command-line argument parsing for complex values

### Security
- **HTTPS**: All S3 communications now use SSL/TLS encryption
- **Credentials**: AWS credentials no longer logged or exposed in errors
- **Validation**: Strict input validation on all API endpoints

## [1.0.0] - 2025-12-01

### Added
- **Core Backup Engine**: SQLite database backup with snapshot isolation
- **Scheduled Backups**: Full cron-based scheduling with timezone support
- **Local Storage**: Store backups on local filesystem with compression
- **Backup Rotation**: Automatic cleanup by age or count-based policies
- **Verification**: Automated backup integrity verification and testing
- **REST API**: Full HTTP API for backup management and monitoring
- **CLI Interface**: Command-line tools for scripts and automation
- **Configuration**: JSON-based configuration with environment variable overrides
- **Logging**: Structured logging with configurable levels
- **Exception Handling**: Global exception handling with user-friendly errors
- **Tests**: Comprehensive unit and integration test suite

### Features
- Automatic backup verification with record counting
- Checksum validation (SHA-256) for backup integrity
- Concurrent backup limiting to prevent resource exhaustion
- Configurable timeout protection for long-running operations
- Request context tracking for debugging
- Event-driven architecture for extensibility
- Dependency injection for testability
- Multiple storage backends (local, S3)
- Backup metadata persistence
- Human-readable error messages

---

## Version History Details

### [1.2.0] Release Highlights

**Kubernetes-First Architecture**: Full Kubernetes support with provided manifests, RBAC configuration, and persistent volume management. Deploy with confidence on any Kubernetes cluster.

**Production-Grade Monitoring**: Prometheus metrics endpoint, detailed health checks, and webhook retry logic for reliable notifications.

**Performance Improvements**: Optimized streaming for large databases, compression support to reduce storage costs, and transfer acceleration for cloud uploads.

**Better Developer Experience**: Enhanced CLI with multiple output formats, pluggable event listeners for custom integrations, and comprehensive compliance audit tooling.

### [1.1.0] Release Highlights

**Cloud Storage Ready**: AWS S3 support with full encryption, versioning, and lifecycle policy integration for enterprise-grade backup management.

**API Expansion**: Restore backups through API, manage schedules at runtime, monitor health with standardized endpoints.

**Enterprise Security**: Audit logging tracks every operation, rate limiting prevents abuse, credentials secured with environment variables.

### [1.0.0] Release Highlights

**Production Ready**: Comprehensive backup solution with scheduling, verification, rotation, and REST API in a single package.

---

## Upgrade Guides

### Upgrading from 1.0.0 to 1.1.0

**Breaking Changes**: None

**Migration Steps**:
1. Update configuration to use environment variables for S3 credentials
2. No database migration required
3. Existing backups remain compatible

```bash
# Update Docker image
docker pull docker.io/username/sqlite-backup:1.1.0

# Restart service
docker-compose up -d
```

### Upgrading from 1.1.0 to 1.2.0

**Breaking Changes**: None

**New Features**:
- Kubernetes deployment recommended over Docker Swarm for production
- New webhook retry logic (automatically enabled)
- Prometheus metrics available at `/metrics`

**Migration Steps**:
1. No database migration required
2. Optional: Update webhook configuration for retry logic
3. Optional: Add Prometheus scrape config for metrics

```bash
# Update Docker image
docker pull docker.io/username/sqlite-backup:1.2.0

# If using Kubernetes, apply new manifests
kubectl apply -f kubernetes/deployment.yaml

# Restart deployment
kubectl rollout restart deployment/sqlite-backup
```

---

## Future Roadmap

### Planned for 1.3.0
- [ ] Incremental backup support
- [ ] Backup encryption at-rest
- [ ] Database replication/failover
- [ ] Web UI dashboard
- [ ] Performance benchmarking tools

### Under Consideration
- [ ] MySQL/PostgreSQL support
- [ ] Google Cloud Storage backend
- [ ] Azure Blob Storage backend
- [ ] Backup branching/snapshots
- [ ] Cost optimization reports

---

## Support

For issues, questions, or contributions, visit:
- GitHub: https://github.com/Sarmkadan/docker-sqlite-backup
- Issues: https://github.com/Sarmkadan/docker-sqlite-backup/issues
- Discussions: https://github.com/Sarmkadan/docker-sqlite-backup/discussions
