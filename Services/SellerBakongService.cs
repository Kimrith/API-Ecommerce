using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Models;
using API_Ecommerce.Services;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace API_Ecommerce.Services
{
    public interface ISellerBakongService
    {
        Task<SellerBakongConfigsDto?> GetConfigBySellerIdAsync(int sellerId);
        Task<SellerBakongConfigsDto> UpsertConfigAsync(int sellerId, UpsertSellerBakongConfigDto dto);
    }

    public class SellerBakongService : ISellerBakongService
    {
        private readonly AppDbContext _context;
        private readonly IBakongService _bakongService;

        public SellerBakongService(AppDbContext context, IBakongService bakongService)
        {
            _context = context;
            _bakongService = bakongService;
        }

        public async Task<SellerBakongConfigsDto?> GetConfigBySellerIdAsync(int sellerId)
        {
            var config = await _context.SellerBakongConfigs
                .FirstOrDefaultAsync(s => s.SellerId == sellerId);

            if (config == null) return null;

            string? qrString = null;
            string? qrImageBase64 = null;

            // Generate a preview KHQR string using the seller's config credentials if BakongId is available
            if (!string.IsNullOrEmpty(config.BakongId))
            {
                var (qr, md5) = _bakongService.GenerateDynamicQr(
                    "PREVIEW-CONFIG", // dummy bill reference for profile setup preview
                    1.00m,            // nominal preview amount
                    config.BakongId,
                    config.MerchantName,
                    config.MerchantCity,
                    config.AcquiringId,
                    "USD"
                );

                qrString = qr;

                if (!string.IsNullOrEmpty(qr))
                {
                    using var qrGenerator = new QRCodeGenerator();
                    using var qrCodeData = qrGenerator.CreateQrCode(qr, QRCodeGenerator.ECCLevel.Q);
                    using var qrCode = new PngByteQRCode(qrCodeData);
                    byte[] qrCodeBytes = qrCode.GetGraphic(20);
                    qrImageBase64 = $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
                }
            }

            return new SellerBakongConfigsDto
            {
                Id = config.Id,
                SellerId = config.SellerId,
                BakongId = config.BakongId,
                MerchantName = config.MerchantName,
                MerchantCity = config.MerchantCity,
                AcquiringId = config.AcquiringId,
                ApiBaseUrl = config.ApiBaseUrl,
                Token = config.Token,
                QrString = qrString,
                QrImageBase64 = qrImageBase64
            };
        }

        public async Task<SellerBakongConfigsDto> UpsertConfigAsync(int sellerId, UpsertSellerBakongConfigDto dto)
        {
            var config = await _context.SellerBakongConfigs
                .FirstOrDefaultAsync(s => s.SellerId == sellerId);

            if (config == null)
            {
                config = new SellerBakongConfigs
                {
                    SellerId = sellerId,
                    BakongId = dto.BakongId,
                    MerchantName = dto.MerchantName,
                    MerchantCity = dto.MerchantCity,
                    AcquiringId = dto.AcquiringId,
                    ApiBaseUrl = dto.ApiBaseUrl,
                    Token = dto.Token
                };
                _context.SellerBakongConfigs.Add(config);
            }
            else
            {
                config.BakongId = dto.BakongId;
                config.MerchantName = dto.MerchantName;
                config.MerchantCity = dto.MerchantCity;
                config.AcquiringId = dto.AcquiringId;
                config.ApiBaseUrl = dto.ApiBaseUrl;
                config.Token = dto.Token;
                _context.SellerBakongConfigs.Update(config);
            }

            await _context.SaveChangesAsync();

            return await GetConfigBySellerIdAsync(sellerId) ?? throw new Exception("Failed to save configuration.");
        }
    }
}