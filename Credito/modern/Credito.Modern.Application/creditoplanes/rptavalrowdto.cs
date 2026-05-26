namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptAval</c> (paridad con <c>usp_RptAval_Result</c> en DA / EDMX).</summary>
public sealed class RptAvalRowDto
{
    public string Grupo { get; init; } = string.Empty;

    public int CreditoId { get; init; }

    public decimal MontoCredito { get; init; }

    public string Estado { get; init; } = string.Empty;

    public string? Persona { get; init; }

    public string? Dni { get; init; }

    public string? Celular { get; init; }
}
