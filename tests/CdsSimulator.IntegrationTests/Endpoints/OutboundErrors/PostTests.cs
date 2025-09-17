using System.Net;
using FluentAssertions;

namespace Defra.TradeImportsCdsSimulator.IntegrationTests.Endpoints.OutboundErrors;

public class PostTests : TestBase.TestBase
{
    private const string SampleOutboundError =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?><NS1:Envelope xmlns:NS1=\"http://www.w3.org/2003/05/soap-envelope\"><NS1:Header><NS2:Security xmlns:NS2=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd\" NS1:role=\"system\"><NS2:UsernameToken><NS2:Username>username</NS2:Username><NS2:Password>password</NS2:Password></NS2:UsernameToken></NS2:Security></NS1:Header><NS1:Body><NS3:HMRCErrorNotification xmlns:NS3=\"http://uk.gov.hmrc.ITSW2.ws\">&lt;NS2:HMRCErrorNotification xmlns:NS2=&quot;http://www.hmrc.gov.uk/webservices/itsw/ws/hmrcerrornotification&quot;&gt;&lt;NS2:ServiceHeader&gt;&lt;NS2:SourceSystem&gt;ALVS&lt;/NS2:SourceSystem&gt;&lt;NS2:DestinationSystem&gt;CDS&lt;/NS2:DestinationSystem&gt;&lt;NS2:CorrelationId&gt;74227759&lt;/NS2:CorrelationId&gt;&lt;NS2:ServiceCallTimestamp&gt;2025-07-08T12:14:01.321&lt;/NS2:ServiceCallTimestamp&gt;&lt;/NS2:ServiceHeader&gt;&lt;NS2:Header&gt;&lt;NS2:SourceCorrelationId&gt;101&lt;/NS2:SourceCorrelationId&gt;&lt;NS2:EntryReference&gt;MRN&lt;/NS2:EntryReference&gt;&lt;NS2:EntryVersionNumber&gt;1&lt;/NS2:EntryVersionNumber&gt;&lt;/NS2:Header&gt;&lt;NS2:Error&gt;&lt;NS2:ErrorCode&gt;ALVSVAL1001&lt;/NS2:ErrorCode&gt;&lt;NS2:ErrorMessage&gt;Error message&lt;/NS2:ErrorMessage&gt;&lt;/NS2:Error&gt;&lt;/NS2:HMRCErrorNotification&gt;</NS3:HMRCErrorNotification></NS1:Body></NS1:Envelope>";

    [Fact]
    public async Task Post_WhenValid_ShouldBeRequestBodyAsResponse()
    {
        var client = CreateHttpClient();

        var response = await client.PostAsync(
            Testing.Endpoints.ErrorNotifications.Post,
            new StringContent(SampleOutboundError)
        );

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}
