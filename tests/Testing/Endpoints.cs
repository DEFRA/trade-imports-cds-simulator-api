using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Defra.TradeImportsCdsSimulator.Testing;

public static class Endpoints
{
    public static class ClearanceRequests
    {
        public static string Post => "ws/CDS/defra/alvsclearancerequestinbound/v1";

        public static string Put => "ws/CDS/defra/alvsclearancerequestinbound/v1";

        public static string List(int? page = null, int? pageSize = null)
        {
            var queryString = QueryString.Empty;

            if (page.HasValue)
            {
                queryString = queryString.Add(nameof(page), page.Value.ToString(CultureInfo.InvariantCulture));
            }

            if (pageSize.HasValue)
            {
                queryString = queryString.Add(nameof(pageSize), pageSize.Value.ToString(CultureInfo.InvariantCulture));
            }

            return $"control/clearance-requests{queryString.Value}";
        }

        public static string Get(string mrn) => $"control/clearance-requests/{Uri.EscapeDataString(mrn)}";
    }

    public static class Scenarios
    {
        public static string List => "control/scenarios";

        public static string Put(string id) => $"control/scenarios/{Uri.EscapeDataString(id)}";
    }

    public static class FinalisationNotifications
    {
        public static string Post => "ws/CDS/defra/alvsfinalisationrequestinbound/v1";

        public static string Put => "ws/CDS/defra/alvsfinalisationrequestinbound/v1";
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
