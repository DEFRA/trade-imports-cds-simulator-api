using System.Net;
using FluentAssertions;

namespace Defra.TradeImportsCdsSimulator.IntegrationTests.Endpoints.OutboundErrors;

public class PostTests : TestBase.TestBase
{
    private const string Decision = """
        <soap:Envelope xmlns:soap="http://www.w3.org/2003/05/soap-envelope"
            xmlns:oas="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd">
            <soap:Header>
                <oas:Security soap:role="system" soap:mustUnderstand="true">
                    <oas:UsernameToken>
                    </oas:UsernameToken>
                </oas:Security>
            </soap:Header>
            <soap:Body>
                <ALVSClearanceRequest xmlns="http://submitimportdocumenthmrcfacade.types.esb.ws.cara.defra.com">
                    <ServiceHeader>
                        <SourceSystem>CDS</SourceSystem>
                        <DestinationSystem>ALVS</DestinationSystem>
                        <CorrelationId>223466889015530205</CorrelationId>
                        <ServiceCallTimestamp>2026-09-16T12:00:01.001Z</ServiceCallTimestamp>
                    </ServiceHeader>
                    <Header>
                        <EntryReference>26GB09160000000002</EntryReference>
                        <EntryVersionNumber>6</EntryVersionNumber>
                        <PreviousVersionNumber>5</PreviousVersionNumber>
                        <DeclarationPartNumber>A56</DeclarationPartNumber>
                        <DeclarationUCR>4GB979990100916-0162-25</DeclarationUCR>
                        <DeclarationType>S</DeclarationType>
                        <SubmitterTURN>GB998038429000</SubmitterTURN>
                        <DeclarantId>GB998038429000</DeclarantId>
                        <DeclarantName>GB998038429000</DeclarantName>
                        <DispatchCountryCode>MA</DispatchCountryCode>
                        <GoodsLocationCode>DEUDEUDEU</GoodsLocationCode>
                    </Header>
                    <Item>
                        <ItemNumber>1</ItemNumber>
                        <CustomsProcedureCode>4000000</CustomsProcedureCode>
                        <TaricCommodityCode>0201100000</TaricCommodityCode>
                        <GoodsDescription>Test</GoodsDescription>
                        <ConsigneeId>GB991900746000</ConsigneeId>
                        <ConsigneeName>GB991900746000</ConsigneeName>
                        <ItemNetMass>1000</ItemNetMass>
                        <ItemOriginCountryCode>MA</ItemOriginCountryCode>
                        <Document>
                            <DocumentCode>N853</DocumentCode>
                            <DocumentReference>CHEDP.XI.2026.0000834</DocumentReference>
                            <DocumentStatus>AE</DocumentStatus>
                            <DocumentControl>P</DocumentControl>
                        </Document>
                        <Check>
                            <CheckCode>H222</CheckCode>
                            <DepartmentCode>PHA</DepartmentCode>
                        </Check>
                    </Item>
                 </ALVSClearanceRequest>
            </soap:Body>
        </soap:Envelope>
        """;

    [Fact]
    public async Task Post_WhenValid_ShouldBeRequestBodyAsResponse()
    {
        var client = CreateHttpClient();

        var response = await client.PostAsync(
            Testing.Endpoints.DecisionNotifications.Post,
            new StringContent(Decision)
        );

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
