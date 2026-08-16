using API_Ecommerce.Commands.Cart;
using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Enums;
using API_Ecommerce.Models;
using Microsoft.EntityFrameworkCore;

namespace API_Ecommerce.Commands.Create
{
    public class CreateOrderCommand
    {
        private readonly AppDbContext _context;

        public CreateOrderCommand(AppDbContext context)
        {
            _context = context;
        }

        public async Task<OrderDtos.Response> ExecuteAsync(long userId, OrderDtos.Create dto, List<CartItemDto> cartItems, string currency = "USD")
        {
            // 1. Verify that the user exists
            var userExists = await _context.Auths.AnyAsync(u => u.Id == userId);
            if (!userExists)
            {
                throw new KeyNotFoundException($"User with ID {userId} was not found.");
            }

            // 2. Verify that the Shipping Address exists and belongs to the user
            var shippingAddressExists = await _context.Addresses.AnyAsync(a => a.Id == dto.ShippingAddressId && a.UserId == userId);
            if (!shippingAddressExists)
            {
                throw new KeyNotFoundException($"Shipping address with ID {dto.ShippingAddressId} was not found or does not belong to the user.");
            }

            // 3. Verify Billing Address if provided, otherwise default to Shipping Address ID
            long billingAddressId = dto.BillingAddressId ?? dto.ShippingAddressId;
            if (dto.BillingAddressId.HasValue)
            {
                var billingAddressExists = await _context.Addresses.AnyAsync(a => a.Id == billingAddressId && a.UserId == userId);
                if (!billingAddressExists)
                {
                    throw new KeyNotFoundException($"Billing address with ID {billingAddressId} was not found or does not belong to the user.");
                }
            }

            // 4. Validate cart items coming from localStorage
            if (cartItems == null || !cartItems.Any())
            {
                throw new ArgumentException("Order must contain at least one item from the cart.");
            }

            decimal subtotal = 0;

            // Calculate subtotal using active discount checks
            foreach (var item in cartItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                {
                    throw new KeyNotFoundException($"Product with ID {item.ProductId} was not found.");
                }

                if (product.Status != ProductStatus.Approved)
                {
                    throw new InvalidOperationException($"Product '{product.Name}' is not available for purchase (Status: {product.Status}).");
                }

                bool hasActiveDiscount = product.DiscountPrice.HasValue && product.DiscountPrice.Value > 0 &&
                    (!product.DiscountStartDate.HasValue || DateTime.UtcNow >= product.DiscountStartDate.Value) &&
                    (!product.DiscountEndDate.HasValue || DateTime.UtcNow <= product.DiscountEndDate.Value);

                decimal unitPrice = hasActiveDiscount ? product.DiscountPrice.Value : product.Price;
                subtotal += unitPrice * item.Quantity;
            }

            // 5. Validate and Calculate Coupon Discount if provided
            decimal discountAmount = 0;
            if (!string.IsNullOrWhiteSpace(dto.CouponCode))
            {
                var coupon = await _context.Coupons
                    .FirstOrDefaultAsync(c => c.Code.ToLower() == dto.CouponCode.ToLower() && c.IsActive);

                if (coupon == null)
                {
                    throw new KeyNotFoundException($"Coupon code '{dto.CouponCode}' is invalid or inactive.");
                }

                if ((coupon.StartsAt.HasValue && DateTime.UtcNow < coupon.StartsAt.Value) ||
                    (coupon.ExpiresAt.HasValue && DateTime.UtcNow > coupon.ExpiresAt.Value))
                {
                    throw new InvalidOperationException($"Coupon code '{dto.CouponCode}' has expired or is not yet active.");
                }

                if (coupon.MinimumAmount.HasValue && subtotal < coupon.MinimumAmount.Value)
                {
                    throw new InvalidOperationException($"Subtotal must be at least {coupon.MinimumAmount.Value} to use this coupon.");
                }

                if (coupon.DiscountType == CouponType.Percentage)
                {
                    discountAmount = subtotal * (coupon.DiscountValue / 100);
                    if (coupon.MaximumDiscountAmount.HasValue && discountAmount > coupon.MaximumDiscountAmount.Value)
                    {
                        discountAmount = coupon.MaximumDiscountAmount.Value;
                    }
                }
                else
                {
                    discountAmount = coupon.DiscountValue;
                    if (discountAmount > subtotal)
                    {
                        discountAmount = subtotal;
                    }
                }

                coupon.TimesUsed++;
                _context.Coupons.Update(coupon);
            }

            // 6. Calculate financial amounts
            decimal taxAmount = 0;
            decimal shippingAmount = 0;
            decimal totalAmount = subtotal + taxAmount + shippingAmount - discountAmount;

            // 7. Generate a unique Order Number
            string orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";

            // 8. Map to Order Entity
            var order = new Order
            {
                UserId = userId,
                OrderNumber = orderNumber,
                Status = OrderStatus.Pending,
                Subtotal = subtotal,
                TaxAmount = taxAmount,
                ShippingAmount = shippingAmount,
                DiscountAmount = discountAmount,
                TotalAmount = totalAmount,
                Currency = currency,
                ShippingAddressId = dto.ShippingAddressId,
                BillingAddressId = billingAddressId,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            // 9. Save Order to Database
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // 9a. Save Order Items to Database with correct discount pricing applied
            foreach (var item in cartItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);

                bool hasActiveDiscount = product != null && product.DiscountPrice.HasValue && product.DiscountPrice.Value > 0 &&
                    (!product.DiscountStartDate.HasValue || DateTime.UtcNow >= product.DiscountStartDate.Value) &&
                    (!product.DiscountEndDate.HasValue || DateTime.UtcNow <= product.DiscountEndDate.Value);

                decimal unitPrice = product != null ? (hasActiveDiscount ? product.DiscountPrice.Value : product.Price) : 0;

                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = unitPrice,
                    TotalPrice = unitPrice * item.Quantity
                };
                _context.OrderItems.Add(orderItem);
            }
            await _context.SaveChangesAsync();

