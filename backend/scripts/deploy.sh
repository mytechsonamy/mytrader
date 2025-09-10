#!/bin/bash

# MyTrader Production Deployment Script
# This script handles building and deploying the MyTrader application

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
IMAGE_NAME="mytrader/api"
CONTAINER_NAME="mytrader-api"
COMPOSE_FILE="docker-compose.production.yml"
ENV_FILE=".env.production"

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to check prerequisites
check_prerequisites() {
    print_status "Checking prerequisites..."
    
    # Check if Docker is installed and running
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed. Please install Docker first."
        exit 1
    fi
    
    if ! docker info &> /dev/null; then
        print_error "Docker is not running. Please start Docker first."
        exit 1
    fi
    
    # Check if Docker Compose is available
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        print_error "Docker Compose is not available. Please install Docker Compose."
        exit 1
    fi
    
    # Use docker compose if available, otherwise fall back to docker-compose
    if docker compose version &> /dev/null; then
        COMPOSE_CMD="docker compose"
    else
        COMPOSE_CMD="docker-compose"
    fi
    
    print_status "Prerequisites check passed."
}

# Function to check environment file
check_environment() {
    print_status "Checking environment configuration..."
    
    cd "$PROJECT_ROOT"
    
    if [ ! -f "$ENV_FILE" ]; then
        print_error "Environment file $ENV_FILE not found!"
        print_warning "Please copy .env.production.template to $ENV_FILE and configure it."
        exit 1
    fi
    
    # Check for required environment variables
    source "$ENV_FILE"
    
    required_vars=("POSTGRES_PASSWORD" "JWT_SECRET_KEY" "REDIS_PASSWORD")
    for var in "${required_vars[@]}"; do
        if [ -z "${!var}" ]; then
            print_error "Required environment variable $var is not set in $ENV_FILE"
            exit 1
        fi
    done
    
    print_status "Environment configuration check passed."
}

# Function to build the application
build_application() {
    print_status "Building MyTrader application..."
    
    cd "$PROJECT_ROOT"
    
    # Build the Docker image
    print_status "Building Docker image: $IMAGE_NAME:${IMAGE_TAG:-latest}"
    docker build -f Dockerfile.production -t "$IMAGE_NAME:${IMAGE_TAG:-latest}" .
    
    print_status "Docker image built successfully."
}

# Function to backup database
backup_database() {
    print_status "Creating database backup..."
    
    cd "$PROJECT_ROOT"
    
    # Create backup directory if it doesn't exist
    mkdir -p backups
    
    # Create backup with timestamp
    BACKUP_FILE="backups/mytrader_backup_$(date +%Y%m%d_%H%M%S).sql"
    
    if $COMPOSE_CMD -f "$COMPOSE_FILE" ps postgres | grep -q "Up"; then
        print_status "Database is running, creating backup..."
        $COMPOSE_CMD -f "$COMPOSE_FILE" exec -T postgres pg_dump -U postgres mytrader > "$BACKUP_FILE"
        print_status "Database backup created: $BACKUP_FILE"
    else
        print_warning "Database container is not running, skipping backup."
    fi
}

# Function to deploy services
deploy_services() {
    print_status "Deploying MyTrader services..."
    
    cd "$PROJECT_ROOT"
    
    # Pull latest images for dependencies
    print_status "Pulling dependency images..."
    $COMPOSE_CMD -f "$COMPOSE_FILE" pull postgres redis nginx
    
    # Deploy core services (database, cache, API)
    print_status "Starting core services..."
    $COMPOSE_CMD -f "$COMPOSE_FILE" up -d postgres redis mytrader-api nginx
    
    # Wait for services to be healthy
    print_status "Waiting for services to be healthy..."
    sleep 30
    
    # Check service health
    check_service_health
    
    print_status "Core services deployed successfully."
}

