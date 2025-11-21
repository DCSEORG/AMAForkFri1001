#!/bin/bash
set -e

echo "======================================"
echo "Expense Management System Deployment"
echo "WITH GenAI Chat Capabilities"
echo "======================================"
echo ""

# Configuration
RESOURCE_GROUP="rg-expense-mgmt-dev"
LOCATION="uksouth"
BASE_NAME="expensemgmt"
TIMESTAMP=$(date +%d%H%M)

echo "Configuration:"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo "  Base Name: $BASE_NAME"
echo "  Timestamp: $TIMESTAMP"
echo ""

# Create resource group if it doesn't exist
echo "Creating resource group..."
az group create \
  --name "$RESOURCE_GROUP" \
  --location "$LOCATION" \
  --output table

echo ""
echo "Deploying infrastructure (App Service + Managed Identity + GenAI)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group "$RESOURCE_GROUP" \
  --template-file ./infra/main.bicep \
  --parameters location="$LOCATION" baseName="$BASE_NAME" timestamp="$TIMESTAMP" deployGenAI=true \
  --output json)

echo "Deployment completed successfully!"
echo ""

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.appServiceName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.appServiceUrl.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.managedIdentityClientId.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.managedIdentityName.value')
OPENAI_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.openAIEndpoint.value')
OPENAI_MODEL_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.openAIModelName.value')
SEARCH_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.properties.outputs.searchEndpoint.value')

echo "Deployment Information:"
echo "  App Service Name: $APP_SERVICE_NAME"
echo "  App Service URL: $APP_SERVICE_URL"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo "  Managed Identity Client ID: $MANAGED_IDENTITY_CLIENT_ID"
echo "  OpenAI Endpoint: $OPENAI_ENDPOINT"
echo "  OpenAI Model: $OPENAI_MODEL_NAME"
echo "  Search Endpoint: $SEARCH_ENDPOINT"
echo ""

# Update script.sql with actual managed identity name
echo "Updating script.sql with managed identity name..."
sed -i "s/mid-AppModAssist-PLACEHOLDER/$MANAGED_IDENTITY_NAME/g" script.sql

# Install required Python packages if not already installed
echo "Installing Python packages..."
pip3 install --quiet pyodbc azure-identity

# Run the Python script to configure database permissions
echo "Configuring database permissions..."
python3 run-sql.py

echo ""
echo "Configuring App Service settings with GenAI endpoints..."
az webapp config appsettings set \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_SERVICE_NAME" \
  --settings \
    "OpenAI__Endpoint=$OPENAI_ENDPOINT" \
    "OpenAI__DeploymentName=$OPENAI_MODEL_NAME" \
    "Search__Endpoint=$SEARCH_ENDPOINT" \
  --output table

echo ""
echo "Deploying application code..."
if [ -f "./app.zip" ]; then
  az webapp deploy \
    --resource-group "$RESOURCE_GROUP" \
    --name "$APP_SERVICE_NAME" \
    --src-path ./app.zip \
    --type zip \
    --async true
  
  echo ""
  echo "Application deployment initiated (running asynchronously)..."
else
  echo "Warning: app.zip not found. Skipping application deployment."
  echo "Please build the application first and ensure app.zip exists."
fi

echo ""
echo "======================================"
echo "Deployment Complete!"
echo "======================================"
echo ""
echo "App URL: $APP_SERVICE_URL/Index"
echo "Chat UI URL: $APP_SERVICE_URL/Chat"
echo ""
echo "Note: Navigate to /Index to view the application"
echo "      Navigate to /Chat to use the GenAI-powered chat interface"
