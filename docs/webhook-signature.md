# Webhook HMAC-SHA256 Signature

This document describes the HMAC-SHA256 signature feature for webhook payloads in docker-sqlite-backup.

## Overview

All webhook notifications sent by the `WebhookClient` class are signed using HMAC-SHA256 when a webhook secret is configured. This provides:

- **Integrity verification**: Ensures the payload hasn't been tampered with in transit
- **Authentication**: Proves the webhook originated from your docker-sqlite-backup instance
- **Non-repudiation**: The sender cannot deny sending the webhook

## Signature Format

The signature header uses the following format:

```
X-Signature: t=<timestamp>,v1=<hex-hmac-sha256>
```

Where:
- `t`: Unix timestamp (seconds since epoch) when the signature was generated
- `v1`: HMAC-SHA256 signature in hexadecimal format

## Configuration

### Option 1: Via Environment Variable

Set the `BACKUP_WEBHOOK_SECRET` environment variable:

```bash
export BACKUP_WEBHOOK_SECRET="your-secure-webhook-secret-here"
```

### Option 2: Via appsettings.json

Add the `WebhookSecret` property to your `AppSettings` configuration:

```json
{
  "AppSettings": {
    "DatabasePath": "backups.sqlite",
    "MaxConcurrentBackups": 3,
    "WebhookSecret": "your-secure-webhook-secret-here",
    ...
  }
}
```

## Secret Recommendations

- Use a **randomly generated string** of at least 32 characters
- Store it securely (environment variable recommended)
- Keep it **private** - anyone with this secret can forge webhooks
- Rotate periodically for security

Example of generating a secure secret:

```bash
# Using openssl
openssl rand -base64 32

# Or using pwgen
pwgen -s 40 1
```

## Verification

To verify the signature on the receiving end:

### Python Example

```python
import hmac
import hashlib
import json
from datetime import datetime

def verify_webhook_signature(payload_bytes, signature_header, secret):
    """
    Verify the HMAC-SHA256 signature.

    Args:
        payload_bytes: Raw bytes of the JSON payload
        signature_header: The X-Signature header value
        secret: Your shared webhook secret

    Returns:
        bool: True if signature is valid
    """
    # Parse signature header
    if not signature_header.startswith("t=") or ",v1=" not in signature_header:
        return False

    parts = signature_header.split(",v1=")
    if len(parts) != 2:
        return False

    timestamp_part = parts[0]
    received_signature = parts[1]

    # Verify timestamp is recent (within 5 minutes)
    try:
        timestamp = int(timestamp_part[2:]) # Remove "t="
        timestamp_dt = datetime.fromtimestamp(timestamp)
        now = datetime.utcnow()
        time_diff = abs((now - timestamp_dt).total_seconds())
        if time_diff > 300: # 5 minutes
            return False
    except (ValueError, IndexError):
        return False

    # Generate expected signature
    expected_signature = hmac.new(
        secret.encode('utf-8'),
        payload_bytes,
        hashlib.sha256
    ).hexdigest()

    # Constant-time comparison to prevent timing attacks
    return hmac.compare_digest(expected_signature, received_signature)

# Usage example
secret = "your-shared-secret"
payload = '{"eventType":"backup.completed","timestamp":"2024-07-21T12:00:00Z","data":{...}}'
payload_bytes = payload.encode('utf-8')
signature_header = "t=1721553600,v1=abc123..."

is_valid = verify_webhook_signature(payload_bytes, signature_header, secret)
print(f"Signature valid: {is_valid}")
```

### Node.js Example

```javascript
const crypto = require('crypto');

function verifyWebhookSignature(payload, signatureHeader, secret) {
    // Parse signature header
    if (!signatureHeader.startsWith('t=') || !signatureHeader.includes(',v1=')) {
        return false;
    }

    const parts = signatureHeader.split(',v1=');
    if (parts.length !== 2) {
        return false;
    }

    const timestampPart = parts[0];
    const receivedSignature = parts[1];

    // Verify timestamp is recent
    const timestamp = parseInt(timestampPart.substring(2), 10); // Remove "t="
    const timestampDate = new Date(timestamp * 1000);
    const now = new Date();
    const timeDiff = Math.abs(now - timestampDate) / 1000;

    if (timeDiff > 300) { // 5 minutes
        return false;
    }

    // Generate expected signature
    const expectedSignature = crypto
        .createHmac('sha256', secret)
        .update(payload)
        .digest('hex');

    // Constant-time comparison
    return crypto.timingSafeEqual(
        Buffer.from(expectedSignature, 'hex'),
        Buffer.from(receivedSignature, 'hex')
    );
}

// Usage example
const secret = 'your-shared-secret';
const payload = '{"eventType":"backup.completed","timestamp":"2024-07-21T12:00:00Z","data":{...}}';
const signatureHeader = 't=1721553600,v1=abc123...';

const isValid = verifyWebhookSignature(payload, signatureHeader, secret);
console.log(`Signature valid: ${isValid}`);
```

### Go Example

