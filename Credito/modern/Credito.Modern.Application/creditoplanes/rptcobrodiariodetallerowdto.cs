namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptCobroDiarioDetalle</c> (paridad con <c>usp_RptCobroDiarioDetalle_Result</c> en DA / EDMX).</summary>
public sealed class RptCobroDiarioDetalleRowDto
{
    public long? Nro { get; init; }

    public string? Cliente { get; init; }

    public string FormaPago { get; init; } = string.Empty;

    public decimal MontoCredito { get; init; }

    public decimal Interes { get; init; }

    public decimal? MontoTotal { get; init; }

    public DateTime FechaPrimerPago { get; init; }

    public DateTime FechaVencimiento { get; init; }

    public decimal? Saldo { get; init; }

    public decimal? TotalPago { get; init; }

    public int? DiasAtrazoMora { get; init; }

    public string? Pagos { get; init; }
}
