// ini untuk search

using KodaStore.Api.DTOs;

namespace KodaStore.Api.Repositories;

public interface IProductQueryRepository
{
    Task<PageItem<ProductSearchResponse>> SearchAsync(ProductSearchCriteria criteria, CancellationToken cancellationToken);
}