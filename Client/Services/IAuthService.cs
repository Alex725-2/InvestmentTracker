using InvestmentTracker.Shared.Models;

namespace InvestmentTracker.Client.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> Login(LoginRequest loginRequest);
        Task<AuthResponse> Register(RegisterRequest registerRequest);
        Task Logout();
        Task<bool> IsLoggedIn();
        Task<string?> GetToken();
    }
}