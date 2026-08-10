using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using Dapper;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace API_Ecommerce.Queries
{
    public class PaymentQueries
    {
        private readonly AppDbContext _context;

        public PaymentQueries(AppDbContext context)
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

        public async Task<object> GetAllPaymentsAsync(PaginationParamsDtos paginationParams)
        {
            const string countSql = "SELECT COUNT(1) FROM Payments;";

            const string dataSql = @"
                SELECT 
                    p.Id,
                    p.OrderId,
                    COALESCE(o.OrderNumber, 'Unknown') AS OrderNumber,
                    COALESCE(u.FullName, 'Unknown') AS CustomerName,
                    COALESCE(u.Email, 'Unknown') AS CustomerEmail,
                    p.PaymentMethod,
                    p.Amount,
                    p.Currency,
                    p.Status,
                    CAST(p.Status AS NVARCHAR(50)) AS StatusString,
                    p.CreatedAt
                FROM Payments p
                LEFT JOIN Orders o ON p.OrderId = o.Id
                LEFT JOIN Auths u ON o.UserId = u.Id
                ORDER BY p.CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            var parameters = new DynamicParameters();
            parameters.Add("Offset", (paginationParams.PageNumber - 1) * paginationParams.PageSize);
            parameters.Add("PageSize", paginationParams.PageSize);

            var connection = await GetOpenConnectionAsync();

            var totalItems = await connection.ExecuteScalarAsync<int>(countSql);
            var items = await connection.QueryAsync(dataSql, parameters);

            return new
            {
                TotalItems = totalItems,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)paginationParams.PageSize),
                Data = items
            };
        }

        public async Task<object> GetPaymentStatisticsAsync()
        {
            const string sql = @"
                SELECT 
                    SUM(CASE WHEN Status = 1 THEN Amount ELSE 0 END) AS totalRevenue,
                    SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS completedCount,
                    SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) AS pendingCount,
                    SUM(CASE WHEN Status = 2 THEN Amount ELSE 0 END) AS refundAmount
                FROM Payments;";

            var connection = await GetOpenConnectionAsync();
            var stats = await connection.QueryFirstOrDefaultAsync(sql);

            return stats ?? new
            {
                totalRevenue = 0m,
                completedCount = 0,
                pendingCount = 0,
                refundAmount = 0m
            };
        }

        public async Task<object> GetPaymentsBySellerIdAsync(int sellerId, PaginationParamsDtos paginationParams)
        {
            const string countSql = @"
                SELECT COUNT(DISTINCT p.Id) 
                FROM Payments p
                INNER JOIN Orders o ON p.OrderId = o.Id
                INNER JOIN order_items oi ON o.Id = oi.OrderId
                INNER JOIN Products pr ON oi.ProductId = pr.Id
                WHERE pr.SellerId = @SellerId;";

            const string dataSql = @"
                SELECT DISTINCT
                    p.Id,
                    p.OrderId,
                    COALESCE(o.OrderNumber, 'Unknown') AS OrderNumber,
                    COALESCE(u.FullName, 'Unknown') AS CustomerName,
                    COALESCE(u.Email, 'Unknown') AS CustomerEmail,
                    p.PaymentMethod,
                    p.Amount,
                    p.Currency,
                    p.Status,
                    CAST(p.Status AS NVARCHAR(50)) AS StatusString,
                    p.CreatedAt
                FROM Payments p
                INNER JOIN Orders o ON p.OrderId = o.Id
                INNER JOIN order_items oi ON o.Id = oi.OrderId
                INNER JOIN Products pr ON oi.ProductId = pr.Id
                LEFT JOIN Auths u ON o.UserId = u.Id
                WHERE pr.SellerId = @SellerId
                ORDER BY p.CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            var parameters = new DynamicParameters();
            parameters.Add("SellerId", sellerId);
            parameters.Add("Offset", (paginationParams.PageNumber - 1) * paginationParams.PageSize);
            parameters.Add("PageSize", paginationParams.PageSize);

            var connection = await GetOpenConnectionAsync();

            var totalItems = await connection.ExecuteScalarAsync<int>(countSql, parameters);
            var items = await connection.QueryAsync(dataSql, parameters);

            return new
            {
                TotalItems = totalItems,
                PageNumber = paginationParams.PageNumber,
                PageSize = paginationParams.PageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)paginationParams.PageSize),
                Data = items
            };
        }
    }
}