# Function to deploy optional services
deploy_optional_services() {
    print_status "Deploying optional services..."
    
    cd "$PROJECT_ROOT"
    
    # Deploy monitoring stack if requested
    if [ "$DEPLOY_MONITORING" = "true" ]; then
        print_status "Deploying monitoring services..."
        $COMPOSE_CMD -f "$COMPOSE_FILE" --profile monitoring up -d
    fi
    
    # Deploy logging stack if requested
    if [ "$DEPLOY_LOGGING" = "true" ]; then
        print_status "Deploying logging services..."
        $COMPOSE_CMD -f "$COMPOSE_FILE" --profile logging up -d
    fi
    
    # Deploy backup service if requested
    if [ "$DEPLOY_BACKUP" = "true" ]; then
        print_status "Deploying backup services..."
        $COMPOSE_CMD -f "$COMPOSE_FILE" --profile backup up -d
    fi
}

# Function to check service health
check_service_health() {
    print_status "Checking service health..."
    
    cd "$PROJECT_ROOT"
    
    # Check API health
    for i in {1..30}; do
        if curl -sf http://localhost:8080/health > /dev/null 2>&1; then
            print_status "API service is healthy."
            break
        else
            if [ $i -eq 30 ]; then
                print_error "API service failed to become healthy."
                print_error "Check logs with: $COMPOSE_CMD -f $COMPOSE_FILE logs mytrader-api"
                exit 1
            fi
            print_status "Waiting for API service... ($i/30)"
            sleep 10
        fi
    done
    
    # Check database health
    if ! $COMPOSE_CMD -f "$COMPOSE_FILE" exec postgres pg_isready -U postgres > /dev/null 2>&1; then
        print_error "Database service is not healthy."
        exit 1
    fi
    print_status "Database service is healthy."
    
    # Check Redis health
    if ! $COMPOSE_CMD -f "$COMPOSE_FILE" exec redis redis-cli ping | grep -q PONG; then
        print_error "Redis service is not healthy."
        exit 1
    fi
    print_status "Redis service is healthy."
}

# Function to run database migrations
run_migrations() {
    print_status "Running database migrations..."
    
    cd "$PROJECT_ROOT"
    
    # Run migrations inside the API container
    $COMPOSE_CMD -f "$COMPOSE_FILE" exec mytrader-api dotnet ef database update --no-build
    
    print_status "Database migrations completed."
}

# Function to show deployment status
show_status() {
    print_status "Deployment Status:"
    print_status "=================="
    
    cd "$PROJECT_ROOT"
    
    # Show running containers
    $COMPOSE_CMD -f "$COMPOSE_FILE" ps
    
    echo
    print_status "Service URLs:"
    print_status "============="
    print_status "API:        http://localhost:8080"
    print_status "Health:     http://localhost:8080/health"
    print_status "Database:   localhost:5432"
    print_status "Redis:      localhost:6379"
    
    if [ "$DEPLOY_MONITORING" = "true" ]; then
        print_status "Grafana:    http://localhost:3000"
        print_status "Prometheus: http://localhost:9090"
    fi
    
    if [ "$DEPLOY_LOGGING" = "true" ]; then
        print_status "Seq Logs:   http://localhost:5341"
    fi
    
    echo
    print_status "Useful Commands:"
    print_status "==============="
    print_status "View logs:   $COMPOSE_CMD -f $COMPOSE_FILE logs -f [service-name]"
    print_status "Stop all:    $COMPOSE_CMD -f $COMPOSE_FILE down"
    print_status "Restart API: $COMPOSE_CMD -f $COMPOSE_FILE restart mytrader-api"
}

# Function to clean up old images and containers
cleanup() {
    print_status "Cleaning up old Docker resources..."
    
    # Remove unused images
    docker image prune -f
    
    # Remove unused containers
    docker container prune -f
    
    # Remove unused volumes (be careful with this)
    if [ "$CLEANUP_VOLUMES" = "true" ]; then
        print_warning "Cleaning up unused volumes (this may delete data!)"
        docker volume prune -f
    fi
    
    print_status "Cleanup completed."
}

