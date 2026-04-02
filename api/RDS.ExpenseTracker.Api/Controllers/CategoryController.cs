using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using RDS.ExpenseTracker.Api.Dtos;
using RDS.ExpenseTracker.Domain.Models;
using RDS.ExpenseTracker.Business.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;


namespace RDS.ExpenseTracker.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _service;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ICategoryService service, IMapper mapper, ILogger<CategoryController> logger)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<CategoryDto>))]
        public async Task<IResult> Get()
        {
            try
            {
                var results = await _service.GetCategories();
                var dtos = _mapper.Map<IEnumerable<CategoryDto>>(results);
                return TypedResults.Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching categories");
                return TypedResults.Problem($"{ex}, {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDto))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IResult> Get(int id)
        {
            try
            {
                var category = await _service.GetCategory(id);
                if (category == null)
                {
                    return TypedResults.NoContent();
                }

                var dto = _mapper.Map<CategoryDto>(category);
                return TypedResults.Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching category with ID {id}", id);
                return TypedResults.Problem(ex.Message);
            }
        }

        [HttpGet("default")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDto))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IResult> GetDefault()
        {
            try
            {
                var category = await _service.GetDefaultCategory();
                if(category == null)
                {
                    return TypedResults.NoContent();
                }

                var dto = _mapper.Map<CategoryDto>(category);
                return TypedResults.Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching default category");
                return TypedResults.Problem(ex.Message);
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
        public async Task<IResult> Post([FromBody] IEnumerable<CategoryDto> dto)
        {
            try
            {
                var categories = _mapper.Map<IEnumerable<Category>>(dto);
                await _service.AddCategories(categories);
                _logger.LogInformation("Categories created: {categories} ", string.Join(" - ", categories.Select(x => $"Id: {x.Id}, Name: {x.Name}")));
                return TypedResults.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating categories: {categories}", string.Join(" - ", dto.Select(x => x.Name)));
                return TypedResults.Problem($"{ex} {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CategoryDto))]
        public async Task<IResult> Put(int id, [FromBody] CategoryDto dto)
        {
            var category = _mapper.Map<Category>(dto);

            try
            {
                await _service.UpdateCategory(category);
                _logger.LogInformation("Category {category.Name} updated with ID {id}", category.Name, id);
                return TypedResults.Ok(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {category.Name}", category.Name);
                return TypedResults.Problem($"{ex} {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IResult> Delete(int id)
        {
            try
            {
                await _service.DeleteCategory(id);
                _logger.LogInformation("Category with ID {id} deleted", id);
                return TypedResults.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category with ID {id}", id);
                return TypedResults.Problem($"{ex} {ex.Message}");
            }
        }
    }
}
