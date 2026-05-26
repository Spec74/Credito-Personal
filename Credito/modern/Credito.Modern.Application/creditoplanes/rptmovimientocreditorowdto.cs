namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptMovimientoCredito</c> (paridad con <c>usp_RptMovimientoCredito_Result</c> en DA).</summary>
public sealed class RptMovimientoCreditoRowDto
{
    public int? MovimientoCajaId { get; init; }

    public DateTime? Fecha { get; init; }

    public string? Operacion { get; init; }

    public string? Glosa { get; init; }

    public decimal? ImportePago { get; init; }

    public decimal? Saldo { get; init; }
}
