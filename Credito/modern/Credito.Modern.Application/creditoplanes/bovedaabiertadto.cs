namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Bóveda abierta de la oficina (paridad <c>BovedaBL.Obtener</c> en <c>BovedaController.Index</c>).</summary>
public sealed record BovedaAbiertaDto(
    int BovedaId,
    int OficinaId,
    decimal SaldoInicial,
    decimal Entradas,
    decimal Salidas,
    decimal SaldoFinal,
    DateTime FechaIniOperacion,
    DateTime? FechaFinOperacion,
    bool IndCierre,
    bool IndTemporal);

/// <summary>Bóveda abierta disponible para transferencia interoficina.</summary>
public sealed record BovedaDestinoTransferenciaDto(
    int BovedaId,
    int OficinaId,
    string Oficina,
    decimal SaldoFinal,
    DateTime FechaIniOperacion);
