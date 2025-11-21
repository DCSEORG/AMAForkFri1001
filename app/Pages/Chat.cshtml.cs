using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class ChatModel : PageModel
{
    public readonly DatabaseService DbService;
    private readonly OpenAIService _openAIService;
    
    public bool IsGenAIConfigured { get; set; }

    public ChatModel(DatabaseService dbService, OpenAIService openAIService)
    {
        DbService = dbService;
        _openAIService = openAIService;
    }

    public void OnGet()
    {
        IsGenAIConfigured = _openAIService.IsConfigured;
    }
}
