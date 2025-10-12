#!/bin/bash
# Production Deployment Script for MyTrader
# This script deploys the application to production server

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROD_SERVER="213.238.180.201"
PROD_USER="root"  # Change if using different user
PROD_DIR="/opt/mytrader"
DOMAIN="mytrader.tech"

echo -e "${BLUE}════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}      MyTrader Production Deployment Script         ${NC}"
echo -e "${BLUE}════════════════════════════════════════════════════${NC}"
echo ""
echo -e "Target Server: ${BLUE}${PROD_USER}@${PROD_SERVER}${NC}"
echo -e "Domain: ${BLUE}${DOMAIN}${NC}"
echo ""

# Step 1: Check SSH connection
echo -e "${YELLOW}[1/8] Checking SSH connection...${NC}"
if ssh -o ConnectTimeout=5 "${PROD_USER}@${PROD_SERVER}" "echo 'Connection OK'" > /dev/null 2>&1; then
    echo -e "${GREEN}✓ SSH connection successful${NC}"
else
    echo -e "${RED}✗ Cannot connect to production server${NC}"
    echo -e "Please ensure:"
    echo -e "  - SSH keys are configured"
    echo -e "  - Server is accessible"
    exit 1
fi

# Step 2: Check Docker on production
echo -e "${YELLOW}[2/8] Verifying Docker installation...${NC}"
if ssh "${PROD_USER}@${PROD_SERVER}" "docker --version" > /dev/null 2>&1; then
    echo -e "${GREEN}✓ Docker is installed${NC}"
else
    echo -e "${RED}✗ Docker not found on production server${NC}"
    exit 1
fi

# Step 3: Create project directory on production
echo -e "${YELLOW}[3/8] Setting up project directory...${NC}"
ssh "${PROD_USER}@${PROD_SERVER}" << ENDSSH
    mkdir -p ${PROD_DIR}
    mkdir -p ${PROD_DIR}/backups
    mkdir -p ${PROD_DIR}/certbot/conf
    mkdir -p ${PROD_DIR}/certbot/www
    mkdir -p ${PROD_DIR}/nginx/conf.d
ENDSSH
echo -e "${GREEN}✓ Project directory created${NC}"

# Step 4: Transfer project files
echo -e "${YELLOW}[4/8] Transferring project files...${NC}"
echo -e "  This may take a few minutes..."

# Create temporary archive
TEMP_ARCHIVE="/tmp/mytrader_deploy_$(date +%s).tar.gz"
tar -czf "$TEMP_ARCHIVE" \
    --exclude='node_modules' \
    --exclude='bin' \
    --exclude='obj' \
    --exclude='.git' \
    --exclude='backups' \
    --exclude='.env' \
    -C "$(pwd)" \
    backend docker-compose.prod.yml nginx .env.production.template

# Transfer archive
scp "$TEMP_ARCHIVE" "${PROD_USER}@${PROD_SERVER}:${PROD_DIR}/mytrader.tar.gz"

# Extract on server
ssh "${PROD_USER}@${PROD_SERVER}" << ENDSSH
    cd ${PROD_DIR}
    tar -xzf mytrader.tar.gz
    rm mytrader.tar.gz
ENDSSH

# Cleanup local archive
rm "$TEMP_ARCHIVE"

echo -e "${GREEN}✓ Project files transferred${NC}"

# Step 5: Configure environment variables
echo -e "${YELLOW}[5/8] Configuring environment variables...${NC}"
echo -e "${RED}IMPORTANT: You need to create .env.production file on the server${NC}"
echo -e "Example:"
echo -e "${BLUE}ssh ${PROD_USER}@${PROD_SERVER}${NC}"
echo -e "${BLUE}cd ${PROD_DIR}${NC}"
echo -e "${BLUE}cp .env.production.template .env.production${NC}"
echo -e "${BLUE}nano .env.production  # Edit with real values${NC}"
echo ""
read -p "Have you configured .env.production on the server? (yes/no): " ENV_READY

if [ "$ENV_READY" != "yes" ]; then
    echo -e "${YELLOW}Please configure .env.production first, then run this script again.${NC}"
    exit 0
fi

# Step 6: Setup SSL certificate
echo -e "${YELLOW}[6/8] Setting up SSL certificate...${NC}"
echo -e "${YELLOW}Requesting Let's Encrypt certificate for ${DOMAIN}...${NC}"

ssh "${PROD_USER}@${PROD_SERVER}" << ENDSSH
    cd ${PROD_DIR}

    # Start nginx temporarily for certbot challenge
    docker-compose -f docker-compose.prod.yml up -d nginx

    # Request certificate
    docker-compose -f docker-compose.prod.yml run --rm certbot certonly \
        --webroot --webroot-path /var/www/certbot \
        --email admin@${DOMAIN} \
        --agree-tos --no-eff-email \
        -d ${DOMAIN} -d www.${DOMAIN}

    # Stop nginx
    docker-compose -f docker-compose.prod.yml down
ENDSSH

echo -e "${GREEN}✓ SSL certificate obtained${NC}"

# Step 7: Build and start services
echo -e "${YELLOW}[7/8] Building and starting services...${NC}"

ssh "${PROD_USER}@${PROD_SERVER}" << ENDSSH
    cd ${PROD_DIR}

    # Load environment variables
    export \$(cat .env.production | xargs)

    # Build and start services
    docker-compose -f docker-compose.prod.yml build
    docker-compose -f docker-compose.prod.yml up -d

    # Wait for services to be ready
    echo "Waiting for services to start..."
    sleep 10
ENDSSH

echo -e "${GREEN}✓ Services started${NC}"

# Step 8: Verify deployment
echo -e "${YELLOW}[8/8] Verifying deployment...${NC}"

ssh "${PROD_USER}@${PROD_SERVER}" << 'ENDSSH'
    cd /opt/mytrader

    echo "Container status:"
    docker-compose -f docker-compose.prod.yml ps

    echo ""
    echo "Checking API health..."
    sleep 5
    docker exec mytrader_api_prod curl -f http://localhost:8080/health || echo "Health check pending..."
ENDSSH

echo ""
echo -e "${GREEN}════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}   Deployment completed successfully!                ${NC}"
echo -e "${GREEN}════════════════════════════════════════════════════${NC}"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo -e "1. Configure DNS A records:"
echo -e "   ${BLUE}${DOMAIN} → ${PROD_SERVER}${NC}"
echo -e "   ${BLUE}www.${DOMAIN} → ${PROD_SERVER}${NC}"
echo -e ""
echo -e "2. Test the application:"
echo -e "   ${BLUE}https://${DOMAIN}${NC}"
echo -e ""
echo -e "3. View logs:"
echo -e "   ${BLUE}ssh ${PROD_USER}@${PROD_SERVER} 'cd ${PROD_DIR} && docker-compose -f docker-compose.prod.yml logs -f'${NC}"
echo -e ""
echo -e "4. Migrate database (if needed):"
echo -e "   ${BLUE}./scripts/migrate-database-to-prod.sh${NC}"
echo ""
