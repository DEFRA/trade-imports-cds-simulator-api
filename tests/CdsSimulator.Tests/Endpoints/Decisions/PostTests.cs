using System.Net;
using Defra.TradeImportsCdsSimulator.Data;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit.Abstractions;

namespace Defra.TradeImportsCdsSimulator.Tests.Endpoints.Decisions;

public class PostTests(SimulatorWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestBase(factory, outputHelper)
{
    private IDbContext MockDbContext { get; } = Substitute.For<IDbContext>();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        services.AddTransient(_ => MockDbContext);
    }

    [Fact]
    public async Task Post_WhenUnauthorized_ShouldBeUnauthorized()
    {
        var client = CreateClient(addDefaultAuthorizationHeader: false);

        var response = await client.PostAsync(
            Testing.Endpoints.DecisionNotifications.Post,
            new StringContent("<xml />")
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_WhenReadOnly_ShouldBeForbidden()
    {
        var client = CreateClient(testUser: TestUser.ReadOnly);

        var response = await client.PostAsync(
            Testing.Endpoints.DecisionNotifications.Post,
            new StringContent("<xml />")
        );

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_WhenValid_ShouldBeRequestBodyAsResponse()
    {
        var client = CreateClient();

        var response = await client.PostAsync(
            Testing.Endpoints.DecisionNotifications.Post,
            new StringContent("<xml alvs=\"true\" />")
        );

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}
