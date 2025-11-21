using Microsoft.Data.SqlClient;
using Azure.Identity;
using ExpenseManagement.Models;
using System.Data;

namespace ExpenseManagement.Services;

public class DatabaseService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseService> _logger;
    private string? _lastError;
    private bool _useDummyData;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _useDummyData = false;
    }

    public string? LastError => _lastError;
    public bool IsUsingDummyData => _useDummyData;

    private SqlConnection GetConnection()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        var managedIdentityClientId = _configuration["ManagedIdentityClientId"];

        if (!string.IsNullOrEmpty(managedIdentityClientId))
        {
            // Use managed identity authentication
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = managedIdentityClientId
            });

            var conn = new SqlConnection(connectionString);
            var accessToken = credential.GetToken(new Azure.Core.TokenRequestContext(
                new[] { "https://database.windows.net/.default" })).Token;
            conn.AccessToken = accessToken;
            return conn;
        }
        else
        {
            // Fallback to connection string authentication
            return new SqlConnection(connectionString);
        }
    }

    private async Task<T> ExecuteWithFallback<T>(Func<Task<T>> operation, Func<T> dummyDataProvider, string operationName)
    {
        try
        {
            _lastError = null;
            _useDummyData = false;
            return await operation();
        }
        catch (Exception ex)
        {
            _lastError = $"Database error in {operationName}: {ex.Message}";
            _logger.LogError(ex, "Database operation failed: {OperationName}", operationName);
            _useDummyData = true;
            return dummyDataProvider();
        }
    }

    public async Task<List<Expense>> GetExpensesAsync(int? userId = null, int? statusId = null)
    {
        return await ExecuteWithFallback(
            async () =>
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                var query = @"
                    SELECT e.ExpenseId, e.UserId, u.UserName, e.CategoryId, c.CategoryName, 
                           e.StatusId, s.StatusName, e.AmountMinor, e.Currency, e.ExpenseDate,
                           e.Description, e.ReceiptFile, e.SubmittedAt, e.ReviewedBy, 
                           rm.UserName as ReviewedByName, e.ReviewedAt, e.CreatedAt
                    FROM dbo.Expenses e
                    JOIN dbo.Users u ON e.UserId = u.UserId
                    JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
                    JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
                    LEFT JOIN dbo.Users rm ON e.ReviewedBy = rm.UserId
                    WHERE (@UserId IS NULL OR e.UserId = @UserId)
                      AND (@StatusId IS NULL OR e.StatusId = @StatusId)
                    ORDER BY e.CreatedAt DESC";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@StatusId", (object?)statusId ?? DBNull.Value);

                var expenses = new List<Expense>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    expenses.Add(new Expense
                    {
                        ExpenseId = reader.GetInt32(0),
                        UserId = reader.GetInt32(1),
                        UserName = reader.GetString(2),
                        CategoryId = reader.GetInt32(3),
                        CategoryName = reader.GetString(4),
                        StatusId = reader.GetInt32(5),
                        StatusName = reader.GetString(6),
                        AmountMinor = reader.GetInt32(7),
                        Currency = reader.GetString(8),
                        ExpenseDate = reader.GetDateTime(9),
                        Description = reader.IsDBNull(10) ? null : reader.GetString(10),
                        ReceiptFile = reader.IsDBNull(11) ? null : reader.GetString(11),
                        SubmittedAt = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                        ReviewedBy = reader.IsDBNull(13) ? null : reader.GetInt32(13),
                        ReviewedByName = reader.IsDBNull(14) ? null : reader.GetString(14),
                        ReviewedAt = reader.IsDBNull(15) ? null : reader.GetDateTime(15),
                        CreatedAt = reader.GetDateTime(16)
                    });
                }
                return expenses;
            },
            () => GetDummyExpenses(),
            "GetExpensesAsync"
        );
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        return await ExecuteWithFallback(
            async () =>
            {
                var expenses = await GetExpensesAsync();
                return expenses.FirstOrDefault(e => e.ExpenseId == expenseId);
            },
            () => GetDummyExpenses().FirstOrDefault(e => e.ExpenseId == expenseId),
            "GetExpenseByIdAsync"
        );
    }

    public async Task<int> CreateExpenseAsync(ExpenseCreate expense)
    {
        return await ExecuteWithFallback(
            async () =>
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                var query = @"
                    INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, 
                                             ExpenseDate, Description, ReceiptFile, CreatedAt)
                    VALUES (@UserId, @CategoryId, 1, @AmountMinor, @Currency, @ExpenseDate, 
                            @Description, @ReceiptFile, SYSUTCDATETIME());
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", expense.UserId);
                cmd.Parameters.AddWithValue("@CategoryId", expense.CategoryId);
                cmd.Parameters.AddWithValue("@AmountMinor", (int)(expense.Amount * 100));
                cmd.Parameters.AddWithValue("@Currency", expense.Currency);
                cmd.Parameters.AddWithValue("@ExpenseDate", expense.ExpenseDate);
                cmd.Parameters.AddWithValue("@Description", (object?)expense.Description ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ReceiptFile", (object?)expense.ReceiptFile ?? DBNull.Value);

                return (int)(await cmd.ExecuteScalarAsync() ?? 0);
            },
            () => 999,
            "CreateExpenseAsync"
        );
    }

    public async Task<bool> UpdateExpenseAsync(ExpenseUpdate expense)
    {
        return await ExecuteWithFallback(
            async () =>
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                var updates = new List<string>();
                var cmd = new SqlCommand { Connection = conn };

                if (expense.CategoryId.HasValue)
                {
                    updates.Add("CategoryId = @CategoryId");
                    cmd.Parameters.AddWithValue("@CategoryId", expense.CategoryId.Value);
                }
                if (expense.Amount.HasValue)
                {
                    updates.Add("AmountMinor = @AmountMinor");
                    cmd.Parameters.AddWithValue("@AmountMinor", (int)(expense.Amount.Value * 100));
                }
                if (expense.ExpenseDate.HasValue)
                {
                    updates.Add("ExpenseDate = @ExpenseDate");
                    cmd.Parameters.AddWithValue("@ExpenseDate", expense.ExpenseDate.Value);
                }
                if (expense.Description != null)
                {
                    updates.Add("Description = @Description");
                    cmd.Parameters.AddWithValue("@Description", expense.Description);
                }
                if (expense.ReceiptFile != null)
                {
                    updates.Add("ReceiptFile = @ReceiptFile");
                    cmd.Parameters.AddWithValue("@ReceiptFile", expense.ReceiptFile);
                }

                if (updates.Count == 0) return true;

                cmd.CommandText = $@"
                    UPDATE dbo.Expenses 
                    SET {string.Join(", ", updates)}
                    WHERE ExpenseId = @ExpenseId AND StatusId = 1";  // Only update drafts

                cmd.Parameters.AddWithValue("@ExpenseId", expense.ExpenseId);

                var affected = await cmd.ExecuteNonQueryAsync();
                return affected > 0;
            },
            () => true,
            "UpdateExpenseAsync"
        );
    }

    public async Task<bool> UpdateExpenseStatusAsync(ExpenseStatusUpdate update)
    {
        return await ExecuteWithFallback(
            async () =>
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                var query = @"
                    UPDATE dbo.Expenses
                    SET StatusId = @StatusId,
                        ReviewedBy = @ReviewedBy,
                        ReviewedAt = SYSUTCDATETIME(),
                        SubmittedAt = CASE WHEN @StatusId = 2 THEN SYSUTCDATETIME() ELSE SubmittedAt END
                    WHERE ExpenseId = @ExpenseId";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ExpenseId", update.ExpenseId);
                cmd.Parameters.AddWithValue("@StatusId", update.StatusId);
                cmd.Parameters.AddWithValue("@ReviewedBy", update.ReviewedBy);

                var affected = await cmd.ExecuteNonQueryAsync();
                return affected > 0;
            },
            () => true,
            "UpdateExpenseStatusAsync"
        );
    }

    public async Task<bool> DeleteExpenseAsync(int expenseId)
    {
        return await ExecuteWithFallback(
            async () =>
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                var query = "DELETE FROM dbo.Expenses WHERE ExpenseId = @ExpenseId AND StatusId = 1";
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@ExpenseId", expenseId);

                var affected = await cmd.ExecuteNonQueryAsync();
                return affected > 0;
            },
            () => true,
            "DeleteExpenseAsync"
        );
    }

    public async Task<List<User>> GetUsersAsync()
    {
        return await ExecuteWithFallback(
            async () =>
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                var query = @"
                    SELECT u.UserId, u.UserName, u.Email, u.RoleId, r.RoleName,
                           u.ManagerId, m.UserName as ManagerName, u.IsActive, u.CreatedAt
                    FROM dbo.Users u
                    JOIN dbo.Roles r ON u.RoleId = r.RoleId
                    LEFT JOIN dbo.Users m ON u.ManagerId = m.UserId
                    WHERE u.IsActive = 1
                    ORDER BY u.UserName";

                using var cmd = new SqlCommand(query, conn);
                var users = new List<User>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    users.Add(new User
                    {
                        UserId = reader.GetInt32(0),
                        UserName = reader.GetString(1),
                        Email = reader.GetString(2),
                        RoleId = reader.GetInt32(3),
                        RoleName = reader.GetString(4),
                        ManagerId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                        ManagerName = reader.IsDBNull(6) ? null : reader.GetString(6),
                        IsActive = reader.GetBoolean(7),
                        CreatedAt = reader.GetDateTime(8)
                    });
                }
                return users;
            },
            () => GetDummyUsers(),
            "GetUsersAsync"
        );
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        return await ExecuteWithFallback(
            async () =>
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                var query = "SELECT CategoryId, CategoryName, IsActive FROM dbo.ExpenseCategories WHERE IsActive = 1";
                using var cmd = new SqlCommand(query, conn);
                
                var categories = new List<Category>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    categories.Add(new Category
                    {
                        CategoryId = reader.GetInt32(0),
                        CategoryName = reader.GetString(1),
                        IsActive = reader.GetBoolean(2)
                    });
                }
                return categories;
            },
            () => GetDummyCategories(),
            "GetCategoriesAsync"
        );
    }

    public async Task<List<Status>> GetStatusesAsync()
    {
        return await ExecuteWithFallback(
            async () =>
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                var query = "SELECT StatusId, StatusName FROM dbo.ExpenseStatus";
                using var cmd = new SqlCommand(query, conn);
                
                var statuses = new List<Status>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    statuses.Add(new Status
                    {
                        StatusId = reader.GetInt32(0),
                        StatusName = reader.GetString(1)
                    });
                }
                return statuses;
            },
            () => GetDummyStatuses(),
            "GetStatusesAsync"
        );
    }

    public async Task<ExpenseSummary> GetExpenseSummaryAsync(int? userId = null)
    {
        return await ExecuteWithFallback(
            async () =>
            {
                using var conn = GetConnection();
                await conn.OpenAsync();

                var query = @"
                    SELECT 
                        COUNT(*) as TotalCount,
                        ISNULL(SUM(AmountMinor), 0) as TotalAmount,
                        SUM(CASE WHEN StatusId = 2 THEN 1 ELSE 0 END) as PendingCount,
                        SUM(CASE WHEN StatusId = 2 THEN AmountMinor ELSE 0 END) as PendingAmount,
                        SUM(CASE WHEN StatusId = 3 THEN 1 ELSE 0 END) as ApprovedCount,
                        SUM(CASE WHEN StatusId = 3 THEN AmountMinor ELSE 0 END) as ApprovedAmount
                    FROM dbo.Expenses
                    WHERE (@UserId IS NULL OR UserId = @UserId)";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new ExpenseSummary
                    {
                        TotalCount = reader.GetInt32(0),
                        TotalAmount = reader.GetInt32(1) / 100.0m,
                        PendingCount = reader.GetInt32(2),
                        PendingAmount = reader.GetInt32(3) / 100.0m,
                        ApprovedCount = reader.GetInt32(4),
                        ApprovedAmount = reader.GetInt32(5) / 100.0m
                    };
                }
                return new ExpenseSummary();
            },
            () => new ExpenseSummary
            {
                TotalCount = 4,
                TotalAmount = 165.64m,
                PendingCount = 1,
                PendingAmount = 25.40m,
                ApprovedCount = 2,
                ApprovedAmount = 137.25m
            },
            "GetExpenseSummaryAsync"
        );
    }

    // Dummy data for fallback
    private List<Expense> GetDummyExpenses()
    {
        return new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 2,
                StatusName = "Submitted",
                AmountMinor = 2540,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-10),
                Description = "Taxi from airport to client site",
                ReceiptFile = "/receipts/alice/taxi_oct20.jpg",
                SubmittedAt = DateTime.Now.AddDays(-9),
                CreatedAt = DateTime.Now.AddDays(-10)
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 2,
                CategoryName = "Meals",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 1425,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-30),
                Description = "Client lunch meeting",
                ReceiptFile = "/receipts/alice/lunch_sep15.jpg",
                SubmittedAt = DateTime.Now.AddDays(-29),
                ReviewedBy = 2,
                ReviewedByName = "Bob Manager",
                ReviewedAt = DateTime.Now.AddDays(-28),
                CreatedAt = DateTime.Now.AddDays(-30)
            },
            new Expense
            {
                ExpenseId = 3,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 3,
                CategoryName = "Supplies",
                StatusId = 1,
                StatusName = "Draft",
                AmountMinor = 799,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-1),
                Description = "Office stationery",
                CreatedAt = DateTime.Now.AddDays(-1)
            },
            new Expense
            {
                ExpenseId = 4,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 4,
                CategoryName = "Accommodation",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 12300,
                Currency = "GBP",
                ExpenseDate = DateTime.Now.AddDays(-60),
                Description = "Hotel during client visit",
                ReceiptFile = "/receipts/alice/hotel_aug10.jpg",
                SubmittedAt = DateTime.Now.AddDays(-59),
                ReviewedBy = 2,
                ReviewedByName = "Bob Manager",
                ReviewedAt = DateTime.Now.AddDays(-58),
                CreatedAt = DateTime.Now.AddDays(-60)
            }
        };
    }

    private List<User> GetDummyUsers()
    {
        return new List<User>
        {
            new User
            {
                UserId = 1,
                UserName = "Alice Example",
                Email = "alice@example.co.uk",
                RoleId = 1,
                RoleName = "Employee",
                ManagerId = 2,
                ManagerName = "Bob Manager",
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-90)
            },
            new User
            {
                UserId = 2,
                UserName = "Bob Manager",
                Email = "bob.manager@example.co.uk",
                RoleId = 2,
                RoleName = "Manager",
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-100)
            }
        };
    }

    private List<Category> GetDummyCategories()
    {
        return new List<Category>
        {
            new Category { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new Category { CategoryId = 2, CategoryName = "Meals", IsActive = true },
            new Category { CategoryId = 3, CategoryName = "Supplies", IsActive = true },
            new Category { CategoryId = 4, CategoryName = "Accommodation", IsActive = true },
            new Category { CategoryId = 5, CategoryName = "Other", IsActive = true }
        };
    }

    private List<Status> GetDummyStatuses()
    {
        return new List<Status>
        {
            new Status { StatusId = 1, StatusName = "Draft" },
            new Status { StatusId = 2, StatusName = "Submitted" },
            new Status { StatusId = 3, StatusName = "Approved" },
            new Status { StatusId = 4, StatusName = "Rejected" }
        };
    }
}
