#!/bin/bash
# Database Migration Script - Local to Production
# This script backs up local database and transfers it to production

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
BACKUP_DIR="./backups"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="mytrader_migration_${TIMESTAMP}.sql"
PROD_SERVER="213.238.180.201"
PROD_USER="root"  # Change this if using different user

echo -e "${BLUE}════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}   MyTrader Database Migration: Local → Production   ${NC}"
echo -e "${BLUE}════════════════════════════════════════════════════${NC}"
echo ""

# Step 1: Backup local database
echo -e "${YELLOW}[1/5] Backing up local database...${NC}"
mkdir -p $BACKUP_DIR

docker exec mytrader_postgres pg_dump -U postgres -d mytrader > "$BACKUP_DIR/$BACKUP_FILE"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Local database backed up successfully${NC}"
    SIZE=$(du -h "$BACKUP_DIR/$BACKUP_FILE" | cut -f1)
    echo -e "  Backup size: ${GREEN}$SIZE${NC}"
else
    echo -e "${RED}✗ Local backup failed!${NC}"
    exit 1
fi

# Step 2: Compress backup
echo -e "${YELLOW}[2/5] Compressing backup...${NC}"
gzip "$BACKUP_DIR/$BACKUP_FILE"
COMPRESSED_FILE="$BACKUP_FILE.gz"
echo -e "${GREEN}✓ Backup compressed${NC}"

# Step 3: Transfer backup to production server
echo -e "${YELLOW}[3/5] Transferring backup to production server...${NC}"
echo -e "  Target: ${BLUE}${PROD_USER}@${PROD_SERVER}${NC}"

scp "$BACKUP_DIR/$COMPRESSED_FILE" "${PROD_USER}@${PROD_SERVER}:/tmp/"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Backup transferred successfully${NC}"
else
    echo -e "${RED}✗ Transfer failed! Check SSH connection.${NC}"
    exit 1
fi

# Step 4: Restore on production
echo -e "${YELLOW}[4/5] Restoring database on production...${NC}"
echo -e "${RED}WARNING: This will replace the production database!${NC}"
read -p "Are you sure you want to continue? (yes/no): " CONFIRM

if [ "$CONFIRM" != "yes" ]; then
    echo -e "${YELLOW}Migration cancelled by user.${NC}"
    exit 0
fi

ssh "${PROD_USER}@${PROD_SERVER}" << 'ENDSSH'
cd /tmp
gunzip -c mytrader_migration_*.sql.gz > mytrader_restore.sql

# Drop existing connections and restore
docker exec mytrader_postgres_prod psql -U postgres -d postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'mytrader' AND pid <> pg_backend_pid();"
docker exec mytrader_postgres_prod psql -U postgres -d postgres -c "DROP DATABASE IF EXISTS mytrader;"
docker exec mytrader_postgres_prod psql -U postgres -d postgres -c "CREATE DATABASE mytrader;"
docker exec -i mytrader_postgres_prod psql -U postgres -d mytrader < mytrader_restore.sql

# Cleanup
rm -f mytrader_migration_*.sql.gz mytrader_restore.sql
ENDSSH

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Database restored on production${NC}"
else
    echo -e "${RED}✗ Restoration failed!${NC}"
    exit 1
fi

# Step 5: Verify restoration
echo -e "${YELLOW}[5/5] Verifying migration...${NC}"
ssh "${PROD_USER}@${PROD_SERVER}" << 'ENDSSH'
docker exec mytrader_postgres_prod psql -U postgres -d mytrader -c "SELECT COUNT(*) as table_count FROM information_schema.tables WHERE table_schema = 'public';"
ENDSSH

echo ""
echo -e "${GREEN}════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}   Migration completed successfully!                  ${NC}"
echo -e "${GREEN}════════════════════════════════════════════════════${NC}"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo -e "1. Verify application is working: ${BLUE}https://mytrader.tech${NC}"
echo -e "2. Check backend logs: ${BLUE}ssh $PROD_USER@$PROD_SERVER 'docker logs mytrader_api_prod'${NC}"
echo -e "3. Test WebSocket connections"
echo ""
