using API_Ecommerce.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace API_Ecommerce.Queries
{
    public class OrderItemQueries
    {
        private readonly string _connectionString;

        public OrderItemQueries(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        public async Task<List<OrderItemDtos.Response>> GetItemsByOrderIdAsync(long orderId)
        {
            var items = new List<OrderItemDtos.Response>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT 
                    oi.Id, 
                    oi.ProductId, 
                    ISNULL(p.Name, 'Unknown Product') AS ProductName,
                    oi.Quantity, 
                    oi.UnitPrice, 
                    oi.TotalPrice
                FROM order_items oi
                LEFT JOIN products p ON oi.ProductId = p.Id
                WHERE oi.OrderId = @OrderId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@OrderId", orderId);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new OrderItemDtos.Response
                {
                    Id = reader.GetInt64(reader.GetOrdinal("Id")),
                    ProductId = reader.GetInt64(reader.GetOrdinal("ProductId")),
                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                    Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                    UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                    TotalPrice = reader.GetDecimal(reader.GetOrdinal("TotalPrice"))
                });
            }

            return items;
        }

        public async Task<List<OrderItemDtos.PurchasedProductResponse>> GetPurchasedItemsByUserIdAsync(long userId)
        {
            var items = new List<OrderItemDtos.PurchasedProductResponse>();

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                        SELECT 
                            oi.Id, 
                            oi.ProductId, 
                            ISNULL(p.Name, 'Unknown Product') AS ProductName,
                            oi.Quantity, 
                            oi.UnitPrice, 
                            oi.TotalPrice,
                            o.OrderNumber,
                            o.CreatedAt AS PurchasedAt
                        FROM order_items oi
                        INNER JOIN orders o ON oi.OrderId = o.Id
                        LEFT JOIN products p ON oi.ProductId = p.Id
                        WHERE o.UserId = @UserId 
                          AND o.Status IN (@Status1, @Status2, @Status3)";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@Status1", (int)Enums.OrderStatus.Processing);
            command.Parameters.AddWithValue("@Status2", (int)Enums.OrderStatus.Shipped);
            command.Parameters.AddWithValue("@Status3", (int)Enums.OrderStatus.Delivered);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new OrderItemDtos.PurchasedProductResponse
                {
                    Id = reader.GetInt64(reader.GetOrdinal("Id")),
                    ProductId = reader.GetInt64(reader.GetOrdinal("ProductId")),
                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                    Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                    UnitPrice = reader.GetDecimal(reader.GetOrdinal("UnitPrice")),
                    TotalPrice = reader.GetDecimal(reader.GetOrdinal("TotalPrice")),
                    OrderNumber = reader.GetString(reader.GetOrdinal("OrderNumber")),
                    PurchasedAt = reader.GetDateTime(reader.GetOrdinal("PurchasedAt"))
                });
            }

            return items;
        }
    }
}