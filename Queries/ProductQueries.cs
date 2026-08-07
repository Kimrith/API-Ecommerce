using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace API_Ecommerce.Queries
{
    public class ProductQueries
    {
        private readonly AppDbContext _context;

        public ProductQueries(AppDbContext context)
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
        /// Retrieves paginated products with optional filtering by status, category, seller, search term, and dynamic sorting.
        /// </summary>
        public async Task<PagedResultDto<ProductResponseDto>> GetAllProductsAsync(
            int pageNumber = 1,
            int pageSize = 10,
            string? searchTerm = null,
            int? categoryId = null,
            int? sellerId = null,
            ProductStatus? status = null,
            string? sortBy = null)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);
            int offset = (pageNumber - 1) * pageSize;

            var whereClauses = new List<string>();
            var dynamicParameters = new DynamicParameters();

            // 1. Search Filter (Name or Description)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                whereClauses.Add("(LOWER(p.Name) LIKE @SearchTerm OR LOWER(p.Description) LIKE @SearchTerm)");
                dynamicParameters.Add("SearchTerm", $"%{searchTerm.Trim().ToLower()}%");
            }

            // 2. Category Filter
            if (categoryId.HasValue)
            {
                whereClauses.Add("p.CategoryId = @CategoryId");
                dynamicParameters.Add("CategoryId", categoryId.Value);
            }

            // 3. Seller Filter
            if (sellerId.HasValue)
            {
                whereClauses.Add("p.SellerId = @SellerId");
                dynamicParameters.Add("SellerId", sellerId.Value);
            }

            // 4. Status Filter (Supports integer or string enum storage)
            if (status.HasValue)
            {
                whereClauses.Add("(p.Status = CAST(@StatusValue AS NVARCHAR(50)) OR LOWER(p.Status) = LOWER(@StatusName))");
                dynamicParameters.Add("StatusValue", (int)status.Value);
                dynamicParameters.Add("StatusName", status.Value.ToString());
            }

            string whereSql = whereClauses.Count > 0
                ? " WHERE " + string.Join(" AND ", whereClauses)
                : string.Empty;

            // 5. Dynamic Sorting
            string orderBySql = sortBy?.ToLower() switch
            {
                "price_asc" => "ORDER BY p.Price ASC",
                "price_desc" => "ORDER BY p.Price DESC",
                "name" => "ORDER BY p.Name ASC",
                "oldest" => "ORDER BY p.CreatedAt ASC",
                _ => "ORDER BY p.CreatedAt DESC"
            };

            dynamicParameters.Add("Offset", offset);
            dynamicParameters.Add("PageSize", pageSize);

            // Combined SQL Query for Page Items and Total Count (Joined with inventory table)
            var sql = $@"
                SELECT 
                    p.Id,
                    p.Name,
                    p.Slug,
                    p.Description,
                    p.Price,
                    p.DiscountPrice,
                    p.DiscountStartDate,
                    p.DiscountEndDate,
                    COALESCE(i.Quantity, 0) AS StockQuantity,
                    COALESCE(i.Quantity - i.ReservedQuantity, 0) AS AvailableQuantity,
                    p.ImageUrl,
                    p.Status,
                    p.PublishAt,
                    p.CategoryId,
                    COALESCE(c.Name, '') AS CategoryName,
                    p.SellerId,
                    COALESCE(u.FullName, 'Unknown') AS SellerName,
                    p.CreatedAt,
                    p.UpdatedAt
                FROM products p
                LEFT JOIN categories c ON p.CategoryId = c.Id
                LEFT JOIN Auths u ON p.SellerId = u.Id
                LEFT JOIN inventory i ON p.Id = i.ProductId AND i.VariantId IS NULL
                {whereSql}
                {orderBySql}
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY;

                SELECT COUNT(1)
                FROM products p
                {whereSql};";

            var connection = await GetOpenConnectionAsync();
            using var multi = await connection.QueryMultipleAsync(sql, dynamicParameters);

            var items = (await multi.ReadAsync<ProductResponseDto>()).ToList();
            int totalItems = await multi.ReadFirstAsync<int>();

            return new PagedResultDto<ProductResponseDto>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };
        }

        /// <summary>
        /// Retrieves a single product by ID with Category, Seller, and Inventory details.
        /// </summary>
        public async Task<ProductResponseDto?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT 
                    p.Id,
                    p.Name,
                    p.Slug,
                    p.Description,
                    p.Price,
                    p.DiscountPrice,
                    p.DiscountStartDate,
                    p.DiscountEndDate,
                    COALESCE(i.Quantity, 0) AS StockQuantity,
                    COALESCE(i.Quantity - i.ReservedQuantity, 0) AS AvailableQuantity,
                    p.ImageUrl,
                    p.Status,
                    p.PublishAt,
                    p.CategoryId,
                    COALESCE(c.Name, '') AS CategoryName,
                    p.SellerId,
                    COALESCE(u.FullName, 'Unknown') AS SellerName,
                    p.CreatedAt,
                    p.UpdatedAt
                FROM products p
                LEFT JOIN categories c ON p.CategoryId = c.Id
                LEFT JOIN Auths u ON p.SellerId = u.Id
                LEFT JOIN inventory i ON p.Id = i.ProductId AND i.VariantId IS NULL
                WHERE p.Id = @Id;";

            var connection = await GetOpenConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<ProductResponseDto>(sql, new { Id = id });
        }

        /// <summary>
        /// Retrieves a single product by Slug.
        /// </summary>
        public async Task<ProductResponseDto?> GetBySlugAsync(string slug)
        {
            const string sql = @"
                SELECT 
                    p.Id,
                    p.Name,
                    p.Slug,
                    p.Description,
                    p.Price,
                    p.DiscountPrice,
                    p.DiscountStartDate,
                    p.DiscountEndDate,
                    COALESCE(i.Quantity, 0) AS StockQuantity,
                    COALESCE(i.Quantity - i.ReservedQuantity, 0) AS AvailableQuantity,
                    p.ImageUrl,
                    p.Status,
                    p.PublishAt,
                    p.CategoryId,
                    COALESCE(c.Name, '') AS CategoryName,
                    p.SellerId,
                    COALESCE(u.FullName, 'Unknown') AS SellerName,
                    p.CreatedAt,
                    p.UpdatedAt
                FROM products p
                LEFT JOIN categories c ON p.CategoryId = c.Id
                LEFT JOIN Auths u ON p.SellerId = u.Id
                LEFT JOIN inventory i ON p.Id = i.ProductId AND i.VariantId IS NULL
                WHERE LOWER(p.Slug) = LOWER(@Slug);";

            var connection = await GetOpenConnectionAsync();
            return await connection.QueryFirstOrDefaultAsync<ProductResponseDto>(sql, new { Slug = slug });
        }

        /// <summary>
        /// Retrieves products uploaded by a specific seller.
        /// </summary>
        public async Task<IEnumerable<ProductResponseDto>> GetBySellerIdAsync(int sellerId)
        {
            const string sql = @"
                SELECT 
                    p.Id,
                    p.Name,
                    p.Slug,
                    p.Description,
                    p.Price,
                    p.DiscountPrice,
                    p.DiscountStartDate,
                    p.DiscountEndDate,
                    COALESCE(i.Quantity, 0) AS StockQuantity,
                    COALESCE(i.Quantity - i.ReservedQuantity, 0) AS AvailableQuantity,
                    p.ImageUrl,
                    p.Status,
                    p.PublishAt,
                    p.CategoryId,
                    COALESCE(c.Name, '') AS CategoryName,
                    p.SellerId,
                    COALESCE(u.FullName, 'Unknown') AS SellerName,
                    p.CreatedAt,
                    p.UpdatedAt
                FROM products p
                LEFT JOIN categories c ON p.CategoryId = c.Id
                LEFT JOIN Auths u ON p.SellerId = u.Id
                LEFT JOIN inventory i ON p.Id = i.ProductId AND i.VariantId IS NULL
                WHERE p.SellerId = @SellerId
                ORDER BY p.CreatedAt DESC;";

            var connection = await GetOpenConnectionAsync();
            return await connection.QueryAsync<ProductResponseDto>(sql, new { SellerId = sellerId });
        }

        /// <summary>
        /// Retrieves aggregate product statistics broken down by status.
        /// Optional sellerId allows filtering statistics for a specific seller.
        /// </summary>
        public async Task<ProductStatisticsDto> GetProductStatisticsAsync(int? sellerId = null)
        {
            var whereClause = sellerId.HasValue ? "WHERE p.SellerId = @SellerId" : string.Empty;

            var sql = $@"
                SELECT 
                    COUNT(1) AS TotalProducts,
                    SUM(CASE WHEN p.Status = 'Draft' THEN 1 ELSE 0 END) AS Draft,
                    SUM(CASE WHEN p.Status = 'Pending' THEN 1 ELSE 0 END) AS Pending,
                    SUM(CASE WHEN p.Status = 'Approved' THEN 1 ELSE 0 END) AS Approved,
                    SUM(CASE WHEN p.Status = 'Rejected' THEN 1 ELSE 0 END) AS Rejected,
                    SUM(CASE WHEN p.Status = 'Archived' THEN 1 ELSE 0 END) AS Archived,
                    SUM(CASE WHEN p.Status = 'Suspended' THEN 1 ELSE 0 END) AS Suspended
                FROM products p
                {whereClause};";

            var connection = await GetOpenConnectionAsync();
            var result = await connection.QueryFirstOrDefaultAsync<ProductStatisticsDto>(sql, new { SellerId = sellerId });

            return result ?? new ProductStatisticsDto();
        }
    }
}