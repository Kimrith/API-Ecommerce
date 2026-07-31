using System.Data;
using API_Ecommerce.DTOs;
using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;

namespace API_Ecommerce.Queries
{
    public class OrderItemQueries
    {
        // =========================================================
        // 1. QUERY: GET ALL ITEMS FOR A SPECIFIC ORDER (RAW SQL)
        // =========================================================
        public record GetOrderItemsByOrderIdQuery(
            long OrderId,
            long? UserId = null
        ) : IRequest<List<OrderItemDtos.Response>>;

        public class GetOrderItemsByOrderIdQueryHandler
            : IRequestHandler<GetOrderItemsByOrderIdQuery, List<OrderItemDtos.Response>>
        {
            private readonly string _connectionString;

            public GetOrderItemsByOrderIdQueryHandler(IConfiguration configuration)
            {
                _connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            public async Task<List<OrderItemDtos.Response>> Handle(
                GetOrderItemsByOrderIdQuery request,
                CancellationToken cancellationToken)
            {
                var sql = @"
                    SELECT 
                        oi.Id,
                        oi.OrderId,
                        oi.ProductId,
                        oi.ProductName,
                        oi.VariantId,
                        oi.VariantName,
                        oi.Sku,
                        oi.Quantity,
                        oi.UnitPrice,
                        oi.TotalPrice,
                        oi.CreatedAt,
                        o.UserId,
                        u.Email AS UserEmail
                    FROM order_items oi
                    INNER JOIN orders o ON oi.OrderId = o.Id
                    LEFT JOIN Auths u ON o.UserId = u.Id
                    WHERE oi.OrderId = @OrderId
                      AND (@UserId IS NULL OR o.UserId = @UserId)
                    ORDER BY oi.Id ASC";

                using IDbConnection db = new SqlConnection(_connectionString);

                var items = await db.QueryAsync<OrderItemDtos.Response>(
                    new CommandDefinition(sql, new { request.OrderId, request.UserId }, cancellationToken: cancellationToken)
                );

                return items.ToList();
            }
        }

        // =========================================================
        // 2. QUERY: GET SINGLE ORDER ITEM BY ID (RAW SQL)
        // =========================================================
        public record GetOrderItemByIdQuery(
            long Id,
            long? UserId = null
        ) : IRequest<OrderItemDtos.Response?>;

        public class GetOrderItemByIdQueryHandler
            : IRequestHandler<GetOrderItemByIdQuery, OrderItemDtos.Response?>
        {
            private readonly string _connectionString;

            public GetOrderItemByIdQueryHandler(IConfiguration configuration)
            {
                _connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            public async Task<OrderItemDtos.Response?> Handle(
                GetOrderItemByIdQuery request,
                CancellationToken cancellationToken)
            {
                var sql = @"
                    SELECT 
                        oi.Id,
                        oi.OrderId,
                        oi.ProductId,
                        oi.ProductName,
                        oi.VariantId,
                        oi.VariantName,
                        oi.Sku,
                        oi.Quantity,
                        oi.UnitPrice,
                        oi.TotalPrice,
                        oi.CreatedAt,
                        o.UserId,
                        u.Email AS UserEmail
                    FROM order_items oi
                    INNER JOIN orders o ON oi.OrderId = o.Id
                    LEFT JOIN Auths u ON o.UserId = u.Id
                    WHERE oi.Id = @Id
                      AND (@UserId IS NULL OR o.UserId = @UserId)";

                using IDbConnection db = new SqlConnection(_connectionString);

                return await db.QueryFirstOrDefaultAsync<OrderItemDtos.Response>(
                    new CommandDefinition(sql, new { request.Id, request.UserId }, cancellationToken: cancellationToken)
                );
            }
        }

        // =========================================================
        // 3. QUERY: GET ALL ORDER ITEMS (RAW SQL)
        // =========================================================
        public record GetAllOrderItemsQuery(
            long? UserId = null
        ) : IRequest<List<OrderItemDtos.Response>>;

        public class GetAllOrderItemsQueryHandler
            : IRequestHandler<GetAllOrderItemsQuery, List<OrderItemDtos.Response>>
        {
            private readonly string _connectionString;

            public GetAllOrderItemsQueryHandler(IConfiguration configuration)
            {
                _connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            public async Task<List<OrderItemDtos.Response>> Handle(
                GetAllOrderItemsQuery request,
                CancellationToken cancellationToken)
            {
                var sql = @"
                    SELECT 
                        oi.Id,
                        oi.OrderId,
                        oi.ProductId,
                        oi.ProductName,
                        oi.VariantId,
                        oi.VariantName,
                        oi.Sku,
                        oi.Quantity,
                        oi.UnitPrice,
                        oi.TotalPrice,
                        oi.CreatedAt,
                        o.UserId,
                        u.Email AS UserEmail
                    FROM order_items oi
                    INNER JOIN orders o ON oi.OrderId = o.Id
                    LEFT JOIN Auths u ON o.UserId = u.Id
                    WHERE (@UserId IS NULL OR o.UserId = @UserId)
                    ORDER BY oi.Id DESC";

                using IDbConnection db = new SqlConnection(_connectionString);

                var items = await db.QueryAsync<OrderItemDtos.Response>(
                    new CommandDefinition(sql, new { request.UserId }, cancellationToken: cancellationToken)
                );

                return items.ToList();
            }
        }
    }
}