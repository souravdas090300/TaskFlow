# TaskFlow Deployment Guide

This guide covers deploying TaskFlow to Azure App Service using a single-service architecture (React + ASP.NET Core API together).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) installed
- [Node.js](https://nodejs.org/) installed (for React build)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli) installed
- Azure account with active subscription

## Architecture Overview

TaskFlow uses a **single-service deployment pattern** where:
- React frontend is built to static files
- Static files are served from ASP.NET Core's `wwwroot` folder
- API endpoints and static files are served from the same App Service
- No CORS configuration needed (single origin)

This architecture is ideal for MVP and simple applications, avoiding the complexity of managing separate frontend and backend services.

## Configuration Summary

| Configuration Item | Value |
|---|---|
| Runtime Stack | `DOTNETCORE\|10.0` |
| Health Check Path | `/health` |
| Build Mode | Automatic (SCM_DO_BUILD_DURING_DEPLOYMENT=true) |
| Architecture | Single Service (React + API) |

## Deployment Steps

### Option 1: Automated Deployment (Recommended)

#### Using PowerShell (Windows)

```powershell
# Make the script executable
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Run deployment
.\deploy-azure.ps1
```

#### Using Bash (Linux/macOS/Git Bash)

```bash
# Make the script executable
chmod +x deploy-azure.sh

# Run deployment
./deploy-azure.sh
```

The script will:
1. Check Azure CLI installation and login status
2. Create resource group (TaskFlowRG)
3. Create App Service Plan (Linux, B1 tier)
4. Create Web App with .NET 10 runtime
5. Configure app settings
6. Build the application
7. Deploy to Azure

### Option 2: Manual Deployment

#### Step 1: Login to Azure

```bash
az login
```

#### Step 2: Create Resource Group

```bash
az group create --name TaskFlowRG --location eastus
```

#### Step 3: Create App Service Plan

```bash
az appservice plan create \
    --name TaskFlowPlan \
    --resource-group TaskFlowRG \
    --sku B1 \
    --is-linux
```

#### Step 4: Create Web App

```bash
az webapp create \
    --name TaskFlow \
    --plan TaskFlowPlan \
    --resource-group TaskFlowRG \
    --runtime "DOTNETCORE|10.0"
```

#### Step 5: Configure App Settings

```bash
az webapp config appsettings set \
    --name TaskFlow \
    --resource-group TaskFlowRG \
    --settings SCM_DO_BUILD_DURING_DEPLOYMENT=true
```

#### Step 6: Build and Package

```bash
# Build the application
dotnet publish -c Release

# Create deployment package
cd bin/Release/net10.0/publish
zip -r ../../../../deploy.zip .
cd ../../../../..
```

#### Step 7: Deploy

```bash
az webapp deployment source config-zip \
    --name TaskFlow \
    --resource-group TaskFlowRG \
    --src deploy.zip
```

## Post-Deployment Verification

### Check Health Endpoint

```bash
curl https://TaskFlow.azurewebsites.net/health
```

Expected response:
```json
{
  "status": "healthy",
  "timestamp": "2026-11-03T15:30:00Z"
}
```

### Access Weather Forecast API

```bash
curl https://TaskFlow.azurewebsites.net/weatherforecast
```

### Access Frontend

Open in browser:
```
https://TaskFlow.azurewebsites.net
```

## Configuration Details

### Program.cs Changes

The API has been configured with:
- Static file serving from `wwwroot`
- Health check endpoint at `/health`
- SPA routing fallback to `index.html`
- Built-in OpenAPI support (.NET 10 native)

### Frontend Build Integration

The `TaskFlow.API.csproj` includes a build target that:
1. Runs `npm install` in the frontend directory
2. Runs `npm run build` to generate React static files
3. Copies the `dist` folder contents to `wwwroot`

This happens automatically during `dotnet publish`.

## Environment Variables

For production, you may need to configure additional app settings:

```bash
# Set environment-specific variables
az webapp config appsettings set \
    --name TaskFlow \
    --resource-group TaskFlowRG \
    --settings \
        ASPNETCORE_ENVIRONMENT=Production \
        ConnectionStrings__DefaultConnection="your-connection-string" \
        Cloudinary__CloudName="your-cloud-name" \
        Cloudinary__ApiKey="your-api-key" \
        Cloudinary__ApiSecret="your-api-secret" \
        Resend__ApiKey="your-resend-api-key"
```

## Scaling

### Scale Up (Upgrade Tier)

```bash
az appservice plan update \
    --name TaskFlowPlan \
    --resource-group TaskFlowRG \
    --sku S1
```

### Scale Out (Add Instances)

```bash
az appservice plan update \
    --name TaskFlowPlan \
    --resource-group TaskFlowRG \
    --number-of-workers 2
```

## Monitoring

### Enable Application Insights

```bash
# Create Application Insights resource
az monitor app-insights component create \
    --app TaskFlowAI \
    --location eastus \
    --resource-group TaskFlowRG \
    --application-type web

# Get instrumentation key
INSTRUMENTATION_KEY=$(az monitor app-insights component show \
    --app TaskFlowAI \
    --resource-group TaskFlowRG \
    --query instrumentationKey -o tsv)

# Configure web app
az webapp config appsettings set \
    --name TaskFlow \
    --resource-group TaskFlowRG \
    --settings APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$INSTRUMENTATION_KEY"
```

### View Logs

```bash
# Enable log streaming
az webapp log tail --name TaskFlow --resource-group TaskFlowRG
```

## Troubleshooting

### Build Fails on Azure

If the build fails during deployment:
1. Check build logs: `az webapp log tail --name TaskFlow --resource-group TaskFlowRG`
2. Ensure Node.js is available in the build environment
3. Consider building locally and deploying the artifacts

### Static Files Not Serving

If the frontend doesn't load:
1. Verify the build target executed successfully
2. Check that `wwwroot` folder exists in the deployment package
3. Ensure `app.UseStaticFiles()` is called before `app.MapControllers()`

### Health Check Failing

If `/health` returns errors:
1. Check application logs: `az webapp log tail --name TaskFlow --resource-group TaskFlowRG`
2. Verify the app is running: `az webapp show --name TaskFlow --resource-group TaskFlowRG`
3. Check runtime configuration matches .NET 10

## Cost Estimate

Based on Azure App Service pricing (as of 2026):

| Plan | Tier | Estimated Cost/Month |
|------|------|---------------------|
| B1   | Basic | ~$15-20 |
| S1   | Standard | ~$70-80 |
| P1   | Premium | ~$150-180 |

The B1 tier is sufficient for MVP and testing.

## Security Considerations

1. **HTTPS Only**: Enable HTTPS-only in production
   ```bash
   az webapp update --name TaskFlow --resource-group TaskFlowRG --https-only true
   ```

2. **Authentication**: Configure JWT Bearer authentication (package already installed)

3. **CORS**: Not needed for single-service architecture

4. **Secrets**: Use Azure Key Vault for sensitive data
   ```bash
   az keyvault create --name TaskFlowKV --resource-group TaskFlowRG --location eastus
   ```

## Alternative: VPS + Docker

If you prefer not to use Azure, see the `DEPLOYMENT_DOCKER.md` guide for VPS deployment with Docker.

## Cleanup

To remove all Azure resources:

```bash
az group delete --name TaskFlowRG --yes --no-wait
```

## Support

For issues:
- Check Azure Portal: https://portal.azure.com
- Review deployment logs
- Verify .NET 10 SDK compatibility
- Check Azure Service Health status

---

**Last Updated:** November 3, 2026
**.NET Version:** 10.0.403
