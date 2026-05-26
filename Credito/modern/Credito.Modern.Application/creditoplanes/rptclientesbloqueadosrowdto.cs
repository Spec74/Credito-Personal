namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptClientesBloqueados</c> (paridad con <c>usp_RptClientesBloqueados_Result</c> en DA / EDMX).</summary>
public sealed class RptClientesBloqueadosRowDto
{
    public string? Agente { get; init; }

    public string NumeroDocumento { get; init; } = string.Empty;

    public string? Cliente { get; init; }

    public string? Direccion { get; init; }

    public string? DireccionRef { get; init; }

    public string? Celular { get; init; }

    public string? Calificacion { get; init; }

    public string? Nota { get; init; }
}
