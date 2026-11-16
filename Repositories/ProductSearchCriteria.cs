namespace KodaStore.Api.Repositories;

public enum ProductSort
{
    CodeAsc,
    CodeDesc,
    NameAsc,
    NameDesc,
    PriceAsc,
    PriceDesc,
    StockAsc,
    StocKDesc
}

public sealed record ProductSearchCriteria
(
    string? Search,
    string? Category,
    decimal? MinPrice,
    decimal? MaxPrice,
    ProductSort Sort,
    int Page,
    int Pagesize
);