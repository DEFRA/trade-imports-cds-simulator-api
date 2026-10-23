using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Defra.TradeImportsCdsSimulator.Data;
using Defra.TradeImportsCdsSimulator.Tests.Utils.InMemoryData;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Defra.TradeImportsCdsSimulator.Tests.Endpoints.Control.Scenarios;

public class ScenarioTests(SimulatorWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestBase(factory, outputHelper)
{
    private MemoryDbContext MockDbContext { get; } = new();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        services.AddSingleton<IDbContext>(_ => MockDbContext);
    }

    private static object Scenario(string name, string ucr) =>
        new { name, model = new { header = new { declarationUCR = ucr } } };

    [Fact]
    public async Task List_WhenUnauthorized_ShouldBeUnauthorized()
    {
        var client = CreateClient(addDefaultAuthorizationHeader: false);

        var response = await client.GetAsync(Testing.Endpoints.Scenarios.List);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_WhenReadOnly_ShouldBeForbidden()
    {
        var client = CreateClient(testUser: TestUser.ReadOnly);

        var response = await client.PutAsJsonAsync(Testing.Endpoints.Scenarios.Put("bison"), Scenario("Bison", "A"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Put_ShouldSave_AndListByName_ReplacingOneWithTheSameId()
    {
        var client = CreateClient();

        (await client.PutAsJsonAsync(Testing.Endpoints.Scenarios.Put("horses"), Scenario("Horses", "A")))
            .StatusCode.Should()
            .Be(HttpStatusCode.OK);
        await client.PutAsJsonAsync(Testing.Endpoints.Scenarios.Put("bison"), Scenario("Bison", "B"));
        await client.PutAsJsonAsync(Testing.Endpoints.Scenarios.Put("horses"), Scenario("Horses", "C"));

        using var json = JsonDocument.Parse(await client.GetStringAsync(Testing.Endpoints.Scenarios.List));
        var scenarios = json.RootElement.EnumerateArray().ToList();

        scenarios.Select(s => s.GetProperty("id").GetString()).Should().Equal("bison", "horses");
        scenarios[1]
            .GetProperty("model")
            .GetProperty("header")
            .GetProperty("declarationUCR")
            .GetString()
            .Should()
            .Be("C");
    }

    [Theory]
    [InlineData("Not A Slug")]
    [InlineData("trailing-")]
    public async Task Put_WhenIdIsNotASlug_ShouldBeBadRequest(string id)
    {
        var client = CreateClient();

        var response = await client.PutAsJsonAsync(Testing.Endpoints.Scenarios.Put(id), Scenario("X", "A"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_WhenNoName_ShouldBeBadRequest()
    {
        var client = CreateClient();

        var response = await client.PutAsJsonAsync(
            Testing.Endpoints.Scenarios.Put("bison"),
            new { name = " ", model = new { } }
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
