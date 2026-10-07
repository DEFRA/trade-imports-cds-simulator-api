using System.Security.Cryptography;
using System.Text;

namespace Defra.TradeImportsCdsSimulator.Utils.Mrn
{
    public static class MrnGenerator
    {
        private const string MrnChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static string GenerateMrn()
        {
            var yy = (DateTime.UtcNow.Year % 100).ToString("D2");

            var sb = new StringBuilder();
            for (var i = 0; i < 14; i++)
                sb.Append(MrnChars[RandomNumberGenerator.GetInt32(MrnChars.Length)]);

            return $"{yy}GB{sb}";
        }
    }
}
