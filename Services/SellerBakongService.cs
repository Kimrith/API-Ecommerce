using API_Ecommerce.Data;
using API_Ecommerce.DTOs;
using API_Ecommerce.Models;
using Microsoft.EntityFrameworkCore;

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

        public SellerBakongService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SellerBakongConfigsDto?> GetConfigBySellerIdAsync(int sellerId)
        {
            var config = await _context.SellerBakongConfigs
                .FirstOrDefaultAsync(s => s.SellerId == sellerId);

            if (config == null) return null;

            return new SellerBakongConfigsDto
            {
                Id = config.Id,
                SellerId = config.SellerId,
                BakongId = config.BakongId,
                MerchantName = config.MerchantName,
                MerchantCity = config.MerchantCity,
                AcquiringId = config.AcquiringId,
                ApiBaseUrl = config.ApiBaseUrl,
                Token = config.Token
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