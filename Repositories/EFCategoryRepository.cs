using System.Data.Common;
using KodaStore.Api.Data;
using KodaStore.Api.DTOs;
using KodaStore.Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KodaStore.Api.Repositories;

public sealed class EFCategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<EFCategoryRepository> _logger;

    public EFCategoryRepository(AppDbContext db, ILogger<EFCategoryRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<CategoryCreateResult> CreateWithProductAsync(string name, IReadOnlyList<string> productCodes, CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            if(await _db.Category.AnyAsync(c => c.CategoryName == name, ct))
            {
                return new CategoryCreateResult(CategoryCreateStatus.DuplicateName, Detail: "Nama sudah terdaftar");
            }

            var category = new Category(name);
            _db.Category.Add(category);
            await _db.SaveChangesAsync(ct);

            var product = await _db.Products.Where(p => p.ProductCode.Contains(p.ProductCode)).ToListAsync(ct);

            if(product.Count != productCodes.Count)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                return new CategoryCreateResult(CategoryCreateStatus.ProductNotFound, Detail: "Satu atau beberapa Product code tidak ditemukan");
            }

            foreach (var item in product)
            {
                item.AssignCategory(category);
            }

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return new CategoryCreateResult(CategoryCreateStatus.Created, new CategoryCreateResponse(category.CategoryId, category.CategoryName, productCodes.ToArray()));

        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            _logger.LogWarning(ex, "Concurrent category assigment detctd");
            return new CategoryCreateResult(CategoryCreateStatus.Conflict, Detail: "Product berubah oleh request lain. muat data ulang");
        }
        catch(DbUpdateException ex) when (ex.InnerException is SqlException {Number: 2601 or 2627})
        {
            await transaction.RollbackAsync(CancellationToken.None);
            return new CategoryCreateResult(CategoryCreateStatus.DuplicateName, Detail: "Kategori dengan nama ini sudah dibuat oleh request lain");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<ProductCategoryStateResponse?> GetProductAssignmentAsync(
        string productCode, CancellationToken ct)
    {
        var state = await _db.Products.AsNoTracking()
            .Where(p => p.ProductCode == productCode)
            .Select(p => new
            {
                p.ProductCode,
                p.CategoryId,
                CategoryName = p.category == null ? null : p.category.CategoryName,
                p.RowVersion
            })
            .SingleOrDefaultAsync(ct);
        return state is null ? null : new ProductCategoryStateResponse(
            state.ProductCode, state.CategoryId, state.CategoryName,
            Convert.ToBase64String(state.RowVersion));
    }

    public async Task<CategoryAssignStatus> AssignProductCompareAndSwapAsync(
        string productCode, int newCategoryId, int? expectedCategoryId,
        CancellationToken ct)
    {
        var newCategory = await _db.Category.AsNoTracking()
            .Where(c => c.CategoryId == newCategoryId)
            .Select(c => new { c.CategoryName })
            .SingleOrDefaultAsync(ct);
        if (newCategory is null) return CategoryAssignStatus.CategoryNotFound;

        int updated;
        try
        {
            updated = await _db.Products
                .Where(p => p.ProductCode == productCode && p.CategoryId == expectedCategoryId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.CategoryId, newCategoryId)
                    .SetProperty(p => p.category, newCategory.CategoryName), ct);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            return CategoryAssignStatus.Conflict;
        }

        if (updated == 1) return CategoryAssignStatus.Update;

        bool exists = await _db.Products.AsNoTracking()
            .AnyAsync(p => p.ProductCode == productCode, ct);
        return exists ? CategoryAssignStatus.Conflict : CategoryAssignStatus.ProductNotFound;
    }

    public async Task<CategoryProductsResponse?> GetProductsAsync(int categoryId, int page, int pageSize, CancellationToken ct)
    {
        var category = await _db.Category.AsNoTracking()
            .Where(c => c.CategoryId == categoryId)
            .Select(c => new { c.CategoryId, c.CategoryName })
            .SingleOrDefaultAsync(ct);

        if (category is null) return null;

        var query = _db.Products.AsNoTracking()
            .Where(p => p.CategoryId == categoryId);
        long totalCount = await query.LongCountAsync(ct);
        var items = await query.OrderBy(p => p.ProductCode)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new CategoryProductResponse(
                p.ProductCode, p.Name, p.Price, p.Stock, p.category!.CategoryName))
            .ToArrayAsync(ct);

        return new CategoryProductsResponse(category.CategoryId, category.CategoryName,
            page, pageSize, totalCount, items);
    }

    // public async Task<ProductDetailResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    // {
    //     var category = await _db.Category.AsNoTracking().Include(p => p.Products).FirstOrDefaultAsync(c => c.CategoryId == id, cancellationToken);

    //     return category is null ? null : MapDetail(category);
    // }

    // private static ProductDetailResponse MapDetail(Category category)
    // {
    //     var items = category.Products.OrderBy(i => i.CategoryId).Select(i => new ProductToCategoryResponse(i.ProductCode, i.Name, i.Price, i.Stock)).ToArray();

    //     return new ProductDetailResponse(category.CategoryId, category.CategoryName, items);
    // }

}