using AwesomeAssertions;
using Defra.TradeImportsCdsSimulator.Endpoints.Control.Declarations;

namespace Defra.TradeImportsCdsSimulator.Tests.Endpoints.Control.Declarations;

// The XML is what the BTMS gateway sent a local simulator, as it stored it.
public class NotificationsTests
{
    private static readonly DateTime s_received = new(2026, 10, 22, 10, 24, 33, DateTimeKind.Utc);

    private const string Decision = """
        <?xml version="1.0" encoding="UTF-8"?><NS1:Envelope xmlns:NS1="http://www.w3.org/2003/05/soap-envelope"><NS1:Header><NS2:Security xmlns:NS2="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" NS1:role="system"><NS2:UsernameToken><NS2:Username>default-username</NS2:Username><NS2:Password>default-password</NS2:Password></NS2:UsernameToken></NS2:Security></NS1:Header><NS1:Body><NS3:DecisionNotification xmlns:NS3="http://uk.gov.hmrc.ITSW2.ws"><NS2:DecisionNotification xmlns:NS2="http://www.hmrc.gov.uk/webservices/itsw/ws/decisionnotification"><NS2:ServiceHeader><NS2:SourceSystem>ALVS</NS2:SourceSystem><NS2:DestinationSystem>CDS</NS2:DestinationSystem><NS2:CorrelationId>17925084515987626790</NS2:CorrelationId><NS2:ServiceCallTimestamp>2026-10-21T07:28:33.231</NS2:ServiceCallTimestamp></NS2:ServiceHeader><NS2:Header><NS2:EntryReference>26GB0P19E7LGB46A2V</NS2:EntryReference><NS2:EntryVersionNumber>1</NS2:EntryVersionNumber><NS2:DecisionNumber>1</NS2:DecisionNumber></NS2:Header><NS2:Item><NS2:ItemNumber>1</NS2:ItemNumber><NS2:Check><NS2:CheckCode>H222</NS2:CheckCode><NS2:DecisionCode>X00</NS2:DecisionCode><NS2:DecisionReason>A Customs Declaration has been submitted however no matching CVEDP(s) have been submitted to Port Health for CVEDP number(s) GBCVD2026.1396392.</NS2:DecisionReason></NS2:Check></NS2:Item></NS2:DecisionNotification></NS3:DecisionNotification></NS1:Body></NS1:Envelope>
        """;

    private const string Error = """
        <?xml version="1.0" encoding="UTF-8"?><NS1:Envelope xmlns:NS1="http://www.w3.org/2003/05/soap-envelope"><NS1:Header><NS2:Security xmlns:NS2="http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd" NS1:role="system"><NS2:UsernameToken><NS2:Username>default-username</NS2:Username><NS2:Password>default-password</NS2:Password></NS2:UsernameToken></NS2:Security></NS1:Header><NS1:Body><NS3:HMRCErrorNotification xmlns:NS3="http://uk.gov.hmrc.ITSW2.ws"><NS2:HMRCErrorNotification xmlns:NS2="http://www.hmrc.gov.uk/webservices/itsw/ws/hmrcerrornotification"><NS2:ServiceHeader><NS2:SourceSystem>ALVS</NS2:SourceSystem><NS2:DestinationSystem>CDS</NS2:DestinationSystem><NS2:CorrelationId>17926636615618204252</NS2:CorrelationId><NS2:ServiceCallTimestamp>2026-10-22T10:07:41.561</NS2:ServiceCallTimestamp></NS2:ServiceHeader><NS2:Header><NS2:SourceCorrelationId>17926636605423372032</NS2:SourceCorrelationId><NS2:EntryReference>26GBO8PNYLZNFEVB47</NS2:EntryReference><NS2:EntryVersionNumber>1</NS2:EntryVersionNumber></NS2:Header><NS2:Error><NS2:ErrorCode>ALVSVAL313</NS2:ErrorCode><NS2:ErrorMessage>The DeclarationUCR field must have a value.</NS2:ErrorMessage></NS2:Error><NS2:Error><NS2:ErrorCode>ERR022</NS2:ErrorCode><NS2:ErrorMessage>There is an issue with the Net Mass.</NS2:ErrorMessage></NS2:Error></NS2:HMRCErrorNotification></NS3:HMRCErrorNotification></NS1:Body></NS1:Envelope>
        """;

    [Fact]
    public void Decision_ShouldReadItsVersionNumberAndEachChecksDecision()
    {
        var decision = Notifications.ParseDecision(s_received, Decision.Trim());

        decision
            .Should()
            .BeEquivalentTo(
                new Decision(
                    s_received,
                    1,
                    1,
                    [
                        new DecisionItem(
                            1,
                            [
                                new DecisionCheck(
                                    "H222",
                                    "X00",
                                    "A Customs Declaration has been submitted however no matching CVEDP(s) have been submitted to Port Health for CVEDP number(s) GBCVD2026.1396392."
                                ),
                            ]
                        ),
                    ]
                )
            );
    }

    [Fact]
    public void Decision_ShouldHaveNoItems_WhenNothingWasChecked()
    {
        var xml = Decision.Trim();
        var withoutItems =
            xml[..xml.IndexOf("<NS2:Item>", StringComparison.Ordinal)]
            + "</NS2:DecisionNotification></NS3:DecisionNotification></NS1:Body></NS1:Envelope>";

        Notifications.ParseDecision(s_received, withoutItems).Items.Should().BeEmpty();
    }

    [Fact]
    public void Error_ShouldReadItsVersionAndEveryError()
    {
        var error = Notifications.ParseError(s_received, Error.Trim());

        error.EntryVersionNumber.Should().Be(1);
        error
            .Errors.Should()
            .Equal(
                new ErrorDetail("ALVSVAL313", "The DeclarationUCR field must have a value."),
                new ErrorDetail("ERR022", "There is an issue with the Net Mass.")
            );
    }
}
