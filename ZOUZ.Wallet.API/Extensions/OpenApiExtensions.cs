namespace ZOUZ.Wallet.API.Extensions;

/// <summary>
/// Extensions pour l'API afin d'ajouter des fonctionnalités OpenAPI
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Alternative à WithOpenApi pour les versions qui ne supportent pas cette méthode directement
    /// </summary>
    public static RouteHandlerBuilder WithSwaggerDoc(this RouteHandlerBuilder builder, string tag = null)
    {
        if (!string.IsNullOrEmpty(tag))
        {
            builder.WithTags(tag);
        }
            
        // Vous pouvez ajouter d'autres configurations Swagger ici si nécessaire
            
        return builder;
    }
}