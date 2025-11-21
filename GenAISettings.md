# GenAI Configuration Settings

## Azure OpenAI Configuration

These settings are automatically configured by the `deploy-with-chat.sh` script:

### App Service Settings
- `OpenAI__Endpoint`: Azure OpenAI endpoint URL (e.g., https://oai-expensemgmt-xxx.openai.azure.com)
- `OpenAI__DeploymentName`: Model deployment name (gpt-4o)
- `Search__Endpoint`: Azure Cognitive Search endpoint (e.g., https://srch-expensemgmt-xxx.search.windows.net)
- `ManagedIdentityClientId`: Client ID of the user-assigned managed identity

### Authentication
All services use Managed Identity authentication via Azure.Identity.DefaultAzureCredential:
- No API keys required
- Automatic token refresh
- Secure credential management

### Model Details
- **Model**: GPT-4o
- **Version**: 2024-08-06
- **Region**: Sweden Central (swedencentral)
- **SKU**: S0 (Standard)
- **Capacity**: 10 tokens per minute

### Search Configuration
- **SKU**: Basic
- **Index Name**: expense-docs
- **Purpose**: RAG (Retrieval-Augmented Generation) context

## Function Calling

The chat interface supports the following functions for database operations:

1. **get_expenses**: Retrieve expenses with optional filtering
   - Parameters: userId (optional), statusId (optional)

2. **create_expense**: Create a new expense
   - Parameters: userId, categoryId, amount, expenseDate, description (optional)

3. **get_expense_summary**: Get expense statistics
   - Parameters: userId (optional)

4. **update_expense_status**: Submit, approve, or reject expenses
   - Parameters: expenseId, statusId, reviewedBy

## RAG Context Documents

The system uses dummy context documents if Search is not configured:
- Expense policy information
- Approval guidelines
- Reimbursement limits
- Receipt requirements

When Search is deployed, these can be replaced with actual indexed documents from the expense management system documentation.

## Deployment Notes

### Without GenAI (deploy.sh)
- App works normally with database operations
- Chat UI displays message that GenAI services are not configured
- Provides link to deploy-with-chat.sh for full experience

### With GenAI (deploy-with-chat.sh)
- Full AI-powered chat assistant
- Natural language expense queries
- Function calling for database operations
- RAG-enhanced responses with policy context
