using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using Azure.Identity;

namespace ExpenseManagement.Services;

public class SearchService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SearchService> _logger;
    private SearchClient? _searchClient;
    private bool _isConfigured;

    public SearchService(IConfiguration configuration, ILogger<SearchService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            var endpoint = _configuration["Search:Endpoint"];
            
            if (string.IsNullOrEmpty(endpoint))
            {
                _logger.LogWarning("Search configuration not found. RAG features will not be available.");
                _isConfigured = false;
                return;
            }

            var managedIdentityClientId = _configuration["ManagedIdentityClientId"];
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = managedIdentityClientId
            });

            _searchClient = new SearchClient(new Uri(endpoint), "expense-docs", credential);
            _isConfigured = true;
            _logger.LogInformation("Search service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Search service");
            _isConfigured = false;
        }
    }

    public bool IsConfigured => _isConfigured;

    public async Task<List<string>> SearchDocumentsAsync(string query, int maxResults = 3)
    {
        if (!_isConfigured || _searchClient == null)
        {
            return new List<string>
            {
                "Search service not configured. RAG context not available."
            };
        }

        try
        {
            var searchOptions = new SearchOptions
            {
                Size = maxResults,
                Select = { "content" }
            };

            var response = await _searchClient.SearchAsync<SearchDocument>(query, searchOptions);
            var results = new List<string>();

            await foreach (var result in response.Value.GetResultsAsync())
            {
                if (result.Document.TryGetValue("content", out var content))
                {
                    results.Add(content?.ToString() ?? string.Empty);
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching documents");
            return new List<string> { $"Search error: {ex.Message}" };
        }
    }

    public List<string> GetDummyContextDocuments()
    {
        return new List<string>
        {
            "Expense Policy: All expenses must be submitted within 30 days of the expense date.",
            "Travel expenses include taxi, train, and flight costs for business purposes.",
            "Meal expenses are reimbursed up to £50 per day for business travel.",
            "All expenses require a valid receipt to be approved.",
            "Managers can approve expenses up to £500. Higher amounts require director approval."
        };
    }
}
