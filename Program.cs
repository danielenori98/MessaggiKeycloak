using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpClient<TokenExchangeService>();
builder.Services.AddHttpClient("peer", c =>
{
    c.BaseAddress = new Uri(builder.Configuration["Peer:BaseUrl"]!);
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/messaggi", [Authorize] (MessaggioDto msg, ClaimsPrincipal user, ILogger<Program> log) =>
{
    var utente = user.FindFirst("preferred_username")?.Value ?? user.FindFirst("sub")?.Value;
    log.LogInformation("Messaggio ricevuto da {Utente}: {Testo}", utente, msg.Testo);
    return Results.Ok(new { ricevuto = true, da = utente, testo = msg.Testo });
});

app.MapPost("/api/invia", [Authorize] async (
    MessaggioDto msg,
    HttpContext ctx,
    TokenExchangeService exchange,
    IHttpClientFactory factory,
    IConfiguration config) =>
{
    var incomingToken = ctx.Request.Headers.Authorization.ToString()["Bearer ".Length..];

    var exchanged = await exchange.ExchangeAsync(incomingToken, config["Peer:Audience"]!);

    var client = factory.CreateClient("peer");
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/messaggi")
    {
        Content = JsonContent.Create(msg)
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", exchanged);

    var response = await client.SendAsync(request);
    var body = await response.Content.ReadAsStringAsync();
    return Results.Content(body, "application/json", statusCode: (int)response.StatusCode);
});

app.Run();

public record MessaggioDto(string Testo);