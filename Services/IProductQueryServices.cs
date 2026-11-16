using KodaStore.Api.DTOs;

namespace KodaStore.Api.Services;

public interface IProductQueryService
{
    Task<PageItem<ProductSearchResponse>> SearchAsync(ProductSearchRequest request, CancellationToken ct);
}