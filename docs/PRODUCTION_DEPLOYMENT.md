# Production Deployment Guide with Nginx Proxy Manager

**Status:** Docker configuration updated
**Services:** API, Frontend, Database, Nginx Proxy Manager, DuckDNS
**Deployment Target:** VPS with IP 77.25.144.193

---

## Overview

This configuration uses:
- **Nginx Proxy Manager**: Manages SSL certificates and reverse proxy
- **DuckDNS**: Keeps your domain `taskflow-sourav.duckdns.org` pointing to your VPS
- **TaskFlow API**: .NET backend (exposed internally)
- **TaskFlow Frontend**: React + Nginx (exposed internally)
- **PostgreSQL**: Database

---

## Architecture

```
Internet → Nginx Proxy Manager (80/443) → TaskFlow Frontend (80)
                                            → TaskFlow API (80)
                                            → DuckDNS (dynamic DNS)
```

**Key Changes from Local Setup:**
- API and Frontend ports changed from host mapping to `expose` (internal only)
- Nginx Proxy Manager handles ports 80 (HTTP) and 443 (HTTPS)
- All traffic goes through Nginx Proxy Manager for SSL termination

---

## Prerequisites

1. VPS with IP: `77.25.144.193`
2. DuckDNS subdomain: `taskflow-sourav.duckdns.org`
3. Docker installed on VPS
4. Domain or DuckDNS configured to point to your VPS

---

## Step 1: Deploy to VPS

### 1.1. Upload Files to VPS

```bash
# On your local machine
scp -r C:\dev\TaskFlow user@77.25.144.193:/home/user/TaskFlow
```

Or use your preferred method (FTP, Git, etc.)

### 1.2. SSH into VPS

```bash
ssh user@77.25.144.193
cd TaskFlow
```

### 1.3. Start Services

```bash
docker-compose up -d --build
```

This will start all 5 services:
- taskflow-api
- taskflow-frontend
- taskflow-db
- nginx_proxy_manager
- duckdns_updater

---

## Step 2: Configure Nginx Proxy Manager

### 2.1. Access Nginx Proxy Manager Dashboard

Open your browser and navigate to:
```
http://77.25.144.193:81
```

**Default credentials:**
- Email: `admin@example.com`
- Password: `changeme`

**⚠️ IMPORTANT:** Change the password immediately after first login!

### 2.2. Add Proxy Host for Frontend

1. Click **Hosts** > **Add Proxy Host**
2. Fill in the details:
   - **Domain Names**: `taskflow-sourav.duckdns.org`
   - **Scheme**: `http`
   - **Forward Hostname/IP**: `taskflow-frontend`
   - **Forward Port**: `80`
3. Click **Save**

### 2.3. Add Proxy Host for API

1. Click **Hosts** > **Add Proxy Host**
2. Fill in the details:
   - **Domain Names**: `api.taskflow-sourav.duckdns.org`
   - **Scheme**: `http`
   - **Forward Hostname/IP**: `taskflow-api`
   - **Forward Port**: `80`
3. Click **Save**

### 2.4. Configure SSL Certificates

1. Click **SSL Certificates** > **Add SSL Certificate**
2. Select **Let's Encrypt**
3. Fill in:
   - **Domain Names**: `taskflow-sourav.duckdns.org, api.taskflow-sourav.duckdns.org`
   - **Email**: Your email for Let's Encrypt notifications
4. Click **Save**

5. Go back to your proxy hosts and enable SSL:
   - Edit the frontend proxy host
   - Enable **SSL Certificate** and select the certificate you created
   - Enable **Force SSL** (redirect HTTP to HTTPS)
   - Enable **HTTP/2 Support**
   - Click **Save**

6. Repeat for the API proxy host

---

## Step 3: Update Google OAuth Configuration

### 3.1. Add Production Redirect URIs to Google Console

Go to Google Cloud Console > APIs & Services > Credentials > Edit your OAuth client ID

Add these **Authorized redirect URIs**:
```
https://taskflow-sourav.duckdns.org/signin-google
https://api.taskflow-sourav.duckdns.org/signin-google
```

### 3.2. Update .env File on VPS

SSH into your VPS and edit the `.env` file:
```bash
nano .env
```

Add your actual Google credentials:
```env
GOOGLE_CLIENT_ID=your-google-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your-google-client-secret
```

Restart the API container:
```bash
docker-compose restart api
```

---

## Step 4: Verify Deployment

### 4.1. Check Services

```bash
docker-compose ps
```

All services should show "Up" status.

### 4.2. Test Frontend

Open browser:
```
https://taskflow-sourav.duckdns.org
```

You should see the React application.

### 4.3. Test API

Open browser:
```
https://api.taskflow-sourav.duckdns.org/health
```

Should return:
```json
{
  "status": "healthy",
  "timestamp": "..."
}
```

### 4.4. Test Database Connection

```
https://api.taskflow-sourav.duckdns.org/db-health
```

Should return:
```json
{
  "status": "healthy",
  "database": "connected"
}
```

### 4.5. Test Google Sign In

Navigate to:
```
https://taskflow-sourav.duckdns.org/login
```

Should redirect to Google OAuth.

---

## Step 5: Access URLs

### Production URLs

