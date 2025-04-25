using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OperationsApi.Models;
using OperationsApi.Data;

[ApiController]
[Route("api/[controller]")] 
public class OperationsController : ControllerBase
{
    private readonly AppDbContext _db; 

    // מילון שמכיל פעולות חוקיות: Add, Subtract, Concat
    private static readonly Dictionary<string, Func<string, string, string>> operations =
        new(StringComparer.OrdinalIgnoreCase)
    {
        { "Add", (a, b) => (int.Parse(a) + int.Parse(b)).ToString() },
        { "Subtract", (a, b) => (int.Parse(a) - int.Parse(b)).ToString() },
        { "Concat", (a, b) => a + b }
    };

    public OperationsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("list")] // מחזיר את כל סוגי הפעולות הקיימות
    public IActionResult GetOperations()
    {
        return Ok(operations.Keys);
    }

    [HttpGet("calculate")] // חישוב פעולה + שמירת היסטוריה + החזרת היסטוריה
    public async Task<IActionResult> Calculate(string field1, string field2, string operation)
    {
        if (!operations.TryGetValue(operation, out var func)) //אם הפעולה קיימת במילון 
            return BadRequest("Operation not supported");

        var result = func(field1, field2);

        var history = new OperationHistory
        {
            Field1 = field1,
            Field2 = field2,
            Operation = operation,
            Result = result,
            ExecutedAt = DateTime.Now
        };

        // הוספת הפעולה ל DB
        _db.OperationHistories.Add(history);
        await _db.SaveChangesAsync();

        // חישוב תחילת החודש הנוכחי
        var now = DateTime.Now;
        var firstOfMonth = new DateTime(now.Year, now.Month, 1);

        // הבאת 3 פעולות אחרונות מאותו סוג פעולה
        var recentSameOps = await _db.OperationHistories
            .Where(h => h.Operation == operation)
            .OrderByDescending(h => h.ExecutedAt)
            .Take(3)
            .ToListAsync();

        // ספירת כמה פעולות מאותו סוג בוצעו החודש
        var countThisMonth = await _db.OperationHistories
            .CountAsync(h => h.Operation == operation && h.ExecutedAt >= firstOfMonth);

        // החזרת התוצאה + ההיסטוריה לקליינט
        return Ok(new
        {
            Result = result,
            Last3SameType = recentSameOps.Select(h => new { h.Field1, h.Field2, h.Result, h.ExecutedAt }),
            CountThisMonth = countThisMonth
        });
    }
}
