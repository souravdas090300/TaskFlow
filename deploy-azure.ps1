# Azure App Service Deployment Script for TaskFlow (PowerShell)
# This script creates the necessary Azure resources and deploys the application

$ErrorActionPreference = "Stop"

# Configuration
$RESOURCE_GROUP = "TaskFlowRG"
$APP_SERVICE_PLAN = "TaskFlowPlan"
$WEB_APP_NAME = "TaskFlow"
$RUNTIME = "DOTNETCORE|10.0"
$SKU = "B1"

Write-Host "=== TaskFlow Azure Deployment ===" -ForegroundColor Green
Write-Host "Resource Group: $RESOURCE_GROUP"
Write-Host "App Service Plan: $APP_SERVICE_PLAN"
Write-Host "Web App Name: $WEB_APP_NAME"
Write-Host "Runtime: $RUNTIME"
Write-Host ""

# Check if Azure CLI is installed
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Host "Error: Azure CLI is not installed. Please install it from https://docs.microsoft.com/cli/azure/install-azure-cli" -ForegroundColor Red
    exit 1
}

# Check if user is logged in
Write-Host "Checking Azure login status..."
$account = az account show 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Please log in to Azure..."
    az login
}

# Create Resource Group
Write-Host "Creating resource group..."
az group create --name $RESOURCE_GROUP --location eastus

# Create App Service Plan (Linux)
Write-Host "Creating App Service Plan..."
az appservice plan create `
    --name $APP_SERVICE_PLAN `
    --resource-group $RESOURCE_GROUP `
    --sku $SKU `
    --is-linux

# Create Web App
Write-Host "Creating Web App..."
az webapp create `
    --name $WEB_APP_NAME `
    --plan $APP_SERVICE_PLAN `
    --resource-group $RESOURCE_GROUP `
    --runtime "$RUNTIME"

# Configure app settings
Write-Host "Configuring app settings..."
az webapp config appsettings set `
    --name $WEB_APP_NAME `
    --resource-group $RESOURCE_GROUP `
    --settings SCM_DO_BUILD_DURING_DEPLOYMENT=true

# Build the application
Write-Host "Building application..."
dotnet publish -c Release

# Create deployment package
Write-Host "Creating deployment package..."
$publishPath = "bin\Release\net10.0\publish"
$zipPath = "deploy.zip"
Compress-Archive -Path "$publishPath\*" -DestinationPath $zipPath -Force

# Deploy to Azure
Write-Host "Deploying to Azure..."
az webapp deployment source config-zip `
    --name $WEB_APP_NAME `
    --resource-group $RESOURCE_GROUP `
    --src $zipPath

# Get the web app URL
$WEB_APP_URL = az webapp show --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP --query defaultHostName -o tsv
Write-Host ""
Write-Host "=== Deployment Complete ===" -ForegroundColor Green
Write-Host "Your application is now available at: https://$WEB_APP_URL"
Write-Host "Health check: https://$WEB_APP_URL/health"
Write-Host ""
