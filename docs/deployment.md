# Deployment Guide

Production-grade deployment strategies for Docker SQLite Backup across different environments.

## Docker Deployment

### Building the Docker Image

```bash
# Build with default tag
docker build -t sqlite-backup:latest .

# Build with version tag
docker build -t sqlite-backup:1.2.0 .

# Build with multiple tags
docker build \
  -t sqlite-backup:latest \
  -t sqlite-backup:1.2.0 \
  -t docker.io/myrepo/sqlite-backup:latest \
  .
```

### Running as a Container

```bash
# Basic run
docker run -d \
  --name sqlite-backup \
  -v /path/to/database:/data \
  -v /path/to/backups:/app/backups \
  -e AppSettings__DatabasePath=/data/app.db \
  -p 5000:5000 \
  sqlite-backup:latest

# View logs
docker logs -f sqlite-backup

# Stop container
docker stop sqlite-backup

# Remove container
docker rm sqlite-backup
```

### Docker Compose Setup

Use `docker-compose.yml` for local development and testing:

```bash
# Start services
docker-compose up -d

# View logs
docker-compose logs -f sqlite-backup

# Stop all services
docker-compose down

# Remove volumes (careful!)
docker-compose down -v
```

### Multi-Stage Build for Optimization

The Dockerfile uses multi-stage builds to minimize image size:

```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /build
COPY . .
RUN dotnet publish -c Release -o /app

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=builder /app .
ENTRYPOINT ["dotnet", "docker-sqlite-backup.dll"]
```

## Kubernetes Deployment

### Prerequisites

- Kubernetes cluster (v1.20+)
- kubectl configured
- PersistentVolume provisioner (NFS, EBS, etc.)

### Storage Setup

Create persistent volumes for database and backups:

```yaml
apiVersion: v1
kind: PersistentVolume
metadata:
  name: sqlite-db-pv
spec:
  capacity:
    storage: 100Gi
  accessModes:
    - ReadWriteOnce
  nfs:
    server: nfs-server.example.com
    path: /exports/sqlite-db

---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: sqlite-db-pvc
  namespace: default
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 100Gi
  volumeName: sqlite-db-pv
```

### Deployment Manifest

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: sqlite-backup
  labels:
    app: sqlite-backup
spec:
  replicas: 1
  selector:
    matchLabels:
      app: sqlite-backup
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  template:
    metadata:
      labels:
        app: sqlite-backup
    spec:
      serviceAccountName: sqlite-backup
      containers:
      - name: sqlite-backup
        image: sqlite-backup:1.2.0
        imagePullPolicy: IfNotPresent
        ports:
        - containerPort: 5000
          name: http
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: AppSettings__DatabasePath
          value: /data/app.db
        - name: AppSettings__LocalStoragePath
          value: /backups
        - name: AppSettings__MaxConcurrentBackups
          value: "2"
        - name: AppSettings__BackupTimeoutSeconds
          value: "3600"
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
          failureThreshold: 3
        readinessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
          failureThreshold: 2
        volumeMounts:
        - name: database
          mountPath: /data
        - name: backups
          mountPath: /backups
      volumes:
      - name: database
        persistentVolumeClaim:
          claimName: sqlite-db-pvc
      - name: backups
        persistentVolumeClaim:
          claimName: sqlite-backups-pvc
```

### Service Manifest

```yaml
apiVersion: v1
kind: Service
metadata:
  name: sqlite-backup
  labels:
    app: sqlite-backup
spec:
  type: ClusterIP
  ports:
  - port: 80
    targetPort: 5000
    protocol: TCP
    name: http
  selector:
    app: sqlite-backup
```

### ServiceAccount and RBAC

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: sqlite-backup

---
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: sqlite-backup
rules:
- apiGroups: [""]
  resources: ["configmaps"]
  verbs: ["get", "list", "watch"]
- apiGroups: [""]
  resources: ["secrets"]
  verbs: ["get"]

---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: sqlite-backup
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: Role
  name: sqlite-backup
subjects:
- kind: ServiceAccount
  name: sqlite-backup
```

### Deploy to Kubernetes

```bash
# Create namespace
kubectl create namespace sqlite-backup

# Create secrets for S3
kubectl create secret generic s3-credentials \
  --from-literal=AWS_ACCESS_KEY_ID=your_key \
  --from-literal=AWS_SECRET_ACCESS_KEY=your_secret \
  -n sqlite-backup

# Apply manifests
kubectl apply -f deployment.yaml -n sqlite-backup

# Verify deployment
kubectl get pods -n sqlite-backup
kubectl logs -f deployment/sqlite-backup -n sqlite-backup

# Port forward for testing
kubectl port-forward svc/sqlite-backup 5000:80 -n sqlite-backup
```

