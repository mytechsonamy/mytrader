#!/bin/bash
# Initial Production Server Setup for MyTrader
# This script prepares the server for deployment

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
PROD_SERVER="213.238.180.201"
PROD_USER="root"
PROD_DIR="/opt/mytrader"

echo -e "${BLUE}════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}   MyTrader Production Server Initial Setup         ${NC}"
echo -e "${BLUE}════════════════════════════════════════════════════${NC}"
echo ""

# Step 1: Test SSH connection
echo -e "${YELLOW}[1/5] Testing SSH connection to ${PROD_SERVER}...${NC}"
if ssh -o ConnectTimeout=5 "${PROD_USER}@${PROD_SERVER}" "echo 'SSH connection successful'" > /dev/null 2>&1; then
    echo -e "${GREEN}✓ SSH connection successful${NC}"
else
    echo -e "${RED}✗ Cannot connect to server${NC}"
    echo -e "Please check:"
    echo -e "  - Server IP is correct: ${PROD_SERVER}"
    echo -e "  - SSH is running on server"
    echo -e "  - You have SSH access"
    echo ""
    echo -e "Try manually: ${BLUE}ssh ${PROD_USER}@${PROD_SERVER}${NC}"
    exit 1
fi

# Step 2: Check Docker installation
echo -e "${YELLOW}[2/5] Checking Docker installation...${NC}"
DOCKER_VERSION=$(ssh "${PROD_USER}@${PROD_SERVER}" "docker --version 2>/dev/null || echo 'not_installed'")

if [[ "$DOCKER_VERSION" == *"not_installed"* ]]; then
    echo -e "${RED}✗ Docker not found${NC}"
    echo -e "${YELLOW}Would you like to install Docker? (yes/no)${NC}"
    read -p "> " INSTALL_DOCKER

    if [ "$INSTALL_DOCKER" == "yes" ]; then
        echo -e "${YELLOW}Installing Docker...${NC}"
        ssh "${PROD_USER}@${PROD_SERVER}" << 'ENDSSH'
            # Update system
            apt-get update
            apt-get upgrade -y

            # Install Docker
            curl -fsSL https://get.docker.com -o get-docker.sh
            sh get-docker.sh
            rm get-docker.sh

            # Install Docker Compose plugin
            apt-get install -y docker-compose-plugin

            # Start Docker
            systemctl start docker
            systemctl enable docker

            # Verify installation
            docker --version
            docker compose version
ENDSSH
        echo -e "${GREEN}✓ Docker installed successfully${NC}"
    else
        echo -e "${RED}Docker is required. Please install it manually.${NC}"
        exit 1
    fi
else
    echo -e "${GREEN}✓ Docker is installed: ${DOCKER_VERSION}${NC}"
fi

# Step 3: Create project directories
echo -e "${YELLOW}[3/5] Creating project directories...${NC}"
ssh "${PROD_USER}@${PROD_SERVER}" << ENDSSH
    # Create main directory
    mkdir -p ${PROD_DIR}

    # Create subdirectories
    mkdir -p ${PROD_DIR}/backend
    mkdir -p ${PROD_DIR}/nginx/conf.d
    mkdir -p ${PROD_DIR}/certbot/conf
    mkdir -p ${PROD_DIR}/certbot/www
    mkdir -p ${PROD_DIR}/backups
    mkdir -p ${PROD_DIR}/logs

    # Set permissions
    chmod -R 755 ${PROD_DIR}

    echo "Directories created:"
    ls -la ${PROD_DIR}/
ENDSSH
echo -e "${GREEN}✓ Project directories created${NC}"

# Step 4: Configure firewall (if UFW is installed)
echo -e "${YELLOW}[4/5] Configuring firewall...${NC}"
ssh "${PROD_USER}@${PROD_SERVER}" << 'ENDSSH'
    if command -v ufw &> /dev/null; then
        # Allow SSH, HTTP, HTTPS
        ufw allow 22/tcp comment 'SSH'
        ufw allow 80/tcp comment 'HTTP'
        ufw allow 443/tcp comment 'HTTPS'

        # Enable firewall (with confirmation skip)
        echo "y" | ufw enable || true

        echo "Firewall rules:"
        ufw status
    else
        echo "UFW not installed, skipping firewall configuration"
    fi
ENDSSH
echo -e "${GREEN}✓ Firewall configured${NC}"

# Step 5: Generate secure passwords
echo -e "${YELLOW}[5/5] Generating secure passwords...${NC}"
POSTGRES_PASS=$(openssl rand -base64 32)
JWT_SECRET=$(openssl rand -base64 64)

echo -e "${GREEN}✓ Passwords generated${NC}"
echo ""

# Display summary
echo -e "${GREEN}════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}   Server Setup Complete!                           ${NC}"
echo -e "${GREEN}════════════════════════════════════════════════════${NC}"
echo ""
echo -e "${YELLOW}Important: Save these credentials securely!${NC}"
echo ""
echo -e "${BLUE}PostgreSQL Password:${NC}"
echo -e "${GREEN}$POSTGRES_PASS${NC}"
echo ""
echo -e "${BLUE}JWT Secret Key:${NC}"
echo -e "${GREEN}$JWT_SECRET${NC}"
echo ""
echo -e "${YELLOW}Next Steps:${NC}"
echo -e "1. Create .env.production file on server:"
echo -e "   ${BLUE}ssh ${PROD_USER}@${PROD_SERVER}${NC}"
echo -e "   ${BLUE}cd ${PROD_DIR}${NC}"
echo -e "   ${BLUE}nano .env.production${NC}"
echo ""
echo -e "2. Add the following content:"
echo -e "${BLUE}---${NC}"
cat << EOF
POSTGRES_DB=mytrader
POSTGRES_USER=postgres
POSTGRES_PASSWORD=$POSTGRES_PASS

JWT_SECRET_KEY=$JWT_SECRET

ALPACA_API_KEY=your-alpaca-api-key-here
ALPACA_API_SECRET=your-alpaca-api-secret-here
EOF
echo -e "${BLUE}---${NC}"
echo ""
echo -e "3. Run deployment script:"
echo -e "   ${BLUE}./scripts/deploy-to-production.sh${NC}"
echo ""

# Save credentials to local file
CREDS_FILE="./credentials-$(date +%Y%m%d_%H%M%S).txt"
cat > "$CREDS_FILE" << EOF
MyTrader Production Credentials
Generated: $(date)

PostgreSQL Password:
$POSTGRES_PASS

JWT Secret Key:
$JWT_SECRET

Server: $PROD_SERVER
Project Directory: $PROD_DIR
EOF

echo -e "${GREEN}✓ Credentials saved to: ${CREDS_FILE}${NC}"
echo -e "${RED}⚠ Keep this file secure and delete after adding to server!${NC}"
echo ""
