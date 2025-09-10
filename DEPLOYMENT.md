# MyTrader Production Deployment Guide

This guide provides step-by-step instructions for deploying MyTrader to production using Docker and Docker Compose.

## Prerequisites

- Docker and Docker Compose installed
- Domain name configured (for SSL/HTTPS)
- Server with at least 4GB RAM and 2 CPU cores
- PostgreSQL and Redis (included in Docker Compose)

## Quick Start

1. **Clone and Configure**
   ```bash
   git clone <your-repo-url>
   cd myTrader/backend
   cp .env.production.template .env.production
   ```

2. **Configure Environment Variables**
   Edit `.env.production` with your actual values:
   ```bash
   nano .env.production
   ```

3. **Deploy**
   ```bash
   ./scripts/deploy.sh deploy --monitoring --logging
   ```

## Detailed Setup

### 1. Environment Configuration

Copy the environment template and configure it:
```bash
cp .env.production.template .env.production
```

**Required Variables to Configure:**
- `DOMAIN`: Your domain name
- `POSTGRES_PASSWORD`: Strong database password
- `JWT_SECRET_KEY`: 256-bit secret key for JWT tokens
- `REDIS_PASSWORD`: Redis authentication password
- `CORS_ORIGINS`: Allowed frontend origins

**Optional but Recommended:**
- `BINANCE_API_KEY` & `BINANCE_API_SECRET`: For live market data
- `SENDGRID_API_KEY`: For email notifications
- `FIREBASE_SERVER_KEY`: For push notifications

### 2. SSL Certificate Setup

For production, you'll need SSL certificates. You can use:

**Option A: Let's Encrypt (Recommended)**
```bash
# Install certbot
sudo apt-get install certbot

# Get certificate
sudo certbot certonly --standalone -d yourdomain.com -d www.yourdomain.com

# Copy certificates to nginx directory
mkdir -p nginx/ssl
sudo cp /etc/letsencrypt/live/yourdomain.com/fullchain.pem nginx/ssl/cert.pem
sudo cp /etc/letsencrypt/live/yourdomain.com/privkey.pem nginx/ssl/key.pem
```

**Option B: Custom Certificates**
Place your SSL certificates in:
- `nginx/ssl/cert.pem` (certificate file)
- `nginx/ssl/key.pem` (private key file)

### 3. Deployment Options

The deployment script supports several options:

**Basic Deployment:**
```bash
./scripts/deploy.sh deploy
```

**Full Deployment with Monitoring:**
```bash
./scripts/deploy.sh deploy --monitoring --logging --backup
```

**Build Only:**
```bash
./scripts/deploy.sh build
```

**Deploy Specific Version:**
```bash
IMAGE_TAG=v1.2.3 ./scripts/deploy.sh deploy
```

### 4. Service Architecture

The production deployment includes:

- **MyTrader API** (Port 8080): Main application server
- **PostgreSQL** (Port 5432): Primary database
- **Redis** (Port 6379): Caching and sessions
- **NGINX** (Ports 80/443): Reverse proxy and SSL termination

**Optional Services:**
- **Grafana** (Port 3000): Monitoring dashboards
- **Prometheus** (Port 9090): Metrics collection
- **Seq** (Port 5341): Centralized logging

### 5. Post-Deployment Verification

After deployment, verify all services:

```bash
# Check service status
./scripts/deploy.sh status

# Check API health
curl https://yourdomain.com/health

# View logs
docker-compose -f docker-compose.production.yml logs -f mytrader-api
```

### 6. Database Migration

The deployment script automatically runs migrations, but you can run them manually:

```bash
./scripts/deploy.sh migrate
```

### 7. Backup Strategy

**Automated Backups:**
Enable the backup service in your deployment:
```bash
./scripts/deploy.sh deploy --backup
```

**Manual Backup:**
```bash
./scripts/deploy.sh backup
```

