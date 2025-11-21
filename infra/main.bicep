// Main Bicep template for Expense Management System
targetScope = 'resourceGroup'

@description('Location for all resources')
param location string = 'uksouth'

@description('Base name for resources')
param baseName string = 'expensemgmt'

@description('Deploy GenAI resources (OpenAI and Search)')
param deployGenAI bool = false

@description('Timestamp for unique naming')
param timestamp string = utcNow('ddHHmm')

// Deploy App Service with Managed Identity
module appServiceModule './app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
    baseName: baseName
    timestamp: timestamp
  }
}

// Conditionally deploy GenAI resources
module genAIModule './genai.bicep' = if (deployGenAI) {
  name: 'genAIDeployment'
  params: {
    location: location
    baseName: baseName
    timestamp: timestamp
    managedIdentityPrincipalId: appServiceModule.outputs.managedIdentityId
  }
  dependsOn: [
    appServiceModule
  ]
}

output appServiceName string = appServiceModule.outputs.appServiceName
output appServiceUrl string = appServiceModule.outputs.appServiceUrl
output managedIdentityId string = appServiceModule.outputs.managedIdentityId
output managedIdentityClientId string = appServiceModule.outputs.managedIdentityClientId
output managedIdentityName string = appServiceModule.outputs.managedIdentityName
output openAIEndpoint string = deployGenAI ? genAIModule.outputs.openAIEndpoint : ''
output openAIName string = deployGenAI ? genAIModule.outputs.openAIName : ''
output openAIModelName string = deployGenAI ? genAIModule.outputs.openAIModelName : ''
output searchEndpoint string = deployGenAI ? genAIModule.outputs.searchEndpoint : ''
output searchName string = deployGenAI ? genAIModule.outputs.searchName : ''
