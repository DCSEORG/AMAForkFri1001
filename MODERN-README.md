# Modern Expense Management System

A modern, cloud-native expense management system built on Azure with AI-powered chat capabilities.

## 🚀 Features

### Core Functionality
- ✅ Create, view, update, and delete expenses
- ✅ Submit expenses for approval
- ✅ Manager approval workflow
- ✅ Category management (Travel, Meals, Supplies, Accommodation, Other)
- ✅ Status tracking (Draft, Submitted, Approved, Rejected)
- ✅ User and role management

### Modern Enhancements
- 🎨 Beautiful gradient UI with responsive design
- 📊 Dashboard with real-time expense statistics
- 🔒 Secure authentication via Azure Managed Identity (no API keys!)
- 🤖 AI-powered chat assistant with natural language queries
- 🔧 Function calling for database operations
- 📚 Interactive Swagger API documentation
- ⚠️ Graceful error handling with dummy data fallback
- 🔍 RAG (Retrieval-Augmented Generation) for contextual responses

## 📋 Prerequisites

- Azure subscription
- Azure CLI installed and logged in (`az login`)
- .NET 8.0 SDK (for local development)
- Python 3.8+ with pip (for database setup)

## 🛠️ Deployment

### Option 1: Basic Deployment (Without AI Chat)

Deploys the expense management system with database connectivity:

```bash
chmod +x deploy.sh
./deploy.sh
```

This deploys:
- Azure App Service (Free tier)
- User-Assigned Managed Identity
- Connects to existing Azure SQL Database

### Option 2: Full Deployment (With AI Chat)

Deploys everything including AI-powered chat:

```bash
chmod +x deploy-with-chat.sh
./deploy-with-chat.sh
```

This deploys everything from Option 1 plus:
- Azure OpenAI Service (GPT-4o model in Sweden)
- Azure Cognitive Search (for RAG context)
- Full AI chat capabilities with function calling

## 📱 Using the Application

### Main Dashboard (`/Index`)
- View expense summary statistics (Total, Pending, Approved)
- See recent expenses in a table format
- Access quick actions (Chat, API Docs)
- Visual status badges for each expense

### AI Chat Assistant (`/Chat`)
Ask the AI assistant in natural language:
- "Show me all submitted expenses"
- "Create a travel expense for £50 on November 20th"
- "What's the total amount of approved expenses?"
- "Approve expense #5 as user 2"

The AI will:
1. Understand your request
2. Call the appropriate database function
3. Return results in a conversational format

### API Documentation (`/swagger`)
Interactive Swagger UI for testing all APIs:
- GET `/api/expenses` - List all expenses
- POST `/api/expenses` - Create new expense
- PUT `/api/expenses/{id}` - Update expense
- PATCH `/api/expenses/{id}/status` - Change status
- DELETE `/api/expenses/{id}` - Delete expense
- GET `/api/users` - List users
- GET `/api/categories` - List categories
- GET `/api/statuses` - List statuses
- POST `/api/chat` - Chat with AI assistant

## 🏗️ Architecture

```
App Service (ASP.NET Core)
    ↓ (Managed Identity)
    ├─→ Azure SQL Database
    ├─→ Azure OpenAI (GPT-4o)
    └─→ Azure Cognitive Search
```

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed architecture diagram.

## 🔐 Security

- **No Secrets**: All authentication uses Azure Managed Identity
- **HTTPS Only**: All connections encrypted with TLS
- **Least Privilege**: Managed Identity has only required permissions
- **Input Validation**: All API inputs validated
- **Error Handling**: Detailed errors shown only in non-production
- **Updated Packages**: Latest secure versions of all dependencies

## 🧪 Testing

### Test the Basic Deployment
1. Navigate to: `https://<app-name>.azurewebsites.net/Index`
2. Verify dashboard loads with expense data
3. Check that database connection warning appears (if using dummy data)
4. Test navigation to Chat page
5. Verify chat shows "GenAI not configured" message

### Test the Full Deployment
1. Navigate to: `https://<app-name>.azurewebsites.net/Index`
2. Verify dashboard loads with real data from database
3. Navigate to `/Chat`
4. Test natural language queries
5. Verify API calls via `/swagger`

### Example Chat Queries
```
"Show me all expenses"
"Create a new meal expense for £25.50 on 2024-11-21 for user 1"
"What's my expense summary?"
"How many pending expenses are there?"
```

## 📂 Project Structure

```
.
├── app/                          # ASP.NET Core application
│   ├── Controllers/              # API controllers
│   ├── Models/                   # Data models
│   ├── Pages/                    # Razor Pages
│   ├── Services/                 # Business logic services
│   └── wwwroot/                  # Static files (CSS, JS)
├── infra/                        # Bicep infrastructure templates
│   ├── main.bicep               # Main orchestrator
│   ├── app-service.bicep        # App Service + Managed Identity
│   └── genai.bicep              # OpenAI + Search
├── deploy.sh                     # Basic deployment script
├── deploy-with-chat.sh          # Full deployment with AI
├── run-sql.py                   # Database setup script
├── script.sql                   # SQL permissions script
├── app.zip                      # Packaged application
├── ARCHITECTURE.md              # Architecture documentation
└── GenAISettings.md             # GenAI configuration guide
```

## 🎨 UI Comparison

### Legacy UI
- Basic gray interface
- Simple forms and tables
- No real-time feedback
- Manual data entry only

### Modern UI
- Beautiful gradient design (purple/blue theme)
- Responsive cards and layouts
- Real-time statistics dashboard
- AI-powered natural language interface
- Modern typography and spacing
- Smooth hover effects and transitions

## 🔧 Configuration

All configuration is managed via:
1. **Bicep Parameters**: Set in `infra/main.bicep`
2. **Environment Variables**: Automatically set by deployment scripts
3. **App Settings**: Configured post-deployment by scripts

Key settings:
- `ConnectionStrings:DefaultConnection` - Azure SQL connection
- `OpenAI:Endpoint` - Azure OpenAI endpoint
- `OpenAI:DeploymentName` - Model deployment name
- `Search:Endpoint` - Cognitive Search endpoint
- `ManagedIdentityClientId` - MI client ID

## 🐛 Troubleshooting

### Database Connection Errors
If you see "Database Connection Error" banner:
1. Check Managed Identity has database permissions
2. Run `python3 run-sql.py` to set permissions
3. Verify `script.sql` has correct MI name
4. Check firewall rules allow App Service

### GenAI Not Working
If chat shows "GenAI not configured":
1. Ensure you used `deploy-with-chat.sh`
2. Check App Service settings have OpenAI endpoints
3. Verify Managed Identity has "Cognitive Services OpenAI User" role
4. Check OpenAI deployment succeeded in Sweden region

### App Won't Start
1. Check App Service logs in Azure Portal
2. Verify app.zip structure (files at root, not in subdirectory)
3. Check .NET 8.0 runtime is configured
4. Verify all NuGet packages restored correctly

## 📝 Development

### Local Development
```bash
cd app
dotnet restore
dotnet run
```

Navigate to `https://localhost:5001`

### Building
```bash
cd app
dotnet build -c Release
```

### Publishing
```bash
cd app
dotnet publish -c Release -o ./publish
cd publish
zip -r ../../app.zip .
```

## 🤝 Contributing

This is a demo project showing how to modernize legacy applications with Azure and AI.

## 📄 License

See [LICENSE](LICENSE) file.

## 🙏 Acknowledgments

- Azure Architecture Best Practices
- Microsoft OpenAI Service Documentation
- ASP.NET Core Team
- Modern UI design principles from Dribbble
