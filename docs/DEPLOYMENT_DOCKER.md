# TaskFlow Docker Deployment Guide

This guide covers deploying TaskFlow to a VPS using Docker with a multi-container architecture (React + Nginx + ASP.NET Core API).

## Prerequisites

- Docker and Docker Compose installed
- VPS with at least 2GB RAM
- Domain name (optional, for HTTPS)
- Basic knowledge of Docker and Linux

## Architecture Overview

TaskFlow uses a **multi-container deployment pattern** where:
- React frontend is built into static files
- Nginx serves static files and reverse-proxies API requests
- ASP.NET Core API handles backend logic
- All containers run on the same Docker network

This architecture provides better isolation and scalability compared to single-service deployment.

## Directory Structure

```
TaskFlow/
├── TaskFlow.API/
├── frontend/
├── Dockerfile (API)
├── Dockerfile.frontend (React)
├── docker-compose.yml
└── nginx.conf
```

## Docker Configuration

### Dockerfile for ASP.NET Core API

Create `Dockerfile` in the root:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["TaskFlow.API/TaskFlow.API.csproj", "TaskFlow.API/"]
COPY ["TaskFlow.Application/TaskFlow.Application.csproj", "TaskFlow.Application/"]
COPY ["TaskFlow.Domain/TaskFlow.Domain.csproj", "TaskFlow.Domain/"]
COPY ["TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj", "TaskFlow.Infrastructure/"]
RUN dotnet restore "TaskFlow.API/TaskFlow.API.csproj"
COPY . .
WORKDIR "/src/TaskFlow.API"
RUN dotnet build "TaskFlow.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "TaskFlow.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "TaskFlow.API.dll"]
```

### Dockerfile for React Frontend

Create `Dockerfile.frontend` in the root:

```dockerfile
# Build stage
FROM node:20-alpine AS build
WORKDIR /app
COPY frontend/package*.json ./
RUN npm install
COPY frontend/ .
RUN npm run build

# Runtime stage (serve with Nginx)
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

### Nginx Configuration

Create `nginx.conf` in the root:

```nginx
events {
    worker_connections 1024;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;

    upstream api {
        server api:80;
    }

    server {
        listen 80;
        server_name localhost;

        # Serve React static files
        location / {
            root /usr/share/nginx/html;
            try_files $uri $uri/ /index.html;
        }

        # Proxy API requests
        location /api/ {
            proxy_pass http://api/;
            proxy_http_version 1.1;
            proxy_set_header Upgrade $http_upgrade;
            proxy_set_header Connection 'upgrade';
            proxy_set_header Host $host;
            proxy_cache_bypass $http_upgrade;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
        }

        # Health check
        location /health {
            proxy_pass http://api/health;
        }
    }
}
```

### Docker Compose Configuration

Create `docker-compose.yml` in the root:

```yaml
version: '3.8'

services:
  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: taskflow-api
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:80
    networks:
      - taskflow-network
    restart: unless-stopped

  frontend:
    build:
      context: .
      dockerfile: Dockerfile.frontend
    container_name: taskflow-frontend
    ports:
      - "80:80"
    depends_on:
      - api
    networks:
      - taskflow-network
    restart: unless-stopped

networks:
  taskflow-network:
    driver: bridge
```

## Deployment Steps

### Step 1: Prepare VPS

```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Install Docker Compose
sudo apt install docker-compose -y

# Add user to docker group
sudo usermod -aG docker $USER
newgrp docker
```

### Step 2: Deploy Code to VPS

```bash
# Clone repository
git clone <your-repo-url> TaskFlow
cd TaskFlow

# Or upload files using scp/rsync
scp -r ./TaskFlow user@your-vps:/home/user/TaskFlow
```

### Step 3: Build and Run Containers

```bash
# Build containers
docker-compose build

# Start containers
docker-compose up -d

# Check status
docker-compose ps
```

### Step 4: Verify Deployment

```bash
# Check API health
curl http://localhost:5000/health

# Check weather forecast
curl http://localhost:5000/weatherforecast

# Access frontend
curl http://localhost
```

## SSL/HTTPS Configuration (Optional)

### Using Let's Encrypt with Certbot

```bash
# Install Certbot
sudo apt install certbot python3-certbot-nginx -y

# Generate certificate
sudo certbot --nginx -d yourdomain.com

# Auto-renewal is configured automatically
```

