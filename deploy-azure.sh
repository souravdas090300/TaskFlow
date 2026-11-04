#!/bin/bash

# Azure App Service Deployment Script for TaskFlow
# This script creates the necessary Azure resources and deploys the application

set -e

# Configuration
RESOURCE_GROUP="TaskFlowRG"
APP_SERVICE_PLAN="TaskFlowPlan"
WEB_APP_NAME="TaskFlow"
RUNTIME="DOTNETCORE|10.0"
SKU="B1"

echo "=== TaskFlow Azure Deployment ==="
echo "Resource Group: $RESOURCE_GROUP"
echo "App Service Plan: $APP_SERVICE_PLAN"
echo "Web App Name: $WEB_APP_NAME"
echo "Runtime: $RUNTIME"
echo ""

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo "Error: Azure CLI is not installed. Please install it from https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
fi

# Check if user is logged in
echo "Checking Azure login status..."
if ! az account show &> /dev/null; then
    echo "Please log in to Azure..."
    az login
fi

# Create Resource Group
echo "Creating resource group..."
az group create --name $RESOURCE_GROUP --location eastus

# Create App Service Plan (Linux)
echo "Creating App Service Plan..."
az appservice plan create \
    --name $APP_SERVICE_PLAN \
    --resource-group $RESOURCE_GROUP \
    --sku $SKU \
    --is-linux

# Create Web App
echo "Creating Web App..."
az webapp create \
    --name $WEB_APP_NAME \
    --plan $APP_SERVICE_PLAN \
    --resource-group $RESOURCE_GROUP \
    --runtime "$RUNTIME"

# Configure app settings
echo "Configuring app settings..."
az webapp config appsettings set \
    --name $WEB_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --settings SCM_DO_BUILD_DURING_DEPLOYMENT=true

# Build the application
echo "Building application..."
dotnet publish -c Release

# Create deployment package
echo "Creating deployment package..."
cd bin/Release/net10.0/publish
zip -r ../../../../deploy.zip .
cd ../../../../..

# Deploy to Azure
echo "Deploying to Azure..."
az webapp deployment source config-zip \
    --name $WEB_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --src deploy.zip

# Get the web app URL
WEB_APP_URL=$(az webapp show --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP --query defaultHostName -o tsv)
echo ""
echo "=== Deployment Complete ==="
echo "Your application is now available at: https://$WEB_APP_URL"
echo "Health check: https://$WEB_APP_URL/health"
echo ""
