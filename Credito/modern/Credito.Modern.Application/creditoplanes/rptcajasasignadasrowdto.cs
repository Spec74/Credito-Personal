namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptCajasAsignadas</c> (paridad con <c>usp_RptCajasAsignadas_Result</c> en DA / EDMX).</summary>
public sealed class RptCajasAsignadasRowDto
{
    public int CajaDiarioId { get; init; }

    public string Caja { get; init; } = string.Empty;

    public string Modo { get; init; } = string.Empty;

    public string? Cajero { get; init; }

    public DateTime FechaIniOperacion { get; init; }

    public DateTime? FechaFinOperacion { get; init; }

    public decimal SaldoInicial { get; init; }

    public decimal Salidas { get; init; }

    public decimal Entradas { get; init; }

    public decimal SaldoFinal { get; init; }

    public string? Resumen { get; init; }
}
