using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace API_Ecommerce.Queries
{
    public class OrderQueries
    {
        private readonly AppDbContext _context;

        public OrderQueries(AppDbContext context)
        {
            _context = context;
        }

        private async Task<IDbConnection> GetOpenConnectionAsync()
        {
            var connection = _context.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }
            return connection;
        }

        // ==========================================
        // 1. Fetch single order by ID with Addresses
        // ==========================================
        public async Task<OrderDtos.Response?> GetOrderByIdAsync(long id)
        {
            const string sql = @"
                SELECT 
                    o.Id,
                    o.OrderNumber,
                    COALESCE(o.UserId, 0) AS UserId,
                    o.Status,
                    o.Subtotal,
                    o.TaxAmount,
                    o.ShippingAmount,
                    o.DiscountAmount,
                    o.TotalAmount,
                    o.Currency,
                    o.Notes,
                    o.CreatedAt,
                    o.UpdatedAt,
                    -- Shipping Address
                    sa.Id AS Shipping_Id,
                    sa.UserId AS Shipping_UserId,
                    sa.AddressType AS Shipping_AddressType,
                    sa.StreetAddress AS Shipping_StreetAddress,
                    sa.City AS Shipping_City,
                    sa.State AS Shipping_State,
                    sa.PostalCode AS Shipping_PostalCode,
                    sa.Country AS Shipping_Country,
                    sa.IsDefault AS Shipping_IsDefault,
                    sa.CreatedAt AS Shipping_CreatedAt,
                    sa.UpdatedAt AS Shipping_UpdatedAt,
                    -- Billing Address
                    ba.Id AS Billing_Id,
                    ba.UserId AS Billing_UserId,
                    ba.AddressType AS Billing_AddressType,
                    ba.StreetAddress AS Billing_StreetAddress,
                    ba.City AS Billing_City,
                    ba.State AS Billing_State,
                    ba.PostalCode AS Billing_PostalCode,
                    ba.Country AS Billing_Country,
                    ba.IsDefault AS Billing_IsDefault,
                    ba.CreatedAt AS Billing_CreatedAt,
                    ba.UpdatedAt AS Billing_UpdatedAt
                FROM Orders o
                LEFT JOIN Addresses sa ON o.ShippingAddressId = sa.Id
                LEFT JOIN Addresses ba ON o.BillingAddressId = ba.Id
                WHERE o.Id = @Id;";

            var connection = await GetOpenConnectionAsync();
            var result = await connection.QueryAsync<dynamic>(sql, new { Id = id });
            var row = result.FirstOrDefault();

            if (row == null) return null;

            return new OrderDtos.Response
            {
                Id = row.Id,
                OrderNumber = row.OrderNumber,
                UserId = row.UserId,
                Status = (OrderStatus)row.Status,
                Subtotal = row.Subtotal,
                TaxAmount = row.TaxAmount,
                ShippingAmount = row.ShippingAmount,
                DiscountAmount = row.DiscountAmount,
                TotalAmount = row.TotalAmount,
                Currency = row.Currency,
                Notes = row.Notes,
                CreatedAt = row.CreatedAt,
                UpdatedAt = row.UpdatedAt,
                ShippingAddress = row.Shipping_Id != null ? new UserAddressResponseDto
                {
                    Id = row.Shipping_Id,
                    UserId = row.Shipping_UserId,
                    AddressType = row.Shipping_AddressType,
                    StreetAddress = row.Shipping_StreetAddress,
                    City = row.Shipping_City,
                    State = row.Shipping_State,
                    PostalCode = row.Shipping_PostalCode,
                    Country = row.Shipping_Country,
                    IsDefault = row.Shipping_IsDefault,
                    CreatedAt = row.Shipping_CreatedAt,
                    UpdatedAt = row.Shipping_UpdatedAt
                } : null,
                BillingAddress = row.Billing_Id != null ? new UserAddressResponseDto
                {
                    Id = row.Billing_Id,
                    UserId = row.Billing_UserId,
                    AddressType = row.Billing_AddressType,
                    StreetAddress = row.Billing_StreetAddress,
                    City = row.Billing_City,
                    State = row.Billing_State,
                    PostalCode = row.Billing_PostalCode,
                    Country = row.Billing_Country,
                    IsDefault = row.Billing_IsDefault,
                    CreatedAt = row.Billing_CreatedAt,
                    UpdatedAt = row.Billing_UpdatedAt
                } : null
            };
        }

        // ==========================================
        // 2. Fetch all orders with Raw SQL, Filters & Pagination
        // ==========================================
        public async Task<object> GetAllOrdersAsync(long? userId, string? userRole, PaginationParamsDtos paginationParams)
        {
            var whereClauses = new List<string>();
            var parameters = new DynamicParameters();

            if (userId.HasValue && userRole == "Customer")
            {
                whereClauses.Add("o.UserId = @UserId");
                parameters.Add("UserId", userId.Value);
            }
            else
            {
                whereClauses.Add("o.Status <> @PendingStatus");
                parameters.Add("PendingStatus", (int)OrderStatus.Pending);
            }

            var whereSql = whereClauses.Count > 0 ? " WHERE " + string.Join(" AND ", whereClauses) : "";

            var countSql = $"SELECT COUNT(1) FROM Orders o{whereSql};";

            var dataSql = $@"
                SELECT 
                    o.Id,
                    o.UserId,
                    COALESCE(u.FullName, 'Unknown') AS CustomerName,
                    COALESCE(u.Email, 'Unknown') AS CustomerEmail,
                    o.TotalAmount,
                    o.Status,
                    CAST(o.Status AS NVARCHAR(50)) AS StatusString,
                    o.CreatedAt,
                    o.OrderNumber,
                    o.Currency,
                    o.Notes,
                    sa.Id AS Shipping_Id,
                    sa.StreetAddress AS Shipping_StreetAddress,
                    sa.City AS Shipping_City,
                    sa.State AS Shipping_State,
                    sa.PostalCode AS Shipping_PostalCode,
                    sa.Country AS Shipping_Country,
                    sa.AddressType AS Shipping_AddressType,
                    sa.IsDefault AS Shipping_IsDefault
                FROM Orders o
                LEFT JOIN Auths u ON o.UserId = u.Id
                LEFT JOIN Addresses sa ON o.ShippingAddressId = sa.Id
                {whereSql}
                ORDER BY o.CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            parameters.Add("Offset", (paginationParams.PageNumber - 1) * paginationParams.PageSize);
            parameters.Add("PageSize", paginationParams.PageSize);

            var connection = await GetOpenConnectionAsync();

            var totalItems = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var rawOrders = await connection.QueryAsync<dynamic>(dataSql, parameters);

            var orders = rawOrders.Select(o => new
            {
                o.Id,
                o.UserId,
                o.CustomerName,
                o.CustomerEmail,
                o.TotalAmount,
                Status = (OrderStatus)o.Status,
                o.StatusString,
                o.CreatedAt,
                o.OrderNumber,
                o.Currency,
                o.Notes,
                ShippingAddress = o.Shipping_Id != null ? new
                {
                    Id = (long)o.Shipping_Id,
                    StreetAddress = (string)o.Shipping_StreetAddress,
                    City = (string)o.Shipping_City,
                    State = (string)o.Shipping_State,
                    PostalCode = (string)o.Shipping_PostalCode,
                    Country = (string)o.Shipping_Country,
                    AddressType = (string)o.Shipping_AddressType,
                    IsDefault = (bool)o.Shipping_IsDefault
                } : null
            }).ToList();

            return new
            {
                TotalItems = totalItems,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)paginationParams.PageSize),
                Data = orders
            };
        }

        // ==========================================
        // 3. Fetch orders for a specific customer ID
        // ==========================================
        public async Task<object> GetOrdersByUserIdAsync(long userId)
        {
            const string sql = @"
                SELECT 
                    o.Id,
                    o.UserId,
                    COALESCE(u.FullName, 'Unknown') AS CustomerName,
                    COALESCE(u.Email, 'Unknown') AS CustomerEmail,
                    o.TotalAmount,
                    o.Status,
                    CAST(o.Status AS NVARCHAR(50)) AS StatusString,
                    o.CreatedAt,
                    o.OrderNumber,
                    o.Currency,
                    o.Notes,
                    sa.Id AS Shipping_Id,
                    sa.StreetAddress AS Shipping_StreetAddress,
                    sa.City AS Shipping_City,
                    sa.State AS Shipping_State,
                    sa.PostalCode AS Shipping_PostalCode,
                    sa.Country AS Shipping_Country,
                    sa.AddressType AS Shipping_AddressType,
                    sa.IsDefault AS Shipping_IsDefault
                FROM Orders o
                LEFT JOIN Auths u ON o.UserId = u.Id
                LEFT JOIN Addresses sa ON o.ShippingAddressId = sa.Id
                WHERE o.UserId = @UserId
                ORDER BY o.CreatedAt DESC;";

            var connection = await GetOpenConnectionAsync();
            var rawOrders = await connection.QueryAsync<dynamic>(sql, new { UserId = userId });

            return rawOrders.Select(o => new
            {
                o.Id,
                o.UserId,
                o.CustomerName,
                o.CustomerEmail,
                o.TotalAmount,
                Status = (OrderStatus)o.Status,
                o.StatusString,
                o.CreatedAt,
                o.OrderNumber,
                o.Currency,
                o.Notes,
                ShippingAddress = o.Shipping_Id != null ? new
                {
                    Id = (long)o.Shipping_Id,
                    StreetAddress = (string)o.Shipping_StreetAddress,
                    City = (string)o.Shipping_City,
                    State = (string)o.Shipping_State,
                    PostalCode = (string)o.Shipping_PostalCode,
                    Country = (string)o.Shipping_Country,
                    AddressType = (string)o.Shipping_AddressType,
                    IsDefault = (bool)o.Shipping_IsDefault
                } : null
            }).ToList();
        }

        // ==========================================
        // 4. Fetch order statistics & chart data (Raw SQL)
        // ==========================================
        public async Task<object> GetOrderStatisticsAsync(long? sellerId = null)
        {
            string statsSql;
            string monthlySql;
            var parameters = new DynamicParameters();

            if (sellerId.HasValue)
            {
                parameters.Add("SellerId", sellerId.Value);

                var hasBakongConfig = await _context.SellerBakongConfigs
                    .AnyAsync(sbc => sbc.SellerId == (int)sellerId.Value && sbc.BakongId != null && sbc.BakongId != "");

                statsSql = $@"
                    SELECT 
                        COUNT(DISTINCT CASE WHEN o.Status <> 0 THEN o.Id END) AS totalOrders,
                        SUM(CASE WHEN o.Status <> 0 THEN oi.TotalPrice ELSE 0 END) AS totalRevenue,
                        COUNT(DISTINCT CASE WHEN o.Status = 0 THEN o.Id END) AS pendingCount,
                        COUNT(DISTINCT CASE WHEN o.Status = 1 THEN o.Id END) AS processingCount,
                        COUNT(DISTINCT CASE WHEN o.Status = 2 THEN o.Id END) AS shippedCount,
                        COUNT(DISTINCT CASE WHEN o.Status = 3 OR o.Status = 5 THEN o.Id END) AS deliveredCount,
                        COUNT(DISTINCT CASE WHEN o.Status = 3 THEN o.Id END) AS completedCount,
                        COUNT(DISTINCT CASE WHEN o.Status = 4 THEN o.Id END) AS cancelledCount,
                        SUM(CASE WHEN o.Status <> 0 AND {(hasBakongConfig ? "osc.SellerCount > 1" : "1 = 1")} THEN oi.TotalPrice ELSE 0 END) AS availableBalance
                    FROM Orders o
                    INNER JOIN order_items oi ON o.Id = oi.OrderId
                    INNER JOIN products p ON oi.ProductId = p.Id
                    LEFT JOIN (
                        SELECT oi2.OrderId, COUNT(DISTINCT p2.SellerId) AS SellerCount
                        FROM order_items oi2
                        INNER JOIN products p2 ON oi2.ProductId = p2.Id
                        GROUP BY oi2.OrderId
                    ) osc ON o.Id = osc.OrderId
                    WHERE p.SellerId = @SellerId;";

                monthlySql = @"
                    SELECT 
                        DATEADD(MONTH, DATEDIFF(MONTH, 0, o.CreatedAt), 0) AS MonthStart,
                        SUM(oi.TotalPrice) AS Revenue
                    FROM Orders o
                    INNER JOIN order_items oi ON o.Id = oi.OrderId
                    INNER JOIN products p ON oi.ProductId = p.Id
                    WHERE p.SellerId = @SellerId AND o.Status <> 0 AND o.CreatedAt >= DATEADD(MONTH, -5, DATEADD(MONTH, DATEDIFF(MONTH, 0, GETUTCDATE()), 0))
                    GROUP BY DATEADD(MONTH, DATEDIFF(MONTH, 0, o.CreatedAt), 0);";
            }
            else
            {
                statsSql = @"
                    SELECT 
                        SUM(CASE WHEN Status <> 0 THEN 1 ELSE 0 END) AS totalOrders,
                        SUM(CASE WHEN Status <> 0 THEN TotalAmount ELSE 0 END) AS totalRevenue,
                        SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) AS pendingCount,
                        SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS processingCount,
                        SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) AS shippedCount,
                        SUM(CASE WHEN Status = 3 OR Status = 5 THEN 1 ELSE 0 END) AS deliveredCount,
                        SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END) AS completedCount,
                        SUM(CASE WHEN Status = 4 THEN 1 ELSE 0 END) AS cancelledCount,
                        SUM(CASE WHEN Status <> 0 THEN TotalAmount ELSE 0 END) AS availableBalance
                    FROM Orders;";

                monthlySql = @"
                    SELECT 
                        DATEADD(MONTH, DATEDIFF(MONTH, 0, CreatedAt), 0) AS MonthStart,
                        SUM(TotalAmount) AS Revenue
                    FROM Orders
                    WHERE Status <> 0 AND CreatedAt >= DATEADD(MONTH, -5, DATEADD(MONTH, DATEDIFF(MONTH, 0, GETUTCDATE()), 0))
                    GROUP BY DATEADD(MONTH, DATEDIFF(MONTH, 0, CreatedAt), 0);";
            }

            var connection = await GetOpenConnectionAsync();

            var stats = await connection.QueryFirstOrDefaultAsync<dynamic>(statsSql, parameters);
            var monthlyData = (await connection.QueryAsync<dynamic>(monthlySql, parameters)).ToDictionary(
                x => ((DateTime)x.MonthStart).ToString("MMM"),
                x => (decimal)x.Revenue
            );

            var now = DateTime.UtcNow;
            var monthlyRevenue = new List<decimal>();
            var monthlyLabels = new List<string>();

            for (int i = 5; i >= 0; i--)
            {
                var targetDate = now.AddMonths(-i);
                var label = targetDate.ToString("MMM");
                monthlyLabels.Add(label);
                monthlyRevenue.Add(monthlyData.ContainsKey(label) ? monthlyData[label] : 0m);
            }

            return new
            {
                totalOrders = (stats?.totalOrders != null) ? (int)stats.totalOrders : 0,
                totalRevenue = (stats?.totalRevenue != null) ? (decimal)stats.totalRevenue : 0m,
                pendingCount = (stats?.pendingCount != null) ? (int)stats.pendingCount : 0,
                processingCount = (stats?.processingCount != null) ? (int)stats.processingCount : 0,
                shippedCount = (stats?.shippedCount != null) ? (int)stats.shippedCount : 0,
                deliveredCount = (stats?.deliveredCount != null) ? (int)stats.deliveredCount : 0,
                completedCount = (stats?.completedCount != null) ? (int)stats.completedCount : 0,
                cancelledCount = (stats?.cancelledCount != null) ? (int)stats.cancelledCount : 0,
                availableBalance = (stats?.availableBalance != null) ? (decimal)stats.availableBalance : 0m,
                analytics = new
                {
                    labels = monthlyLabels,
                    data = monthlyRevenue
                }
            };
        }

        /// <summary>
        /// Retrieves paginated orders containing products belonging to a specific seller ID.
        /// </summary>
        public async Task<object> GetOrdersBySellerIdAsync(long sellerId, PaginationParamsDtos paginationParams)
        {
            var whereClauses = new List<string> { "p.SellerId = @SellerId" };
            var parameters = new DynamicParameters();
            parameters.Add("SellerId", sellerId);

            var whereSql = " WHERE " + string.Join(" AND ", whereClauses);

            // Use DISTINCT on o.Id to avoid duplicate orders if an order contains multiple products from the same seller
            var countSql = $@"
                SELECT COUNT(DISTINCT o.Id) 
                FROM Orders o
                INNER JOIN order_items oi ON o.Id = oi.OrderId
                INNER JOIN products p ON oi.ProductId = p.Id
                {whereSql};";

            var dataSql = $@"
                SELECT DISTINCT
                    o.Id,
                    o.UserId,
                    COALESCE(u.FullName, 'Unknown') AS CustomerName,
                    COALESCE(u.Email, 'Unknown') AS CustomerEmail,
                    o.TotalAmount,
                    o.Status,
                    CAST(o.Status AS NVARCHAR(50)) AS StatusString,
                    o.CreatedAt,
                    o.OrderNumber,
                    o.Currency,
                    o.Notes,
                    sa.Id AS Shipping_Id,
                    sa.StreetAddress AS Shipping_StreetAddress,
                    sa.City AS Shipping_City,
                    sa.State AS Shipping_State,
                    sa.PostalCode AS Shipping_PostalCode,
                    sa.Country AS Shipping_Country,
                    sa.AddressType AS Shipping_AddressType,
                    sa.IsDefault AS Shipping_IsDefault
                FROM Orders o
                INNER JOIN order_items oi ON o.Id = oi.OrderId
                INNER JOIN products p ON oi.ProductId = p.Id
                LEFT JOIN Auths u ON o.UserId = u.Id
                LEFT JOIN Addresses sa ON o.ShippingAddressId = sa.Id
                {whereSql}
                ORDER BY o.CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            parameters.Add("Offset", (paginationParams.PageNumber - 1) * paginationParams.PageSize);
            parameters.Add("PageSize", paginationParams.PageSize);

            var connection = await GetOpenConnectionAsync();

            var totalItems = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var rawOrders = await connection.QueryAsync<dynamic>(dataSql, parameters);

            var orders = rawOrders.Select(o => new
            {
                o.Id,
                o.UserId,
                o.CustomerName,
                o.CustomerEmail,
                o.TotalAmount,
                Status = (OrderStatus)o.Status,
                o.StatusString,
                o.CreatedAt,
                o.OrderNumber,
                o.Currency,
                o.Notes,
                ShippingAddress = o.Shipping_Id != null ? new
                {
                    Id = (long)o.Shipping_Id,
                    StreetAddress = (string)o.Shipping_StreetAddress,
                    City = (string)o.Shipping_City,
                    State = (string)o.Shipping_State,
                    PostalCode = (string)o.Shipping_PostalCode,
                    Country = (string)o.Shipping_Country,
                    AddressType = (string)o.Shipping_AddressType,
                    IsDefault = (bool)o.Shipping_IsDefault
                } : null
            }).ToList();

            return new
            {
                TotalItems = totalItems,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)paginationParams.PageSize),
                Data = orders
            };
        }
    }
}