**Restore from Backup:**
```bash
docker-compose -f docker-compose.production.yml exec postgres psql -U postgres -d mytrader < backups/backup_file.sql
```

## Production Checklist

Before going live, ensure:

- [ ] Environment variables are configured with production values
- [ ] SSL certificates are installed and valid
- [ ] Database passwords are secure (not default values)
- [ ] JWT secret key is randomly generated and secure
- [ ] CORS origins are configured for your frontend domains
- [ ] Firewall is configured to allow only necessary ports
- [ ] Backup strategy is in place and tested
- [ ] Monitoring is enabled and alerts are configured
- [ ] Log retention policies are set

## Monitoring and Maintenance

### Health Checks

All services have built-in health checks:
- API: `https://yourdomain.com/health`
- Database: Automatic container health check
- Redis: Automatic container health check

### Logs

View service logs:
```bash
# API logs
docker-compose -f docker-compose.production.yml logs -f mytrader-api

# Database logs
docker-compose -f docker-compose.production.yml logs -f postgres

# All services
docker-compose -f docker-compose.production.yml logs -f
```

### Updates

To update the application:
```bash
# Build new version
IMAGE_TAG=v1.2.4 ./scripts/deploy.sh build

# Deploy update
IMAGE_TAG=v1.2.4 ./scripts/deploy.sh deploy --skip-backup
```

### Resource Monitoring

Monitor resource usage:
```bash
# Container resource usage
docker stats

# System resources
htop
df -h
```

## Scaling Considerations

For high-traffic deployments:

1. **Database Scaling:**
   - Use read replicas for reporting queries
   - Consider connection pooling (PgBouncer)
   - Implement database sharding if needed

2. **API Scaling:**
   - Run multiple API instances behind load balancer
   - Use external Redis for session storage
   - Implement API rate limiting

3. **Caching:**
   - Use Redis Cluster for cache scaling
   - Implement CDN for static assets
   - Add application-level caching

## Security Best Practices

1. **Network Security:**
   - Use private Docker networks
   - Implement firewall rules
   - Use VPN for administrative access

2. **Application Security:**
   - Keep dependencies updated
   - Use secrets management (Docker Secrets/Kubernetes Secrets)
   - Implement API rate limiting
   - Enable HTTPS everywhere

3. **Database Security:**
   - Use strong passwords
   - Enable SSL for database connections
   - Regular security updates
   - Database access logging

## Troubleshooting

### Common Issues

**1. API Won't Start:**
```bash
# Check logs
docker-compose -f docker-compose.production.yml logs mytrader-api

# Common causes:
# - Database connection issues
# - Missing environment variables
# - Port conflicts
```

**2. Database Connection Failed:**
```bash
# Check database status
docker-compose -f docker-compose.production.yml exec postgres pg_isready

# Reset database connection
docker-compose -f docker-compose.production.yml restart postgres mytrader-api
```

**3. SSL Certificate Issues:**
```bash
# Check certificate validity
openssl x509 -in nginx/ssl/cert.pem -text -noout

# Update certificates
sudo certbot renew
```

**4. High Memory Usage:**
```bash
# Check container memory usage
docker stats

# Restart services if needed
docker-compose -f docker-compose.production.yml restart
```

### Support Commands

```bash
# Show deployment status
./scripts/deploy.sh status

# Clean up unused resources
./scripts/deploy.sh cleanup

# Create emergency backup
./scripts/deploy.sh backup

# View help
./scripts/deploy.sh --help
```

## Performance Optimization

1. **Database Optimization:**
   - Index frequently queried columns
   - Use connection pooling
   - Regular VACUUM and ANALYZE

2. **API Optimization:**
   - Enable response compression
   - Use Redis for caching
   - Implement query optimization

3. **Infrastructure Optimization:**
   - Use SSD storage
   - Allocate adequate RAM
   - Monitor CPU usage

---

For support or questions about deployment, check the [API Documentation](API_DOCUMENTATION.md) or create an issue in the repository.