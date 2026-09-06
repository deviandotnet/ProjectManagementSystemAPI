using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PMS.API;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Projects.ExportProjectExcel;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;

namespace PMS.IntegrationTests.Security;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SecurityTestCollection
{
    public const string Name = "Security integration tests";
}

[Collection(SecurityTestCollection.Name)]
public sealed class SecurityIntegrationTests
{
    private const string AllowedOrigin = "http://localhost:5173";

    [Fact]
    public async Task Cors_Should_AllowConfiguredOriginAndRejectUnknownOrigin()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(
            "Testing",
            new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = AllowedOrigin
            });
        using HttpClient client = CreateClient(factory);

        using var allowedRequest = CreatePreflightRequest(AllowedOrigin);
        using HttpResponseMessage allowedResponse = await client.SendAsync(allowedRequest);

        allowedResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        allowedResponse.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle(AllowedOrigin);
        allowedResponse.Headers.GetValues("Access-Control-Allow-Methods").Should().Contain("POST");

        using var rejectedRequest = CreatePreflightRequest("https://untrusted.example");
        using HttpResponseMessage rejectedResponse = await client.SendAsync(rejectedRequest);

        rejectedResponse.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    [Fact]
    public void Cors_Should_FailStartup_WhenConfiguredOriginIsMalformed()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(
            "Testing",
            new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "https://example.com/application/"
            });

        Action startApplication = () => factory.CreateClient();

        startApplication.Should().Throw<Exception>()
            .WithMessage("*CORS origins must use scheme://host[:port]*");
    }

    [Theory]
    [InlineData("RateLimiting:Global:PermitLimit")]
    [InlineData("RateLimiting:Global:WindowInSeconds")]
    [InlineData("RateLimiting:Authentication:PermitLimit")]
    [InlineData("RateLimiting:Authentication:WindowInSeconds")]
    [InlineData("RateLimiting:Authentication:SegmentsPerWindow")]
    [InlineData("RateLimiting:Export:ConcurrencyLimit")]
    [InlineData("RateLimiting:Export:PermitLimit")]
    [InlineData("RateLimiting:Export:WindowInSeconds")]
    public void RateLimiting_Should_FailStartup_WhenConfiguredValueIsNotPositive(string configurationKey)
    {
        using WebApplicationFactory<Program> factory = CreateFactory(
            "Testing",
            new Dictionary<string, string?>
            {
                [configurationKey] = "0"
            });

        Action startApplication = () => factory.CreateClient();

        startApplication.Should().Throw<Exception>()
            .WithMessage("*All rate-limit values must be greater than zero*");
    }

    [Fact]
    public async Task GlobalLimiter_Should_ReturnProblemDetailsAndRetryAfter_WithoutSharingDifferentIps()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(
            "Testing",
            new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = AllowedOrigin,
                ["RateLimiting:Global:PermitLimit"] = "2",
                ["RateLimiting:Global:WindowInSeconds"] = "60"
            });
        using HttpClient client = CreateClient(factory);

        using HttpResponseMessage first = await SendFromIpAsync(client, "/health/live", "203.0.113.10");
        using HttpResponseMessage second = await SendFromIpAsync(client, "/health/live", "203.0.113.10");
        using HttpResponseMessage rejected = await SendFromIpAsync(
            client,
            "/health/live",
            "203.0.113.10",
            AllowedOrigin);
        using HttpResponseMessage otherIp = await SendFromIpAsync(client, "/health/live", "203.0.113.11");

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertRateLimitedAsync(rejected, expectCors: true);
        otherIp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthenticationLimiter_Should_ApplySharedStricterIpLimitToRegisterAndLogin()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory(
            "Testing",
            new Dictionary<string, string?>
            {
                ["RateLimiting:Global:PermitLimit"] = "100",
                ["RateLimiting:Authentication:PermitLimit"] = "2",
                ["RateLimiting:Authentication:WindowInSeconds"] = "60",
                ["RateLimiting:Authentication:SegmentsPerWindow"] = "6"
            });
        using HttpClient client = CreateClient(factory);

        using HttpResponseMessage register = await PostJsonFromIpAsync(client, "/api/users", "198.51.100.20");
        using HttpResponseMessage login = await PostJsonFromIpAsync(client, "/api/users/login", "198.51.100.20");
        using HttpResponseMessage rejected = await PostJsonFromIpAsync(client, "/api/users/login", "198.51.100.20");
        using HttpResponseMessage otherIp = await PostJsonFromIpAsync(client, "/api/users/login", "198.51.100.21");

        register.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        login.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        await AssertRateLimitedAsync(rejected);
        otherIp.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ExportLimiter_Should_RejectFourthConcurrentRequestPerUser()
    {
        var handler = new ControlledExportHandler(block: true);
        await using WebApplicationFactory<Program> factory = CreateExportFactory(handler, exportPermitLimit: 10);
        using HttpClient client = CreateAuthenticatedClient(factory, "user-one");

        Task<HttpResponseMessage>[] activeRequests = Enumerable.Range(0, 3)
            .Select(_ => client.GetAsync($"/api/projects/{Guid.NewGuid()}/export/excel"))
            .ToArray();

        await handler.WaitUntilThreeRequestsEnteredAsync();
        using HttpResponseMessage rejected = await client.GetAsync(
            $"/api/projects/{Guid.NewGuid()}/export/excel");

        await AssertRateLimitedAsync(rejected);

        handler.Release();
        HttpResponseMessage[] completed = await Task.WhenAll(activeRequests);
        try
        {
            completed.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.OK);
        }
        finally
        {
            foreach (HttpResponseMessage response in completed)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task ExportLimiter_Should_EnforceMinuteLimitPerUser()
    {
        var handler = new ControlledExportHandler(block: false);
        await using WebApplicationFactory<Program> factory = CreateExportFactory(handler, exportPermitLimit: 2);
        using HttpClient firstUser = CreateAuthenticatedClient(factory, "user-one");
        using HttpClient secondUser = CreateAuthenticatedClient(factory, "user-two");

        using HttpResponseMessage first = await firstUser.GetAsync($"/api/projects/{Guid.NewGuid()}/export/excel");
        using HttpResponseMessage second = await firstUser.GetAsync($"/api/projects/{Guid.NewGuid()}/export/excel");
        using HttpResponseMessage rejected = await firstUser.GetAsync($"/api/projects/{Guid.NewGuid()}/export/excel");
        using HttpResponseMessage independentUser = await secondUser.GetAsync($"/api/projects/{Guid.NewGuid()}/export/excel");

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertRateLimitedAsync(rejected);
        independentUser.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Scalar_Should_BeAvailableOnlyInDevelopment_AndUseGlobalLimiter()
    {
        await using WebApplicationFactory<Program> developmentFactory = CreateFactory(
            "Development",
            new Dictionary<string, string?>
            {
                ["RateLimiting:Global:PermitLimit"] = "1"
            });
        using HttpClient developmentClient = CreateClient(developmentFactory);

        using HttpResponseMessage scalar = await developmentClient.GetAsync("/scalar/v1");
        using HttpResponseMessage limitedOpenApi = await developmentClient.GetAsync("/openapi/v1.json");

        scalar.StatusCode.Should().Be(HttpStatusCode.OK);
        await AssertRateLimitedAsync(limitedOpenApi);

        await using WebApplicationFactory<Program> productionFactory = CreateFactory("Production");
        using HttpClient productionClient = CreateClient(productionFactory);

        using HttpResponseMessage productionScalar = await productionClient.GetAsync("/scalar/v1");
        using HttpResponseMessage productionOpenApi = await productionClient.GetAsync("/openapi/v1.json");

        productionScalar.StatusCode.Should().Be(HttpStatusCode.NotFound);
        productionOpenApi.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static WebApplicationFactory<Program> CreateExportFactory(
        ControlledExportHandler handler,
        int exportPermitLimit) => CreateFactory(
            "Testing",
            new Dictionary<string, string?>
            {
                ["RateLimiting:Global:PermitLimit"] = "100",
                ["RateLimiting:Export:ConcurrencyLimit"] = "3",
                ["RateLimiting:Export:PermitLimit"] = exportPermitLimit.ToString(),
                ["RateLimiting:Export:WindowInSeconds"] = "60"
            },
            services =>
            {
                services.RemoveAll<IQueryHandler<ExportProjectExcelQuery, ExportProjectExcelResponse>>();
                services.AddSingleton<IQueryHandler<ExportProjectExcelQuery, ExportProjectExcelResponse>>(handler);
                services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthenticationHandler.TestScheme;
                        options.DefaultChallengeScheme = TestAuthenticationHandler.TestScheme;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        TestAuthenticationHandler.TestScheme,
                        _ => { });
            });

    private static WebApplicationFactory<Program> CreateFactory(
        string environment,
        IReadOnlyDictionary<string, string?>? settings = null,
        Action<IServiceCollection>? configureServices = null)
    {
        string databaseName = Guid.NewGuid().ToString();

        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(settings ?? new Dictionary<string, string?>()));
            builder.ConfigureServices(services =>
            {
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(databaseName)
                    .Options;

                services.AddSingleton(options);
                services.AddScoped(_ => new ApplicationDbContext(options));
                configureServices?.Invoke(services);
            });
        });
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    private static HttpClient CreateAuthenticatedClient(
        WebApplicationFactory<Program> factory,
        string userId)
    {
        HttpClient client = CreateClient(factory);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userId);
        return client;
    }

    private static HttpRequestMessage CreatePreflightRequest(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/users");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,authorization");
        return request;
    }

    private static async Task<HttpResponseMessage> SendFromIpAsync(
        HttpClient client,
        string path,
        string ipAddress,
        string? origin = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Forwarded-For", ipAddress);
        request.Headers.Add("X-Forwarded-Proto", "https");
        if (origin is not null)
        {
            request.Headers.Add("Origin", origin);
        }

        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostJsonFromIpAsync(
        HttpClient client,
        string path,
        string ipAddress)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(new { })
        };
        request.Headers.Add("X-Forwarded-For", ipAddress);
        request.Headers.Add("X-Forwarded-Proto", "https");
        return await client.SendAsync(request);
    }

    private static async Task AssertRateLimitedAsync(
        HttpResponseMessage response,
        bool expectCors = false)
    {
        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter.Should().NotBeNull();
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        string body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Too Many Requests");

        if (expectCors)
        {
            response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle(AllowedOrigin);
            response.Headers.GetValues("Access-Control-Expose-Headers").Should().Contain(RetryAfterHeaderName);
        }
    }

    private const string RetryAfterHeaderName = "Retry-After";

    private sealed class ControlledExportHandler(bool block)
        : IQueryHandler<ExportProjectExcelQuery, ExportProjectExcelResponse>
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _threeRequestsEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _requestCount;

        public async Task<Result<ExportProjectExcelResponse>> Handle(
            ExportProjectExcelQuery query,
            CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _requestCount) >= 3)
            {
                _threeRequestsEntered.TrySetResult();
            }

            if (block)
            {
                await _release.Task.WaitAsync(cancellationToken);
            }

            return Result.Success(new ExportProjectExcelResponse
            {
                FileContent = [1, 2, 3],
                FileName = "project.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            });
        }

        internal Task WaitUntilThreeRequestsEnteredAsync() =>
            _threeRequestsEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));

        internal void Release() => _release.TrySetResult();
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        internal const string TestScheme = "SecurityTests";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string userId = Request.Headers.Authorization.ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault() ?? "test-user";
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId)],
                TestScheme);
            var principal = new ClaimsPrincipal(identity);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, TestScheme)));
        }
    }
}
