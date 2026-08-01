using API_Ecommerce.Models;
using Dapper;
using MediatR;
using Microsoft.Data.SqlClient;
using System.Data;

namespace API_Ecommerce.Queries
{
    public class CouponQueries
    {
        // =========================================================
        // 1. QUERY: GET COUPON BY ID (RAW SQL)
        // =========================================================
        public record GetCouponByIdQuery(long Id) : IRequest<Coupon?>;

        public class GetCouponByIdQueryHandler : IRequestHandler<GetCouponByIdQuery, Coupon?>
        {
            private readonly string _connectionString;

            public GetCouponByIdQueryHandler(IConfiguration configuration)
            {
                _connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            public async Task<Coupon?> Handle(GetCouponByIdQuery request, CancellationToken cancellationToken)
            {
                var sql = @"
                    SELECT 
                        Id,
                        Code,
                        Description,
                        DiscountType,
                        DiscountValue,
                        MinimumAmount,
                        MaximumDiscountAmount,
                        UsageLimit,
                        UsageLimitPerUser,
                        TimesUsed,
                        IsActive,
                        StartsAt,
                        ExpiresAt,
                        CreatedAt,
                        UpdatedAt
                    FROM coupons
                    WHERE Id = @Id";

                using IDbConnection db = new SqlConnection(_connectionString);

                return await db.QueryFirstOrDefaultAsync<Coupon>(
                    new CommandDefinition(sql, new { request.Id }, cancellationToken: cancellationToken)
                );
            }
        }

        // =========================================================
        // 2. QUERY: GET COUPON BY CODE (RAW SQL - Useful at Checkout)
        // =========================================================
        public record GetCouponByCodeQuery(string Code) : IRequest<Coupon?>;

        public class GetCouponByCodeQueryHandler : IRequestHandler<GetCouponByCodeQuery, Coupon?>
        {
            private readonly string _connectionString;

            public GetCouponByCodeQueryHandler(IConfiguration configuration)
            {
                _connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            public async Task<Coupon?> Handle(GetCouponByCodeQuery request, CancellationToken cancellationToken)
            {
                var sql = @"
                    SELECT 
                        Id,
                        Code,
                        Description,
                        DiscountType,
                        DiscountValue,
                        MinimumAmount,
                        MaximumDiscountAmount,
                        UsageLimit,
                        UsageLimitPerUser,
                        TimesUsed,
                        IsActive,
                        StartsAt,
                        ExpiresAt,
                        CreatedAt,
                        UpdatedAt
                    FROM coupons
                    WHERE Code = @Code";

                using IDbConnection db = new SqlConnection(_connectionString);

                return await db.QueryFirstOrDefaultAsync<Coupon>(
                    new CommandDefinition(sql, new { Code = request.Code.Trim().ToUpper() }, cancellationToken: cancellationToken)
                );
            }
        }

        // =========================================================
        // 3. QUERY: GET ALL COUPONS (RAW SQL)
        // =========================================================
        public record GetAllCouponsQuery() : IRequest<List<Coupon>>;

        public class GetAllCouponsQueryHandler : IRequestHandler<GetAllCouponsQuery, List<Coupon>>
        {
            private readonly string _connectionString;

            public GetAllCouponsQueryHandler(IConfiguration configuration)
            {
                _connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            public async Task<List<Coupon>> Handle(GetAllCouponsQuery request, CancellationToken cancellationToken)
            {
                var sql = @"
                    SELECT 
                        Id,
                        Code,
                        Description,
                        DiscountType,
                        DiscountValue,
                        MinimumAmount,
                        MaximumDiscountAmount,
                        UsageLimit,
                        UsageLimitPerUser,
                        TimesUsed,
                        IsActive,
                        StartsAt,
                        ExpiresAt,
                        CreatedAt,
                        UpdatedAt
                    FROM coupons
                    ORDER BY Id DESC";

                using IDbConnection db = new SqlConnection(_connectionString);

                var coupons = await db.QueryAsync<Coupon>(
                    new CommandDefinition(sql, cancellationToken: cancellationToken)
                );

                return coupons.ToList();
            }
        }
    }
}