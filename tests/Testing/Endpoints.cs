using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Defra.TradeImportsCdsSimulator.Testing;

public static class Endpoints
{
    public static class ClearanceRequests
    {
        public static string Post => "ws/CDS/defra/alvsclearancerequestinbound/v1";
        
        public static string Put => "ws/CDS/defra/alvsclearancerequestinbound/v1";
    }

    public static class DecisionNotifications
    {
        public static string Post => "ws/CDS/defra/alvsclearanceinbound/v1";

        public static string Get(string? mrn, DateTime? from, DateTime? to)
        {
            var queryString = QueryString.Empty;

            if (!string.IsNullOrEmpty(mrn))
            {
                queryString = queryString.Add(nameof(mrn), mrn);
            }

            if (from.HasValue)
            {
                queryString = queryString.Add(nameof(from), from.Value.ToString("O", CultureInfo.InvariantCulture));
            }

            if (to.HasValue)
            {
                queryString = queryString.Add(nameof(to), to.Value.ToString("O", CultureInfo.InvariantCulture));
            }

            return $"decision-notifications{queryString.Value}";
        }
    }

    public static class ErrorNotifications
    {
        public static string Post => "ws/CDS/defra/alvsclearanceinbound/v1";

        public static string Get(string? mrn, DateTime? from, DateTime? to)
        {
            var queryString = QueryString.Empty;

            if (!string.IsNullOrEmpty(mrn))
            {
                queryString = queryString.Add(nameof(mrn), mrn);
            }

            if (from.HasValue)
            {
                queryString = queryString.Add(nameof(from), from.Value.ToString("O", CultureInfo.InvariantCulture));
            }

            if (to.HasValue)
            {
                queryString = queryString.Add(nameof(to), to.Value.ToString("O", CultureInfo.InvariantCulture));
            }

            return $"error-notifications{queryString.Value}";
        }
    }
}
