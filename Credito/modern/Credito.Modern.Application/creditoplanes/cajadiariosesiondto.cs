namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Sesión de caja diario abierta (paridad modelo MVC <c>CajaDiario.cshtml</c> cabecera).</summary>
public sealed record CajaDiarioSesionDto(
    int CajaDiarioId,
    int CajaId,
    string CajaDenominacion,
    DateTime FechaIniOperacion,
    decimal SaldoInicial,
    decimal Entradas,
    decimal Salidas,
    decimal SaldoFinal,
    bool IndCierre,
    bool EsCajaCentral);
