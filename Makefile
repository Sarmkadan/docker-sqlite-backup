# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================
# Makefile for Docker SQLite Backup
# Development, build, test, and deployment targets

.PHONY: help build test clean lint format docker docker-push deploy-k8s deploy-docker \
        install-tools dev-setup run monitor backup-test restore-test compliance-audit \
        release publish docs

# Configuration
PROJECT_NAME = docker-sqlite-backup
IMAGE_NAME = sqlite-backup
DOCKER_REGISTRY ?= docker.io
DOCKER_USERNAME ?= username
VERSION ?= 1.2.0
CONFIGURATION ?= Release

# Color output
RED = \033[0;31m
GREEN = \033[0;32m
YELLOW = \033[1;33m
NC = \033[0m

.DEFAULT_GOAL := help

help: ## Display this help message
	@echo "$(GREEN)Docker SQLite Backup - Development Makefile$(NC)"
	@echo ""
	@echo "$(YELLOW)Build & Test:$(NC)"
	@echo "  make build              Build the project (Release)"
	@echo "  make test               Run all tests"
	@echo "  make lint               Run code analysis"
	@echo "  make format             Format code with dotnet format"
	@echo "  make clean              Clean build artifacts"
	@echo ""
	@echo "$(YELLOW)Docker:$(NC)"
	@echo "  make docker             Build Docker image"
	@echo "  make docker-push        Push Docker image to registry"
	@echo "  make docker-local       Build image locally (no registry)"
	@echo ""
	@echo "$(YELLOW)Development:$(NC)"
	@echo "  make dev-setup          Setup development environment"
	@echo "  make run                Run application in development mode"
	@echo "  make watch              Watch for file changes and rebuild"
	@echo ""
	@echo "$(YELLOW)Deployment:$(NC)"
	@echo "  make deploy-docker      Deploy using Docker Compose"
	@echo "  make deploy-k8s         Deploy to Kubernetes"
	@echo "  make stop               Stop Docker Compose deployment"
	@echo ""
	@echo "$(YELLOW)Testing:$(NC)"
	@echo "  make backup-test        Run full backup workflow test"
	@echo "  make restore-test       Test restore functionality"
	@echo "  make compliance-audit   Run compliance audit"
	@echo "  make monitor            Monitor running backup service"
	@echo ""
	@echo "$(YELLOW)Maintenance:$(NC)"
	@echo "  make docs               Generate documentation"
	@echo "  make version            Show project version"
	@echo "  make release            Create GitHub release"
	@echo "  make publish            Publish to NuGet"

# Build targets
build: ## Build project in Release configuration
	@echo "$(GREEN)Building $(PROJECT_NAME)...$(NC)"
	dotnet restore
	dotnet build -c $(CONFIGURATION) -v minimal
	@echo "$(GREEN)✓ Build completed$(NC)"

build-debug: ## Build in Debug configuration
	@echo "$(GREEN)Building $(PROJECT_NAME) (Debug)...$(NC)"
	dotnet restore
	dotnet build -c Debug -v minimal

# Test targets
test: ## Run all tests with coverage
	@echo "$(GREEN)Running tests...$(NC)"
	dotnet test -c $(CONFIGURATION) -v normal /p:CollectCoverage=true /p:CoverageFormat=cobertura
	@echo "$(GREEN)✓ Tests completed$(NC)"

test-watch: ## Run tests in watch mode
	@echo "$(GREEN)Running tests in watch mode...$(NC)"
	dotnet watch --project "tests" test -c $(CONFIGURATION)

test-failed: ## Run only failed tests
	@echo "$(GREEN)Running failed tests...$(NC)"
	dotnet test -c $(CONFIGURATION) --filter "LastResult=Failed" -v normal

coverage: test ## Generate code coverage report
	@echo "$(GREEN)Generating coverage report...$(NC)"
	@echo "Coverage reports: TestResults/*/coverage.cobertura.xml"

# Code quality
lint: ## Run code analysis
	@echo "$(GREEN)Running code analysis...$(NC)"
	dotnet build -c $(CONFIGURATION) -v minimal /p:EnforceCodeStyleInBuild=true

format: ## Format code with dotnet format
	@echo "$(GREEN)Formatting code...$(NC)"
	dotnet format --verify-no-changes --verbosity diagnostic || dotnet format
	@echo "$(GREEN)✓ Code formatted$(NC)"

format-check: ## Check if code needs formatting
	@echo "$(GREEN)Checking code format...$(NC)"
	dotnet format --verify-no-changes --verbosity diagnostic

# Cleaning
clean: ## Clean build artifacts
	@echo "$(GREEN)Cleaning build artifacts...$(NC)"
	dotnet clean -c $(CONFIGURATION) -v minimal
	rm -rf bin/ obj/ TestResults/
	rm -f *.trx
	@echo "$(GREEN)✓ Clean completed$(NC)"

# Docker targets
docker: ## Build Docker image with tags
	@echo "$(GREEN)Building Docker image...$(NC)"
	docker build -t $(IMAGE_NAME):latest \
	             -t $(IMAGE_NAME):$(VERSION) \
	             -t $(DOCKER_REGISTRY)/$(DOCKER_USERNAME)/$(IMAGE_NAME):latest \
	             -t $(DOCKER_REGISTRY)/$(DOCKER_USERNAME)/$(IMAGE_NAME):$(VERSION) \
	             .
	@echo "$(GREEN)✓ Docker image built$(NC)"
	@echo "  $(IMAGE_NAME):latest"
	@echo "  $(IMAGE_NAME):$(VERSION)"

docker-local: ## Build Docker image locally (no registry tags)
	@echo "$(GREEN)Building Docker image (local)...$(NC)"
	docker build -t $(IMAGE_NAME):latest -t $(IMAGE_NAME):$(VERSION) .
	@echo "$(GREEN)✓ Docker image built$(NC)"

docker-push: docker ## Push Docker image to registry
	@echo "$(GREEN)Pushing Docker image...$(NC)"
	docker push $(DOCKER_REGISTRY)/$(DOCKER_USERNAME)/$(IMAGE_NAME):$(VERSION)
	docker push $(DOCKER_REGISTRY)/$(DOCKER_USERNAME)/$(IMAGE_NAME):latest
	@echo "$(GREEN)✓ Docker image pushed$(NC)"

docker-shell: ## Start interactive shell in Docker image
	@echo "$(GREEN)Starting Docker shell...$(NC)"
	docker run -it --rm $(IMAGE_NAME):latest /bin/sh

docker-logs: ## Show Docker container logs
	@docker logs -f sqlite-backup 2>/dev/null || echo "$(RED)Container not running$(NC)"

# Development targets
dev-setup: ## Setup development environment
	@echo "$(GREEN)Setting up development environment...$(NC)"
	dotnet tool restore
	dotnet nuget add source https://api.nuget.org/v3/index.json --name nuget 2>/dev/null || true
	@echo "$(GREEN)✓ Setup completed$(NC)"

run: build ## Run application in development mode
	@echo "$(GREEN)Running application...$(NC)"
	dotnet run --configuration $(CONFIGURATION) --no-build

watch: ## Watch source files and rebuild on changes
	@echo "$(GREEN)Watching for changes...$(NC)"
	dotnet watch --project . run --configuration $(CONFIGURATION)

# Deployment targets
deploy-docker: docker-local ## Deploy using Docker Compose
	@echo "$(GREEN)Deploying with Docker Compose...$(NC)"
	docker-compose up -d
	@echo "$(GREEN)✓ Deployment started$(NC)"
	@echo "  API: http://localhost:5000"
	@sleep 2
	@curl -s http://localhost:5000/health | jq . || echo "Service starting..."

deploy-k8s: ## Deploy to Kubernetes
	@echo "$(GREEN)Deploying to Kubernetes...$(NC)"
	kubectl apply -f examples/03-kubernetes-deployment.yaml
	@echo "$(GREEN)✓ Kubernetes deployment started$(NC)"
	@echo "  Namespace: sqlite-backup"
	@echo "  Monitor: kubectl logs -f deployment/sqlite-backup -n sqlite-backup"

stop: ## Stop Docker Compose deployment
	@echo "$(GREEN)Stopping services...$(NC)"
	docker-compose down
	@echo "$(GREEN)✓ Services stopped$(NC)"

# Testing workflows
backup-test: deploy-docker ## Run complete backup workflow test
	@echo "$(GREEN)Testing backup workflow...$(NC)"
	@sleep 3
	bash examples/04-backup-monitoring.sh http://localhost:5000
	@echo "$(GREEN)✓ Backup test completed$(NC)"

restore-test: deploy-docker ## Test restore functionality
	@echo "$(GREEN)Testing restore functionality...$(NC)"
	@sleep 3
	bash examples/05-restore-from-backup.sh http://localhost:5000
	@echo "$(GREEN)✓ Restore test completed$(NC)"

compliance-audit: deploy-docker ## Run compliance audit
	@echo "$(GREEN)Running compliance audit...$(NC)"
	@sleep 3
	bash examples/07-compliance-audit.sh http://localhost:5000
	@cat audit-report.json | jq .
	@echo "$(GREEN)✓ Compliance audit completed$(NC)"

monitor: ## Monitor running backup service
	@echo "$(GREEN)Monitoring backup service...$(NC)"
	@watch -n 10 'curl -s http://localhost:5000/health | jq .'

# Documentation
docs: ## Generate documentation
	@echo "$(GREEN)Generating documentation...$(NC)"
	@echo "Documentation already exists in:"
	@echo "  - README.md (main)"
	@echo "  - docs/getting-started.md"
	@echo "  - docs/architecture.md"
	@echo "  - docs/api-reference.md"
	@echo "  - docs/deployment.md"
	@echo "  - docs/faq.md"
	@echo "  - examples/README.md"

# Version and release
version: ## Show project version
	@echo "$(GREEN)Project Version:$(NC) $(VERSION)"
	@dotnet --version

release: ## Create GitHub release
	@echo "$(GREEN)Creating GitHub release v$(VERSION)...$(NC)"
	@gh release create v$(VERSION) \
	  --title "Version $(VERSION)" \
	  --notes "See CHANGELOG.md for details"
	@echo "$(GREEN)✓ Release created$(NC)"

publish: docker-push ## Publish package to NuGet
	@echo "$(GREEN)Publishing to NuGet...$(NC)"
	@echo "TODO: Configure NuGet publishing"

# Utility targets
version-bump: ## Bump version (interactive)
	@read -p "Enter new version: " VERSION; \
	echo "VERSION=$${VERSION}" > .env
	@echo "$(GREEN)Version updated to $${VERSION}$(NC)"

shell: ## Open shell in project directory
	@$(SHELL)

install-tools: ## Install development tools
	@echo "$(GREEN)Installing development tools...$(NC)"
	dotnet tool install -g dotnet-format
	dotnet tool install -g dotnet-coverage
	dotnet tool install -g gh 2>/dev/null || echo "gh already installed"
	@echo "$(GREEN)✓ Tools installed$(NC)"

list-targets: help ## List all makefile targets
	@grep '^[a-zA-Z_-]*:.*##' Makefile | sed 's/:.*##//' | column -t

.SILENT: version help list-targets
