// Azure OpenAI and Cognitive Search for GenAI Chat UI
@description('Location for all resources')
param location string = resourceGroup().location

@description('Managed Identity ID to grant access')
param managedIdentityPrincipalId string

@description('Base name for resources')
param baseName string = 'expensemgmt'

@description('Timestamp for unique naming')
param timestamp string = utcNow('ddHHmm')

// Azure OpenAI Service
resource openAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: 'oai-${baseName}-${timestamp}'
  location: 'swedencentral'  // GPT-4o available in Sweden
  kind: 'OpenAI'
  sku: {
    name: 'S0'  // Standard tier
  }
  properties: {
    customSubDomainName: 'oai-${baseName}-${timestamp}'
    publicNetworkAccess: 'Enabled'
  }
}

// GPT-4o Deployment
resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAI
  name: 'gpt-4o'
  sku: {
    name: 'Standard'
    capacity: 10
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-08-06'
    }
  }
}

// Azure Cognitive Search for RAG
resource searchService 'Microsoft.Search/searchServices@2023-11-01' = {
  name: 'srch-${baseName}-${timestamp}'
  location: location
  sku: {
    name: 'basic'  // Basic tier for development
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
  }
}

// Role assignment: Cognitive Services OpenAI User for Managed Identity
resource openAIRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAI.id, managedIdentityPrincipalId, 'CognitiveServicesOpenAIUser')
  scope: openAI
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd') // Cognitive Services OpenAI User
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Role assignment: Search Index Data Reader for Managed Identity
resource searchRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchService.id, managedIdentityPrincipalId, 'SearchIndexDataReader')
  scope: searchService
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '1407120a-92aa-4202-b7e9-c0e197c71c8f') // Search Index Data Reader
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output openAIEndpoint string = openAI.properties.endpoint
output openAIName string = openAI.name
output openAIModelName string = gpt4oDeployment.name
output searchEndpoint string = 'https://${searchService.name}.search.windows.net'
output searchName string = searchService.name