```go
package main

import (
    "crypto/hmac"
    "crypto/sha256"
    "encoding/hex"
    "strconv"
    "strings"
    "time"
)

func verifyWebhookSignature(payload []byte, signatureHeader string, secret string) bool {
    // Parse signature header
    if !strings.HasPrefix(signatureHeader, "t=") || !strings.Contains(signatureHeader, ",v1=") {
        return false
    }

    parts := strings.SplitN(signatureHeader, ",v1=", 2)
    if len(parts) != 2 {
        return false
    }

    timestampPart := parts[0]
    receivedSignature := parts[1]

    // Verify timestamp is recent
    timestampStr := strings.TrimPrefix(timestampPart, "t=")
    timestamp, err := strconv.ParseInt(timestampStr, 10, 64)
    if err != nil {
        return false
    }

    timestampTime := time.Unix(timestamp, 0)
    now := time.Now().UTC()
    timeDiff := now.Sub(timestampTime).Abs().Seconds()

    if timeDiff > 300 { // 5 minutes
        return false
    }

    // Generate expected signature
    mac := hmac.New(sha256.New, []byte(secret))
    mac.Write(payload)
    expectedSignature := hex.EncodeToString(mac.Sum(nil))

    // Constant-time comparison
    return hmac.Equal([]byte(expectedSignature), []byte(receivedSignature))
}
```

## Security Considerations

1. **Always verify signatures** on the receiving end to prevent forged requests
2. **Use HTTPS** for all webhook endpoints to prevent interception
3. **Rotate secrets** periodically (e.g., every 90 days)
4. **Validate timestamps** to prevent replay attacks (accept only recent timestamps)
5. **Log verification failures** for security monitoring

## Integration Example

### Sending Service (docker-sqlite-backup)

```csharp
// Configure with secret from environment variable
var settings = new AppSettings
{
    WebhookSecret = Environment.GetEnvironmentVariable("BACKUP_WEBHOOK_SECRET")
};

var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<WebhookClient>();
var webhookClient = new WebhookClient(logger, settings);

// Send backup notification
await webhookClient.SendBackupNotificationAsync(
    "https://your-webhook-receiver.example.com/api/webhooks/backup",
    backupResult
);
```

### Receiving Service

```csharp
[HttpPost("webhooks/backup")]
public async Task<IActionResult> ReceiveBackupWebhook(
    [FromHeader(Name = "X-Signature")] string signatureHeader,
    [FromBody] JsonElement payload)
{
    var secret = Configuration["WebhookSecret"];
    var payloadJson = payload.GetRawText();

    if (!VerifySignature(payloadJson, signatureHeader, secret))
    {
        return Unauthorized("Invalid signature");
    }

    // Process the webhook...
    return Ok();
}

private bool VerifySignature(string payload, string signatureHeader, string secret)
{
    if (string.IsNullOrEmpty(signatureHeader))
    {
        return false;
    }

    // Parse and verify signature (implementation depends on your language)
    // ...
}
```

## Testing

You can test the signature generation with:

```bash
# Generate a test payload
cat > /tmp/payload.json << EOF
{
  "eventType": "backup.completed",
  "timestamp": "2024-07-21T12:00:00Z",
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "scheduleId": "550e8400-e29b-41d4-a716-446655440001",
    "status": 0,
    "backupFilePath": "/backups/db-2024-07-21T120000Z.sqlite",
    "backupFileSizeBytes": 1048576,
    "checksum": "a1b2c3..."
  }
}
EOF

# Generate signature (using Python example above)
python3 -c "
import hmac, hashlib, json, sys
secret = 'test-secret-key-1234567890'
with open('/tmp/payload.json') as f:
    payload = f.read()
mac = hmac.new(secret.encode(), payload.encode(), hashlib.sha256).hexdigest()
timestamp = 1721553600 # Fixed timestamp for testing
print(f'X-Signature: t={timestamp},v1={mac}')
"
```

## Troubleshooting

### No signature header is being sent

**Possible causes:**
1. Webhook secret is not configured
2. Secret is empty or null
3. Constructor was called without the secret parameter

**Solution:** Ensure `WebhookSecret` is set in your configuration.

### Signature verification fails

**Common issues:**
1. **Secret mismatch**: The secret used to generate the signature doesn't match the one used for verification
2. **Payload modification**: The payload was altered after signing (e.g., pretty-printing changes JSON structure)
3. **Timestamp too old**: The signature is more than 5 minutes old
4. **Encoding mismatch**: Different character encodings were used (ensure UTF-8)
5. **Whitespace differences**: Extra whitespace in JSON payload

**Solution:** Ensure consistent secret, payload format, and timestamp validation.

### HMAC-SHA256 vs other algorithms

This implementation uses **HMAC-SHA256** specifically because:
- SHA256 provides strong cryptographic security
- HMAC provides keyed-hash authentication (prevents length-extension attacks)
- Widely supported across programming languages
- FIPS 140-2 compliant

Do not use weaker algorithms like MD5 or SHA1 for security-sensitive applications.

## References

- [RFC 2104 - HMAC: Keyed-Hashing for Message Authentication](https://tools.ietf.org/html/rfc2104)
- [FIPS 180-4 - Secure Hash Standard (SHA-256)](https://csrc.nist.gov/publications/detail/fips/180-4/final)
- [OWASP - Webhook Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Webhook_Security_Cheat_Sheet.html)