# TaskFlow

An E-Commerce solution built with .NET 10, React, PostgreSQL, and Docker.

## Quick Start

### Local Development

```bash
# Set up environment variables
cp .env.dev .env
# Edit .env with your Google OAuth credentials

# Build and run with Docker
docker-compose -f docker-compose.dev.yml up -d

# Access the application
# Frontend: http://localhost
# API: http://localhost:5000
# Health Check: http://localhost:5000/health
```

### Documentation

Comprehensive documentation is available in the [docs/](docs/) folder:

- [Getting Started](docs/GETTING_STARTED.md) - Quick start guide
- [Environment Setup](docs/ENVIRONMENT_SETUP.md) - Environment variable configuration
- [Database Setup](docs/DATABASE_SETUP.md) - PostgreSQL and EF Core configuration
- [Google Authentication](docs/GOOGLE_AUTH_SETUP.md) - OAuth integration guide
- [Production Deployment](docs/PRODUCTION_DEPLOYMENT.md) - VPS deployment with Nginx Proxy Manager
- [Azure Deployment](docs/DEPLOYMENT.md) - Azure App Service deployment guide
- [Docker Deployment](docs/DEPLOYMENT_DOCKER.md) - Docker VPS deployment guide

## Architecture

- **Backend**: ASP.NET Core 10.0 (API, Application, Domain, Infrastructure layers)
- **Frontend**: React 19 + TypeScript + Vite
- **Database**: PostgreSQL 17
- **Deployment**: Docker Compose with Nginx Proxy Manager
- **Authentication**: Google OAuth 2.0

## Technology Stack

### Backend
- .NET 10.0
- Entity Framework Core 10.0
- PostgreSQL 17
- Serilog (logging)
- Google Authentication

### Frontend
- React 19.2.4
- TypeScript 5.9.3
- Vite 8.0.1
- Nginx (production)

### Infrastructure
- Docker & Docker Compose
- Nginx Proxy Manager (SSL certificates)
- DuckDNS (dynamic DNS)

## Features

- ✅ Google Sign In/Sign Up
- ✅ User management with PostgreSQL
- ✅ Database migrations
- ✅ Health check endpoints
- ✅ Docker containerization
- ✅ SSL/TLS with Let's Encrypt
- ✅ Dynamic DNS with DuckDNS

## Development Status

| Component | Status |
|-----------|--------|
| .NET 10 Upgrade | ✅ Complete |
| PostgreSQL Integration | ✅ Complete |
| Google OAuth | ✅ Infrastructure Ready |
| Authentication Endpoints | ✅ Complete |
| User Entity | ✅ Complete |
| Database Migrations | ✅ Complete |
| Docker Configuration | ✅ Complete |
| Nginx Proxy Manager | ✅ Configured |
| Production Deployment | ⏳ Ready for deployment |

## Project Structure

```
TaskFlow/
├── TaskFlow.API/          # Web API layer
├── TaskFlow.Application/  # Business logic layer
├── TaskFlow.Domain/       # Domain entities
├── TaskFlow.Infrastructure/ # Database configuration
├── TaskFlow.UnitTests/    # Unit tests
├── frontend/              # React frontend
├── docs/                  # Documentation
├── docker-compose.yml    # Docker configuration
└── .env                  # Environment variables
```

## Quick Links

- [Deployment Guide](docs/PRODUCTION_DEPLOYMENT.md)
- [Google Auth Setup](docs/GOOGLE_AUTH_SETUP.md)
- [Database Setup](docs/DATABASE_SETUP.md)
- [Docker Deployment](docs/DEPLOYMENT_DOCKER.md)

## License

Proprietary - All rights reserved.

