namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Fila común para grillas <c>SaldosController</c> (caja diario / chica / bóveda).</summary>
public sealed record SaldoCajaSesionRowDto(
    int Id,
    string Caja,
    string? Usuario,
    decimal SaldoInicial,
    decimal SaldoFinal,
    DateTime FechaIniOperacion,
    DateTime? FechaFinOperacion,
    bool IndCierre,
    bool TransBoveda);
