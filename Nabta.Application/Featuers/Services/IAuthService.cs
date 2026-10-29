using Nabta.Application.Dtos.Auth;
namespace Nabta.Application.Featuers.Services
{   public interface IAuthService
    {
        Task<AuthModel> RegisterAsync(RegisterDto dto);

        Task<AuthModel> LoginAsync(LoginDto loginDto);

        Task SignOut();

        Task<AuthModel> RefreshTokenAsync(string token);

        Task<bool> RevokeToken(string token);
    }
}