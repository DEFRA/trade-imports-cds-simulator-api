using System.Diagnostics.CodeAnalysis;
using Defra.TradeImportsCdsSimulator.Configuration;
using Microsoft.AspNetCore.Authentication;

namespace Defra.TradeImportsCdsSimulator.Authentication;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationAuthorization(this IServiceCollection services)
    {
        services.AddOptions<AclOptions>().BindConfiguration("Acl");

        services
            .AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(
                BasicAuthenticationHandler.SchemeName,
                _ => { }
            );

        services
            .AddAuthorizationBuilder()
            .AddPolicy(
                PolicyNames.Read,
                builder => builder.RequireAuthenticatedUser().RequireClaim(Claims.Scope, Scopes.Read)
            )
            .AddPolicy(
                PolicyNames.Write,
                builder => builder.RequireAuthenticatedUser().RequireClaim(Claims.Scope, Scopes.Write)
            );

        return services;
    }
}
