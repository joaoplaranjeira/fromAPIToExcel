using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Otw.Clevvo.App.Members.Import.Models;
using Otw.Clevvo.App.Members.Import.Infrastructure.Configuration;

namespace Otw.Clevvo.App.Members.Import.Infrastructure.Services;

public class HttpRetryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpRetryService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpRetryService(HttpClient httpClient, ILogger<HttpRetryService> logger, IOptions<AppSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        
        // Ensure cookie is set on the HttpClient
        var apiSettings = settings.Value.Api;
        if (!string.IsNullOrEmpty(apiSettings.CookieValue))
        {
            // Clear existing Cookie headers and add the new one
            _httpClient.DefaultRequestHeaders.Remove("Cookie");
            _httpClient.DefaultRequestHeaders.Add("Cookie", apiSettings.CookieValue);
            _logger.LogDebug("✅ Cookie configurado no HttpClient (length: {Length})", apiSettings.CookieValue.Length);
        }
        else
        {
            _logger.LogWarning("⚠️ Cookie não configurado - valor vazio ou nulo");
        }
    }

    public async Task<T?> GetWithRetryAsync<T>(string url, string context = "request") where T : class
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Primeiro pedido falhou para {Context}: {Error}", context, ex.Message);
            _logger.LogInformation("A aguardar 2 segundos antes de tentar novamente...");
            
            // Wait 2 seconds before retry
            await Task.Delay(2000);
            
            try
            {
                _logger.LogInformation("A tentar novamente o pedido para {Context}", context);
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (Exception retryEx)
            {
                _logger.LogError("Nova tentativa falhou para {Context}: {Error}", context, retryEx.Message);
                throw; // Re-throw the exception if retry also fails
            }
        }
    }

    public async Task<TResponse?> PostWithRetryAsync<TRequest, TResponse>(string url, TRequest data, string context = "request") 
        where TRequest : class 
        where TResponse : class
    {
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Primeiro pedido POST falhou para {Context}: {Error}", context, ex.Message);
            _logger.LogInformation("A aguardar 2 segundos antes de tentar novamente...");
            
            // Wait 2 seconds before retry
            await Task.Delay(2000);
            
            try
            {
                _logger.LogInformation("A tentar novamente o pedido POST para {Context}", context);
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
            }
            catch (Exception retryEx)
            {
                _logger.LogError("Nova tentativa POST falhou para {Context}: {Error}", context, retryEx.Message);
                throw; // Re-throw the exception if retry also fails
            }
        }
    }

    public async Task<TResponse?> PutWithRetryAsync<TRequest, TResponse>(string url, TRequest data, string context = "request") 
        where TRequest : class 
        where TResponse : class
    {
        try
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PutAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Primeiro pedido PUT falhou para {Context}: {Error}", context, ex.Message);
            _logger.LogInformation("A aguardar 2 segundos antes de tentar novamente...");
            
            // Wait 2 seconds before retry
            await Task.Delay(2000);
            
            try
            {
                _logger.LogInformation("A tentar novamente o pedido PUT para {Context}", context);
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync(url, content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
            }
            catch (Exception retryEx)
            {
                _logger.LogError("Nova tentativa PUT falhou para {Context}: {Error}", context, retryEx.Message);
                throw; // Re-throw the exception if retry also fails
            }
        }
    }
}