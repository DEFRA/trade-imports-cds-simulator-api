namespace Defra.TradeImportsCdsSimulator.Testing;

public static class Endpoints
{
    public static class DecisionNotifications
    {
        public static string Post => "alvsclearance/decisionnotification/v1";
    }

    public static class ErrorNotifications
    {
        public static string Post => "alvsclearance/errornotification/v1";
    }
}
