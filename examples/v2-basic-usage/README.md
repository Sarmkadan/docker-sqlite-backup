1 # Docker SQLite Backup v2 Basic Usage Example
2
3 This example demonstrates the basic usage of Docker SQLite Backup v2.
4
5 ## What's Included
6
7 - A simple test database with sample data
8 - A docker-compose configuration with one daily backup schedule
9 - A script to run the example and demonstrate API usage
10
11 ## Prerequisites
12
13 - Docker and Docker Compose installed
14 - SQLite3 (for creating test database)
15 - curl (for API testing)
16
17 ## Running the Example
18
19 1. Make the script executable:
20    ```bash
21    chmod +x run-example.sh
22    ```
23
24 2. Run the script:
25    ```bash
26    ./run-example.sh
27    ```
28
29 The script will:
30 - Create a test SQLite database with sample data
31 - Start the docker-sqlite-backup container
32 - Trigger a manual backup via the API
33 - Display backup history
34 - Show health status
35
36 ## Clean Up
37
38 To clean up after testing:
39    ```bash
40    docker-compose down
41    ```
42
43 ## Customization
44
45 You can modify the docker-compose.yml file to:
46 - Change backup schedules
47 - Adjust retention policies
48 - Configure different storage backends
49 - Add S3 configuration