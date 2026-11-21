# Mercato (TaskFlow)

A full-stack e-commerce portfolio app — storefront brand **Mercato** — built with **.NET 10**, **React 19 + TypeScript + Vite**, **PostgreSQL 17**, **Stripe**, **Resend**, and **Docker**.

Production demo: `https://taskflow-das.duckdns.org` (test-mode Stripe payments; a demo banner is shown site-wide)

## Quick Start

### Local development with Docker

```bash
# Create/refresh env files from the template and fill in values
cp .env.example .env        # used by Compose for variable interpolation
cp .env.example .env.dev    # used by the dev stack via env_file

# Required values include DB_PASSWORD, GOOGLE_CLIENT_ID/SECRET,
# Stripe__SecretKey, VITE_STRIPE_PUBLISHABLE_KEY, and App__PublicUrl.

# Start API + frontend + PostgreSQL + migrator
docker compose -f docker-compose.dev.yml up -d --build
```

Useful URLs:

- Frontend: `http://localhost`
- API: `http://localhost:5000`
- Frontend nginx proxy: `http://localhost/api/...`
- Health: `http://localhost:5000/health` and `http://localhost:5000/db-health`

### Frontend only

```bash
cd frontend
npm install
npm run dev        # http://localhost:5173, proxies /api and /health to localhost:5000
npm test           # Vitest
npm run lint       # ESLint
npm run build      # tsc + Vite production build -> frontend/dist
```

### Backend only

```bash
dotnet restore
dotnet ef database update --project TaskFlow.Infrastructure --startup-project TaskFlow.API
dotnet run --project TaskFlow.API
```

## Tests

```bash
dotnet test TaskFlow.UnitTests          # fast unit tests, no Docker needed
dotnet test TaskFlow.IntegrationTests   # Testcontainers PostgreSQL, needs Docker

cd frontend
npm test
npm run lint
npm run build
```
Database migrations are handled by the migrator service defined in 'docker-compose.dev.yml'

migration: dotnet ef migrations list --project TaskFlow.Infrastructure --startup-project TaskFlow.API

## Production deployment

Production runs on an Oracle Cloud VM (`92.5.144.125`, user `ubuntu`) behind **Nginx Proxy Manager** + **DuckDNS** (`taskflow-das.duckdns.org`).

### One-time VPS setup

```bash
# 1. SSH in
ssh -i <key>.pem ubuntu@92.5.144.125

# 2. Install Docker (if not present)
sudo apt update && sudo apt install -y docker.io docker-compose-v2
sudo usermod -aG docker ubuntu   # re-login after this

# 3. Clone the repo
cd ~ && git clone https://github.com/<owner>/TaskFlow.git && cd TaskFlow

# 4. Create .env — either copy .env.prod.example and fill values,
#    or let the deploy workflow generate it from GitHub secrets
cp .env.prod.example .env

# 5. Open ports 22, 80, 443 in the OCI Security List AND iptables
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 80 -j ACCEPT
sudo iptables -I INPUT 6 -m state --state NEW -p tcp --dport 443 -j ACCEPT
sudo netfilter-persistent save

# 6. Start the stack (pulls prebuilt GHCR images)
sudo docker compose pull
sudo docker compose up -d
```

Then in **Nginx Proxy Manager** (`http://92.5.144.125:81`): add a proxy host for `taskflow-das.duckdns.org` → `http://taskflow-frontend:80`, request a Let's Encrypt certificate, and enable HTTP/2. DuckDNS keeps the subdomain pointed at the VPS IP. The frontend nginx already routes `/api`, `/health`, and `/signin-google` to the API container.

Day-to-day deploys are automated — see below. Full walkthrough: [Production Deployment](docs/PRODUCTION_DEPLOYMENT.md).

### Automated deploy (Primary Method)

`.github/workflows/deploy.yml` builds and pushes the API, frontend, and migrator images to GHCR on pushes to `main`, then SSHes into the VPS, resets the repo, writes `.env` from GitHub secrets, pulls images, and runs `docker compose up -d` with `ASPNETCORE_ENVIRONMENT=Production`.

Required GitHub secrets: `DB_PASSWORD`, `JWT_KEY`, `STRIPE_SECRET_KEY`, `STRIPE_WEBHOOK_SECRET`, `RESEND_API_KEY`, `RESEND_FROM_EMAIL` (or `RESEND__FROMEMAIL`), `APP_PUBLIC_URL` (`https://taskflow-das.duckdns.org`), `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `VITE_STRIPE_PUBLISHABLE_KEY` (optional — checkout redirects to a server-created Stripe session), `DUCKDNS_SUBDOMAIN`, `DUCKDNS_TOKEN`, `VPS_HOST_IP`, `VPS_USERNAME`, `VPS_SSH_PRIVATE_KEY`.

In production the API refuses to start without `Jwt:Key`, `Stripe:SecretKey`, `Stripe:WebhookSecret`, Resend settings, and an absolute `App:PublicUrl` — a crash-looping `taskflow-api` after deploy almost always means one of these secrets is empty.

### Manual VPS deploy (Fallback)

If the CI/CD pipeline fails, you can manually pull the latest code and restart the containers on the server:

```bash
# On the server via SSH
cd ~/TaskFlow
git fetch origin main
git reset --hard origin/main
sudo docker compose pull
sudo docker compose up -d
```

`docker-compose.prod.yml`/`docker-compose.yml` consume prebuilt images via `DOCKER_IMAGE_*`; `--build` is only used by `docker-compose.dev.yml` or for local testing.

Then in Nginx Proxy Manager (`http://92.5.144.125:81`), proxy `taskflow-das.duckdns.org` to `http://taskflow-frontend:80` and issue a Let's Encrypt certificate. The frontend nginx already routes `/api`, `/health`, `/weatherforecast`, and `/signin-google` to the API container.

