# MU Online Dual-Realm Deployment Guide

## Prerequisites

- Docker & Docker Compose 2.x
- .NET 9 SDK (for local development)
- PostgreSQL 15+ client tools (for manual DB operations)
- Git

## Environment Variables Setup

### Required Variables

Create a `.env` file in the project root based on `.env.template`:

```bash
# Database Configuration
POSTGRES_HOST=localhost
POSTGRES_PORT=5438
POSTGRES_DB=openmu
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<your-secure-password>

MYSQL_HOST=localhost
MYSQL_PORT=3307
MYSQL_DB=database_login
MYSQL_USER=root
MYSQL_PASSWORD=<your-secure-password>

# Service URLs
GAMESERVER_URL=http://localhost:8080
PLAYERWEB_URL=http://localhost:3007

# Security
ALLOWED_HOSTS=localhost;*.mutopia.local;127.0.0.1
```

**CRITICAL:** Never commit `.env` to version control. Only `.env.template` should be committed.

## Database Initialization

### First-Time Setup

1. Start PostgreSQL container:
```bash
docker compose up -d database
```

2. Wait for database to be healthy:
```bash
docker compose ps database
# Should show "healthy" status
```

3. Initialize schema (OpenMU auto-migrates on first run):
```bash
docker compose up -d openmu-server
# Watch logs: docker compose logs -f openmu-server
```

### Manual Migration (if needed)

```bash
# Apply EF Core migrations manually
cd src/server-s6/src/Startup
dotnet ef database update --connection "Host=localhost;Port=5438;Database=openmu;Username=postgres;Password=<password>"
```

## Docker Compose Deployment

### Development Environment

```bash
# Build images
docker compose build

# Start all services
docker compose up -d

# Verify health status
docker compose ps
# All services should show "healthy" or "running"

# Check logs
docker compose logs -f
```

### Production Environment

1. **Update environment variables** in `.env`:
   - Use strong passwords (min 16 characters)
   - Set HTTPS URLs for GameserverUrl and PlayerWebUrl
   - Restrict ALLOWED_HOSTS to actual domain names

2. **Enable HTTPS** (add reverse proxy like Traefik or nginx):
```yaml
# Add to docker-compose.yml
  traefik:
    image: traefik:v2.10
    # ... Traefik configuration for HTTPS
```

3. **Deploy with production settings**:
```bash
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

## Health Check Verification

### Service Health Endpoints

After deployment, verify all services are healthy:

```bash
# Database
docker exec openmu-db pg_isready -U postgres
# Expected: "postgres:5432 - accepting connections"

# Game Server
curl -f http://localhost:8080/health
# Expected: HTTP 200 OK

# Web Portal
curl -f http://localhost:3007/health
# Expected: HTTP 200 OK

# All services status
docker compose ps
# All should show "Up" with "(healthy)" status
```

### Automated Health Monitoring

Docker Compose automatically checks health every 30 seconds. View status:
```bash
watch -n 5 'docker compose ps'
```

## Rollback Procedures

### Quick Rollback (Docker)

```bash
# Stop current deployment
docker compose down

# Restore previous image tag
docker tag mu_openmu-server:previous mu_openmu-server:latest

# Restart
docker compose up -d
```

### Database Rollback

```bash
# Stop services
docker compose stop openmu-server openmu-playerweb

# Restore from backup (see Backup section)
docker exec -i openmu-db psql -U postgres openmu < backup_YYYYMMDD.sql

# Restart services
docker compose start openmu-server openmu-playerweb
```

## Backup and Restore Procedures

### Automated Daily Backup

Add to crontab:
```bash
0 2 * * * /path/to/backup-script.sh
```

`backup-script.sh`:
```bash
#!/bin/bash
BACKUP_DIR=/backup/openmu
DATE=$(date +%Y%m%d_%H%M%S)

# PostgreSQL backup
docker exec openmu-db pg_dump -U postgres openmu | gzip > ${BACKUP_DIR}/openmu_${DATE}.sql.gz

# Keep last 30 days
find ${BACKUP_DIR} -name "openmu_*.sql.gz" -mtime +30 -delete
```

### Manual Backup

```bash
# Full database dump
docker exec openmu-db pg_dump -U postgres openmu > openmu_backup.sql

# Compressed backup
docker exec openmu-db pg_dump -U postgres openmu | gzip > openmu_backup.sql.gz
```

### Restore from Backup

```bash
# Stop services accessing database
docker compose stop openmu-server openmu-playerweb

# Restore database
gunzip -c openmu_backup.sql.gz | docker exec -i openmu-db psql -U postgres openmu

# Restart services
docker compose start openmu-server openmu-playerweb

# Verify data integrity
docker compose logs -f openmu-server
```

## Monitoring and Troubleshooting

### View Logs

```bash
# All services
docker compose logs -f

# Specific service
docker compose logs -f openmu-server

# Last 100 lines
docker compose logs --tail=100 openmu-server

# Since timestamp
docker compose logs --since 2026-08-27T10:00:00 openmu-server
```

### Common Issues

#### Service Won't Start

```bash
# Check container status
docker compose ps

# Inspect container
docker inspect openmu-server

# Check health check logs
docker inspect openmu-server | grep -A 10 Health
```

#### Database Connection Issues

```bash
# Test PostgreSQL connection
docker exec openmu-db psql -U postgres -c "\l"

# Check network connectivity
docker compose exec openmu-server ping database
```

#### Port Already in Use

```bash
# Find process using port
lsof -i :8080  # Linux/Mac
netstat -ano | findstr :8080  # Windows

# Change port in docker-compose.yml or kill process
```

## Security Checklist

Before production deployment:

- [ ] All passwords changed from defaults
- [ ] `.env` file NOT committed to git
- [ ] HTTPS enabled (not HTTP)
- [ ] `AllowedHosts` restricted to actual domains
- [ ] Firewall rules configured (only expose necessary ports)
- [ ] Database backups automated
- [ ] Security updates applied to base images
- [ ] Secrets scanning enabled in CI/CD
- [ ] Health monitoring alerts configured

## Performance Tuning

### PostgreSQL

Edit `docker-compose.yml`:
```yaml
  database:
    environment:
      # Add performance settings
      POSTGRES_SHARED_BUFFERS: "256MB"
      POSTGRES_WORK_MEM: "16MB"
      POSTGRES_MAX_CONNECTIONS: "200"
```

### .NET Application

Edit `src/server-s6/appsettings.json`:
```json
{
  "Kestrel": {
    "Limits": {
      "MaxConcurrentConnections": 1000,
      "MaxRequestBodySize": 10485760
    }
  }
}
```

## Support and Documentation

- Architecture docs: `docs/DUAL_SEASON_ARCHITECTURE_AND_EVALUATION_REPORT.md`
- OpenMU upstream: https://github.com/MUnique/OpenMU
- Issue tracker: [Your GitHub Issues URL]

## Update Log

| Date | Version | Changes |
|------|---------|---------|
| 2026-08-27 | 1.0 | Initial deployment guide with health checks and environment variables |

---

**Last Updated:** 2026-08-27  
**Maintainer:** Solo-Code Team
