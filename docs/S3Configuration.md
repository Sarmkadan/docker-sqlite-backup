# S3Configuration

The `S3Configuration` class encapsulates the settings required to interact with an Amazon S3-compatible storage service for backup operations. It provides configuration options for authentication, bucket details, encryption, and object lifecycle management, enabling seamless integration with AWS S3 or custom S3-compatible endpoints.

## API

### `public string AccessKeyId`
The AWS access key ID used for authenticating requests to the S3 service. This value is required for all non-anonymous operations unless alternative authentication mechanisms (e.g., IAM roles) are in use.

### `public string SecretAccessKey`
The AWS secret access key corresponding to the `AccessKeyId`. This credential is used to sign requests and must be kept confidential. Required unless alternative authentication is configured.

### `public string BucketName`
The name of the S3 bucket where backup files will be stored. The bucket must exist and the configured credentials must have sufficient permissions (e.g., `s3:PutObject`, `s3:GetObject`).

### `public string RegionName`
The AWS region where the bucket is located (e.g., `us-east-1`). This value is used to construct the service endpoint. For custom endpoints, this may be ignored or used as a fallback.

### `public string ObjectKeyPrefix`
A prefix prepended to all object keys when storing files in the bucket. Useful for organizing backups under a common path (e.g., `backups/production/`). May be empty.

### `public bool UseSSL`
Specifies whether to use HTTPS (`true`) or HTTP (`false`) for requests. Defaults to `true` for security. Disable only for testing or trusted internal networks.

### `public bool EnableServerSideEncryption`
If `true`, enables AWS-managed server-side encryption (SSE-S3) for stored objects. Encryption keys are managed by AWS. Requires the bucket to have encryption enabled.

### `public string StorageClass`
The S3 storage class for stored objects (e.g., `STANDARD`, `GLACIER`, `REDUCED_REDUNDANCY`). Defaults to `STANDARD` if not specified. Note that lifecycle transitions (e.g., to Glacier) may override this value.

### `public string? CustomEndpoint`
An optional custom endpoint URL for S3-compatible services (e.g., MinIO, Wasabi). If provided, this overrides the default AWS endpoint. Example: `"https://s3.example.com"`.

### `public int? TransitionToGlacierDays`
The number of days after object creation before transitioning to the Glacier storage class. If `null`, no transition is configured. Requires the bucket to have a lifecycle policy supporting transitions.

### `public override bool IsValid`
Validates the configuration by checking for required fields (`AccessKeyId`, `SecretAccessKey`, `BucketName`, `RegionName`) and logical consistency (e.g., `TransitionToGlacierDays` must be positive if set). Returns `true` if all checks pass; otherwise, `false`.

### `public override async Task<bool> TestConnectionAsync`
Asynchronously tests the connection to the S3 service by attempting to list objects in the configured bucket (limited to 1 object). Returns `true` if the operation succeeds, indicating valid credentials and permissions. Throws:
- `AmazonS3Exception` if the S3 service returns an error (e.g., invalid credentials, bucket not found).
- `ArgumentException` if the configuration is invalid (see `IsValid`).
- `HttpRequestException` for network-related failures.
