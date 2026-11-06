namespace Credito.Modern.Application.CreditoCartera;

/// <summary>Fila devuelta por <c>CREDITO.usp_ListarSaldoCartera</c> (paridad con <c>usp_ListarSaldoCartera_Result</c> en DA).</summary>
public sealed class ListarSaldoCarteraRowDto
{
    public int AgenteId { get; init; }

    /// <summary>Nombre legible del gestor (enriquecido tras el USP).</summary>
    public string? AgenteNombre { get; init; }

    public int OficinaId { get; init; }

    /// <summary>Denominación de oficina (enriquecida tras el USP).</summary>
    public string? OficinaNombre { get; init; }

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
