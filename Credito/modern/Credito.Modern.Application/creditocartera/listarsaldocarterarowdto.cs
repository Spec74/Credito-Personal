namespace Credito.Modern.Application.CreditoCartera;

/// <summary>Fila devuelta por <c>CREDITO.usp_ListarSaldoCartera</c> (paridad con <c>usp_ListarSaldoCartera_Result</c> en DA).</summary>
public sealed class ListarSaldoCarteraRowDto
{
    public int AgenteId { get; init; }

    public int OficinaId { get; init; }

    public int? NroDesembolsos { get; init; }

    public decimal? MontoDesembolsos { get; init; }

    public decimal? SaldoCartera { get; init; }

    public int? NroClientesSaldoCartera { get; init; }

    public decimal SaldoMoraCartera { get; init; }

    public int NroClientesSaldoMoraCartera { get; init; }

    public decimal SaldoVencido { get; init; }

    public decimal SaldoMorosidad { get; init; }

    public int NroClientesNuevos { get; init; }

    public DateTime? FechaCierre { get; init; }
}
