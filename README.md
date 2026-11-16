# TaskFlow

A full-stack e-commerce solution built with **.NET 10**, **React 19 + TypeScript + Vite**, **PostgreSQL 17**, **Stripe**, and **Docker**.

## Quick Start

### Local Development

```bash
# Copy and fill in environment variables
cp .env.example .env          # or .env.dev for the dev compose stack
# Edit .env with your Google OAuth + Stripe test credentials

# Start the dev stack (API + frontend + PostgreSQL)
docker compose -f docker-compose.dev.yml up -d --build

# Access the application
# Frontend: http://localhost
# API:      http://localhost:5000  (also proxied at http://localhost/api)
# Health:   http://localhost:5000/health
```

For local frontend development without Docker:

```bash
cd frontend
npm install
npm run dev        # http://localhost:5173 - vite proxies /api to localhost:5000
```

### Run tests

```bash
dotnet test TaskFlow.UnitTests          # fast, no Docker needed
dotnet test TaskFlow.IntegrationTests   # needs Docker (Testcontainers PostgreSQL)
```

## Production Deployment (Oracle Cloud)

Deployed on an Oracle Cloud VM behind **Nginx Proxy Manager** with **DuckDNS** (`taskflow-das.duckdns.org`).

```powershell
# From Windows - upload the project to the server
scp -i "C:\Users\dasau\ssh-key-2026-11-15.key" -r C:\dev\TaskFlow ubuntu@92.5.144.125:~/TaskFlow
```

```bash
# On the server (ssh -i "C:\Users\dasau\ssh-key-2026-11-15.key" ubuntu@92.5.144.125)
sudo apt install -y docker.io docker-compose-v2 git
sudo usermod -aG docker $USER && newgrp docker

cd ~/TaskFlow
cp .env.prod .env       # fill in DB_PASSWORD, Stripe__*, Resend__*, etc.
docker compose up -d --build
```

Then in Nginx Proxy Manager (`http://92.5.144.125:81`): add proxy host
`taskflow-das.duckdns.org` → `http://taskflow-frontend:80` and issue a
Let's Encrypt certificate (DNS challenge via DuckDNS).

Full guide: [docs/PRODUCTION_DEPLOYMENT.md](docs/PRODUCTION_DEPLOYMENT.md)

## Architecture

- **Backend**: ASP.NET Core 10 minimal APIs, layered (API / Application / Domain / Infrastructure)
- **Frontend**: React 19 + TypeScript + Vite, served by nginx (proxies `/api` to the API container)
- **Database**: PostgreSQL 17 + EF Core migrations (incl. ARM64-aware `Dockerfile.migrator`)
- **Payments**: Stripe (Checkout + webhooks, idempotent `payment_intent.succeeded` handling, PaymentReview for stock conflicts, admin payment-issues API)
- **Email**: Resend (production) / mock (development)
- **Auth**: JWT + Google OAuth
- **Infra**: Docker Compose, Nginx Proxy Manager (SSL), DuckDNS

## Features

- ✅ Product catalog with categories, images, variants
- ✅ Shopping cart, addresses, coupons-ready order totals (tax/shipping rules)
- ✅ Orders: Stripe + Cash on Delivery, cancellation, admin status/tracking
- ✅ Stripe webhook idempotency + atomic stock decrement
- ✅ Payment issue tracking for admin review (unknown order, closed order, double charge, stock conflict)
- ✅ Admin dashboard (stats, orders, products, categories)
- ✅ Rate limiting, global exception handling, forwarded headers
- ✅ Docker Compose (dev / prod / full stack) + ARM64 migration bundle
- ✅ Automated backups/restore scripts

## Documentation

- [Getting Started](docs/GETTING_STARTED.md) - local development
- [Environment Setup](docs/ENVIRONMENT_SETUP.md) - env var reference
- [Production Deployment](docs/PRODUCTION_DEPLOYMENT.md) - VPS + Nginx Proxy Manager + DuckDNS
- [Backup & Rollback](docs/BACKUP_AND_ROLLBACK.md) - DB backups, restore, app rollback
- [Mutation Testing](docs/MUTATION_TESTING.md) - Stryker.NET for pricing logic
- [CI/CD Setup](docs/CI_CD_SETUP.md) - GitHub Actions
- [Google Authentication](docs/GOOGLE_AUTH_SETUP.md) - OAuth setup
- [Database Setup](docs/DATABASE_SETUP.md) - PostgreSQL & EF Core

## Project Structure

```
TaskFlow/
├── TaskFlow.API/               # Minimal API endpoints, middleware, Program.cs
├── TaskFlow.Application/       # Services (CartService, OrderService, CheckoutService), DTOs, repository interfaces
├── TaskFlow.Domain/            # Entities, constants
├── TaskFlow.Infrastructure/    # EF Core DbContext, repositories, Stripe/Resend services, migrations
├── TaskFlow.UnitTests/         # Moq/xUnit unit tests (Checkout, Domain, Services)
├── TaskFlow.IntegrationTests/  # Testcontainers PostgreSQL integration tests
├── frontend/                   # React + Vite storefront & admin
├── docs/                       # Documentation
├── scripts/                    # backup-db.sh / restore-db.sh
├── Dockerfile.migrator         # ARM-aware EF migrations bundle
└── docker-compose*.yml         # dev / prod / full-stack configurations
```

## License

Proprietary - All rights reserved.
