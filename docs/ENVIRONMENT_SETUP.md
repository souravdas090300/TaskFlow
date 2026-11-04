# Environment Configuration Guide

This guide explains how to configure environment variables for TaskFlow across different environments (development, production, and demo).

## Environment Files Structure

TaskFlow uses a structured approach to environment configuration:

- **`.env.example`** - Template file (safe to commit to Git)
- **`.env.dev`** - Development environment (local development)
- **`.env.prod`** - Production environment (VPS deployment)
- **`.env`** - Active environment (created by copying from dev/prod)

## Security Note

All actual environment files (`.env`, `.env.dev`, `.env.prod`) are **protected by `.gitignore`** and will not be committed to version control. Only `.env.example` is tracked in Git.

## Quick Start

### 1. For Local Development

```bash
# Copy the development template
cp .env.dev .env

# Edit .env with your actual values
# Add your Google OAuth credentials
# Set your database password
```

### 2. For Production Deployment

```bash
# Copy the production template
cp .env.prod .env

# Edit .env with your actual values
# Use STRONG passwords
# Add your production Google OAuth credentials
# Add your DuckDNS token
```

### 3. For Demo/Purposes

```bash
# Use the example as a reference
# Do not use .env.example directly - it has placeholder values
cp .env.example .env
# Then fill in actual values
```

## Environment Variables

### Application Configuration

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment | `Development` or `Production` | Yes |
| `ASPNETCORE_URLS` | Application URL binding | `http://+:80` | Yes |

### Database Configuration

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `DB_HOST` | Database host | `db` (Docker) or `localhost` (local) | Yes |
| `DB_PORT` | Database port | `5432` | Yes |
| `DB_NAME` | Database name | `taskflow` | Yes |
| `DB_USER` | Database username | `taskflow` | Yes |
| `DB_PASSWORD` | Database password | Strong password (16+ chars) | Yes |

**Note:** For production, use a strong, random password (minimum 16 characters, mixed case, numbers, symbols).

### Google OAuth Configuration

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `GOOGLE_CLIENT_ID` | Google OAuth client ID | `123456789.apps.googleusercontent.com` | Yes (for auth) |
| `GOOGLE_CLIENT_SECRET` | Google OAuth client secret | `GOCSPX-abc123xyz789` | Yes (for auth) |

**How to get these:**
1. Go to https://console.cloud.google.com/apis/credentials
2. Create OAuth 2.0 credentials
3. Set redirect URI: `http://localhost:5000/signin-google` (dev) or `https://yourdomain.com/signin-google` (prod)

### DuckDNS Configuration

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `DUCKDNS_SUBDOMAIN` | DuckDNS subdomain | `taskflow-sourav` | Yes (for production) |
| `DUCKDNS_TOKEN` | DuckDNS authentication token | `abc123def456` | Yes (for production) |

**How to get these:**
1. Go to https://www.duckdns.org/
2. Create a free account
3. Get your token from the dashboard

## Docker Compose Files

### Development Mode

Uses `docker-compose.dev.yml` with `.env.dev`:

```bash
# Set environment file
cp .env.dev .env

# Start with development configuration
docker-compose -f docker-compose.dev.yml up -d
```

**Development Differences:**
- API exposed on port 5000 (for direct access)
- Frontend exposed on port 80
- PostgreSQL exposed on port 5432 (for local tools)
- Uses separate volume `postgres-data-dev`
- Nginx Proxy Manager and DuckDNS not included

### Production Mode

Uses `docker-compose.yml` with `.env.prod`:

```bash
# Set environment file
cp .env.prod .env

# Start with production configuration
docker-compose up -d
```

**Production Differences:**
- All services internal to Docker network
- Nginx Proxy Manager handles HTTPS
- DuckDNS updates dynamic DNS
- API and frontend accessible only through Nginx
- Uses persistent volumes for production data

## Environment Variable Precedence

Variables are loaded in this order (later values override earlier ones):

1. Default values in `docker-compose.yml` (e.g., `${DB_HOST:-db}`)
2. Values from `.env` file
3. Values passed via command line: `docker-compose up -e DB_PASSWORD=secure123`

## Configuration in ASP.NET Core

The application reads environment variables using the following pattern:

- `ConnectionStrings__DefaultConnection` - Built from DB_* variables
- `Authentication__Google__ClientId` - From `GOOGLE_CLIENT_ID`
- `Authentication__Google__ClientSecret` - From `GOOGLE_CLIENT_SECRET`

This is handled automatically by Docker Compose environment variable mapping.

## Security Best Practices

### 1. Never Commit Secrets

All environment files except `.env.example` are in `.gitignore`:

```gitignore
.env
.env.local
.env.*.local
.env.dev
.env.prod
.env.staging
```

### 2. Use Strong Passwords

Production passwords should be:
- Minimum 16 characters
- Mixed case (uppercase and lowercase)
- Include numbers
- Include special characters
- Generated randomly (not human-readable)

Generate with:
```bash
# Linux/Mac
openssl rand -base64 32

# PowerShell
Add-Type -AssemblyName System.Web; [System.Web.Security.Membership]::GeneratePassword(32, 5)
```

### 3. Rotate Credentials Regularly

- Change database passwords every 90 days
- Rotate Google OAuth secrets if compromised
- Regenerate DuckDNS tokens if exposed

### 4. Different Environments

Never use the same credentials for development and production:
- Development: Weak passwords are acceptable
- Production: Strong passwords are mandatory
- Staging: Separate credentials from production

### 5. Use Secret Management (Advanced)

For production, consider using:
- Docker Secrets
- HashiCorp Vault
- AWS Secrets Manager
- Azure Key Vault
- Kubernetes Secrets

## Troubleshooting

### Issue: Environment Variables Not Loading

**Solution:** Check that `.env` file exists in the project root:

```bash
ls -la .env
```

### Issue: Docker Compose Can't Find Variables

**Solution:** Ensure variables are defined before starting:

```bash
# Verify variable is set
echo $DB_PASSWORD

# Or test with docker-compose config
docker-compose config
```

### Issue: Database Connection Fails

**Solution:** Check DB_HOST setting:
- Development: `DB_HOST=localhost`
- Production: `DB_HOST=db` (Docker service name)

### Issue: Google OAuth Fails

**Solution:** Verify:
1. Client ID and Secret are set in `.env`
2. Redirect URI matches in Google Console
3. Correct HTTP vs HTTPS for your environment

## Example .env Files

### Development (.env.dev)

```env
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://+:80
DB_HOST=localhost
DB_PORT=5432
DB_NAME=taskflow
DB_USER=taskflow
DB_PASSWORD=TaskFlow@2026
GOOGLE_CLIENT_ID=123456789.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=GOCSPX-abc123
DUCKDNS_SUBDOMAIN=
DUCKDNS_TOKEN=
```

### Production (.env.prod)

```env
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:80
DB_HOST=db
DB_PORT=5432
DB_NAME=taskflow
DB_USER=taskflow
DB_PASSWORD=xK9#mP2$vL8&nQ5!zR3%wE7@sD1
GOOGLE_CLIENT_ID=987654321.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=GOCSPX-xyz789
DUCKDNS_SUBDOMAIN=taskflow-sourav
DUCKDNS_TOKEN=abc123def456ghi789
```

## Related Documentation

- [Getting Started Guide](GETTING_STARTED.md)
- [Database Setup](DATABASE_SETUP.md)
- [Google Authentication Setup](GOOGLE_AUTH_SETUP.md)
- [Production Deployment](PRODUCTION_DEPLOYMENT.md)
- [Security Audit](../SECURITY_AUDIT.md)