            // 10. Fetch the created order with related Address details for the response
            var createdOrder = await _context.Orders
                .Include(o => o.ShippingAddress)
                .Include(o => o.BillingAddress)
                .FirstAsync(o => o.Id == order.Id);

            // 11. Map to OrderDtos.Response and return
            return new OrderDtos.Response
            {
                Id = createdOrder.Id,
                OrderNumber = createdOrder.OrderNumber,
                UserId = createdOrder.UserId ?? userId,
                Status = createdOrder.Status,
                Subtotal = createdOrder.Subtotal,
                TaxAmount = createdOrder.TaxAmount,
                ShippingAmount = createdOrder.ShippingAmount,
                DiscountAmount = createdOrder.DiscountAmount,
                TotalAmount = createdOrder.TotalAmount,
                Currency = createdOrder.Currency,
                ShippingAddress = createdOrder.ShippingAddress != null ? MapAddressToDto(createdOrder.ShippingAddress) : null,
                BillingAddress = createdOrder.BillingAddress != null ? MapAddressToDto(createdOrder.BillingAddress) : null,
                Notes = createdOrder.Notes,
                CreatedAt = createdOrder.CreatedAt,
                UpdatedAt = createdOrder.UpdatedAt
            };
        }

        private static UserAddressResponseDto MapAddressToDto(Address address)
        {
            return new UserAddressResponseDto
            {
                Id = address.Id,
                UserId = address.UserId,
                AddressType = address.AddressType,
                StreetAddress = address.StreetAddress,
                City = address.City,
                State = address.State,
                PostalCode = address.PostalCode,
                Country = address.Country,
                IsDefault = address.IsDefault,
                CreatedAt = address.CreatedAt,
                UpdatedAt = address.UpdatedAt
            };
        }
    }
}