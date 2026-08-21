using API_Ecommerce.Commands.Cart;
using API_Ecommerce.DTOs;

namespace API_Ecommerce.Services
{
    public interface ITelegramNotificationService
    {
        Task SendPaidOrderAlertAsync(
            OrderDtos.Response order,
            List<CartItemDto>? items = null,
            string? customerName = null,
            string? customerEmail = null,
            string? customerPhone = null,
            string? addressText = null,
            string? paymentMethod = "BakongKHQR"
        );
    }
}