![Header image](https://github.com/DougChisholm/App-Mod-Assist/blob/main/repo-header.png)

# App-Mod-Assist

A project to show how GitHub coding agent can turn screenshots of legacy apps into working proof-of-concepts for cloud native Azure replacements if the legacy database schema is also provided.

## ✅ Modernization Complete!

This repository now contains a fully modernized expense management system with:

### 🎨 Modern Web Application
- Beautiful gradient-based UI with responsive design
- ASP.NET Core 8.0 with Razor Pages
- Real-time expense dashboard with statistics
- Interactive Swagger API documentation

### 🔐 Secure Azure Infrastructure
- Azure App Service (Free/Basic tier)
- User-Assigned Managed Identity (no secrets!)
- Azure SQL Database integration
- Infrastructure as Code (Bicep templates)

### 🤖 AI-Powered Features
- Azure OpenAI GPT-4o integration
- Natural language chat interface
- Function calling for database operations
- RAG (Retrieval-Augmented Generation) pattern
- Azure Cognitive Search for context

### 📦 What's Included

```
├── app/                    # Modern ASP.NET Core application
├── infra/                  # Bicep infrastructure templates
├── deploy.sh              # Basic deployment (no AI)
├── deploy-with-chat.sh    # Full deployment with AI
├── app.zip                # Pre-built application package
├── ARCHITECTURE.md        # Architecture diagram
├── MODERN-README.md       # Detailed documentation
└── Modern-Screenshots/    # UI descriptions
```

## 🚀 Quick Start

### Prerequisites
- Azure subscription
- Azure CLI (`az login`)
- .NET 8.0 SDK (for development)
- Python 3.8+ (for database setup)

### Deploy Without AI (Free Tier)
```bash
chmod +x deploy.sh
./deploy.sh
```

### Deploy With AI Chat (Requires Azure OpenAI)
```bash
chmod +x deploy-with-chat.sh
./deploy-with-chat.sh
```

After deployment, access your app at:
- **Dashboard**: `https://<app-name>.azurewebsites.net/Index`
- **AI Chat**: `https://<app-name>.azurewebsites.net/Chat`
- **API Docs**: `https://<app-name>.azurewebsites.net/swagger`

## 📖 Documentation

- **[MODERN-README.md](MODERN-README.md)** - Complete usage guide
- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Architecture & connections
- **[GenAISettings.md](GenAISettings.md)** - AI configuration details
- **[Modern-Screenshots/UI-DESCRIPTION.md](Modern-Screenshots/UI-DESCRIPTION.md)** - UI comparison

## 🔑 Key Features

### From Legacy to Modern
| Legacy | Modern |
|--------|--------|
| Windows 95 UI | Gradient material design |
| No API | Full REST API + Swagger |
| Manual only | AI-powered chat |
| No security | Managed Identity auth |
| Single page views | Responsive dashboard |

### Security & Performance
- ✅ Zero secrets in code (Managed Identity)
- ✅ Async database operations
- ✅ CodeQL validated (0 vulnerabilities)
- ✅ Latest secure packages
- ✅ Graceful error handling

### AI Capabilities
Ask questions like:
- "Show me all submitted expenses"
- "Create a travel expense for £50"
- "What's my expense total?"
- "Approve expense #5"

## 🧪 Testing

The application includes:
- Dummy data fallback if database unavailable
- Warning banners for configuration issues
- All APIs testable via Swagger UI
- Chat works with or without AI deployed

## 🛠️ Development

```bash
cd app
dotnet restore
dotnet build
dotnet run
```

See **[MODERN-README.md](MODERN-README.md)** for complete development instructions.

## 📊 Azure Resources Deployed

- **App Service** - Hosts the web application
- **Managed Identity** - Secure authentication
- **Azure SQL Database** - Data storage (connects to existing)
- **Azure OpenAI** - GPT-4o model (optional)
- **Cognitive Search** - RAG context (optional)

## 📝 Original Instructions

WARNING: COLLABORATORS MUST FORK THE REPO AGAIN EVERY TIME THEY RUN THE CODING AGENT TO TEST IT TO NOT POLLUTE THIS BASE TEMPLATE

### To Use This Template:
1. Fork this repo
2. Replace screenshots in `Legacy-Screenshots/` with your app
3. Replace `Database-Schema/database_schema.sql` with your schema
4. Open GitHub Copilot agent and say "modernise my app"
5. Clone the generated code
6. Run `az login` and set subscription
7. Run `./deploy.sh` or `./deploy-with-chat.sh`

## 🙏 Acknowledgments

Built following Azure best practices from:
- Azure Architecture Center
- Microsoft OpenAI documentation
- ASP.NET Core patterns
- Modern UI design principles

