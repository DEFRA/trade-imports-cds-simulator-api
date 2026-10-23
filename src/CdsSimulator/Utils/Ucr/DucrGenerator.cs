using System.Security.Cryptography;
using System.Text;

namespace Defra.TradeImportsCdsSimulator.Utils.Ucr
{
    // A Declaration UCR (DUCR), for a request that comes without one: BTMS refuses a clearance request
    // with no DeclarationUCR (ALVSVAL313). A DUCR is the year's last digit, the declarant's EORI and a
    // reference of the trader's own: GB999000000001 in 2026 gives 6GB999000000001-K3QZ8T1M.
    public static class DucrGenerator
    {
        private const string ReferenceChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public static string GenerateDucr(string? declarantId)
        {
            var year = DateTime.UtcNow.Year % 10;
            var eori = (declarantId ?? string.Empty).Trim().ToUpperInvariant();
            var country =
                eori.Length >= 2 && char.IsAsciiLetter(eori[0]) && char.IsAsciiLetter(eori[1]) ? eori[..2] : "GB";
            var digits = new string(eori.Where(char.IsAsciiDigit).ToArray());

            var reference = new StringBuilder();
            for (var i = 0; i < 8; i++)
                reference.Append(ReferenceChars[RandomNumberGenerator.GetInt32(ReferenceChars.Length)]);

            return $"{year}{country}{digits}-{reference}";
        }
    }
}
