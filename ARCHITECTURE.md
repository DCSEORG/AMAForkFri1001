# Azure Services Architecture

## Expense Management System - Modern Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                         AZURE CLOUD SERVICES                          │
└──────────────────────────────────────────────────────────────────────┘

                    ┌─────────────────────────┐
                    │   Azure App Service     │
                    │  (ASP.NET Core Web App) │
                    │   - Razor Pages UI      │
                    │   - REST APIs           │
                    │   - Swagger Docs        │
                    └───────────┬─────────────┘
                                │
                    ┌───────────▼─────────────┐
                    │ User-Assigned Managed   │
                    │      Identity (MI)      │
                    │  mid-AppModAssist-xxx   │
                    └───────────┬─────────────┘
                                │
            ┌───────────────────┼───────────────────┐
            │                   │                   │
    ┌───────▼────────┐  ┌──────▼──────┐  ┌────────▼─────────┐
    │  Azure SQL DB  │  │  Azure      │  │  Azure Cognitive │
    │  ExpenseDB     │  │  OpenAI     │  │     Search       │
    │                │  │  (GPT-4o)   │  │  (RAG Context)   │
    │  - Users       │  │  Sweden     │  │                  │
    │  - Expenses    │  │  Region     │  │  - expense-docs  │
    │  - Categories  │  │             │  │    index         │
    └────────────────┘  └─────────────┘  └──────────────────┘

## Connection Details

### 1. App Service → Managed Identity
   - App Service uses User-Assigned Managed Identity for authentication
   - No API keys or secrets stored in code

### 2. Managed Identity → Azure SQL Database
   - Authentication: Azure AD (Managed Identity)
   - Permissions: db_datareader, db_datawriter
   - Connection secured via TLS

### 3. Managed Identity → Azure OpenAI
   - RBAC Role: "Cognitive Services OpenAI User"
   - Endpoint: https://oai-{name}.openai.azure.com
   - Model: gpt-4o (deployed in Sweden Central)

### 4. Managed Identity → Cognitive Search
   - RBAC Role: "Search Index Data Reader"
   - Used for RAG pattern (Retrieval-Augmented Generation)
   - Provides context for AI chat responses

## Deployment Options

### Basic Deployment (deploy.sh)
- App Service
- Managed Identity
- Connects to existing Azure SQL DB
- Chat UI shows "GenAI not configured" message

### Full Deployment (deploy-with-chat.sh)
- All components from basic deployment
- + Azure OpenAI Service
- + Azure Cognitive Search
- Full AI-powered chat capabilities with function calling

## Security Features

1. **No Secrets in Code**: All authentication uses Managed Identity
2. **HTTPS Only**: All connections encrypted
3. **Least Privilege**: MI has only required permissions
4. **Error Handling**: Graceful fallback to dummy data if DB unavailable
5. **Audit Trail**: All expense changes tracked with user and timestamp
