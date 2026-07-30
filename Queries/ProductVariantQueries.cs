using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace API_Ecommerce.Queries
{
	public class ProductVariantQueries
	{
		private readonly AppDbContext _context;

		public ProductVariantQueries(AppDbContext context)
		{
			_context = context;
		}

		// Helper to get and automatically open connection if closed
		private async Task<IDbConnection> GetOpenConnectionAsync()
		{
			var connection = _context.Database.GetDbConnection();
			if (connection.State != ConnectionState.Open)
			{
				await connection.OpenAsync();
			}
			return connection;
		}

		/// <summary>
		/// Retrieves all variants belonging to a specific product.
		/// Set includeSuspended = false for customers (hides StockQuantity <= 0 or Status != Active).
		/// Set includeSuspended = true for Sellers/Admins so they can see and manage all variants.
		/// </summary>
		public async Task<IEnumerable<ProductVariantResponseDto>> GetByProductIdAsync(
			int productId,
			bool includeSuspended = false)
		{
			var whereClauses = new List<string> { "pv.ProductId = @ProductId" };
			var dynamicParameters = new DynamicParameters();
			dynamicParameters.Add("ProductId", productId);

			// Hide suspended/out-of-stock variants for public users
			if (!includeSuspended)
			{
				whereClauses.Add("pv.StockQuantity > 0");
			}

			string whereSql = " WHERE " + string.Join(" AND ", whereClauses);

			string sql = $@"
                SELECT 
                    pv.Id,
                    pv.ProductId,
                    pv.Title,
                    pv.Sku,
                    pv.Price,
                    pv.DiscountPrice,
                    pv.StockQuantity,
                    pv.ImageUrl,
                    pv.Size,
                    pv.Color,
                    pv.CreatedAt,
                    pv.UpdatedAt
                FROM ProductVariants pv
                {whereSql}
                ORDER BY pv.CreatedAt ASC;";

			using var connection = await GetOpenConnectionAsync();
			return await connection.QueryAsync<ProductVariantResponseDto>(sql, dynamicParameters);
		}

		/// <summary>
		/// Retrieves a single product variant by its unique ID.
		/// </summary>
		public async Task<ProductVariantResponseDto?> GetByIdAsync(int id)
		{
			const string sql = @"
                SELECT 
                    pv.Id,
                    pv.ProductId,
                    pv.Title,
                    pv.Sku,
                    pv.Price,
                    pv.DiscountPrice,
                    pv.StockQuantity,
                    pv.ImageUrl,
                    pv.Size,
                    pv.Color,
                    pv.CreatedAt,
                    pv.UpdatedAt
                FROM ProductVariants pv
                WHERE pv.Id = @Id;";

			using var connection = await GetOpenConnectionAsync();
			return await connection.QueryFirstOrDefaultAsync<ProductVariantResponseDto>(sql, new { Id = id });
		}

		/// <summary>
		/// Retrieves a single product variant by its unique SKU code.
		/// </summary>
		public async Task<ProductVariantResponseDto?> GetBySkuAsync(string sku)
		{
			const string sql = @"
                SELECT 
                    pv.Id,
                    pv.ProductId,
                    pv.Title,
                    pv.Sku,
                    pv.Price,
                    pv.DiscountPrice,
                    pv.StockQuantity,
                    pv.ImageUrl,
                    pv.Size,
                    pv.Color,
                    pv.CreatedAt,
                    pv.UpdatedAt
                FROM ProductVariants pv
                WHERE LOWER(pv.Sku) = LOWER(@Sku);";

			using var connection = await GetOpenConnectionAsync();
			return await connection.QueryFirstOrDefaultAsync<ProductVariantResponseDto>(sql, new { Sku = sku });
		}

		/// <summary>
		/// Checks if a SKU already exists (useful for validation during create/update commands).
		/// </summary>
		public async Task<bool> SkuExistsAsync(string sku, int? excludeVariantId = null)
		{
			const string sql = @"
                SELECT CASE WHEN EXISTS (
                    SELECT 1 
                    FROM ProductVariants 
                    WHERE LOWER(Sku) = LOWER(@Sku) 
                      AND (@ExcludeVariantId IS NULL OR Id <> @ExcludeVariantId)
                ) THEN 1 ELSE 0 END;";

			using var connection = await GetOpenConnectionAsync();
			return await connection.ExecuteScalarAsync<bool>(sql, new
			{
				Sku = sku,
				ExcludeVariantId = excludeVariantId
			});
		}
	}
}