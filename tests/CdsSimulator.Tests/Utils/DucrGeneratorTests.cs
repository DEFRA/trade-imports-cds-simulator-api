using AwesomeAssertions;
using Defra.TradeImportsCdsSimulator.Utils.Ucr;

namespace Defra.TradeImportsCdsSimulator.Tests.Utils;

public class DucrGeneratorTests
{
    private static readonly int s_year = DateTime.UtcNow.Year % 10;

    [Fact]
    public void Ducr_ShouldBeTheYearDigitTheDeclarantsEoriAndAReference()
    {
        var ducr = DucrGenerator.GenerateDucr("GB999000000001");

        ducr.Should().MatchRegex($"^{s_year}GB999000000001-[A-Z0-9]{{8}}$");
    }

    [Fact]
    public void Ducr_ShouldTakeTheCountryFromTheEori()
    {
        DucrGenerator.GenerateDucr("xi123456789012").Should().StartWith($"{s_year}XI123456789012-");
    }

    [Fact]
    public void Ducr_ShouldDefaultToGb_WhenThereIsNoDeclarant()
    {
        DucrGenerator.GenerateDucr(null).Should().MatchRegex($"^{s_year}GB-[A-Z0-9]{{8}}$");
    }

    [Fact]
    public void Ducr_ShouldFitTheDeclarationUcrField_ForTheLongestEori()
    {
        DucrGenerator.GenerateDucr("GB123456789012345").Length.Should().BeLessThanOrEqualTo(35);
    }
}
