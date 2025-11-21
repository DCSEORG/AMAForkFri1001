using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly DatabaseService _dbService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(DatabaseService dbService, ILogger<ExpensesController> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expenses, optionally filtered by user and/or status
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Expense>>> GetExpenses([FromQuery] int? userId = null, [FromQuery] int? statusId = null)
    {
        var expenses = await _dbService.GetExpensesAsync(userId, statusId);
        return Ok(expenses);
    }

    /// <summary>
    /// Get a specific expense by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetExpense(int id)
    {
        var expense = await _dbService.GetExpenseByIdAsync(id);
        if (expense == null)
            return NotFound();
        return Ok(expense);
    }

    /// <summary>
    /// Create a new expense
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<int>> CreateExpense([FromBody] ExpenseCreate expense)
    {
        var expenseId = await _dbService.CreateExpenseAsync(expense);
        return CreatedAtAction(nameof(GetExpense), new { id = expenseId }, expenseId);
    }

    /// <summary>
    /// Update an existing expense (only drafts can be updated)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateExpense(int id, [FromBody] ExpenseUpdate expense)
    {
        expense.ExpenseId = id;
        var success = await _dbService.UpdateExpenseAsync(expense);
        if (!success)
            return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Update expense status (submit, approve, reject)
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<ActionResult> UpdateExpenseStatus(int id, [FromBody] ExpenseStatusUpdate update)
    {
        update.ExpenseId = id;
        var success = await _dbService.UpdateExpenseStatusAsync(update);
        if (!success)
            return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Delete an expense (only drafts can be deleted)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteExpense(int id)
    {
        var success = await _dbService.DeleteExpenseAsync(id);
        if (!success)
            return NotFound();
        return NoContent();
    }

    /// <summary>
    /// Get expense summary statistics
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<ExpenseSummary>> GetSummary([FromQuery] int? userId = null)
    {
        var summary = await _dbService.GetExpenseSummaryAsync(userId);
        return Ok(summary);
    }
}
