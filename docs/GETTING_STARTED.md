# Getting Started with TaskFlow

This guide will help you set up and run TaskFlow locally and prepare for production deployment.

## Prerequisites

- Docker and Docker Compose installed
- .NET 10 SDK (for local development without Docker)
- Node.js (for frontend development)
- Git (for version control)

## Local Development with Docker (Recommended)

### 1. Clone the Repository

```bash
git clone <repository-url>
cd TaskFlow
```

### 2. Configure Environment Variables

For local development, use the pre-configured development environment file:

```bash
# Copy the development environment template
cp .env.dev .env

# Edit .env with your actual values
# Add your Google OAuth credentials
# Set your database password
```

**Reference:** See [Environment Setup Guide](ENVIRONMENT_SETUP.md) for detailed configuration instructions.

**Note:** For local development without Google OAuth, you can leave the credentials empty. The application will show a warning but will still start, allowing you to test other endpoints.

### 3. Start the Application

```bash
# Use the development Docker Compose configuration
docker-compose -f docker-compose.dev.yml up -d --build
```

This will build and start:
- TaskFlow API (port 5000)
- TaskFlow Frontend (port 80)
- PostgreSQL database (port 5432)

**Note:** The development configuration does not include Nginx Proxy Manager or DuckDNS, which are only needed for production.

### 4. Verify the Application

```bash
# Check all containers are running
docker-compose ps

# Check API health
curl http://localhost:5000/health

# Check database connection
curl http://localhost:5000/db-health
```

### 5. Access the Application

- **Frontend**: http://localhost
- **API**: http://localhost:5000
- **Nginx Proxy Manager**: http://localhost:81
  - Default credentials: `admin@example.com` / `changeme`

### 6. Stop the Application

```bash
docker-compose down
```

## Local Development without Docker

### 1. Install Dependencies

**Backend (.NET):**
```bash
dotnet restore
```

**Frontend (Node.js):**
```bash
cd frontend
npm install
```

### 2. Start PostgreSQL

You can use Docker for the database only:
```bash
docker-compose up -d db
```

### 3. Configure Connection String

Update `TaskFlow.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=taskflow;Username=taskflow;Password=${DB_PASSWORD}"
  },
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    }
  }
}
```

### 4. Run Migrations

```bash
dotnet ef database update --project TaskFlow.Infrastructure --startup-project TaskFlow.API
```

### 5. Start the API

```bash
cd TaskFlow.API
dotnet run
```

### 6. Start the Frontend

```bash
cd frontend
npm run dev
```

- **Frontend**: http://localhost:5173 (Vite default)
- **API**: http://localhost:5000

## Project Structure

```
TaskFlow/
├── TaskFlow.API/          # ASP.NET Core Web API
│   ├── Program.cs          # Application entry point
│   ├── appsettings.json   # Configuration
│   └── Dockerfile         # Docker build configuration
├── TaskFlow.Application/  # Business logic layer
├── TaskFlow.Domain/       # Domain entities
│   └── User.cs            # User entity
├── TaskFlow.Infrastructure/ # Database configuration
│   ├── ApplicationDbContext.cs
│   └── Migrations/         # EF Core migrations
├── TaskFlow.UnitTests/    # Unit tests
├── frontend/              # React frontend
│   ├── src/
│   │   ├── App.tsx
│   │   └── main.tsx
│   ├── package.json
│   └── Dockerfile.frontend
├── docs/                  # Documentation
├── docker-compose.yml    # Docker orchestration
├── .env                  # Environment variables (not in git)
└── .gitignore           # Git ignore rules
```

## Available Endpoints

### Health & Diagnostics
- `GET /health` - Application health check
- `GET /db-health` - Database connection check

### Sample API
- `GET /weatherforecast` - Sample weather data endpoint

### Authentication
- `GET /login` - Initiate Google sign-in
- `GET /logout` - Sign out current user
- `GET /auth/user` - Get current authenticated user (requires auth)

## Configuration

### Environment Variables

| Variable | Description | Required |
|----------|-------------|-----------|
| `GOOGLE_CLIENT_ID` | Google OAuth Client ID | Yes (for auth) |
| `GOOGLE_CLIENT_SECRET` | Google OAuth Client Secret | Yes (for auth) |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | Yes |

### Database Configuration

Default database credentials (for development):
- Database: `taskflow`
- Username: `taskflow`
- Password: Set a strong password via DB_PASSWORD environment variable

**Security Note:** Change these credentials before production deployment.

## Troubleshooting

### Port Already in Use

If you get a port conflict:
```bash
# Check what's using the port
netstat -ano | findstr :5000

# Or change the port in docker-compose.yml
```

### Database Connection Failed

```bash
# Check database container
docker-compose logs db

# Restart database
docker-compose restart db
```

### Docker Build Fails

```bash
# Clean up and rebuild
docker-compose down
docker system prune -a
docker-compose up -d --build
```

### Migration Issues

```bash
# Remove last migration
dotnet ef migrations remove --project TaskFlow.Infrastructure --startup-project TaskFlow.API

# Create new migration
dotnet ef migrations add MigrationName --project TaskFlow.Infrastructure --startup-project TaskFlow.API

# Apply migration
dotnet ef database update --project TaskFlow.Infrastructure --startup-project TaskFlow.API
```

## Next Steps

1. **Set up Google OAuth** - Follow [Google Auth Setup](GOOGLE_AUTH_SETUP.md)
2. **Configure Production Deployment** - Follow [Production Deployment](PRODUCTION_DEPLOYMENT.md)
3. **Build E-commerce Features** - Add products, cart, orders
4. **Add Tests** - Implement unit and integration tests

## Additional Resources

- [Database Setup Guide](DATABASE_SETUP.md)
- [Google Authentication Guide](GOOGLE_AUTH_SETUP.md)
- [Production Deployment Guide](PRODUCTION_DEPLOYMENT.md)
- [Docker Deployment Guide](DEPLOYMENT_DOCKER.md)
- [Azure Deployment Guide](DEPLOYMENT.md)

## Support

For issues or questions:
- Check the documentation in the `docs/` folder
- Review Docker logs: `docker-compose logs`
- Check application logs in the API container
