# TaskFlow Documentation Index

Complete documentation for the TaskFlow e-commerce solution.

## Quick Links

- [Getting Started](GETTING_STARTED.md) - Quick start guide for local development
- [Environment Setup](ENVIRONMENT_SETUP.md) - Environment variable configuration
- [Production Deployment](PRODUCTION_DEPLOYMENT.md) - VPS deployment with Nginx Proxy Manager
- [Google Authentication](GOOGLE_AUTH_SETUP.md) - OAuth integration guide
- [Database Setup](DATABASE_SETUP.md) - PostgreSQL and EF Core configuration

---

## Documentation Overview

### Development

1. **[Getting Started](GETTING_STARTED.md)**
   - Prerequisites
   - Local development with Docker
   - Local development without Docker
   - Project structure
   - Available endpoints
   - Configuration
   - Troubleshooting

2. **[Environment Setup](ENVIRONMENT_SETUP.md)**
   - Environment files structure (.env.dev, .env.prod, .env.example)
   - Environment variables reference
   - Docker Compose configuration for different environments
   - Security best practices
   - Troubleshooting

### Deployment

2. **[Production Deployment](PRODUCTION_DEPLOYMENT.md)**
   - Architecture overview
   - Prerequisites
   - Step-by-step deployment to VPS
   - Nginx Proxy Manager configuration
   - SSL certificate setup
   - DuckDNS configuration
   - Security considerations
   - Maintenance procedures

3. **[Docker Deployment](DEPLOYMENT_DOCKER.md)**
   - Docker deployment alternatives
   - Multi-container architecture
   - Nginx reverse proxy configuration
   - Environment variables
   - Scaling options
   - Monitoring and logging
   - Troubleshooting

4. **[Azure Deployment](DEPLOYMENT.md)**
   - Azure App Service deployment
   - Runtime configuration
   - Build automation
   - Deployment scripts
   - Scaling options
   - Monitoring setup

### Infrastructure

5. **[Database Setup](DATABASE_SETUP.md)**
   - PostgreSQL integration
   - Entity Framework Core configuration
   - User entity structure
   - Migration commands
   - Connection string configuration
   - Production database setup
   - Security considerations

### Authentication

6. **[Google Authentication](GOOGLE_AUTH_SETUP.md)**
   - Google Cloud Console setup
   - OAuth consent screen configuration
   - Credential generation
   - Environment configuration
   - Testing procedures
   - Security best practices
   - Frontend integration examples

---

## Deployment Decision Guide

### Choose Deployment Method Based on Your Needs

| Scenario | Recommended Deployment | Documentation |
|----------|------------------------|----------------|
| Local Development | Docker Compose | [Getting Started](GETTING_STARTED.md) |
| MVP / Testing | Docker on VPS | [Docker Deployment](DEPLOYMENT_DOCKER.md) |
| Production with Custom Domain | Docker + Nginx Proxy Manager | [Production Deployment](PRODUCTION_DEPLOYMENT.md) |
| Managed Cloud Service | Azure App Service | [Azure Deployment](DEPLOYMENT.md) |
| Enterprise with Auto-scaling | Azure App Service with Scale | [Azure Deployment](DEPLOYMENT.md) |

---

## Documentation by Topic

### Authentication & Security
- [Google Authentication](GOOGLE_AUTH_SETUP.md) - OAuth integration
- Security best practices in each deployment guide

### Database
- [Database Setup](DATABASE_SETUP.md) - PostgreSQL configuration
- Migration management
- Connection strings
- Backup procedures

### Infrastructure
- Docker configuration in [Getting Started](GETTING_STARTED.md)
- Nginx configuration in [Production Deployment](PRODUCTION_DEPLOYMENT.md)
- SSL/TLS setup
- Dynamic DNS with DuckDNS

### Monitoring & Maintenance
- Container monitoring in [Production Deployment](PRODUCTION_DEPLOYMENT.md)
- Logging procedures
- Backup strategies
- Update procedures

---

## Current Status

| Component | Status | Documentation |
|-----------|--------|----------------|
| .NET 10 Upgrade | ✅ Complete | [Audit Report](../AUDIT_REPORT.md) |
| PostgreSQL Integration | ✅ Complete | [Database Setup](DATABASE_SETUP.md) |
| Google OAuth | ✅ Infrastructure Ready | [Google Auth Setup](GOOGLE_AUTH_SETUP.md) |
| Docker Configuration | ✅ Complete | [Getting Started](GETTING_STARTED.md) |
| Production Deployment | ⏳ Ready | [Production Deployment](PRODUCTION_DEPLOYMENT.md) |

---

## Project Audit

- [Audit Report](../AUDIT_REPORT.md) - Feature audit and test report
- [Test Report](../TEST_REPORT.md) - Local Docker test results

---

## Quick Reference

### Environment Variables

```env
GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your-client-secret
```

### Database Connection String

```
Host=db;Port=5432;Database=taskflow;Username=taskflow;Password=${DB_PASSWORD}
```

### Production URLs

- Frontend: https://taskflow-sourav.duckdns.org
- API: https://api.taskflow-sourav.duckdns.org
- Nginx Admin: http://77.25.144.193:81

---

## Contributing

When adding new features:
1. Update the relevant documentation
2. Add endpoints to the [Getting Started](GETTING_STARTED.md) guide
3. Update the project audit if major changes are made
4. Ensure all deployment guides are up to date

---

## Support

For deployment issues:
- Check the relevant deployment guide
- Review Docker logs: `docker-compose logs`
- Check application logs: `docker-compose logs api`
- Verify environment variables are set correctly

---

**Last Updated:** November 4, 2026
**Version:** 1.0
