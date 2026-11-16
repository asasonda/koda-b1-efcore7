using KodaStore.Api.Data;
using KodaStore.Api.DTOs;
using KodaStore.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace KodaStore.Api.Repositories;

public sealed class EfProductQueryRepository : IProductQueryRepository
{
    private readonly AppDbContext _db;

    public EfProductQueryRepository(AppDbContext db)
    {
        _db = db;
    }
    public async Task<PageItem<ProductSearchResponse>> SearchAsync(ProductSearchCriteria criteria, CancellationToken cancellationToken)
    {
        IQueryable<Products> query = _db.Products.AsNoTracking();

        if (criteria.Search is { Length > 0 } keyword)
        {
            query = query.Where(p => p.ProductCode.StartsWith(keyword) || p.Name.StartsWith(keyword));
        }

        if(criteria.Category is {Length > 0 } category)
        {
            query = query.Where(p => p.category == category);
        }
        if(criteria.MinPrice is decimal minPrice)
        {
            query = query.Where(p => p.Price >= minPrice);
        }
        if(criteria.MaxPrice is decimal maxPrice)
        {
            query = query.Where(p => p.Price <= maxPrice);
        }

        long TotalCount = await query.LongCountAsync(cancellationToken);

        IOrderedQueryable<Products> sorted = criteria.Sort switch
        {
            ProductSort.CodeAsc => query.OrderBy(p => p.ProductCode),
            ProductSort.CodeDesc => query.OrderByDescending(p => p.ProductCode),

            ProductSort.NameAsc => query.OrderBy(p => p.Name).ThenBy(p => p.ProductCode),
            ProductSort.NameDesc => query.OrderByDescending(p => p.Name).ThenBy(p => p.ProductCode),

            ProductSort.PriceAsc => query.OrderBy(p => p.Price).ThenBy(p => p.ProductCode),
            ProductSort.PriceDesc => query.OrderByDescending(p => p.Price).ThenBy(p => p.ProductCode),

            ProductSort.StockAsc => query.OrderBy(p => p.Stock).ThenBy(p => p.ProductCode),
            ProductSort.StocKDesc => query.OrderByDescending(p => p.Stock).ThenBy(p => p.ProductCode),

            _ => throw new ArgumentOutOfRangeException(nameof(criteria.Sort))
        };

        int skip = (criteria.Page -1) * criteria.Pagesize;

        ProductSearchResponse[] items = await sorted.Skip(skip).Take(criteria.Pagesize).Select(p => new ProductSearchResponse(
            p.ProductCode,
            p.Name,
            p.Category,
            p.Price,
            p.Stock
        )),ToArrayAsync(cancellationToken);

        long totalPages = (TotalCount / criteria.PageSize) + (TotalCount % criteria.PageSize == 0 ? 0 : 1);

        return new PagedResult<ProductSearchItemResponse>(
            criteria.Page, criteria.PageSize, TotalCount, totalPages, items
        );

    }
}