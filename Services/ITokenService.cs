using API_Ecommerce.Models;

namespace API_Ecommerce.Services
{
    public interface ITokenService
    {
        string GenerateToken(Auth user);
    }
}