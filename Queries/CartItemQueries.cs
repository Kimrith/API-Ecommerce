using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace API_Ecommerce.Queries
{
    // 1. QUERY DEFINITION
    public record GetCartQuery(
        long? UserId,
        string? SessionId
    ) : IRequest<CartDtos.Response?>;

    // 2. QUERY HANDLER USING RAW SQL (SQL SERVER)
    public class GetCartQueryHandler : IRequestHandler<GetCartQuery, CartDtos.Response?>
    {
        private readonly AppDbContext _context;

        public GetCartQueryHandler(AppDbContext context)
        {
            _context = context;
        }

        public async Task<CartDtos.Response?> Handle(GetCartQuery request, CancellationToken cancellationToken)
        {
            if (!request.UserId.HasValue && string.IsNullOrWhiteSpace(request.SessionId))
            {
                return new CartDtos.Response();
            }

            var connection = _context.Database.GetDbConnection();

            const string sql = @"
                DECLARE @TargetCartId BIGINT;

                -- Prioritize UserId, fallback to SessionId
                SELECT TOP 1 @TargetCartId = Id 
                FROM carts 
                WHERE (@UserId IS NOT NULL AND UserId = @UserId)
                   OR (@UserId IS NULL AND SessionId = @SessionId);

                IF @TargetCartId IS NOT NULL
                BEGIN
                    SELECT 
                        -- Cart Fields (First Entity)
                        c.Id AS Id,
                        c.UserId AS UserId,
                        c.SessionId AS SessionId,
                        c.CreatedAt AS CreatedAt,
                        c.UpdatedAt AS UpdatedAt,
                        c.ExpiresAt AS ExpiresAt,
                        c.AppliedCouponCode AS AppliedCouponCode,
                        c.DiscountAmount AS DiscountAmount,
                        c.TotalAmount AS TotalAmount,
                        
                        -- Cart Item Fields (Second Entity - Dapper splits on 'Id')
                        ci.Id AS Id,
                        ci.ProductId AS ProductId,
                        ISNULL(p.Name, '') AS ProductName,
                        p.ImageUrl AS ProductImageUrl,
                        ci.VariantId AS VariantId,
                        pv.Title AS VariantName,
                        ci.Quantity AS Quantity,
                        ci.Price AS Price
                    FROM carts c
                    LEFT JOIN cart_items ci ON c.Id = ci.CartId
                    LEFT JOIN products p ON ci.ProductId = p.Id
                    LEFT JOIN ProductVariants pv ON ci.VariantId = pv.Id
                    WHERE c.Id = @TargetCartId;
                END";

            CartDtos.Response? cartResponse = null;

            await connection.QueryAsync<CartDtos.Response, CartItemDtos.Response, CartDtos.Response>(
                sql,
                (cart, item) =>
                {
                    if (cartResponse == null)
                    {
                        cartResponse = cart;
                        cartResponse.Items = new List<CartItemDtos.Response>();
                    }

                    if (item != null && item.Id != 0)
                    {
                        cartResponse.Items.Add(item);
                    }

                    return cartResponse;
                },
                param: new
                {
                    UserId = request.UserId,
                    SessionId = request.SessionId
                },
                splitOn: "Id" // Matches the second 'Id' column alias to split entities correctly
            );

            if (cartResponse != null && cartResponse.Items.Count > 0)
            {
                cartResponse.SubtotalAmount = cartResponse.Items.Sum(i => i.Quantity * i.Price);

                // If TotalAmount wasn't stored/calculated in DB yet, fallback to Subtotal - Discount
                if (cartResponse.TotalAmount <= 0)
                {
                    cartResponse.TotalAmount = Math.Max(0, cartResponse.SubtotalAmount - cartResponse.DiscountAmount);
                }
            }

            return cartResponse ?? new CartDtos.Response();
        }
    }
}