#!/bin/bash
# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================
# Example 6: AWS S3 Backup Setup
# Configure Docker SQLite Backup to store backups on AWS S3 with encryption.

set -e

AWS_PROFILE="${1:-default}"
BUCKET_NAME="${2:-}"
AWS_REGION="${3:-us-east-1}"

echo "=== AWS S3 Backup Configuration ==="
echo ""

# Verify AWS CLI is installed
if ! command -v aws &> /dev/null; then
    echo "ERROR: AWS CLI is required. Install it from https://aws.amazon.com/cli/"
    exit 1
fi

# Get AWS credentials
echo "Checking AWS credentials..."
AWS_ACCOUNT=$(aws --profile "$AWS_PROFILE" sts get-caller-identity --query Account --output text)
AWS_USER=$(aws --profile "$AWS_PROFILE" sts get-caller-identity --query Arn --output text)
echo "✓ AWS Profile: $AWS_PROFILE"
echo "  Account: $AWS_ACCOUNT"
echo "  User: $AWS_USER"
echo ""

# Validate or create S3 bucket
if [ -z "$BUCKET_NAME" ]; then
    echo "Creating new S3 bucket..."
    BUCKET_NAME="sqlite-backups-$RANDOM"
    aws --profile "$AWS_PROFILE" s3 mb "s3://$BUCKET_NAME" --region "$AWS_REGION"
    echo "✓ Bucket created: $BUCKET_NAME"
else
    echo "Verifying bucket exists: $BUCKET_NAME"
    if aws --profile "$AWS_PROFILE" s3 ls "s3://$BUCKET_NAME" > /dev/null 2>&1; then
        echo "✓ Bucket exists: $BUCKET_NAME"
    else
        echo "ERROR: Bucket not found or not accessible: $BUCKET_NAME"
        exit 1
    fi
fi

echo ""

# Enable versioning
echo "Enabling S3 bucket versioning..."
aws --profile "$AWS_PROFILE" s3api put-bucket-versioning \
    --bucket "$BUCKET_NAME" \
    --versioning-configuration Status=Enabled
echo "✓ Versioning enabled"

# Enable encryption
echo "Enabling S3 server-side encryption..."
aws --profile "$AWS_PROFILE" s3api put-bucket-encryption \
    --bucket "$BUCKET_NAME" \
    --server-side-encryption-configuration '{
        "Rules": [{
            "ApplyServerSideEncryptionByDefault": {
                "SSEAlgorithm": "AES256"
            }
        }]
    }'
echo "✓ Encryption enabled"

# Block public access
echo "Blocking public access..."
aws --profile "$AWS_PROFILE" s3api put-public-access-block \
    --bucket "$BUCKET_NAME" \
    --public-access-block-configuration \
    "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true"
echo "✓ Public access blocked"

# Get AWS credentials
echo ""
echo "Retrieving AWS credentials..."
AWS_ACCESS_KEY=$(aws --profile "$AWS_PROFILE" configure get aws_access_key_id)
AWS_SECRET_KEY=$(aws --profile "$AWS_PROFILE" configure get aws_secret_access_key)

if [ -z "$AWS_ACCESS_KEY" ] || [ -z "$AWS_SECRET_KEY" ]; then
    echo "ERROR: Could not retrieve AWS credentials"
    echo "Create credentials at: https://console.aws.amazon.com/iam/home#/security_credentials"
    exit 1
fi

echo "✓ Credentials retrieved"
echo ""

