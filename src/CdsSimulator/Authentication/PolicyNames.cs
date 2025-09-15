using System.Diagnostics.CodeAnalysis;

namespace Defra.TradeImportsCdsSimulator.Authentication;

[ExcludeFromCodeCoverage]
public static class PolicyNames
{
    public const string Read = nameof(Scopes.Read);
    public const string Write = nameof(Scopes.Write);
}
