#!/bin/bash
# Database Backup Script for MyTrader
# Usage: ./backup-database.sh [local|remote]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
BACKUP_DIR="./backups"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="mytrader_backup_${TIMESTAMP}.sql"

# Determine if backup is local or remote
MODE=${1:-local}

echo -e "${YELLOW}Starting MyTrader database backup...${NC}"

# Create backup directory if it doesn't exist
mkdir -p $BACKUP_DIR

if [ "$MODE" == "local" ]; then
    echo -e "${GREEN}Backing up LOCAL database...${NC}"

    # Backup from local Docker container
    docker exec mytrader_postgres pg_dump -U postgres -d mytrader > "$BACKUP_DIR/$BACKUP_FILE"

    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ Local backup successful!${NC}"
        echo -e "Backup file: ${GREEN}$BACKUP_DIR/$BACKUP_FILE${NC}"

        # Compress the backup
        gzip "$BACKUP_DIR/$BACKUP_FILE"
        echo -e "${GREEN}✓ Backup compressed: $BACKUP_DIR/$BACKUP_FILE.gz${NC}"

        # Show backup size
        SIZE=$(du -h "$BACKUP_DIR/$BACKUP_FILE.gz" | cut -f1)
        echo -e "Backup size: ${GREEN}$SIZE${NC}"
    else
        echo -e "${RED}✗ Backup failed!${NC}"
        exit 1
    fi

elif [ "$MODE" == "remote" ]; then
    echo -e "${GREEN}Backing up PRODUCTION database...${NC}"

    # Backup from production Docker container
    docker exec mytrader_postgres_prod pg_dump -U postgres -d mytrader > "$BACKUP_DIR/$BACKUP_FILE"

    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ Production backup successful!${NC}"
        echo -e "Backup file: ${GREEN}$BACKUP_DIR/$BACKUP_FILE${NC}"

        # Compress the backup
        gzip "$BACKUP_DIR/$BACKUP_FILE"
        echo -e "${GREEN}✓ Backup compressed: $BACKUP_DIR/$BACKUP_FILE.gz${NC}"

        # Show backup size
        SIZE=$(du -h "$BACKUP_DIR/$BACKUP_FILE.gz" | cut -f1)
        echo -e "Backup size: ${GREEN}$SIZE${NC}"
    else
        echo -e "${RED}✗ Backup failed!${NC}"
        exit 1
    fi
else
    echo -e "${RED}Invalid mode. Use: ./backup-database.sh [local|remote]${NC}"
    exit 1
fi

# Clean up old backups (keep last 7 days)
echo -e "${YELLOW}Cleaning up old backups (keeping last 7 days)...${NC}"
find $BACKUP_DIR -name "mytrader_backup_*.sql.gz" -mtime +7 -delete
echo -e "${GREEN}✓ Cleanup complete${NC}"

echo -e "${GREEN}Backup process completed successfully!${NC}"
