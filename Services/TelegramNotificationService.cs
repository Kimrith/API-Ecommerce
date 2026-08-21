using System.Text;
using System.Text.Json;
using API_Ecommerce.Commands.Cart;
using API_Ecommerce.DTOs;

namespace API_Ecommerce.Services
{
    public class TelegramNotificationService : ITelegramNotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _botToken;
        private readonly string _chatId;
        private readonly ILogger<TelegramNotificationService> _logger;

        public TelegramNotificationService(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<TelegramNotificationService> logger)
        {
            _httpClient = httpClient;
            _botToken = config["Telegram:BotToken"] ?? throw new ArgumentNullException("Telegram:BotToken is missing");
            _chatId = config["Telegram:ChatId"] ?? throw new ArgumentNullException("Telegram:ChatId is missing");
            _logger = logger;
        }

        public async Task SendPaidOrderAlertAsync(
            OrderDtos.Response order,
            List<CartItemDto>? items = null,
            string? customerName = null,
            string? customerEmail = null,
            string? customerPhone = null,
            string? addressText = null,
            string? paymentMethod = "BakongKHQR")
        {
            try
            {
                var name = !string.IsNullOrWhiteSpace(customerName) ? customerName : $"User #{order.UserId}";
                var email = !string.IsNullOrWhiteSpace(customerEmail) ? customerEmail : "N/A";
                var phone = !string.IsNullOrWhiteSpace(customerPhone) ? customerPhone : "N/A";
                var address = !string.IsNullOrWhiteSpace(addressText) ? addressText : "No shipping address provided";

                // Build cart item list
                var itemsSummary = new StringBuilder();
                if (items != null && items.Any())
                {
                    foreach (var item in items)
                    {
                        var prodName = !string.IsNullOrWhiteSpace(item.ProductName)
                            ? item.ProductName
                            : $"Product #{item.ProductId}";

                        itemsSummary.AppendLine($"• {prodName} × {item.Quantity} ({order.Currency} {item.Price:F2})");
                    }
                }
                else
                {
                    itemsSummary.AppendLine("• <i>(No item details provided)</i>");
                }

                var message =
$@"🛍️ <b>PAID ORDER CONFIRMED!</b>
━━━━━━━━━━━━━━━━━━
🆔 <b>Order ID:</b> <code>#{order.Id}</code>
🧾 <b>Order No:</b> <code>{order.OrderNumber}</code>

👤 <b>Customer Info:</b>
• <b>Name:</b> {name}
• <b>Email:</b> {email}
• <b>Phone:</b> <code>{phone}</code>
• <b>Address:</b> {address}

💳 <b>Payment Details:</b>
• <b>Method:</b> {paymentMethod}
• <b>Status:</b> ✅ <b>PAID (Processing)</b>

📦 <b>Order Items:</b>
{itemsSummary.ToString().TrimEnd()}
━━━━━━━━━━━━━━━━━━
💰 <b>Total Paid:</b> <b>{order.Currency} {order.TotalAmount:F2}</b>
⏰ <b>Time:</b> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

                var payload = new
                {
                    chat_id = _chatId,
                    text = message,
                    parse_mode = "HTML",
                    reply_markup = new
                    {
                        inline_keyboard = new[]
                        {
                            new[]
                            {
                                new { text = "🔗 Open Order in Dashboard", url = $"https://yourstore.com/admin/orders/{order.Id}" }
                            }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"https://api.telegram.org/bot{_botToken}/sendMessage", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Telegram API Error: {StatusCode} - {Body}", response.StatusCode, errorBody);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Telegram notification for Order #{OrderNumber}", order.OrderNumber);
            }
        }
    }
}