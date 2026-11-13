using System.Text.Json;

public class TokenExchangeService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;

    public TokenExchangeService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task<string> ExchangeAsync(string subjectToken, string targetAudience)
    {
        var tokenUrl = $"{_config["Keycloak:Authority"]}/protocol/openid-connect/token";

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:token-exchange",
            ["client_id"] = _config["Keycloak:ClientId"]!,
            ["client_secret"] = _config["Keycloak:ClientSecret"]!,
            ["subject_token"] = subjectToken,
            ["subject_token_type"] = "urn:ietf:params:oauth:token-type:access_token",
            ["requested_token_type"] = "urn:ietf:params:oauth:token-type:access_token",
            ["audience"] = targetAudience
        };

        var response = await _http.PostAsync(tokenUrl, new FormUrlEncodedContent(form));
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Token exchange fallito ({(int)response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }
}