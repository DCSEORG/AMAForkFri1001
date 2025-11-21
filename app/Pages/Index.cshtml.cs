using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Services;
using ExpenseManagement.Models;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    public readonly DatabaseService DbService;
    
    public List<Expense> Expenses { get; set; } = new();
    public ExpenseSummary Summary { get; set; } = new();

    public IndexModel(DatabaseService dbService)
    {
        DbService = dbService;
    }

    public async Task OnGetAsync()
    {
        Expenses = await DbService.GetExpensesAsync();
        Summary = await DbService.GetExpenseSummaryAsync();
    }
}