### Update Nginx Configuration for HTTPS

Modify `nginx.conf`:

```nginx
server {
    listen 80;
    server_name yourdomain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name yourdomain.com;

    ssl_certificate /etc/letsencrypt/live/yourdomain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/yourdomain.com/privkey.pem;

    # ... rest of configuration
}
```

## Environment Variables

Create `.env` file:

```env
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=your-connection-string
Cloudinary__CloudName=your-cloud-name
Cloudinary__ApiKey=your-api-key
Cloudinary__ApiSecret=your-api-secret
Resend__ApiKey=your-resend-api-key
```

Update `docker-compose.yml`:

```yaml
services:
  api:
    env_file:
      - .env
```

## Database Setup (Optional)

If using PostgreSQL:

```yaml
services:
  api:
    # ... existing config
    depends_on:
      - db

  db:
    image: postgres:16-alpine
    container_name: taskflow-db
    environment:
      POSTGRES_DB: taskflow
      POSTGRES_USER: taskflow
      POSTGRES_PASSWORD: your-password
    volumes:
      - postgres-data:/var/lib/postgresql/data
    networks:
      - taskflow-network
    restart: unless-stopped

volumes:
  postgres-data:
```

## Monitoring

### View Logs

```bash
# View all logs
docker-compose logs -f

# View API logs
docker-compose logs -f api

# View frontend logs
docker-compose logs -f frontend
```

### Resource Monitoring

```bash
# Container stats
docker stats

# Disk usage
docker system df
```

## Scaling

### Scale API

```bash
# Scale to 3 instances
docker-compose up -d --scale api=3
```

Note: You'll need a load balancer (like Nginx) to distribute traffic.

## Backup

### Backup Database

```bash
# Backup PostgreSQL
docker exec taskflow-db pg_dump -U taskflow taskflow > backup.sql

# Restore
docker exec -i taskflow-db psql -U taskflow taskflow < backup.sql
```

### Backup Volume

```bash
# Backup volume
docker run --rm -v taskflow_postgres-data:/data -v $(pwd):/backup alpine tar czf /backup/postgres-backup.tar.gz /data
```

## Troubleshooting

### Container Won't Start

```bash
# Check logs
docker-compose logs api

# Inspect container
docker inspect taskflow-api

# Rebuild
docker-compose build --no-cache api
docker-compose up -d api
```

### Network Issues

```bash
# Check network
docker network inspect taskflow_taskflow-network

# Restart network
docker-compose down
docker-compose up -d
```

### Port Conflicts

```bash
# Check what's using the port
sudo lsof -i :80
sudo lsof -i :5000

# Change ports in docker-compose.yml
```

## Security Considerations

1. **Use non-root user in containers**
2. **Enable HTTPS in production**
3. **Use secrets management for sensitive data**
4. **Regularly update base images**
5. **Limit container resources**
6. **Use firewall (UFW)**
   ```bash
   sudo ufw allow 80/tcp
   sudo ufw allow 443/tcp
   sudo ufw enable
   ```

## Cost Estimate

Based on common VPS providers (as of 2026):

| Provider | Plan | RAM | CPU | Estimated Cost/Month |
|----------|------|-----|-----|---------------------|
| DigitalOcean | Basic | 2GB | 1 vCPU | ~$6 |
| Linode | Nanode | 2GB | 1 vCPU | ~$5 |
| AWS | t3.small | 2GB | 2 vCPU | ~$20 |
| Azure | B1s | 1GB | 1 vCPU | ~$15 |

Docker deployment is generally more cost-effective than Azure App Service for small applications.

## Cleanup

```bash
# Stop containers
docker-compose down

# Remove volumes
docker-compose down -v

# Remove images
docker system prune -a
```

## Comparison: Azure vs Docker

| Feature | Azure App Service | Docker VPS |
|---------|------------------|------------|
| Setup Complexity | Low | Medium |
| Cost | Higher ($15+/month) | Lower ($5-20/month) |
| Scalability | Easy (auto-scale) | Manual |
| Maintenance | Managed | Self-managed |
| Control | Limited | Full |
| Best For | MVP, Quick Deploy | Production, Cost-Optimized |

## Support

For issues:
- Check Docker logs: `docker-compose logs`
- Verify network connectivity
- Check port availability
- Review Docker documentation

---

**Last Updated:** November 3, 2026
**.NET Version:** 10.0.403
