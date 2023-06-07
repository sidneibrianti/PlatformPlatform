using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.AccountManagement.Tests.Infrastructure;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Api.Tenants;

public sealed class TenantEndpointsTests : IDisposable
{
    // This string represents a custom DateTime format based on the built-in format "o".
    // The format "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFK" is used to avoid trailing zeros in the DateTime string.
    // The 'F's in the format are upper-case to indicate that trailing zeros should be removed.
    // See https://stackoverflow.com/a/17349663
    private const string Iso8601TimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFK";

    private readonly SqliteInMemoryDbContextFactory<AccountManagementDbContext> _sqliteInMemoryDbContextFactory;
    private readonly WebApplicationFactory<Program> _webApplicationFactory;

    public TenantEndpointsTests()
    {
        _sqliteInMemoryDbContextFactory = new SqliteInMemoryDbContextFactory<AccountManagementDbContext>();

        _webApplicationFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app's AccountManagementDbContext registration.
                var descriptor = services.Single(d =>
                    d.ServiceType == typeof(DbContextOptions<AccountManagementDbContext>));
                services.Remove(descriptor);

                // Add AccountManagementDbContext using SqLiteDbContextFactory
                services.AddScoped(_ => _sqliteInMemoryDbContextFactory.CreateContext());

                // Add DbContextOptions<AccountManagementDbContext> to the service collection.
                services.AddScoped(_ => _sqliteInMemoryDbContextFactory.CreateOptions());

                services.AddTransient<DatabaseSeeder>();
            });
        });

        var serviceScope = _webApplicationFactory.Services.CreateScope();
        serviceScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    }

    public void Dispose()
    {
        _sqliteInMemoryDbContextFactory.Dispose();
    }

    [Fact]
    public async Task CreateTenant_WhenValid_ShouldCreateTenant()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();

        // Act
        var response = await httpClient.PostAsJsonAsync("/api/tenants",
            new CreateTenant.Command("TestTenant", "foo", "test@test.com", "1234567890")
        );

        // Assert
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().BeEmpty();

        response.Content.Headers.ContentType.Should().BeNull();
        response.Headers.Location!.ToString().StartsWith("/api/tenants/").Should().BeTrue();
        response.Headers.Location!.ToString().Length.Should().Be($"/api/tenants/{TenantId.NewId()}".Length);
    }

    [Fact]
    public async Task CreateTenant_WhenInvalid_ShouldNotCreateTenant()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();

        // Act
        var response = await httpClient.PostAsJsonAsync("/api/tenants",
            new CreateTenant.Command("TestTenant", "a", "ab", null)
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        const string expectedBody =
            """{"type":"https://httpstatuses.com/400","title":"Bad Request","status":400,"Errors":[{"code":"Email","message":"'Email' is not a valid email address."},{"code":"Subdomain","message":"'Subdomain' must be between 3 and 30 characters. You entered 1 characters."}]}""";
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task GetTenant_WhenTenantExists_ShouldReturnTenant()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();
        var tenantId = DatabaseSeeder.Tenant1Id;

        // Act
        var response = await httpClient.GetAsync($"/api/tenants/{tenantId}");

        // Assert
        response.EnsureSuccessStatusCode();

        var tenantDto = await response.Content.ReadFromJsonAsync<TenantResponseDto>();
        const string tenantName = DatabaseSeeder.Tenant1Name;
        var createdAt = tenantDto?.CreatedAt.ToString(Iso8601TimeFormat);

        var expectedBody =
            $@"{{""id"":""{tenantId}"",""createdAt"":""{createdAt}"",""modifiedAt"":null,""name"":""{tenantName}"",""state"":0,""email"":""test@test.com"",""phone"":""1234567890""}}";
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task GetTenant_WhenTenantDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingTenantId = new TenantId(999);

        var httpClient = _webApplicationFactory.CreateClient();

        // Act
        var response = await httpClient.GetAsync($"/api/tenants/{nonExistingTenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        const string expectedBody =
            """{"type":"https://httpstatuses.com/404","title":"Not Found","status":404,"detail":"Tenant with id '999' not found."}""";
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTenant_WhenValid_ShouldUpdateTenant()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();

        var tenantId = DatabaseSeeder.Tenant1Id;

        // Act
        var response = await httpClient.PutAsJsonAsync($"/api/tenants/{tenantId}",
            new UpdateTenant.Command {Name = "UpdatedName", Email = "updated@test.com", Phone = "0987654321"}
        );

        // Assert
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType.Should().BeNull();
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTenant_WhenInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();

        var tenantId = DatabaseSeeder.Tenant1Id;

        // Act
        var response = await httpClient.PutAsJsonAsync($"/api/tenants/{tenantId}",
            new UpdateTenant.Command {Name = "Invalid Email", Email = "@test.com", Phone = "0987654321"}
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTenant_WhenTenantDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();
        const string nonExistingTenantId = "999";

        // Act
        var response = await httpClient.PutAsJsonAsync($"/api/tenants/{nonExistingTenantId}",
            new UpdateTenant.Command {Name = "UpdatedName", Email = "updated@test.com", Phone = "0987654321"}
        );

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        const string expectedBody =
            """{"type":"https://httpstatuses.com/404","title":"Not Found","status":404,"detail":"Tenant with id '999' not found."}""";
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();
        const string nonExistingTenantId = "999";

        // Act
        var response = await httpClient.DeleteAsync($"/api/tenants/{nonExistingTenantId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        const string expectedBody =
            """{"type":"https://httpstatuses.com/404","title":"Not Found","status":404,"detail":"Tenant with id '999' not found."}""";

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantWithUsersExists_ShouldReturnBadRequest()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();
        var tenantId = DatabaseSeeder.Tenant1Id;

        // Act
        var response = await httpClient.DeleteAsync($"/api/tenants/{tenantId}");

        // Assert
        const string expectedBody =
            """{"type":"https://httpstatuses.com/400","title":"Bad Request","status":400,"Errors":[{"code":"Id","message":"All users must be deleted before the tenant can be deleted."}]}""";
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTenant_WhenTenantExistsWithNoUsers_ShouldReturnBadRequest()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();
        var tenantId = DatabaseSeeder.Tenant1Id;
        var _ = await httpClient.DeleteAsync($"/api/users/{DatabaseSeeder.User1Id}");

        // Act
        var response = await httpClient.DeleteAsync($"/api/tenants/{tenantId}");

        // Assert
        response.EnsureSuccessStatusCode();

        response.Content.Headers.ContentType.Should().BeNull();
        response.Headers.Location.Should().BeNull();

        // Verify that is deleted
        var getResponse = await httpClient.GetAsync($"/api/tenants/{tenantId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}