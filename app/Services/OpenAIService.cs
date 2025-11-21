using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;

namespace ExpenseManagement.Services;

public class OpenAIService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIService> _logger;
    private OpenAIClient? _client;
    private string? _deploymentName;
    private bool _isConfigured;

    public OpenAIService(IConfiguration configuration, ILogger<OpenAIService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            var endpoint = _configuration["OpenAI:Endpoint"];
            _deploymentName = _configuration["OpenAI:DeploymentName"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(_deploymentName))
            {
                _logger.LogWarning("OpenAI configuration not found. Chat features will use dummy responses.");
                _isConfigured = false;
                return;
            }

            var managedIdentityClientId = _configuration["ManagedIdentityClientId"];
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = managedIdentityClientId
            });

            _client = new OpenAIClient(new Uri(endpoint), credential);
            _isConfigured = true;
            _logger.LogInformation("OpenAI service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize OpenAI service");
            _isConfigured = false;
        }
    }

    public bool IsConfigured => _isConfigured;

    public async Task<string> GetCompletionAsync(string prompt, List<ChatRequestMessage>? messages = null)
    {
        if (!_isConfigured || _client == null || _deploymentName == null)
        {
            return "GenAI services are not deployed. To enable AI-powered chat, deploy using deploy-with-chat.sh script.";
        }

        try
        {
            var chatMessages = messages ?? new List<ChatRequestMessage>();
            if (chatMessages.Count == 0)
            {
                chatMessages.Add(new ChatRequestSystemMessage("You are a helpful assistant for an expense management system."));
                chatMessages.Add(new ChatRequestUserMessage(prompt));
            }

            var chatOptions = new ChatCompletionsOptions(_deploymentName, chatMessages);

            var response = await _client.GetChatCompletionsAsync(chatOptions);
            return response.Value.Choices[0].Message.Content ?? "No response generated.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat completion");
            return $"Error: {ex.Message}";
        }
    }

    public async Task<string> GetCompletionWithFunctionsAsync(
        string userMessage,
        List<ChatCompletionsFunctionToolDefinition> functions,
        Func<string, string, Task<string>> functionHandler)
    {
        if (!_isConfigured || _client == null || _deploymentName == null)
        {
            return "GenAI services are not deployed. To enable AI-powered chat, deploy using deploy-with-chat.sh script.";
        }

        try
        {
            var messages = new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage("You are a helpful assistant for an expense management system. Use the available functions to help users manage their expenses."),
                new ChatRequestUserMessage(userMessage)
            };

            var options = new ChatCompletionsOptions(_deploymentName, messages);
            foreach (var function in functions)
            {
                options.Tools.Add(function);
            }

            var response = await _client.GetChatCompletionsAsync(options);
            var choice = response.Value.Choices[0];

            // Check if the model wants to call a function
            if (choice.FinishReason == CompletionsFinishReason.ToolCalls && choice.Message.ToolCalls.Count > 0)
            {
                var toolCall = choice.Message.ToolCalls[0] as ChatCompletionsFunctionToolCall;
                if (toolCall != null)
                {
                    // Execute the function
                    var functionResult = await functionHandler(toolCall.Name, toolCall.Arguments);

                    // Send the function result back to the model
                    messages.Add(new ChatRequestAssistantMessage(choice.Message));
                    messages.Add(new ChatRequestToolMessage(functionResult, toolCall.Id));

                    var finalOptions = new ChatCompletionsOptions(_deploymentName, messages);
                    var finalResponse = await _client.GetChatCompletionsAsync(finalOptions);
                    return finalResponse.Value.Choices[0].Message.Content ?? "No response generated.";
                }
            }

            return choice.Message.Content ?? "No response generated.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting chat completion with functions");
            return $"Error: {ex.Message}";
        }
    }
}
