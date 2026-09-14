namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.usp_RptSaldosCaja</c> (paridad con <c>usp_RptSaldosCaja_Result</c> en DA / EDMX).</summary>
public sealed class RptSaldosCajaRowDto
{
    public int MovimientoCajaId { get; init; }

    public string Operacion { get; init; } = string.Empty;

    public DateTime FechaReg { get; init; }

    public string? Codigo { get; init; }

    public string? Cliente { get; init; }

    public decimal ImportePago { get; init; }

    public bool IndEntrada { get; init; }

    public string? Glosa { get; init; }

    public string TipoPago { get; init; } = string.Empty;

    /// <summary>Paridad grillas CajaDiario (ACTIVO/ANULADO). En el SP de reporte siempre true.</summary>
    public bool EstadoActivo { get; init; } = true;
}
