1 #!/bin/bash
2
3 # v2 Basic Usage Example
4 # This script demonstrates basic usage of docker-sqlite-backup v2 features
5
6 set -e
7
8 echo "Docker SQLite Backup v2 Basic Usage Example"
9 echo "=========================================="
10
11 # Create a test database
12 echo "Creating test database..."
13 mkdir -p data backups
14
15 # Create a simple SQLite database with some test data
16 sqlite3 data/test.db "CREATE TABLE IF NOT EXISTS users (id INTEGER PRIMARY KEY, name TEXT, email TEXT);"
17 sqlite3 data/test.db "INSERT INTO users (name, email) VALUES ('John Doe', 'john@example.com');"
18 sqlite3 data/test.db "INSERT INTO users (name, email) VALUES ('Jane Smith', 'jane@example.com');"
19
20 echo "Test database created with sample data"
21
22 # Create docker-compose.yml for this example
23 cat > docker-compose.yml << 'EOF'
24 version: '3.8'
25
26 services:
27   sqlite-backup:
28     image: sarmkadan/docker-sqlite-backup:latest
29     container_name: sqlite-backup-example
30     restart: unless-stopped
31     environment:
32       - AppSettings__DatabasePath=/data/test.db
33       - AppSettings__LocalStoragePath=/backups
34       - AppSettings__RetentionDays=7
35       - Schedules__0__Id=daily-backup
36       - Schedules__0__Name=Daily Backup
37       - Schedules__0__CronExpression=0 2 * * *
38       - Schedules__0__IsEnabled=true
39       - Schedules__0__StorageType=Local
40     volumes:
41       - ./data:/data:ro
42       - ./backups:/backups
43     ports:
44       - "8080:8080"
45     healthcheck:
46       test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8080/health"]
47       interval: 30s
48       timeout: 10s
49       retries: 3
50       start_period: 30s
51 EOF
52
53 echo "Starting Docker SQLite Backup container..."
54
55 # Start the service
56 docker-compose up -d
57
57 # Wait a moment for the service to start
58 sleep 10
59
60 # Check if the container is running
61 if docker ps | grep -q sqlite-backup-example; then
62     echo "Container is running successfully"
63 else
64     echo "Error: Container failed to start"
65     docker-compose logs
66     exit 1
67 fi
68
69 # Test the health endpoint
70 echo "Checking health status..."
71 if curl -f http://localhost:8080/health; then
72     echo "Health check passed"
73 else
74     echo "Health check failed"
75     exit 1
76 fi
77
78 # Trigger a manual backup
79 echo "Triggering manual backup..."
80 curl -X POST http://localhost:8080/api/backup/trigger \
81     -H "Content-Type: application/json" \
82     -d '{"scheduleId": "daily-backup", "storageType": "Local"}'
83
84 echo "Checking backup history..."
85 curl http://localhost:8080/api/backup/list?scheduleId=daily-backup
86
87 echo "Example completed successfully!"
88 echo "To clean up, run: docker-compose down"