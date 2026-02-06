1 # Docker Guide
2 
3 Comprehensive guide for running Docker SQLite Backup in production environments.
4 
5 ## Table of Contents
6 
7 - [Quick Start](#quick-start)
8 - [Docker Compose](#docker-compose)
9 - [Production Deployment](#production-deployment)
10 - [Environment Variables](#environment-variables)
11 - [Volume Management](#volume-management)
12 - [Network Configuration](#network-configuration)
13 - [Health Checks](#health-checks)
14 - [Logging](#logging)
15 - [Updating](#updating)
16 - [Security Best Practices](#security-best-practices)
17 - [Troubleshooting](#troubleshooting)
18 - [Performance Tuning](#performance-tuning)
19 - [Multi-Container Setup](#multi-container-setup)
20 - [Docker Compose Examples](#docker-compose-examples)
21 - [Kubernetes Deployment](#kubernetes-deployment)
22 - [Backup Storage Patterns](#backup-storage-patterns)
23 
24 ## Quick Start
25 
26 Get started with Docker SQLite Backup in under 5 minutes:
27 
28 ### 1. Create a docker-compose.yml file
29 
30 ```yaml
31 version: '3.8'
32 
33 services:
34   sqlite-backup:
35     image: sarmkadan/docker-sqlite-backup:latest
36     container_name: sqlite-backup
37     restart: unless-stopped
38     environment:
39       - AppSettings__DatabasePath=/data/production.db
40       - AppSettings__LocalStoragePath=/backups
41       - AppSettings__RetentionDays=30
42       - AppSettings__MaxConcurrentBackups=3
43     volumes:
44       - ./data:/data
45       - ./backups:/backups
46     ports:
47       - "8080:8080"
48     healthcheck:
49       test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8080/health"]
50       interval: 30s
51       timeout: 10s
52       retries: 3
53       start_period: 30s
50 ```
51 
52 ### 2. Start the container
53 
54 ```bash
55 docker-compose up -d
56 ```
57 
58 ### 3. Verify it's working
59 
60 ```bash
61 # Check container status
62 docker ps
63 
64 # Check health status
65 docker inspect --format='{{json .State.Health}}' sqlite-backup
66 
67 # View logs
68 docker logs sqlite-backup
69 
70 # Test health endpoint
71 curl http://localhost:8080/health
72 ```
73 
74 ### 4. Configure your backup schedule
75 
76 Edit `docker-compose.yml` to add schedules:
77 
78 ```yaml
79 services:
80   sqlite-backup:
81     # ... existing configuration ...
82     environment:
83       # ... existing environment ...
84       - Schedules__0__Id=daily-backup
85       - Schedules__0__Name=Daily Backup at 2 AM
86       - Schedules__0__CronExpression=0 2 * * *
87       - Schedules__0__IsEnabled=true
88       - Schedules__0__StorageType=Local
89       - Schedules__0__RotationStrategy=AgeAndCount
90 ```
91 
92 ### 5. Restart to apply changes
93 
94 ```bash
95 docker-compose down && docker-compose up -d
96 ```
97 
98 ## Docker Compose
99 
100 ### Basic Configuration
101 
101 The simplest working configuration:
102 
103 ```yaml
104 version: '3.8'
105 
106 services:
107   sqlite-backup:
108     image: sarmkadan/docker-sqlite-backup:latest
109     container_name: sqlite-backup
110     restart: unless-stopped
111     environment:
112       - AppSettings__DatabasePath=/data/app.db
113       - AppSettings__LocalStoragePath=/backups
114     volumes:
115       - ./data:/data
116       - ./backups:/backups
117     ports:
118       - "8080:8080"
119     healthcheck:
120       test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8080/health"]
121       interval: 30s
122       timeout: 10s
123       retries: 3
124       start_period: 30s
125 ```
126 
127 ### Development Configuration
128 
128 For development with hot-reload:
129 
130 ```yaml
131 version: '3.8'
132 
133 services:
134   sqlite-backup:
135     build:
136       context: .
137       dockerfile: Dockerfile
138     container_name: sqlite-backup-dev
139     restart: unless-stopped
140     environment:
141       - ASPNETCORE_ENVIRONMENT=Development
142       - AppSettings__DatabasePath=/data/dev.db
143       - AppSettings__LocalStoragePath=/backups
144       - Logging__LogLevel__Default=Debug
145     volumes:
146       - ./data:/data
147       - ./backups:/backups
148       - ./src:/app/src
149     ports:
150       - "8080:8080"
151     healthcheck:
151       test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8080/health"]
152       interval: 30s
153       timeout: 10s
154       retries: 3
155 ```
156 
157 ### Production Configuration
158 
158 For production with S3 storage:
159 
160 ```yaml
161 version: '3.8'
162 
163 services:
164   sqlite-backup:
165     image: sarmkadan/docker-sqlite-backup:latest
166     container_name: sqlite-backup-prod
167     restart: unless-stopped
168     environment:
169       - AppSettings__DatabasePath=/data/production.db
168       - AppSettings__LocalStoragePath=/backups
169       - AppSettings__RetentionDays=90
170       - AppSettings__MaxConcurrentBackups=2
171       - AppSettings__BackupTimeoutSeconds=7200
172       - Schedules__0__Id=daily-backup
173       - Schedules__0__Name=Daily Backup at 2 AM
174       - Schedules__0__CronExpression=0 2 * * *
175       - Schedules__0__IsEnabled=true
176       - Schedules__0__StorageType=S3
177       - Schedules__0__RotationStrategy=Age
178       - Schedules__0__RetentionDays=90
179       - Schedules__0__S3Config__BucketName=my-backups
180       - Schedules__0__S3Config__Region=us-east-1
181       - Schedules__0__S3Config__EnableEncryption=true
182     volumes:
183       - ./data:/data
184       - ./backups:/backups
184     ports:
185       - "8080:8080"
186     healthcheck:
187       test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8080/health"]
188       interval: 30s
189       timeout: 10s
190       retries: 3
191       start_period: 30s
192 ```
193 
194 ## Production Deployment
195 
196 ### Prerequisites
197 
198 - Docker 20.10+ (recommended 24.0+)
199 - Docker Compose 2.0+
200 - 512MB+ RAM (1GB+ recommended for production)
201 - 1 CPU core minimum
202 - 50MB+ disk space for the container
203 - Network access to your SQLite database
204 - For S3: AWS credentials configured
205 
206 ### Recommended Deployment Architecture
207 
208 ```
209 [Application with SQLite DB] ←→ [Docker SQLite Backup Container] ←→ [Backup Storage]
210                                      ↑
211                              [Monitoring System]
212 ```
213 
214 ### Deployment Checklist
215 
216 - [ ] Verify database file path is correct
217 - [ ] Set appropriate retention policy
218 - [ ] Configure storage backend (local or S3)
219 - [ ] Set up health checks
220 - [ ] Configure logging
221 - [ ] Set resource limits
222 - [ ] Configure backup schedules
223 - [ ] Test restore procedure
224 - [ ] Set up monitoring and alerts
225 
226 ## Environment Variables
227 
228 All configuration is done through environment variables. The application follows the standard ASP.NET Core configuration pattern using double-underscore (`__`) for nested properties.
229 
229 ### AppSettings Configuration
230 
231 | Environment Variable | Default | Description |
232 |-------------------|---------|-------------|
233 | AppSettings__DatabasePath | (required) | Path to SQLite database file inside container |
234 | AppSettings__LocalStoragePath | backups | Local backup directory |
235 | AppSettings__MaxConcurrentBackups | 3 | Maximum parallel backup operations |
236 | AppSettings__BackupTimeoutSeconds | 3600 | Timeout per backup operation in seconds |
237 | AppSettings__ScheduleCheckIntervalSeconds | 60 | How often to check schedules |
238 | AppSettings__EnableVerificationByDefault | true | Auto-verify all backups |
239 | AppSettings__RetentionDays | 30 | Default retention period in days |
240 | AppSettings__MaxBackupCount | 10 | Maximum backups to keep |
241 | AppSettings__CompressionEnabled | false | Enable backup compression |
242 | AppSettings__EnableMetrics | true | Enable Prometheus metrics endpoint |
243 
244 ### Schedule Configuration
245 
246 Schedules are configured as arrays with zero-based indices:
247 
248 | Environment Variable | Description |
249 |-------------------|-------------|
250 | Schedules__0__Id | Unique schedule identifier |
251 | Schedules__0__Name | Human-readable name |
252 | Schedules__0__CronExpression | Cron expression (e.g., "0 2 * * *") |
253 | Schedules__0__IsEnabled | Enable/disable schedule |
254 | Schedules__0__StorageType | Local or S3 |
255 | Schedules__0__RotationStrategy | Age, Count, or AgeAndCount |
256 | Schedules__0__RetentionDays | Retention days (if Age strategy) |
257 | Schedules__0__MaxBackupCount | Max backups to keep (if Count strategy) |
258 | Schedules__0__CompressionEnabled | Enable compression for this schedule |
259 
260 ### S3 Configuration (if using S3)
261 
262 | Environment Variable | Description |
263 |-------------------|-------------|
264 | Schedules__0__S3Config__BucketName | S3 bucket name |
265 | Schedules__0__S3Config__Region | AWS region |
266 | Schedules__0__S3Config__AccessKeyId | AWS access key |
267 | Schedules__0__S3Config__SecretAccessKey | AWS secret key |
268 | Schedules__0__S3Config__EnableEncryption | Enable server-side encryption |
269 | Schedules__0__S3Config__EnableVersioning | Enable versioning |
270 | Schedules__0__S3Config__StorageClass | S3 storage class |
271 
272 ### Logging Configuration
273 
274 | Environment Variable | Default | Description |
275 |-------------------|---------|-------------|
276 | Logging__LogLevel__Default | Information | Default log level |
277 | Logging__LogLevel__System | Warning | System log level |
278 | Logging__LogLevel__Microsoft | Warning | Microsoft log level |
279 
280 ### Example: Full Environment Configuration
281 
282 ```bash
283 # Database and storage
284 export AppSettings__DatabasePath=/data/production.db
285 export AppSettings__LocalStoragePath=/backups
286 export AppSettings__RetentionDays=90
287 
288 # Performance
289 export AppSettings__MaxConcurrentBackups=2
290 export AppSettings__BackupTimeoutSeconds=7200
291 
292 # Schedules (daily at 2 AM)
293 export Schedules__0__Id=daily-backup
294 export Schedules__0__Name="Daily Backup"
295 export Schedules__0__CronExpression="0 2 * * *"
296 export Schedules__0__IsEnabled=true
297 export Schedules__0__StorageType=Local
298 export Schedules__0__RotationStrategy=AgeAndCount
299 
299 # S3 Configuration (if using S3)
300 export Schedules__1__Id=weekly-s3
301 export Schedules__1__Name="Weekly S3 Backup"
302 export Schedules__1__CronExpression="0 3 * * 0"
303 export Schedules__1__IsEnabled=true
304 export Schedules__1__StorageType=S3
305 export Schedules__1__S3Config__BucketName=my-backups
306 export Schedules__1__S3Config__Region=us-east-1
307 ```
308 
309 ## Volume Management
310 
311 ### Database Volume
312 
313 Mount your SQLite database file:
314 
315 ```yaml
316 volumes:
317   - /path/to/your/database.db:/data/database.db:ro
318 ```
319 
320 **Important**: Use `:ro` (read-only) flag to prevent accidental modifications.
321 
322 ### Backup Volume
323 
324 Mount your backup directory:
325 
326 ```yaml
327 volumes:
328   - /path/to/backups:/backups
329 ```
330 
331 **Permissions**: Ensure the container can write to this directory:
332 
333 ```bash
334 mkdir -p /path/to/backups
335 chmod 777 /path/to/backups  # For testing only - use proper permissions in production
336 ```
337 
338 ### Named Volumes vs Host Paths
339 
339 **Host Paths** (recommended for development):
340 - Easy to access and debug
341 - Direct file system access
342 - Good for small deployments
343 
344 ```yaml
345 volumes:
346   - ./data:/data
347   - ./backups:/backups
348 ```
349 
350 **Named Volumes** (recommended for production):
351 - Better performance for large files
352 - Managed by Docker
353 - Automatic backup support
354 - Easy to migrate
355 
356 ```yaml
357 volumes:
358   - db_data:/data
359   - backup_data:/backups
360 
360 volumes:
361   db_data:
362   backup_data:
363 ```
364 
365 ## Network Configuration
366 
366 ### Port Mapping
367 
367 The application exposes port `8080` for HTTP traffic:
368 
369 ```yaml
370 ports:
371   - "8080:8080"
372 ```
373 
374 **Custom Host Port**: Map to a different host port if needed:
375 
376 ```yaml
377 ports:
378   - "9000:8080"  # Host port 9000 → Container port 8080
379 ```
380 
381 ### Network Isolation
382 
383 For security, use custom networks:
384 
385 ```yaml
386 networks:
387   backup_network:
388     driver: bridge
389 
389 services:
390   sqlite-backup:
391     networks:
392       - backup_network
393 
393 networks:
394   backup_network:
395 ```
396 
397 ### Host Networking
398 
399 For maximum performance (use with caution):
400 
401 ```yaml
402 network_mode: host
403 ```
404 
405 **Note**: This bypasses Docker networking and uses the host network directly.
406 
407 ## Health Checks
408 
408 ### Built-in Health Check
409 
409 The container includes a health check that verifies:
410 - Application is running
411 - HTTP server is responding
412 - Database is accessible
413 
414 ```yaml
415 healthcheck:
416   test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8080/health"]
417   interval: 30s
418   timeout: 10s
419   retries: 3
420   start_period: 30s
421 ```
422 
423 ### Health Check Endpoint
424 
424 The `/health` endpoint returns:
425 
426 ```json
427 {
428   "status": "Healthy",
429   "timestamp": "2026-05-18T10:00:00Z",
430   "lastBackup": {
431     "scheduleId": "daily-backup",
432     "timestamp": "2026-05-18T02:00:00Z",
433     "status": "Success"
434   },
435   "checks": [
436     {
437       "name": "Database",
438       "status": "Healthy",
439       "details": "Database accessible"
440     },
441     {
442       "name": "Storage",
443       "status": "Healthy",
444       "details": "Backup directory writable"
445     }
446   ]
447 }
448 ```
449 
450 ### Monitoring Integration
451 
451 Configure your monitoring system to check the health endpoint:
452 
453 ```yaml
454 # Prometheus example
455 - job_name: 'sqlite-backup'
456   metrics_path: '/metrics'
457   static_configs:
458     - targets: ['sqlite-backup:8080']
459 ```
459 
460 ## Logging
461 
462 ### Log Levels
463 
463 Control log verbosity with environment variables:
464 
465 ```bash
466 # Production logging (recommended)
467 export Logging__LogLevel__Default=Information
468 export Logging__LogLevel__System=Warning
469 export Logging__LogLevel__Microsoft=Warning
470 
471 # Debug logging (for troubleshooting)
472 export Logging__LogLevel__Default=Debug
473 ```
474 
466 ### Log Output
467 
468 Logs are written to stdout/stderr and can be viewed with:
469 
470 ```bash
471 # View logs
472 docker logs sqlite-backup
473 
473 # Follow logs in real-time
474 docker logs -f sqlite-backup
475 
475 # View last 100 lines
476 docker logs --tail=100 sqlite-backup
477 
477 # View logs with timestamps
478 docker logs -t sqlite-backup
479 ```
479 
480 ### Log Rotation
481 
482 For production, configure log rotation:
483 
484 ```bash
485 # Configure Docker log driver
486 docker run --log-opt max-size=10m --log-opt max-file=3 ...
487 ```
488 
489 Or in docker-compose.yml:
490 
491 ```yaml
492 logging:
493   driver: "json-file"
494   options:
495     max-size: "10m"
496     max-file: "3"
497 ```
498 
499 ## Updating
500 
501 ### Update to Latest Version
502 
503 ```bash
504 # Pull latest image
505 docker-compose pull
506 
507 # Restart with new image
508 docker-compose up -d --build
509 ```
508 
510 ### Update Configuration
511 
512 After updating, review your configuration:
513 
514 ```bash
515 # Check running version
516 docker inspect sqlite-backup --format='{{.Config.Image}}'
517 
518 # Compare with latest
519 docker-compose pull && docker-compose config
520 ```
521 
522 ### Rollback
523 
524 ```bash
525 # Revert to previous image
526 docker-compose down
527 docker-compose up -d sqlite-backup:old-tag
528 ```
529 
524 ## Security Best Practices
525 
526 ### Run as Non-root User
527 
528 The container runs as user `backup` (UID 1000) by default:
529 
530 ```dockerfile
531 USER backup
532 ```
533 
534 ### Secrets Management
535 
536 For AWS credentials, use Docker secrets or environment files:
537 
538 ```yaml
539 # Option 1: Environment file
540 env_file: .env
541 
542 # Option 2: Docker secrets
543 secrets:
544   - aws_credentials
545 ```
546 
547 ### Network Security
548 
549 - Use custom networks to isolate the container
549 - Limit exposed ports
550 - Use firewalls to restrict access
551 - Enable TLS for external access
552 
553 ### File Permissions
554 
555 ```bash
556 # Database file should be readable by container
557 chmod 644 /path/to/database.db
558 
559 # Backup directory should be writable
560 chmod 775 /path/to/backups
561 chown 1000:1000 /path/to/backups
562 ```
563 
564 ## Troubleshooting
565 
566 ### Container Won't Start
567 
568 ```bash
569 # Check logs
570 docker logs sqlite-backup
571 
572 # Check container status
573 docker ps -a
574 
575 # Inspect container
576 docker inspect sqlite-backup
577 ```
578 
579 Common issues:
580 - Database file not found
581 - Missing environment variables
582 - Permission issues on volumes
583 - Port already in use
584 
585 ### Health Check Failing
586 
587 ```bash
588 # Check health status
589 docker inspect --format='{{json .State.Health}}' sqlite-backup
590 
590 # Test endpoint manually
591 docker exec sqlite-backup wget -O- http://localhost:8080/health
592 ```
593 
594 Common causes:
595 - Database file path incorrect
596 - Database file permissions
597 - Port 8080 already in use
598 - Container can't access database
599 
600 ### Database Locked Errors
601 
602 ```bash
603 # Check for other processes
604 lsof /path/to/database.db
605 
606 # Increase timeout
607 export AppSettings__BackupTimeoutSeconds=7200
608 ```
609 
607 ### High Memory Usage
608 
609 ```bash
610 # Check memory usage
611 docker stats sqlite-backup
612 
613 # Reduce concurrent backups
614 export AppSettings__MaxConcurrentBackups=1
615 ```
616 
617 ## Performance Tuning
618 
619 ### Resource Limits
619 
620 ```yaml
621 deploy:
622   resources:
623     limits:
624       cpus: '1.0'
625       memory: 512M
626     reservations:
627       cpus: '0.5'
628       memory: 256M
629 ```
628 
629 ### Concurrent Backups
630 
631 ```bash
632 # Increase for faster backups (but more resource usage)
633 export AppSettings__MaxConcurrentBackups=4
634 
633 # Increase timeout for large databases
634 export AppSettings__BackupTimeoutSeconds=7200
635 ```
636 
637 ### Compression
638 
639 ```bash
639 # Enable compression for smaller backups
640 export AppSettings__CompressionEnabled=true
641 export Schedules__0__CompressionEnabled=true
642 ```
643 
644 ## Multi-Container Setup
645 
646 ### With Application Container
647 
648 ```yaml
649 version: '3.8'
650 
651 services:
652   app:
653     image: my-app:latest
654     volumes:
655       - ./data:/data
656     depends_on:
657       - sqlite-backup
658 
659   sqlite-backup:
659     image: sarmkadan/docker-sqlite-backup:latest
660     environment:
661       - AppSettings__DatabasePath=/data/production.db
662       - AppSettings__LocalStoragePath=/backups
663     volumes:
664       - ./data:/data:ro
665       - ./backups:/backups
666     depends_on:
667       - app
668 ```
669 
670 ## Docker Compose Examples
671 
672 ### Example 1: Simple Local Backup
673 
674 ```yaml
675 version: '3.8'
676 
677 services:
678   sqlite-backup:
679     image: sarmkadan/docker-sqlite-backup:latest
679     container_name: sqlite-backup
680     restart: unless-stopped
681     environment:
682       - AppSettings__DatabasePath=/data/app.db
683       - AppSettings__LocalStoragePath=/backups
684       - AppSettings__RetentionDays=30
685     volumes:
686       - ./data:/data:ro
687       - ./backups:/backups
688     ports:
689       - "8080:8080"
689     healthcheck:
690       test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8080/health"]
691       interval: 30s
692       timeout: 10s
693       retries: 3
694       start_period: 30s
695 ```
696 
697 ### Example 2: S3 Backup with Encryption
698 
699 ```yaml
700 version: '3.8'
701 
702 services:
703   sqlite-backup:
704     image: sarmkadan/docker-sqlite-backup:latest
705     restart: unless-stopped
706     environment:
707       - AppSettings__DatabasePath=/data/production.db
708       - AppSettings__LocalStoragePath=/backups
709       - AppSettings__RetentionDays=90
710       - Schedules__0__Id=daily-s3
711       - Schedules__0__Name=Daily S3 Backup
712       - Schedules__0__CronExpression=0 2 * * *
713       - Schedules__0__IsEnabled=true
713       - Schedules__0__StorageType=S3
714       - Schedules__0__RotationStrategy=Age
715       - Schedules__0__RetentionDays=90
716       - Schedules__0__S3Config__BucketName=my-backups
717       - Schedules__0__S3Config__Region=us-east-1
718       - Schedules__0__S3Config__EnableEncryption=true
719       - Schedules__0__S3Config__EnableVersioning=true
720     volumes:
721       - ./data:/data:ro
722     ports:
723       - "8080:8080"
724     healthcheck:
725       test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8080/health"]
726       interval: 30s
726       timeout: 10s
727       retries: 3
728       start_period: 30s
729 ```
730 
731 ### Example 3: Multiple Schedules
732 
733 ```yaml
734 version: '3.8'
735 
736 services:
737   sqlite-backup:
738     image: sarmkadan/docker-sqlite-backup:latest
739     restart: unless-stopped
740     environment:
741       - AppSettings__DatabasePath=/data/production.db
742       - AppSettings__LocalStoragePath=/backups
743       - Schedules__0__Id=daily-local
744       - Schedules__0__Name=Daily Local Backup
745       - Schedules__0__CronExpression=0 2 * * *
746       - Schedules__0__IsEnabled=true
747       - Schedules__0__StorageType=Local
748       - Schedules__0__RotationStrategy=Count
749       - Schedules__0__MaxBackupCount=7
750       - Schedules__1__Id=weekly-s3
750       - Schedules__1__Name=Weekly S3 Backup
751       - Schedules__1__CronExpression=0 3 * * 0
752       - Schedules__1__IsEnabled=true
753       - Schedules__1__StorageType=S3
754       - Schedules__1__RotationStrategy=Age
755       - Schedules__1__RetentionDays=365
756     volumes:
757       - ./data:/data:ro
758       - ./backups:/backups
759     ports:
760       - "8080:8080"
761     healthcheck:
762       test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost:8080/health"]
763       interval: 30s
764       timeout: 10s
765       retries: 3
766       start_period: 30s
767 ```
768 
769 ## Kubernetes Deployment
770 
771 ### Kubernetes Deployment Example
772 
773 ```yaml
774 apiVersion: apps/v1
775 kind: Deployment
776 metadata:
777   name: sqlite-backup
778 spec:
779     replicas: 1
780     selector:
781       matchLabels:
782         app: sqlite-backup
783     template:
784       metadata:
785         labels:
786           app: sqlite-backup
787       spec:
788         containers:
789         - name: backup
790           image: sarmkadan/docker-sqlite-backup:latest
791           ports:
792           - containerPort: 8080
793           env:
794           - name: AppSettings__DatabasePath
794             value: /data/production.db
795           - name: AppSettings__LocalStoragePath
796             value: /backups
797           - name: AppSettings__RetentionDays
798             value: "90"
799           - name: Schedules__0__Id
800             value: daily-backup
801           - name: Schedules__0__Name
802             value: Daily Backup
803           - name: Schedules__0__CronExpression
804             value: "0 2 * * *"
805           - name: Schedules__0__IsEnabled
806             value: "true"
807           - name: Schedules__0__StorageType
808             value: Local
809           volumeMounts:
810           - name: data
811             mountPath: /data
812             readOnly: true
813           - name: backups
814             mountPath: /backups
815         volumes:
816         - name: data
817           persistentVolumeClaim:
818             claimName: app-data-pvc
819         - name: backups
820           persistentVolumeClaim:
821             claimName: backup-data-pvc
822         restartPolicy: Always
823 ```
824 
825 ### Kubernetes Service
826 
827 ```yaml
828 apiVersion: v1
829 kind: Service
830 metadata:
831   name: sqlite-backup
832 spec:
833   selector:
834     app: sqlite-backup
835   ports:
836     - protocol: TCP
837       port: 8080
838       targetPort: 8080
839   type: ClusterIP
840 ```
841 
842 ### Kubernetes Ingress
843 
844 ```yaml
845 apiVersion: networking.k8s.io/v1
846 kind: Ingress
847 metadata:
848   name: sqlite-backup-ingress
849 spec:
850   rules:
851   - host: backup.example.com
852     http:
853       paths:
854       - path: /
855         pathType: Prefix
856         backend:
857           service:
858             name: sqlite-backup
859             port:
860               number: 8080
861 ```
862 
838 ### Kubernetes Persistent Volumes
839 
840 For production, use PersistentVolumes:
841 
842 ```yaml
843 apiVersion: v1
844 kind: PersistentVolume
845 metadata:
846   name: backup-pv
847 spec:
848   capacity:
849     storage: 10Gi
850   accessModes:
851     - ReadWriteOnce
852   persistentVolumeReclaimPolicy: Retain
853   hostPath:
854     path: /mnt/backups
855 
856 ---
857 
858 apiVersion: v1
859 kind: PersistentVolumeClaim
860 metadata:
861   name: backup-pvc
862 spec:
863   accessModes:
864     - ReadWriteOnce
865   resources:
866     requests:
867       storage: 10Gi
868 ```
869 
870 ## Backup Storage Patterns
871 
872 ### Pattern 1: Local Storage Only
873 
874 Suitable for development and small applications.
875 
876 ```yaml
877 volumes:
878   - ./backups:/backups
879 ```
879 
880 ### Pattern 2: S3 Storage Only
881 
882 Suitable for cloud deployments.
883 
884 ```yaml
885 environment:
886   - Schedules__0__StorageType=S3
887   - Schedules__0__S3Config__BucketName=my-backups
888   - Schedules__0__S3Config__Region=us-east-1
```
889 
889 ### Pattern 3: Tiered Storage (Local + S3)
890 
891 Daily local backups for quick restore, weekly S3 for long-term archive.
892 
893 ```yaml
894 # Two schedules in docker-compose.yml
895 environment:
896   # Daily local backup
897   - Schedules__0__Id=daily-local
898   - Schedules__0__CronExpression=0 2 * * *
899   - Schedules__0__StorageType=Local
900   
900   # Weekly S3 backup
901   - Schedules__1__Id=weekly-s3
901   - Schedules__1__CronExpression=0 3 * * 0
902   - Schedules__1__StorageType=S3
902   - Schedules__1__S3Config__BucketName=my-backups
903 ```
904 
905 ### Pattern 4: Compliance-Grade Storage
906 
907 Immutable backups with versioning and encryption.
908 
909 ```yaml
910 environment:
911   - Schedules__0__Id=compliance-daily
912   - Schedules__0__CronExpression=0 2 * * *
913   - Schedules__0__StorageType=S3
914   - Schedules__0__RotationStrategy=Age
915   - Schedules__0__RetentionDays=2555
916   - Schedules__0__S3Config__BucketName=compliance-backups
917   - Schedules__0__S3Config__EnableVersioning=true
918   - Schedules__0__S3Config__EnableEncryption=true
919   - Schedules__0__S3Config__EnableObjectLock=true
920 ```