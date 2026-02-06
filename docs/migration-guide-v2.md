1 # Migration Guide: v1.0 to v2.0
2 
3 This guide helps you migrate from Docker SQLite Backup v1.0.0 to v2.0.0.
4 
5 ## Overview
6 
7 v2.0.0 introduces significant improvements to Docker integration, enhanced health checks, and updated dependencies for .NET 10 compatibility. All public APIs remain stable, but deployment configurations have been refined.
8 
9 ## Key Changes
10 
11 ### Docker & Deployment
12 
13 - **Port Change**: Application now binds to port `8080` (previously `5000`)
14 - **Health Check Enhancement**: Improved HEALTHCHECK directive with wget fallback support
15 - **Alpine Optimization**: Updated base images to latest Alpine .NET 10 variants
16 - **Non-root User**: Application runs as `backup` user (UID 1000) for enhanced security
17 
18 ### Dependency Updates
19 
20 - All NuGet packages pinned to `.NET 10.0.0` releases
21 - AWS SDK.S3 updated to `3.7.400.1` (latest stable)
22 - Cronos scheduler remains at `0.13.0` (stable)
23 
24 ### Configuration Changes
25 
26 Environment variables remain compatible, but review the following:
27 
28 ```bash
29 # v2.0.0 docker-compose.yml now uses:
30 ASPNETCORE_URLS: "http://0.0.0.0:8080"
31 
32 # Update your port mappings:
33 ports:
34 - "8080:8080" # Changed from 5000:5000
34 ```
35 
36 ## Migration Steps
37 
38 ### 1. Update Docker Compose (if using local setup)
39 
40 Replace port `5000` with `8080`:
41 
42 ```yaml
43 # Before (v1.0)
44 ports:
45 - "5000:5000"
46 
47 # After (v2.0)
48 ports:
49 - "8080:8080"
50 ```
51 
52 Update health check endpoint:
53 
54 ```yaml
55 # Before (v1.0)
56 healthcheck:
57 test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
58 
59 # After (v2.0)
60 healthcheck:
61 test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8080/health"]
62 ```
63 
64 ### 2. Update Environment Variables
65 
66 If you're using Kubernetes or custom Docker configurations, update ASPNETCORE_URLS:
67 
68 ```bash
69 # Before
70 ASPNETCORE_URLS=http://0.0.0.0:5000
71 
72 # After
73 ASPNETCORE_URLS=http://0.0.0.0:8080
74 ```
75 
76 ### 3. Update Reverse Proxy Configuration
77 
78 If you're running behind Nginx, Caddy, or similar:
79 
80 ```nginx
81 # Nginx example (update upstream)
82 upstream sqlite_backup {
83 server localhost:8080; # Changed from 5000
84 }
85 ```
86 
87 ### 4. Update Monitoring & Alerting
88 
89 Update any monitoring rules that check the health endpoint:
90 
91 ```bash
92 # Old health check URL
93 curl http://localhost:5000/health
94 
95 # New health check URL
96 curl http://localhost:8080/health
97 ```
98 
99 ### 5. Firewall & Network Rules
100 
101 If you have firewall rules for port `5000`, update them to use port `8080`:
102 
103 ```bash
104 # Allow port 8080
105 sudo ufw allow 8080/tcp
106 ```
107 
108 ## Breaking Changes
109 
110 **Port Migration Required**
111 - The default port has changed from `5000` to `8080`
112 - Applications connecting to this service must update their connection URLs
113 - If you have external DNS records pointing to port 5000, update them
114 
115 ## Non-Breaking Changes
116 
117 - NuGet package names remain unchanged
118 - API endpoints remain the same (only port differs)
119 - Configuration schema is fully backward compatible
120 - S3 backup/restore functionality unchanged
121 
122 ## Rollback Procedure
123 
124 If you need to revert to v1.0.0:
125 
126 ```bash
127 # Update docker-compose.yml to use old image
128 image: sqlite-backup:1.0.0
129 
130 # Revert ports
131 ports:
132 - "5000:5000"
133 
134 # Revert environment
135 ASPNETCORE_URLS: "http://0.0.0.0:5000"
136 
137 # Rebuild and restart
138 docker-compose up -d --build
139 ```
140 
141 ## Troubleshooting
142 
143 ### Health Check Failures
144 
145 If health checks are failing after upgrade:
146 
147 ```bash
148 # Check if application is listening on 8080
149 docker exec sqlite-backup netstat -tlnp | grep 8080
150 
151 # Test health endpoint directly
152 docker exec sqlite-backup wget -O- http://localhost:8080/health
153 ```
154 
155 ### Port Already in Use
156 
157 If port 8080 is already in use:
158 
159 ```bash
160 # Find process using port 8080
161 sudo lsof -i :8080
162 
163 # Or in docker-compose.yml, map to different port:
164 ports:
165 - "9000:8080" # Map container 8080 to host 9000
166 ```
167 
168 ### Performance After Upgrade
169 
170 v2.0.0 includes Alpine optimization that may reduce memory footprint:
171 
172 ```bash
173 # Check container resource usage
174 docker stats sqlite-backup
175 ```