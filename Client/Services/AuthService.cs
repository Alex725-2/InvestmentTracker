using Blazored.LocalStorage;
using InvestmentTracker.Shared.Models;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;

namespace InvestmentTracker.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private readonly AuthenticationStateProvider _authStateProvider;

        public AuthService(HttpClient httpClient,
                           ILocalStorageService localStorage,
                           AuthenticationStateProvider authStateProvider)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
            _authStateProvider = authStateProvider;
        }

        public async Task<AuthResponse> Login(LoginRequest loginRequest)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginRequest);
            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse != null && authResponse.IsSuccess)
                {
                    await _localStorage.SetItemAsync("authToken", authResponse.Token);
                    ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(authResponse.Token);
                    return authResponse;
                }
            }
            return new AuthResponse { IsSuccess = false, Message = "Login failed" };
        }

        public async Task<AuthResponse> Register(RegisterRequest registerRequest)
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", registerRequest);
            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (authResponse != null && authResponse.IsSuccess)
                {
                    await _localStorage.SetItemAsync("authToken", authResponse.Token);
                    ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(authResponse.Token);
                    return authResponse;
                }
            }
            return new AuthResponse { IsSuccess = false, Message = "Registration failed" };
        }

        public async Task Logout()
        {
            await _localStorage.RemoveItemAsync("authToken");
            ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();
        }

        public async Task<bool> IsLoggedIn()
        {
            var token = await GetToken();
            return !string.IsNullOrEmpty(token);
        }

        public async Task<string?> GetToken()
        {
            return await _localStorage.GetItemAsync<string>("authToken");
        }
    }
}