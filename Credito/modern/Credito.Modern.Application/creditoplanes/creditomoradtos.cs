namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila de <c>CREDITO.CreditoMora</c> (paridad grid legacy <c>ListarCreditoMoraGrd</c>).</summary>
public sealed class CreditoMoraRowDto
{
    public int CreditoMoraId { get; set; }
    public int CreditoId { get; set; }
    public int? MovimientoCajaId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Mora { get; set; }
    public int DiasAtrazo { get; set; }
    public decimal SaldoMora { get; set; }
    public decimal InteresMora { get; set; }
}

public sealed record CreditoMoraSaldoPostergadoResponse(decimal SaldoPostergado, int RegistrosPendientes);

public sealed record CreditoMoraIndicadorResponse(bool IndMoraProducto, decimal SaldoPostergado);