## AWS EC2 Deployment

### Prerequisites

- EC2 instance with Ubuntu 22.04 LTS
- .NET 10 runtime installed
- IAM role with S3 access (for backup storage)

### Installation Steps

```bash
# SSH into instance
ssh ubuntu@your-instance-ip

# Install .NET 10
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --version latest --install-dir /usr/local/bin

# Add to PATH
echo 'export PATH=$PATH:/usr/local/bin' >> ~/.bashrc
source ~/.bashrc

# Clone repository
git clone https://github.com/Sarmkadan/docker-sqlite-backup.git
cd docker-sqlite-backup

# Restore and build
dotnet restore
dotnet build -c Release

# Create systemd service
sudo tee /etc/systemd/system/sqlite-backup.service > /dev/null <<EOF
[Unit]
Description=SQLite Backup Service
After=network.target

[Service]
Type=simple
User=ubuntu
WorkingDirectory=/home/ubuntu/docker-sqlite-backup
ExecStart=/usr/local/bin/dotnet run --configuration Release
Restart=on-failure
RestartSec=10

Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="AppSettings__DatabasePath=/data/app.db"

[Install]
WantedBy=multi-user.target
EOF

# Enable and start service
sudo systemctl daemon-reload
sudo systemctl enable sqlite-backup
sudo systemctl start sqlite-backup

# Check status
sudo systemctl status sqlite-backup

# View logs
sudo journalctl -u sqlite-backup -f
```

### Auto-Scaling Setup

Use AWS Auto Scaling with Application Load Balancer:

```bash
# Create launch template
aws ec2 create-launch-template \
  --launch-template-name sqlite-backup \
  --version-description "SQLite Backup v1.2.0" \
  --launch-template-data '{
    "ImageId": "ami-0c55b159cbfafe1f0",
    "InstanceType": "t3.medium",
    "IamInstanceProfile": {"Name": "sqlite-backup-role"},
    "UserData": "base64-encoded-startup-script"
  }'

# Create Auto Scaling group
aws autoscaling create-auto-scaling-group \
  --auto-scaling-group-name sqlite-backup-asg \
  --launch-template LaunchTemplateName=sqlite-backup \
  --min-size 1 \
  --max-size 3 \
  --desired-capacity 2
```

## Docker Swarm Deployment

For smaller deployments, Docker Swarm is simpler than Kubernetes:

```bash
# Initialize swarm (on manager node)
docker swarm init

# Create service
docker service create \
  --name sqlite-backup \
  --replicas 2 \
  --publish 5000:5000 \
  --env ASPNETCORE_ENVIRONMENT=Production \
  --mount type=bind,source=/data,target=/data \
  sqlite-backup:latest

# View service
docker service ls
docker service ps sqlite-backup

# Update service
docker service update \
  --image sqlite-backup:1.2.1 \
  sqlite-backup

# Scale service
docker service scale sqlite-backup=3
```

## Production Configuration

### Environment Variables

```bash
# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:5000
ASPNETCORE_HTTPS_PORT=5001

# Application
AppSettings__DatabasePath=/data/app.db
AppSettings__LocalStoragePath=/backups
AppSettings__MaxConcurrentBackups=2
AppSettings__BackupTimeoutSeconds=3600
AppSettings__EnableVerificationByDefault=true
AppSettings__RetentionDays=30
AppSettings__MaxBackupCount=10

# Logging
Logging__LogLevel__Default=Information
Logging__LogLevel__System=Warning
Logging__LogLevel__Microsoft=Warning

# S3 Configuration
AppSettings__S3BucketName=company-backups
AppSettings__S3Region=us-east-1
AWS_ACCESS_KEY_ID=${AWS_KEY}
AWS_SECRET_ACCESS_KEY=${AWS_SECRET}

# Notifications
NotificationSettings__WebhookUrl=https://alerts.company.com/backup
NotificationSettings__NotifyOnFailure=true
```

### Health Checks

Configure monitoring to check `/health` endpoint:

```bash
# Prometheus scrape config
scrape_configs:
  - job_name: 'sqlite-backup'
    static_configs:
      - targets: ['localhost:5000']
    metrics_path: '/metrics'
```

### Backup Strategies for Production

#### Strategy 1: Hot Standby (High Availability)

Run multiple backup instances with shared storage:

```yaml
# Kubernetes
kind: Deployment
metadata:
  name: sqlite-backup
spec:
  replicas: 2  # Run 2 instances
  volumeClaimTemplates:
  - metadata:
      name: backups
    spec:
      accessModes: ["ReadWriteMany"]  # Shared storage
      storage: 1Ti
```

#### Strategy 2: Primary + Replica

