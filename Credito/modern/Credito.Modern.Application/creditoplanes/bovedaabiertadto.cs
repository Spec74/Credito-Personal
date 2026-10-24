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
    bool IndTemporal,
    decimal SumaMedios = 0m,
    decimal DeltaMedios = 0m);

/// <summary>Bóveda abierta disponible para transferencia interoficina.</summary>
public sealed record BovedaDestinoTransferenciaDto(
    int BovedaId,
    int OficinaId,
    string Oficina,
    decimal SaldoFinal,
    DateTime FechaIniOperacion);

/// <summary>Posición de efectivo de una sede (principal abierta). Cartera no entra.</summary>
public sealed record BovedaRedPosicionDto(
    int OficinaId,
    string Oficina,
    bool IndPrincipal,
    int? BovedaId,
    decimal SaldoInicial,
    decimal Entradas,
    decimal Salidas,
    decimal SaldoFinal,
    decimal SumaMedios,
    decimal DeltaMedios,
    decimal MontoCajas,
    decimal MontoCajaChica,
    decimal EfectivoOficina);

/// <summary>Fila leftover en <c>BovedaMovTemp</c> hacia la bóveda destino. El USP no acepta: solo anula (flag 2).</summary>
public sealed record BovedaTransferenciaPendienteDto(
    int BovedaMovTempId,
    decimal Monto,
    string OficinaOrigen,
    string? Descripcion,
    string UsuarioReg,
    DateTime FechaReg);
