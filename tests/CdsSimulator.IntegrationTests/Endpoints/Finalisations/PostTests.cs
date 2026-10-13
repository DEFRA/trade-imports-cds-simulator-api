using System.Net;
using System.Text;
using AwesomeAssertions;

namespace Defra.TradeImportsCdsSimulator.IntegrationTests.Endpoints.Finalisations;

public class PostTests : TestBase.TestBase
{
    private const string FinalisationXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Finalisation xmlns="http://notifyfinalisedstatehmrcfacade.types.esb.ws.cara.defra.com">
          <ServiceHeader>
            <SourceSystem>ALVS</SourceSystem>
            <DestinationSystem>CDS</DestinationSystem>
          </ServiceHeader>
          <Header>
            <EntryReference>INTMRN123</EntryReference>
            <EntryVersionNumber>1</EntryVersionNumber>
            <FinalState>1</FinalState>
            <DecisionNumber>1</DecisionNumber>
            <ManualAction>N</ManualAction>
          </Header>
        </Finalisation>
        """;

    [Fact]
    public async Task Post_WhenValidXml_ShouldReturnCreated()
    {
        var client = CreateHttpClient();

        var response = await client.PostAsync(
            Testing.Endpoints.FinalisationNotifications.Post,
            new StringContent(FinalisationXml, Encoding.UTF8, "application/xml")
        );

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
