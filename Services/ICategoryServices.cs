using KodaStore.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace KodaStore.Api.Services;

public interface ICategoryService
{
    // Task<ProductDetailResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<CategoryCreateResult> CreateWithProductAsync(CreateCategoryWithProductRequest request, CancellationToken ct);
    Task<CategoryCreateResult?> GetProductAsync(int categoryId, int page, int pageSize, CancellationToken ct);
    Task<ProductCategoryStateResponse?> GetProductAssigmentAsync(string productCode, CancellationToken ct);
    Task<CategoryAssignStatus> AssignProductsAsync(string productCode, AssignedProductCategoryRequest request, CancellationToken ct);
}