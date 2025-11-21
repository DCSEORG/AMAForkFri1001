using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly DatabaseService _dbService;

    public UsersController(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    /// <summary>
    /// Get all active users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<User>>> GetUsers()
    {
        var users = await _dbService.GetUsersAsync();
        return Ok(users);
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly DatabaseService _dbService;

    public CategoriesController(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    /// <summary>
    /// Get all active expense categories
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Category>>> GetCategories()
    {
        var categories = await _dbService.GetCategoriesAsync();
        return Ok(categories);
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StatusesController : ControllerBase
{
    private readonly DatabaseService _dbService;

    public StatusesController(DatabaseService dbService)
    {
        _dbService = dbService;
    }

    /// <summary>
    /// Get all expense statuses
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Status>>> GetStatuses()
    {
        var statuses = await _dbService.GetStatusesAsync();
        return Ok(statuses);
    }
}
