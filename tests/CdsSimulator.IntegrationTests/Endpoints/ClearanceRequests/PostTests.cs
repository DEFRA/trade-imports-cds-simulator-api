using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using AwesomeAssertions;
using Defra.TradeImportsCdsSimulator.IntegrationTests.Clients;

namespace Defra.TradeImportsCdsSimulator.IntegrationTests.Endpoints.ClearanceRequests
{
    [Collection("UsesWireMockClient")]
    public class PostTests(WireMockClient wireMockClient) : TestBase.TestBase
    {
        private const string Decision = """
            <?xml version="1.0" encoding="UTF-8"?>
            <NS1:Envelope xmlns:NS1="http://www.w3.org/2003/05/soap-envelope">
            	<NS1:Header>
            		<NS2:Security xmlns:NS2="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" NS1:role="system">
            			<NS2:UsernameToken>
            				<NS2:Username>testuser</NS2:Username>
            				<NS2:Password>password</NS2:Password>
            			</NS2:UsernameToken>
            		</NS2:Security>
            	</NS1:Header>
            	<NS1:Body>
                    <ALVSClearanceRequest xmlns="http://submitimportdocumenthmrcfacade.types.esb.ws.cara.defra.com">
            	        <ServiceHeader>
            		        <SourceSystem>CHIEF</SourceSystem>
            		        <DestinationSystem>ALVS</DestinationSystem>
            		        <CorrelationId></CorrelationId>
            		        <ServiceCallTimestamp></ServiceCallTimestamp>
            	        </ServiceHeader>
            	        <Header>
            		        <EntryReference></EntryReference>
            		        <EntryVersionNumber>3</EntryVersionNumber>
            		        <PreviousVersionNumber>2</PreviousVersionNumber>
            		        <DeclarationUCR>1GB116445386000-KDA35932Y1BHX</DeclarationUCR>
            		        <DeclarationType>S</DeclarationType>
            		        <ArrivalDateTime>202201031224</ArrivalDateTime>
            		        <SubmitterTURN>123456789012</SubmitterTURN>
            		        <DeclarantId>GB123456789013</DeclarantId>
            		        <DeclarantName>REDACTED</DeclarantName>
            		        <DispatchCountryCode>CN</DispatchCountryCode>
            		        <GoodsLocationCode>GBSTN</GoodsLocationCode>
            		        <MasterUCR>GB/CNS1-SCT16S6LF00000</MasterUCR>
            	        </Header>
            	        <Item>
            		        <ItemNumber>1</ItemNumber>
            		        <CustomsProcedureCode>4000000</CustomsProcedureCode>
            		        <TaricCommodityCode>2309105100</TaricCommodityCode>
            		        <GoodsDescription>DOG CHEW</GoodsDescription>
            		        <ConsigneeId>GB123456789012</ConsigneeId>
            		        <ConsigneeName>REDACTED</ConsigneeName>
            		        <ItemNetMass>5011.200</ItemNetMass>
            		        <ItemOriginCountryCode>CN</ItemOriginCountryCode>
            		        <Document>
            			        <DocumentCode>N853</DocumentCode>
            			        <DocumentReference>GBCVD2021.1396392</DocumentReference>
            			        <DocumentStatus>AE</DocumentStatus>
            			        <DocumentControl>P</DocumentControl>
            		        </Document>
            		        <Check>
            			        <CheckCode>H222</CheckCode>
            			        <DepartmentCode>PHA</DepartmentCode>
            		        </Check>
            	        </Item>
            	        <Item>
            		        <ItemNumber>2</ItemNumber>
            		        <CustomsProcedureCode>4000000</CustomsProcedureCode>
            		        <TaricCommodityCode>2309103300</TaricCommodityCode>
            		        <GoodsDescription>DOG CHEW</GoodsDescription>
            		        <ConsigneeId>GB123456789012</ConsigneeId>
            		        <ConsigneeName>REDACTED</ConsigneeName>
            		        <ItemNetMass>3198.720</ItemNetMass>
            		        <ItemOriginCountryCode>CN</ItemOriginCountryCode>
            		        <Document>
            			        <DocumentCode>N853</DocumentCode>
            			        <DocumentReference>GBCVD2021.1396392</DocumentReference>
            			        <DocumentStatus>AE</DocumentStatus>
            			        <DocumentControl>P</DocumentControl>
            		        </Document>
            		        <Check>
            			        <CheckCode>H222</CheckCode>
            			        <DepartmentCode>PHA</DepartmentCode>
            		        </Check>
            	        </Item>
            	        <Item>
            		        <ItemNumber>3</ItemNumber>
            		        <CustomsProcedureCode>4000000</CustomsProcedureCode>
            		        <TaricCommodityCode>2309109000</TaricCommodityCode>
            		        <GoodsDescription>DOG CHEWS</GoodsDescription>
            		        <ConsigneeId>GB123456789012</ConsigneeId>
            		        <ConsigneeName>REDACTED</ConsigneeName>
            		        <ItemNetMass>144.000</ItemNetMass>
            		        <ItemOriginCountryCode>CN</ItemOriginCountryCode>
            		        <Document>
            			        <DocumentCode>N853</DocumentCode>
            			        <DocumentReference>GBCVD2021.1396386</DocumentReference>
            			        <DocumentStatus>AE</DocumentStatus>
            			        <DocumentControl>P</DocumentControl>
            		        </Document>
            		        <Check>
            			        <CheckCode>H222</CheckCode>
            			        <DepartmentCode>PHA</DepartmentCode>
            		        </Check>
            	        </Item>
            	        <Item>
            		        <ItemNumber>4</ItemNumber>
            		        <CustomsProcedureCode>4000000</CustomsProcedureCode>
            		        <TaricCommodityCode>2309101100</TaricCommodityCode>
            		        <GoodsDescription>DOG CHEW</GoodsDescription>
            		        <ConsigneeId>GB123456789012</ConsigneeId>
            		        <ConsigneeName>REDACTED</ConsigneeName>
            		        <ItemNetMass>7560.000</ItemNetMass>
            		        <ItemOriginCountryCode>CN</ItemOriginCountryCode>
            		        <Document>
            			        <DocumentCode>N853</DocumentCode>
            			        <DocumentReference>GBCVD2021.1396386</DocumentReference>
            			        <DocumentStatus>AE</DocumentStatus>
            			        <DocumentControl>P</DocumentControl>
            		        </Document>
            		        <Check>
            			        <CheckCode>H222</CheckCode>
            			        <DepartmentCode>PHA</DepartmentCode>
            		        </Check>
            	        </Item>
            	        <Item>
            		        <ItemNumber>5</ItemNumber>
            		        <CustomsProcedureCode>4000000</CustomsProcedureCode>
            		        <TaricCommodityCode>2309103100</TaricCommodityCode>
            		        <GoodsDescription>DOG CHEWS</GoodsDescription>
            		        <ConsigneeId>GB123456789012</ConsigneeId>
            		        <ConsigneeName>REDACTED</ConsigneeName>
            		        <ItemNetMass>518.400</ItemNetMass>
            		        <ItemOriginCountryCode>CN</ItemOriginCountryCode>
            		        <Document>
            			        <DocumentCode>N853</DocumentCode>
            			        <DocumentReference>GBCVD2021.1396386</DocumentReference>
            			        <DocumentStatus>AE</DocumentStatus>
            			        <DocumentControl>P</DocumentControl>
            		        </Document>
            		        <Check>
            			        <CheckCode>H222</CheckCode>
            			        <DepartmentCode>PHA</DepartmentCode>
            		        </Check>
            	        </Item>
            	        <Item>
            		        <ItemNumber>6</ItemNumber>
            		        <CustomsProcedureCode>4000000</CustomsProcedureCode>
            		        <TaricCommodityCode>2309103100</TaricCommodityCode>
            		        <GoodsDescription>DOG CHEWS</GoodsDescription>
            		        <ConsigneeId>GB123456789012</ConsigneeId>
            		        <ConsigneeName>REDACTED</ConsigneeName>
            		        <ItemNetMass>8985.600</ItemNetMass>
            		        <ItemOriginCountryCode>CN</ItemOriginCountryCode>
            		        <Document>
            			        <DocumentCode>N853</DocumentCode>
            			        <DocumentReference>GBCVD2021.1396399</DocumentReference>
            			        <DocumentStatus>AE</DocumentStatus>
            			        <DocumentControl>P</DocumentControl>
            		        </Document>
            		        <Check>
            			        <CheckCode>H222</CheckCode>
            			        <DepartmentCode>PHA</DepartmentCode>
            		        </Check>
            	        </Item>
                    </ALVSClearanceRequest>
            	</NS1:Body>
            </NS1:Envelope>
            """;

        [Fact]
        public async Task Post_WhenValid_ShouldBeRequestBodyAsResponse()
        {
            await wireMockClient.ResetAsync();
            await wireMockClient.StubBtmsClearanceRequestAsync();

            using var client = CreateHttpClient();

            var response = await client.PostAsync(
                Testing.Endpoints.ClearanceRequests.Post,
                new StringContent(Decision, Encoding.UTF8, "text/xml")
            );

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var btmsRequestPosted = await wireMockClient.WasBtmsClearanceRequestPostedAsync();
            btmsRequestPosted.Should().BeTrue();
        }
    }
}
