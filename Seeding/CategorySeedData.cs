using KodaStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace KodaStore.Api.Seeding;

public static class CategorySeedData
{
    public static void Seed(DbContext context)
    {
        var products = context.Set<Products>().ToList();
        var categories = context.Set<Category>().ToList();
        var names = products.Select(p => p.Category.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var name in names)
        {
            if (categories.Any(c => string.Equals(c.CategoryName, name, StringComparison.OrdinalIgnoreCase)))
                continue;
            var category = new Category(name);
            context.Set<Category>().Add(category);
            categories.Add(category);
        }
        context.SaveChanges();
        foreach (var product in products)
        {
            var category = categories.Single(c => string.Equals(
                c.CategoryName, product.category.Trim(), StringComparison.OrdinalIgnoreCase));
            if (product.CategoryId != category.CategoryId)
                product.AssignCategory(category);
        }
        context.SaveChanges();
    }

    public static async Task SeedAsync(DbContext context, CancellationToken ct)
    {
        var products = await context.Set<Products>().ToListAsync(ct);
        var categories = await context.Set<Category>().ToListAsync(ct);
        var names = products.Select(p => p.category.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var name in names)
        {
            if (categories.Any(c => string.Equals(c.CategoryName, name, StringComparison.OrdinalIgnoreCase)))
                continue;
            var category = new Category(name);
            context.Set<Category>().Add(category);
            categories.Add(category);
        }
        await context.SaveChangesAsync(ct);

        foreach (var product in products)
        {
            var category = categories.Single(c => string.Equals(
                c.CategoryName, product.Category.Trim(), StringComparison.OrdinalIgnoreCase));
            if (product.CategoryId != category.CategoryId)
                product.AssignCategory(category);
        }
        await context.SaveChangesAsync(ct);
    }
}