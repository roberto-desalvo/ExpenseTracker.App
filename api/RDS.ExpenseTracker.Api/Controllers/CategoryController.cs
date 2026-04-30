using Microsoft.AspNetCore.Mvc;
using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.Domain.Services;

namespace RDS.ExpenseTracker.Api.Controllers;

[Route("api/[controller]")]
public class CategoryController : ApiControllerBase
{
    private readonly ICategoryService _service;

    public CategoryController(ICategoryService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CategoryDto>))]
    public Task<IActionResult> Get([FromQuery] string? name)
        => ExecuteAsync(() => _service.GetCategories(name));

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Get(int id)
        => ExecuteAsync(() => _service.GetCategory(id));

    [HttpGet("default")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetDefault()
        => ExecuteAsync(() => _service.GetDefaultCategory());

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Post([FromBody] IEnumerable<CategoryDto> dto)
        => ExecuteAsync(() => _service.AddCategories(dto));

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDto))]
    public Task<IActionResult> Put([FromBody] CategoryDto dto)
        => ExecuteAsync(() => _service.UpdateCategory(dto));

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<IActionResult> Delete(int id)
        => ExecuteAsync(() => _service.DeleteCategory(id));
}
