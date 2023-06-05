
extern alias Api;
extern alias IdentityServer;

using System.Text.Json;

using FluentAssertions;

using IdentityModel.Client;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Explore.Duende.IdentityServer.IntegrationTests;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public async Task Given_IdentityClient_When_GetDiscoveryDocument_ThenSuccess()
    {
        // Given
        var identityFactory = new WebApplicationFactory<IdentityServer::Program>();

        var identityClient = identityFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost:6001")
        });

        // When
        var disco = await identityClient.GetDiscoveryDocumentAsync();

        // Then
        disco.Should().NotBeNull();
        disco.IsError.Should().BeFalse();
    }

    [TestMethod]
    public async Task Given_IdentityClient_When_RequestAToken_ThenSuccess()
    {
        // Given
        var identityFactory = new WebApplicationFactory<IdentityServer::Program>();

        var identityClient = identityFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://identityserver")
        });
        var disco = await identityClient.GetDiscoveryDocumentAsync();

        // When
        var tokenResponse = await identityClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = disco.TokenEndpoint,

            ClientId = "explore.duende.api.clientid",
            ClientSecret = "secret",
            Scope = "explore.duende.api"
        });

        // Then
        tokenResponse.Should().NotBeNull();
        tokenResponse.IsError.Should().BeFalse();
        tokenResponse.AccessToken?.Length.Should().BeGreaterThan(100);
    }

    [TestMethod]
    public async Task Given_IdentityClient_When_CallApi_ThenSuccess()
    {
        // Given
        var identityFactory = new WebApplicationFactory<IdentityServer::Program>();

        var identityClient = identityFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://identityserver")

        });

        var disco = await identityClient.GetDiscoveryDocumentAsync();

        var tokenResponse = await identityClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = disco.TokenEndpoint,

            ClientId = "explore.duende.api.clientid",
            ClientSecret = "secret",
            Scope = "explore.duende.api"
        });

        var identityHandler = identityFactory.Server.CreateHandler();

        //-----------------------------------------------------------------
        // API
        // https://stackoverflow.com/questions/39390339/integration-testing-with-in-memory-identityserver
        var apiFactory = new WebApplicationFactory<Api::Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddAuthentication(defaultScheme: "NOT-Bearer")// NOT-Bearer to avoid conflict with actual Api::Progam auth
                        .AddJwtBearer("NOT-Bearer", options =>
                        {
                            options.Authority = "https://identityserver";
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateAudience = false
                            };
                            // IMPORTANT PART HERE
                            options.BackchannelHttpHandler = identityHandler;
                        });
                });
            });

        var apiClient = apiFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://apiserver")
        });

        apiClient.SetBearerToken(tokenResponse.AccessToken ?? "");

        // When

        var response = await apiClient.GetAsync("/identity");

        // Then

        response.Should().NotBeNull();

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.IsSuccessStatusCode.Should().BeTrue();

        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var serialized = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });

        serialized.Should().Be(serialized.ToString());
    }

    [TestMethod]
    public async Task Given_ApiClient_When_CallWeather_ThenSuccess()
    {
        // Given
        var apiFactory = new WebApplicationFactory<Api::Program>();

        var apiClient = apiFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://apiserver")
        });

        // When

        var response = await apiClient.GetAsync("/WeatherForecast");

        // Then


        response.Should().NotBeNull();
        response.IsSuccessStatusCode.Should().BeTrue();

        var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var serialized = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });

        serialized.Should().Be(serialized.ToString());
    }
}