| Service | URL |
|---------|-----|
| Frontend | https://taskflow-sourav.duckdns.org |
| API | https://api.taskflow-sourav.duckdns.org |
| API Health | https://api.taskflow-sourav.duckdns.org/health |
| Database Health | https://api.taskflow-sourav.duckdns.org/db-health |
| Login | https://taskflow-sourav.duckdns.org/login |
| Current User | https://api.taskflow-sourav.duckdns.org/auth/user |
| Nginx Proxy Manager | http://77.25.144.193:81 |

### Add to Google OAuth Console

**Authorized redirect URIs:**
```
https://taskflow-sourav.duckdns.org/signin-google
https://api.taskflow-sourav.duckdns.org/signin-google
```

---

## Step 6: DuckDNS Configuration

The DuckDNS container is already configured with:
- **Subdomain**: `taskflow-sourav`
- **Token**: Provided in your config

This will automatically update DuckDNS to point to `77.25.144.193`.

**Your production domain:** `taskflow-sourav.duckdns.org`

---

## Security Considerations

### 1. Change Nginx Proxy Manager Password

**Default:** `admin@example.com` / `changeme`

After first login:
1. Go to User > Your Profile
2. Change email and password
3. Enable Two-Factor Authentication (recommended)

### 2. Secure Database

- Remove port 5432 exposure (already done in new config)
- Change database password from default
- Use strong randomly generated password

### 3. Environment Variables

- Never commit `.env` file to version control
- Use different credentials for production
- Consider using Docker secrets for sensitive data

### 4. Firewall Configuration

On your VPS, configure firewall to allow only:
- Port 80 (HTTP)
- Port 443 (HTTPS)
- Port 22 (SSH)
- Block all other ports

---

## Troubleshooting

### Issue: Cannot access Nginx Proxy Manager

**Solution:**
```bash
# Check if container is running
docker-compose ps nginx-proxy-manager

# Check logs
docker-compose logs nginx-proxy-manager

# Restart container
docker-compose restart nginx-proxy-manager
```

### Issue: SSL Certificate Not Issuing

**Common causes:**
1. Domain not pointing to VIP (check DuckDNS status)
2. Port 80 not accessible from internet
3. DNS propagation delay (can take up to 24 hours)

**Solution:**
1. Verify DuckDNS is updated: Check duckdns.org
2. Test port 80: `curl http://77.25.144.193`
3. Check Nginx Proxy Manager logs for errors

### Issue: Frontend Not Loading

**Solution:**
```bash
# Check frontend container
docker-compose logs frontend

# Check if frontend is reachable from Nginx Proxy Manager
docker-compose exec nginx-proxy-manager ping taskflow-frontend
```

### Issue: API Not Responding

**Solution:**
```bash
# Check API container
docker-compose logs api

# Check database connection
docker-compose logs db

# Restart API
docker-compose restart api
```

---

## Maintenance

### Update Application

```bash
# Pull latest code
git pull

# Rebuild and restart
docker-compose up -d --build
```

### Backup Database

```bash
# Backup PostgreSQL
docker exec taskflow-db pg_dump -U taskflow taskflow > backup.sql

# Restore
docker exec -i taskflow-db psql -U taskflow taskflow < backup.sql
```

### View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
docker-compose logs -f frontend
docker-compose logs -f nginx-proxy-manager
```

---

## Monitoring

### Nginx Proxy Manager Dashboard

Access at: `http://77.25.144.193:81`

Shows:
- Traffic statistics
- Certificate status
- Proxy host configurations
- Access logs

### Docker Stats

```bash
docker stats
```

Shows resource usage for all containers.

---

## Next Steps

### 1. Add Custom Domain (Optional)

If you want to use your own domain instead of DuckDNS:
1. Buy a domain
2. Create A record pointing to `77.25.144.193`
3. Update Nginx Proxy Manager with new domain
4. Update Google OAuth redirect URIs
5. Update SSL certificate

### 2. Configure Email Service

The Resend package is installed. Configure it for:
- Welcome emails
- Password reset (if you add password authentication)
- Order confirmations
- Shipping notifications

### 3. Configure Cloudinary

The Cloudinary package is installed. Configure it for:
- Product image uploads
- User profile pictures
- Document storage

### 4. Add Backup Strategy

Set up automated database backups:
- Daily backups to cloud storage
- Point-in-time recovery capability
- Backup retention policy

---

## Production Checklist

- [ ] Docker containers deployed to VPS
- [ ] Nginx Proxy Manager configured
- [ ] SSL certificates obtained and enabled
- [ ] Frontend accessible via HTTPS
- [ ] API accessible via HTTPS
- [ ] Database connected and migrations applied
- [ ] Google OAuth credentials added to .env
- [ ] Google OAuth redirect URIs updated
- [ ] Sign in flow tested
- [ ] Database health check passing
- [ ] Nginx Proxy Manager password changed
- [ ] DuckDNS updating correctly
- [ ] Firewall configured
- [ ] Database password changed from default
- [ ] Backup strategy in place

---

**Production URL:** https://taskflow-sourav.duckdns.org
**API URL:** https://api.taskflow-sourav.duckdns.org
**Nginx Proxy Manager:** http://77.25.144.193:81