Full guides:

- [Production Deployment](docs/PRODUCTION_DEPLOYMENT.md)
- [CI/CD Setup](docs/CI_CD_SETUP.md)
- [Backup & Rollback](docs/BACKUP_AND_ROLLBACK.md)

## Admin access

There is no default admin account. Create or promote one on the server:

```bash
cd ~/TaskFlow
bash scripts/create-admin.sh <email> <password> [firstName] [lastName]
```

The script registers through `/api/auth/register` if needed, then sets `Users.Role = 'Admin'` in Postgres. Sign in at `/admin/login` (admins who sign in through `/login` are redirected to `/admin`). For the dev compose DB use `DB_CONTAINER=taskflow-db-dev`.

## Architecture

- **Backend**: ASP.NET Core 10 minimal APIs, layered as API / Application / Domain / Infrastructure.
- **Frontend**: React 19 + TypeScript + Vite, built to static files and served by nginx inside `Dockerfile.frontend`.
- **Database**: PostgreSQL 17 + EF Core; `Dockerfile.migrator` runs the migration bundle once per deploy and is ARM64-aware.
- **Auth**: JWT access/refresh tokens, email verification, password reset, and Google sign-in via Google Identity Services (ID token validated server-side; new Google users are created as `Customer`).
- **Payments**: Stripe Checkout + webhooks with idempotent handling, `PaymentReview` stock-conflict flow, and an admin payment-issues API.
- **Email**: Resend in production, mock email service in development.
- **Edge**: Nginx Proxy Manager terminates HTTPS and sends traffic to the frontend container; the frontend nginx proxies `/api`, `/health`, `/weatherforecast`, and `/signin-google` to the API.
- **CI/CD**: GitHub Actions builds images in the cloud, pushes to GHCR, and deploys to the Oracle VPS.

## Features

- Product catalog with categories, images, variants, search, filters, and sorting; catalog seeded/upserted by slug on API startup
- Authenticated and guest carts (guest carts merge into the user cart on login), addresses, and saved user profiles
- Checkout with saved-address selection, order totals with tax/shipping/free-shipping rules
- Orders via Stripe Checkout (server-created session) or Cash on Delivery, cancellation, admin status/tracking
- Stripe webhook idempotency, atomic stock decrement, payment issue tracking
- Admin dashboard for stats, orders, products, categories, and payment issues
- Responsive storefront (mobile header/menu), public legal pages `/privacy-policy` and `/terms-of-service` (static HTML for verification bots)
- Rate limiting, global exception handling, forwarded headers
- Docker Compose dev/prod stacks, ARM64 migration bundle, backup/restore scripts

## Documentation

- [Documentation index](docs/INDEX.md)
- [Getting Started](docs/GETTING_STARTED.md)
- [Environment Setup](docs/ENVIRONMENT_SETUP.md)
- [Database Setup](docs/DATABASE_SETUP.md)
- [Google Authentication](docs/GOOGLE_AUTH_SETUP.md)
- [Resend Email Setup](docs/REGISTER-EMAIL.md)
- [CI/CD Setup](docs/CI_CD_SETUP.md)
- [Production Deployment](docs/PRODUCTION_DEPLOYMENT.md)
- [Docker Deployment](docs/DEPLOYMENT_DOCKER.md)
- [Azure Deployment](docs/DEPLOYMENT.md)
- [Backup & Rollback](docs/BACKUP_AND_ROLLBACK.md)
- [Mutation Testing](docs/MUTATION_TESTING.md)

## Project structure

```text
TaskFlow/
├── TaskFlow.API/                  # Minimal API host: Program.cs, Endpoints, Middleware, Services
├── TaskFlow.Application/          # DTOs, service interfaces, cart/order/checkout logic
├── TaskFlow.Domain/               # Entities and domain constants
├── TaskFlow.Infrastructure/       # EF Core DbContext, repositories, migrations, Stripe/Resend services
├── TaskFlow.UnitTests/            # xUnit/Moq unit tests
├── TaskFlow.IntegrationTests/     # Testcontainers PostgreSQL integration tests
├── frontend/                      # React + Vite app
│   └── src/
│       ├── api/                   # Axios clients: auth, products, categories, cart, orders, stripe, admin
│       ├── components/            # Navbar, layouts, route guards, shared UI
│       ├── pages/                 # Storefront, checkout, orders, auth, admin, legal pages
│       └── store/                 # Zustand stores: auth, cart, guest cart
├── docs/                          # Documentation
├── scripts/                       # backup-db.sh / restore-db.sh / create-admin.sh
├── .github/workflows/deploy.yml   # Build/push images and deploy to the VPS
├── Dockerfile                     # API image
├── Dockerfile.frontend            # Frontend build + nginx image
├── Dockerfile.migrator            # EF Core migrations bundle image
├── nginx.conf                     # Frontend nginx reverse-proxy config
├── docker-compose.dev.yml         # Local dev stack; builds images
├── docker-compose.prod.yml        # Production stack; pulls DOCKER_IMAGE_* images
├── docker-compose.yml             # Default compose file, same prod-style image flow
├── Directory.Packages.props       # Central NuGet package versions
├── global.json                    # .NET SDK pin
└── stryker-config.json            # Mutation testing config
```

Notes:

- Env templates live in `.env.example`; real env files are `.env`, `.env.dev`, and `.env.prod` and must not be committed.
- The root `package.json`/`node_modules` are not used by the app; frontend dependencies live in `frontend/package.json`.
- `TaskFlow.IntegrationTestsIntegration/` is currently empty and can be ignored/removed.

## License

Proprietary - All rights reserved.