using KodaStore.Api.DTOs;
using KodaStore.Api.Repositories;
using KodaStore.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace KodaStore.Api.Controllers;

[ApiController]
[Route("api/v1/product")]

public sealed class ProductQueryController : ControllerBase
{
    private readonly IProductQueryService _service;

    public ProductQueryController(IProductQueryService service)
    {
        _service = service;
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(PageItem<ProductSearchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PageItem<ProductSearchResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(PageItem<ProductSearchResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PageItem<ProductSearchResponse>>> Search([FromQuery] ProductSearchRequest request, CancellationToken cancellationToken)
    {
        var respon = await _service.SearchAsync(request, cancellationToken);

        return Ok(respon);
    }
}