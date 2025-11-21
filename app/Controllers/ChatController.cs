using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Services;
using ExpenseManagement.Models;
using Azure.AI.OpenAI;
using System.Text.Json;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly OpenAIService _openAIService;
    private readonly DatabaseService _dbService;
    private readonly SearchService _searchService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        OpenAIService openAIService,
        DatabaseService dbService,
        SearchService searchService,
        ILogger<ChatController> logger)
    {
        _openAIService = openAIService;
        _dbService = dbService;
        _searchService = searchService;
        _logger = logger;
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public int? UserId { get; set; }
    }

    public class ChatResponse
    {
        public string Response { get; set; } = string.Empty;
        public bool IsUsingDummyData { get; set; }
        public bool IsGenAIConfigured { get; set; }
    }

    /// <summary>
    /// Send a message to the AI chat assistant
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request)
    {
        if (!_openAIService.IsConfigured)
        {
            return Ok(new ChatResponse
            {
                Response = "GenAI services are not deployed. To enable AI-powered chat, deploy using deploy-with-chat.sh script. You can still use the regular expense management interface.",
                IsUsingDummyData = _dbService.IsUsingDummyData,
                IsGenAIConfigured = false
            });
        }

        try
        {
            // Get RAG context
            var contextDocs = _searchService.IsConfigured
                ? await _searchService.SearchDocumentsAsync(request.Message)
                : _searchService.GetDummyContextDocuments();

            var context = string.Join("\n", contextDocs);

            // Define available functions
            var functions = new List<ChatCompletionsFunctionToolDefinition>
            {
                new ChatCompletionsFunctionToolDefinition
                {
                    Name = "get_expenses",
                    Description = "Get all expenses, optionally filtered by user ID or status ID",
                    Parameters = BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new
                        {
                            userId = new { type = "integer", description = "Filter by user ID (optional)" },
                            statusId = new { type = "integer", description = "Filter by status ID: 1=Draft, 2=Submitted, 3=Approved, 4=Rejected (optional)" }
                        }
                    })
                },
                new ChatCompletionsFunctionToolDefinition
                {
                    Name = "create_expense",
                    Description = "Create a new expense",
                    Parameters = BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new
                        {
                            userId = new { type = "integer", description = "User ID who creates the expense" },
                            categoryId = new { type = "integer", description = "Category ID: 1=Travel, 2=Meals, 3=Supplies, 4=Accommodation, 5=Other" },
                            amount = new { type = "number", description = "Expense amount in GBP" },
                            expenseDate = new { type = "string", description = "Expense date in ISO format" },
                            description = new { type = "string", description = "Expense description" }
                        },
                        required = new[] { "userId", "categoryId", "amount", "expenseDate" }
                    })
                },
                new ChatCompletionsFunctionToolDefinition
                {
                    Name = "get_expense_summary",
                    Description = "Get expense summary statistics for a user",
                    Parameters = BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new
                        {
                            userId = new { type = "integer", description = "User ID (optional, null for all users)" }
                        }
                    })
                },
                new ChatCompletionsFunctionToolDefinition
                {
                    Name = "update_expense_status",
                    Description = "Update expense status (submit, approve, or reject)",
                    Parameters = BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new
                        {
                            expenseId = new { type = "integer", description = "Expense ID to update" },
                            statusId = new { type = "integer", description = "New status ID: 2=Submitted, 3=Approved, 4=Rejected" },
                            reviewedBy = new { type = "integer", description = "User ID of the reviewer" }
                        },
                        required = new[] { "expenseId", "statusId", "reviewedBy" }
                    })
                }
            };

            // Create enhanced prompt with context
            var enhancedPrompt = $@"Context from expense management system documentation:
{context}

User question: {request.Message}

Please help the user with their expense management question. Use the available functions when needed to interact with the database.";

            var response = await _openAIService.GetCompletionWithFunctionsAsync(
                enhancedPrompt,
                functions,
                HandleFunctionCall
            );

            return Ok(new ChatResponse
            {
                Response = response,
                IsUsingDummyData = _dbService.IsUsingDummyData,
                IsGenAIConfigured = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return Ok(new ChatResponse
            {
                Response = $"Error processing your request: {ex.Message}",
                IsUsingDummyData = _dbService.IsUsingDummyData,
                IsGenAIConfigured = false
            });
        }
    }

    private async Task<string> HandleFunctionCall(string functionName, string argumentsJson)
    {
        _logger.LogInformation("Handling function call: {FunctionName} with args: {Args}", functionName, argumentsJson);

        try
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson);

            switch (functionName)
            {
                case "get_expenses":
                    {
                        int? userId = args.ContainsKey("userId") && args["userId"].ValueKind != JsonValueKind.Null
                            ? args["userId"].GetInt32()
                            : null;
                        int? statusId = args.ContainsKey("statusId") && args["statusId"].ValueKind != JsonValueKind.Null
                            ? args["statusId"].GetInt32()
                            : null;

                        var expenses = await _dbService.GetExpensesAsync(userId, statusId);
                        return JsonSerializer.Serialize(expenses);
                    }

                case "create_expense":
                    {
                        var expense = new ExpenseCreate
                        {
                            UserId = args["userId"].GetInt32(),
                            CategoryId = args["categoryId"].GetInt32(),
                            Amount = args["amount"].GetDecimal(),
                            ExpenseDate = DateTime.Parse(args["expenseDate"].GetString() ?? DateTime.Now.ToString()),
                            Description = args.ContainsKey("description") ? args["description"].GetString() : null
                        };

                        var expenseId = await _dbService.CreateExpenseAsync(expense);
                        return JsonSerializer.Serialize(new { expenseId, message = "Expense created successfully" });
                    }

                case "get_expense_summary":
                    {
                        int? userId = args.ContainsKey("userId") && args["userId"].ValueKind != JsonValueKind.Null
                            ? args["userId"].GetInt32()
                            : null;

                        var summary = await _dbService.GetExpenseSummaryAsync(userId);
                        return JsonSerializer.Serialize(summary);
                    }

                case "update_expense_status":
                    {
                        var update = new ExpenseStatusUpdate
                        {
                            ExpenseId = args["expenseId"].GetInt32(),
                            StatusId = args["statusId"].GetInt32(),
                            ReviewedBy = args["reviewedBy"].GetInt32()
                        };

                        var success = await _dbService.UpdateExpenseStatusAsync(update);
                        return JsonSerializer.Serialize(new { success, message = success ? "Status updated successfully" : "Failed to update status" });
                    }

                default:
                    return JsonSerializer.Serialize(new { error = $"Unknown function: {functionName}" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
