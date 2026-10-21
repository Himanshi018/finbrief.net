using FinBrief.Core.Dtos;
using FinBrief.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinBrief.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    // GET /api/categories
    // The category tabs, in display order.
    [HttpGet]
    public async Task<List<CategoryDto>> GetCategories() =>
        await db.Categories.AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryDto(c.Slug, c.Name))
            .ToListAsync();
}
