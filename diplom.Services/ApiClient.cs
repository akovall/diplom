using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace diplom.Services
{
    public class ApiClient
    {
        private static ApiClient? _instance;
        private static readonly object _lock = new();

        public static ApiClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ApiClient();
                    }
                }
                return _instance;
            }
        }

        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        public event Action? Unauthorized;

        // Current user info (set after login)
        public string? Token { get; private set; }
        public int UserId { get; private set; }
        public string Username { get; private set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = string.Empty;
        public string Role { get; private set; } = string.Empty;
        public bool IsAuthenticated => !string.IsNullOrEmpty(Token);

        public string BaseUrl { get; set; } = "http://localhost:5074";

        private ApiClient()
        {
            _http = new HttpClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        private void SetAuthHeader()
        {
            if (!string.IsNullOrEmpty(Token))
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", Token);
            }
        }

        private async Task EnsureAuthorizedAsync(HttpResponseMessage response)
        {
            if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
                return;

            // Session can be revoked server-side (e.g., login from another client).
            var body = string.Empty;
            try { body = await response.Content.ReadAsStringAsync(); } catch { }

            Logout();
            Unauthorized?.Invoke();
            throw new UnauthorizedAccessException(string.IsNullOrWhiteSpace(body) ? "Unauthorized" : body);
        }

        // === Auth ===

        public async Task<bool> LoginAsync(string username, string password)
        {
            var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/auth/login", new
            {
                username,
                password
            }, _jsonOptions);

            if (!response.IsSuccessStatusCode)
                return false;

            var result = await response.Content.ReadFromJsonAsync<AuthResult>(_jsonOptions);
            if (result == null) return false;

            Token = result.Token;
            UserId = result.UserId;
            Username = result.Username;
            FullName = result.FullName;
            JobTitle = result.JobTitle;
            Role = result.Role;
            SetAuthHeader();
            return true;
        }

        public async Task<(bool Success, string Error)> RegisterAsync(
            string username, string password, string fullName, string jobTitle)
        {
            var response = await _http.PostAsJsonAsync($"{BaseUrl}/api/auth/register", new
            {
                username,
                password,
                fullName,
                jobTitle
            }, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                return (false, errorText);
            }

            var result = await response.Content.ReadFromJsonAsync<AuthResult>(_jsonOptions);
            if (result == null) return (false, "Empty response");

            Token = result.Token;
            UserId = result.UserId;
            Username = result.Username;
            FullName = result.FullName;
            JobTitle = result.JobTitle;
            Role = result.Role;
            SetAuthHeader();
            return (true, string.Empty);
        }

        public async Task<List<string>> GetProfessionsAsync(bool forRegistration)
        {
            var endpoint = forRegistration ? "/api/auth/professions" : "/api/users/professions";
            return await GetAsync<List<string>>(endpoint) ?? new List<string>();
        }

        public void Logout()
        {
            Token = null;
            UserId = 0;
            Username = string.Empty;
            FullName = string.Empty;
            JobTitle = string.Empty;
            Role = string.Empty;
            _http.DefaultRequestHeaders.Authorization = null;
        }

        public async Task LogoutAsync()
        {
            if (IsAuthenticated)
            {
                try
                {
                    await PostAsync("/api/presence/logout");
                }
                catch
                {
                    // best-effort
                }
            }

            try
            {
                await RealTimeService.Instance.StopAsync();
            }
            catch
            {
                // best-effort
            }

            Logout();
        }

        // === Generic HTTP methods ===

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            var response = await _http.GetAsync($"{BaseUrl}{endpoint}");
            await EnsureAuthorizedAsync(response);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
        }

        public async Task<T?> PostAsync<T>(string endpoint, object data)
        {
            var response = await _http.PostAsJsonAsync($"{BaseUrl}{endpoint}", data, _jsonOptions);
            await EnsureAuthorizedAsync(response);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
        }

        public async Task<bool> PostAsync(string endpoint, object? data = null)
        {
            var response = await _http.PostAsJsonAsync($"{BaseUrl}{endpoint}", data ?? new { }, _jsonOptions);
            await EnsureAuthorizedAsync(response);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> PutAsync(string endpoint, object data)
        {
            var response = await _http.PutAsJsonAsync($"{BaseUrl}{endpoint}", data, _jsonOptions);
            await EnsureAuthorizedAsync(response);
            return response.IsSuccessStatusCode;
        }

        public async Task<T?> PutAsync<T>(string endpoint, object data)
        {
            var response = await _http.PutAsJsonAsync($"{BaseUrl}{endpoint}", data, _jsonOptions);
            await EnsureAuthorizedAsync(response);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
        }

        public async Task DeleteAsync(string endpoint)
        {
            var response = await _http.DeleteAsync($"{BaseUrl}{endpoint}");
            await EnsureAuthorizedAsync(response);
            response.EnsureSuccessStatusCode();
        }

        // Internal DTO for auth response
        private class AuthResult
        {
            public string Token { get; set; } = string.Empty;
            public int UserId { get; set; }
            public string Username { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string JobTitle { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }
    }
}
