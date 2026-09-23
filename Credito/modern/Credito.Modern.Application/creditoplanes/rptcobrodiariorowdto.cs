namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptCobroDiario</c> (paridad con <c>usp_RptCobroDiario_Result</c> en DA / EDMX).</summary>
public sealed class RptCobroDiarioRowDto
{
    public long? Nro { get; init; }

    public int? Orden { get; init; }

    public int CreditoId { get; init; }

    public string? Cliente { get; init; }

    public string? Celular { get; init; }

    public decimal MontoCredito { get; init; }

    public decimal Interes { get; init; }

    public decimal? CuotaPlan { get; init; }

    public decimal? Saldo { get; init; }

    public int? DiasAtrazo { get; init; }

    public int? NroCuotasPen { get; init; }

    public decimal? CuotaTotal { get; init; }

    public string? Direccion { get; init; }

    public DateTime? FechaPago { get; init; }

    /// <summary>True si el crédito tiene al menos un pago CUO real (columna del SP moderno).</summary>
    public bool? TienePagoReal { get; init; }

    public DateTime FechaPrimerPago { get; init; }

    public DateTime FechaVencimiento { get; init; }

    public decimal? Mora { get; init; }

    public decimal? MontoTotal { get; init; }

    public string? Negocio { get; init; }

    public string FormaPago { get; init; } = string.Empty;

    public decimal? TopeCredito { get; init; }

    public string? ClasificacionRiesgoSBS { get; init; }
}
