using System.Net;
using System.Text.Json;
using System.Xml.Serialization;
using AwesomeAssertions;
using BtmsGateway.Contract.CustomsDeclarations;
using Defra.TradeImportsCdsSimulator.Data;
using Defra.TradeImportsCdsSimulator.Tests.Utils.InMemoryData;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using DbEntities = Defra.TradeImportsCdsSimulator.Data.Entities;

namespace Defra.TradeImportsCdsSimulator.Tests.Endpoints.Control.Declarations;

public class GetTests(SimulatorWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : EndpointTestBase(factory, outputHelper)
{
    private static readonly DateTime s_start = new(2026, 10, 22, 9, 0, 0, DateTimeKind.Utc);

    private MemoryDbContext MockDbContext { get; } = new();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        base.ConfigureTestServices(services);

        services.AddSingleton<IDbContext>(_ => MockDbContext);
    }

    // Stored as the simulator stores a request it was sent: the contract, serialised to XML.
    private void AddRequest(string mrn, byte version, int minutes, string? ucr = null)
    {
        var request = new ClearanceRequest
        {
            ServiceHeader = new ServiceHeader { SourceSystem = "CDS", DestinationSystem = "ALVS" },
            Header = new ClearanceRequestHeader
            {
                EntryReference = mrn,
                EntryVersionNumber = version,
                DeclarationUcr = ucr,
            },
            Items = [new Item { ItemNumber = 1, TaricCommodityCode = "0201100021" }],
        };
        using var writer = new StringWriter();
        new XmlSerializer(typeof(ClearanceRequest)).Serialize(writer, request);

        MockDbContext.ClearanceRequests.AddTestData(
            new DbEntities.ClearanceRequest
            {
                Id = $"{mrn}-{version}",
                Mrn = mrn,
                EntryVersionNumber = version,
                Timestamp = s_start.AddMinutes(minutes),
                Xml = writer.ToString(),
            }
        );
    }

    [Fact]
    public async Task List_WhenUnauthorized_ShouldBeUnauthorized()
    {
        var client = CreateClient(addDefaultAuthorizationHeader: false);

        var response = await client.GetAsync(Testing.Endpoints.ClearanceRequests.List());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_WhenWriteOnly_ShouldBeForbidden()
    {
        var client = CreateClient(testUser: TestUser.WriteOnly);

        var response = await client.GetAsync(Testing.Endpoints.ClearanceRequests.List());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_ShouldGiveOneRowPerMrn_TheMostRecentlySentFirst_APageAtATime()
    {
        AddRequest("26GBAAAAAAAAAAAAAA", 1, 0);
        AddRequest("26GBBBBBBBBBBBBBBB", 1, 1);
        AddRequest("26GBAAAAAAAAAAAAAA", 2, 2);
        AddRequest("26GBCCCCCCCCCCCCCC", 1, 3);
        var client = CreateClient();

        using var first = JsonDocument.Parse(
            await client.GetStringAsync(Testing.Endpoints.ClearanceRequests.List(pageSize: 2))
        );
        using var second = JsonDocument.Parse(
            await client.GetStringAsync(Testing.Endpoints.ClearanceRequests.List(page: 2, pageSize: 2))
        );

        Rows(first).Should().Equal(("26GBCCCCCCCCCCCCCC", 1), ("26GBAAAAAAAAAAAAAA", 2));
        first.RootElement.GetProperty("hasMore").GetBoolean().Should().BeTrue();
        Rows(second).Should().Equal(("26GBBBBBBBBBBBBBBB", 1));
        second.RootElement.GetProperty("hasMore").GetBoolean().Should().BeFalse();
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task List_WhenPageOutOfRange_ShouldBeBadRequest(int page, int pageSize)
    {
        var client = CreateClient();

        var response = await client.GetAsync(Testing.Endpoints.ClearanceRequests.List(page, pageSize));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_WhenNoSuchMrn_ShouldBeNotFound()
    {
        var client = CreateClient();

        var response = await client.GetAsync(Testing.Endpoints.ClearanceRequests.Get("26GBNOTHERE0000000"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldGiveEachVersionAsTheContractsJson_AndWhatBtmsSentBack()
    {
        AddRequest("26GBAAAAAAAAAAAAAA", 1, 0, ucr: "6GB999000000001-ABCDEFGH");
        AddRequest("26GBAAAAAAAAAAAAAA", 2, 5, ucr: "6GB999000000001-ABCDEFGH");
        MockDbContext.DecisionNotifications.AddTestData(
            new DbEntities.Notification
            {
                Id = "decision",
                Mrn = "26GBAAAAAAAAAAAAAA",
                Timestamp = s_start.AddMinutes(1),
                Xml = """
                <Envelope><Body><DecisionNotification><Header><EntryReference>26GBAAAAAAAAAAAAAA</EntryReference><EntryVersionNumber>1</EntryVersionNumber><DecisionNumber>1</DecisionNumber></Header><Item><ItemNumber>1</ItemNumber><Check><CheckCode>H222</CheckCode><DecisionCode>X00</DecisionCode></Check></Item></DecisionNotification></Body></Envelope>
                """,
            }
        );
        MockDbContext.ErrorNotifications.AddTestData(
            new DbEntities.Notification
            {
                Id = "error",
                Mrn = "26GBAAAAAAAAAAAAAA",
                Timestamp = s_start.AddMinutes(6),
                Xml = """
                <Envelope><Body><HMRCErrorNotification><Header><EntryVersionNumber>2</EntryVersionNumber></Header><Error><ErrorCode>ERR022</ErrorCode><ErrorMessage>Net Mass</ErrorMessage></Error></HMRCErrorNotification></Body></Envelope>
                """,
            }
        );
        var client = CreateClient();

        using var json = JsonDocument.Parse(
            await client.GetStringAsync(Testing.Endpoints.ClearanceRequests.Get("26GBAAAAAAAAAAAAAA"))
        );
        var root = json.RootElement;

        root.GetProperty("mrn").GetString().Should().Be("26GBAAAAAAAAAAAAAA");
        var versions = root.GetProperty("versions").EnumerateArray().ToList();
        versions.Select(v => v.GetProperty("entryVersionNumber").GetInt32()).Should().Equal(1, 2);
        var header = versions[1].GetProperty("request").GetProperty("header");
        header.GetProperty("declarationUCR").GetString().Should().Be("6GB999000000001-ABCDEFGH");
        header.GetProperty("entryVersionNumber").GetInt32().Should().Be(2);
        root.GetProperty("finalisations").GetArrayLength().Should().Be(0);

        var check = root.GetProperty("decisions")[0].GetProperty("items")[0].GetProperty("checks")[0];
        check.GetProperty("checkCode").GetString().Should().Be("H222");
        check.GetProperty("decisionCode").GetString().Should().Be("X00");
        var error = root.GetProperty("errors")[0];
        error.GetProperty("entryVersionNumber").GetInt32().Should().Be(2);
        error.GetProperty("errors")[0].GetProperty("errorCode").GetString().Should().Be("ERR022");
    }

    private static IEnumerable<(string, int)> Rows(JsonDocument page) =>
        page
            .RootElement.GetProperty("items")
            .EnumerateArray()
            .Select(row => (row.GetProperty("mrn").GetString()!, row.GetProperty("entryVersionNumber").GetInt32()));
}
