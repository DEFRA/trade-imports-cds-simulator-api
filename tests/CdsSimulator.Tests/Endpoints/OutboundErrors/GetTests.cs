using System.Net;
using Defra.TradeImportsCdsSimulator.Data;
using Defra.TradeImportsCdsSimulator.Data.Entities;
using Defra.TradeImportsCdsSimulator.Tests.Utils.InMemoryData;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Abstractions;

namespace Defra.TradeImportsCdsSimulator.Tests.Endpoints.OutboundErrors;

public class GetTests(SimulatorWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestBase(factory, outputHelper)
{
    private MemoryDbContext MockDbContext { get; } = new MemoryDbContext();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        services.AddSingleton<IDbContext>(_ => MockDbContext);
    }

    [Fact]
    public async Task Get_WhenUnauthorized_ShouldBeUnauthorized()
    {
        var client = CreateClient(addDefaultAuthorizationHeader: false);

        var response = await client.GetAsync(Testing.Endpoints.ErrorNotifications.Get("mrn", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_WhenReadOnly_ShouldBeForbidden()
    {
        var client = CreateClient(testUser: TestUser.WriteOnly);

        var response = await client.GetAsync(Testing.Endpoints.ErrorNotifications.Get("mrn", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_WhenNoParameters_ShouldBeBadRequest()
    {
        var client = CreateClient();

        var response = await client.GetAsync(Testing.Endpoints.ErrorNotifications.Get(null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await VerifyJson(await response.Content.ReadAsStringAsync()).ScrubMember("traceId");
    }

    [Fact]
    public async Task Get_WhenFromGreaterThanTo_ShouldBeBadRequest()
    {
        var client = CreateClient();

        var response = await client.GetAsync(
            Testing.Endpoints.ErrorNotifications.Get(null, DateTime.UtcNow.AddDays(1), DateTime.UtcNow)
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await VerifyJson(await response.Content.ReadAsStringAsync()).ScrubMember("traceId");
    }

    [Fact]
    public async Task Get_WhenValidAndNoResults_ShouldBeRequestBodyAsResponse()
    {
        var client = CreateClient();

        var response = await client.GetAsync(
            Testing.Endpoints.ErrorNotifications.Get(null, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow)
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await VerifyJson(await response.Content.ReadAsStringAsync()).ScrubMember("traceId");
    }

    [Fact]
    public async Task Get_WhenValidAndResults_ShouldBeRequestBodyAsResponse()
    {
        MockDbContext.ErrorNotifications.AddTestData(
            new ErrorNotification()
            {
                Mrn = "test",
                Timestamp = DateTime.UtcNow.AddSeconds(-10),
                Id = "test1",
                Xml = "test xml",
            }
        );
        var client = CreateClient();

        var response = await client.GetAsync(
            Testing.Endpoints.ErrorNotifications.Get(null, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow)
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await VerifyJson(await response.Content.ReadAsStringAsync()).ScrubMember("traceId");
    }
}