Primary handles backups, replica provides failover:

```bash
# Primary
AppSettings__IsPrimary=true
AppSettings__ReplicaUrl=http://replica:5000

# Replica
AppSettings__IsPrimary=false
AppSettings__PrimaryUrl=http://primary:5000
```

#### Strategy 3: Tiered Storage

Local for recent backups, S3 for archive:

```json
{
  "Schedules": [
    {
      "Id": "daily-local",
      "CronExpression": "0 2 * * *",
      "StorageType": "Local",
      "MaxBackupCount": 7
    },
    {
      "Id": "weekly-s3",
      "CronExpression": "0 3 * * 0",
      "StorageType": "S3",
      "RetentionDays": 365
    }
  ]
}
```

## Monitoring & Alerting

### Prometheus Metrics

```yaml
# prometheus.yml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: 'sqlite-backup'
    static_configs:
      - targets: ['localhost:5000']
    metrics_path: '/metrics'
```

### Grafana Dashboard

Import pre-built dashboards or create custom ones:

```json
{
  "dashboard": {
    "title": "SQLite Backup",
    "panels": [
      {
        "title": "Backup Success Rate",
        "targets": [
          {
            "expr": "rate(backup_total_count{status=\"success\"}[1h])"
          }
        ]
      },
      {
        "title": "Average Backup Duration",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, backup_duration_seconds)"
          }
        ]
      }
    ]
  }
}
```

### Log Aggregation

Configure centralized logging with ELK stack:

```yaml
# Filebeat config
filebeat.inputs:
- type: log
  enabled: true
  paths:
    - /var/log/sqlite-backup/*.log

output.elasticsearch:
  hosts: ["elasticsearch:9200"]
```

## Disaster Recovery

### Backup Verification

Enable automatic verification to catch corrupted backups:

```json
{
  "AppSettings": {
    "EnableVerificationByDefault": true
  }
}
```

### Restore Testing

Periodically test restores to ensure recoverability:

```bash
#!/bin/bash
# Weekly restore test
0 0 * * 0 /usr/local/bin/test-restore.sh

# test-restore.sh
BACKUP=$(sqlite-backup latest-backup)
sqlite-backup restore \
  --backup $BACKUP \
  --output /tmp/test-restore.db \
  --verify
```

### Point-in-Time Recovery

Keep multiple backup schedules for different retention:

```json
{
  "Schedules": [
    {
      "Id": "hourly-7days",
      "CronExpression": "0 * * * *",
      "RetentionDays": 7
    },
    {
      "Id": "daily-30days",
      "CronExpression": "0 2 * * *",
      "RetentionDays": 30
    },
    {
      "Id": "weekly-1year",
      "CronExpression": "0 3 * * 0",
      "RetentionDays": 365
    }
  ]
}
```

## Security Best Practices

### Secrets Management

Use environment-specific secret management:

```bash
# Kubernetes Secrets
kubectl create secret generic s3-creds \
  --from-literal=AccessKeyId=${AWS_KEY} \
  --from-literal=SecretAccessKey=${AWS_SECRET}

# AWS Secrets Manager
aws secretsmanager create-secret \
  --name sqlite-backup/s3 \
  --secret-string '{"key":"...","secret":"..."}'
```

### Network Security

Use network policies and firewall rules:

```yaml
# Kubernetes NetworkPolicy
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: sqlite-backup-deny-all
spec:
  podSelector:
    matchLabels:
      app: sqlite-backup
  policyTypes:
  - Ingress
  ingress:
  - from:
    - podSelector:
        matchLabels:
          role: monitoring
```

### Access Control

Implement role-based access control:

```bash
# AWS IAM Policy for S3 backups
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject",
        "s3:ListBucket"
      ],
      "Resource": "arn:aws:s3:::company-backups/*"
    }
  ]
}
```

## Troubleshooting Deployments

### Container fails to start

```bash
# Check logs
docker logs sqlite-backup

# Verify configuration
docker run --rm sqlite-backup:latest \
  dotnet docker-sqlite-backup.dll --validate-config

# Check permissions
docker run -it --rm \
  -v /path/to/db:/data \
  sqlite-backup:latest \
  ls -la /data
```

### Kubernetes pod crashing

```bash
# Describe pod
kubectl describe pod sqlite-backup-xxx

# View logs
kubectl logs sqlite-backup-xxx
kubectl logs sqlite-backup-xxx --previous  # Previous crashed instance

# Get events
kubectl get events
```

### Health checks failing

```bash
# Manual health check
curl http://localhost:5000/health -v

# Check detailed health
curl http://localhost:5000/health/detailed | jq .

# View application logs
docker logs sqlite-backup | grep -i health
```