# Create appsettings.json with S3 configuration
echo "Creating appsettings.json with S3 configuration..."
cat > appsettings.production.json <<EOF
{
  "AppSettings": {
    "DatabasePath": "app.db",
    "LocalStoragePath": "backups",
    "MaxConcurrentBackups": 2,
    "BackupTimeoutSeconds": 3600,
    "EnableVerificationByDefault": true,
    "RetentionDays": 90,
    "MaxBackupCount": 30
  },
  "Schedules": [
    {
      "Id": "daily-s3-backup",
      "Name": "Daily S3 Backup",
      "CronExpression": "0 2 * * *",
      "IsEnabled": true,
      "StorageType": "S3",
      "RotationStrategy": "Age",
      "RetentionDays": 90,
      "S3Config": {
        "BucketName": "$BUCKET_NAME",
        "Region": "$AWS_REGION",
        "AccessKeyId": "$AWS_ACCESS_KEY",
        "SecretAccessKey": "$AWS_SECRET_KEY",
        "EnableEncryption": true,
        "EnableVersioning": true,
        "StorageClass": "STANDARD_IA",
        "UseTransferAcceleration": false
      }
    },
    {
      "Id": "weekly-local-backup",
      "Name": "Weekly Local Backup",
      "CronExpression": "0 3 * * 0",
      "IsEnabled": true,
      "StorageType": "Local",
      "RotationStrategy": "Count",
      "MaxBackupCount": 4
    }
  ],
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
EOF

echo "✓ Created appsettings.production.json"
echo ""

# Create environment file for Docker
cat > .env.s3 <<EOF
ASPNETCORE_ENVIRONMENT=Production
AppSettings__DatabasePath=app.db
AppSettings__LocalStoragePath=backups
AppSettings__MaxConcurrentBackups=2
AppSettings__BackupTimeoutSeconds=3600
AppSettings__S3BucketName=$BUCKET_NAME
AppSettings__S3Region=$AWS_REGION
AWS_ACCESS_KEY_ID=$AWS_ACCESS_KEY
AWS_SECRET_ACCESS_KEY=$AWS_SECRET_KEY
AWS_DEFAULT_REGION=$AWS_REGION
EOF

echo "✓ Created .env.s3"
echo ""

# Create Docker run command
cat > run-s3-backup.sh <<EOF
#!/bin/bash
docker run -d \\
  --name sqlite-backup-s3 \\
  --env-file .env.s3 \\
  -v \$(pwd)/app.db:/app/app.db \\
  -v \$(pwd)/backups:/app/backups \\
  -p 5000:5000 \\
  sqlite-backup:latest
EOF

chmod +x run-s3-backup.sh
echo "✓ Created run-s3-backup.sh"
echo ""

# Test S3 bucket access
echo "Testing S3 bucket access..."
TEST_FILE=$(mktemp)
echo "test" > "$TEST_FILE"
aws --profile "$AWS_PROFILE" s3 cp "$TEST_FILE" "s3://$BUCKET_NAME/test-file.txt" > /dev/null 2>&1
aws --profile "$AWS_PROFILE" s3 rm "s3://$BUCKET_NAME/test-file.txt" > /dev/null 2>&1
rm "$TEST_FILE"
echo "✓ S3 bucket access verified"
echo ""

# Summary
echo "=== S3 Configuration Summary ==="
echo "Bucket: $BUCKET_NAME"
echo "Region: $AWS_REGION"
echo "Versioning: Enabled"
echo "Encryption: AES256"
echo "Public Access: Blocked"
echo ""
echo "Configuration files created:"
echo "  - appsettings.production.json (with S3 settings)"
echo "  - .env.s3 (environment variables)"
echo "  - run-s3-backup.sh (Docker run script)"
echo ""
echo "Next steps:"
echo "  1. Review configurations and adjust retention as needed"
echo "  2. Build Docker image: docker build -t sqlite-backup:latest ."
echo "  3. Run with S3: ./run-s3-backup.sh"
echo "  4. Check logs: docker logs sqlite-backup-s3"
echo "  5. View backups: aws s3 ls s3://$BUCKET_NAME --recursive"
echo ""
echo "Security best practices:"
echo "  - Store .env.s3 securely (not in git)"
echo "  - Use IAM role instead of access keys (if on EC2/ECS)"
echo "  - Rotate access keys regularly"
echo "  - Enable MFA Delete for additional protection"
echo ""
echo "Cost optimization:"
echo "  - Use STANDARD_IA for infrequent access"
echo "  - Set S3 lifecycle policies to move old backups to Glacier"
echo "  - Monitor bucket size: aws s3 ls s3://$BUCKET_NAME --summarize --human-readable --recursive"
