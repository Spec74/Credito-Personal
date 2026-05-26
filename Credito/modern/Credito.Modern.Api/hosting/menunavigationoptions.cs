namespace Credito.Modern.Api.Hosting;



/// <summary>Comportamiento del menú en la API moderna (migración desde query string).</summary>

public sealed class MenuNavigationOptions

{

    public const string SectionName = "Menu";



    /// <summary>

    /// Si es <c>false</c>, <c>GET /api/v1/menu</c> solo acepta identidad por JWT (no <c>oficinaId</c>/<c>usuarioId</c> en query).

    /// Recomendado en producción.

    /// </summary>

    public bool PermiteParametrosQuery { get; set; }

}