# Function to show help
show_help() {
    echo "MyTrader Deployment Script"
    echo "=========================="
    echo
    echo "Usage: $0 [OPTIONS] [COMMAND]"
    echo
    echo "Commands:"
    echo "  deploy     Full deployment (default)"
    echo "  build      Build application only"
    echo "  start      Start services"
    echo "  stop       Stop services"
    echo "  restart    Restart services"
    echo "  status     Show service status"
    echo "  logs       Show service logs"
    echo "  backup     Create database backup"
    echo "  migrate    Run database migrations"
    echo "  cleanup    Clean up Docker resources"
    echo
    echo "Options:"
    echo "  --monitoring     Deploy monitoring stack (Grafana, Prometheus)"
    echo "  --logging        Deploy logging stack (Seq)"
    echo "  --backup         Deploy backup service"
    echo "  --skip-build     Skip building the application"
    echo "  --skip-backup    Skip database backup"
    echo "  --cleanup-volumes Include volumes in cleanup (destructive!)"
    echo "  --help           Show this help"
    echo
    echo "Environment Variables:"
    echo "  IMAGE_TAG        Docker image tag (default: latest)"
    echo "  ENV_FILE         Environment file (default: .env.production)"
    echo
    echo "Examples:"
    echo "  $0 deploy --monitoring --logging"
    echo "  $0 build"
    echo "  $0 status"
    echo "  IMAGE_TAG=v1.2.3 $0 deploy --skip-backup"
}

# Parse command line arguments
COMMAND="deploy"
DEPLOY_MONITORING="false"
DEPLOY_LOGGING="false"
DEPLOY_BACKUP="false"
SKIP_BUILD="false"
SKIP_BACKUP="false"
CLEANUP_VOLUMES="false"

while [[ $# -gt 0 ]]; do
    case $1 in
        deploy|build|start|stop|restart|status|logs|backup|migrate|cleanup)
            COMMAND="$1"
            shift
            ;;
        --monitoring)
            DEPLOY_MONITORING="true"
            shift
            ;;
        --logging)
            DEPLOY_LOGGING="true"
            shift
            ;;
        --backup)
            DEPLOY_BACKUP="true"
            shift
            ;;
        --skip-build)
            SKIP_BUILD="true"
            shift
            ;;
        --skip-backup)
            SKIP_BACKUP="true"
            shift
            ;;
        --cleanup-volumes)
            CLEANUP_VOLUMES="true"
            shift
            ;;
        --help)
            show_help
            exit 0
            ;;
        *)
            print_error "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Main execution
main() {
    print_status "Starting MyTrader deployment..."
    print_status "Command: $COMMAND"
    print_status "Project root: $PROJECT_ROOT"
    
    case $COMMAND in
        deploy)
            check_prerequisites
            check_environment
            if [ "$SKIP_BACKUP" = "false" ]; then
                backup_database
            fi
            if [ "$SKIP_BUILD" = "false" ]; then
                build_application
            fi
            deploy_services
            run_migrations
            deploy_optional_services
            show_status
            ;;
        build)
            check_prerequisites
            build_application
            ;;
        start)
            check_prerequisites
            check_environment
            cd "$PROJECT_ROOT"
            $COMPOSE_CMD -f "$COMPOSE_FILE" up -d
            ;;
        stop)
            cd "$PROJECT_ROOT"
            $COMPOSE_CMD -f "$COMPOSE_FILE" down
            ;;
        restart)
            cd "$PROJECT_ROOT"
            $COMPOSE_CMD -f "$COMPOSE_FILE" restart
            ;;
        status)
            cd "$PROJECT_ROOT"
            show_status
            ;;
        logs)
            cd "$PROJECT_ROOT"
            $COMPOSE_CMD -f "$COMPOSE_FILE" logs -f
            ;;
        backup)
            check_prerequisites
            check_environment
            backup_database
            ;;
        migrate)
            check_prerequisites
            check_environment
            run_migrations
            ;;
        cleanup)
            cleanup
            ;;
    esac
    
    print_status "MyTrader deployment script completed successfully!"
}

# Run main function
main "$@"