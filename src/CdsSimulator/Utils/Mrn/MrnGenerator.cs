using System.Text;

namespace Defra.TradeImportsCdsSimulator.Utils.Mrn
{
    public static class MrnGenerator
    {
        private static readonly char[] s_mrnChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

        public static string GenerateMrn()
        {
            var yy = (DateTime.UtcNow.Year % 100).ToString("D2");
            var rnd = new Random();
            var sb = new StringBuilder();
            for (var i = 0; i < 14; i++)
                sb.Append(s_mrnChars[rnd.Next(s_mrnChars.Length)]);
            return $"{yy}GB{sb}";
        }
    }
}
