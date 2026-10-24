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

/// <summary>Saldo de una marca en una sede (bóveda principal abierta).</summary>
public sealed record BovedaMedioSedeDto(
    int OficinaId,
    string Oficina,
    bool IndPrincipal,
    int? BovedaId,
    decimal Monto,
    bool EsLegado = false);

/// <summary>Marca de medio (Yape, BCP…) con total y, en central, desglose por sucursal.</summary>
public sealed record BovedaMedioMarcaDto(
    short TipoPagoId,
    string Marca,
    string Variant,
    decimal Total,
    IReadOnlyList<BovedaMedioSedeDto> Sedes);

/// <summary>
/// Resumen estructurado. Central: total de red + lista por oficina.
/// Sucursal: solo su bóveda. No usa ítems 6–8 del catálogo como si fueran marcas.
/// </summary>
public sealed record BovedaMediosRedDto(
    bool EsCentral,
    int OficinaId,
    string Oficina,
    IReadOnlyList<BovedaMedioMarcaDto> Marcas);

/// <summary>Fila leftover en <c>BovedaMovTemp</c> hacia la bóveda destino. El USP no acepta: solo anula (flag 2).</summary>
public sealed record BovedaTransferenciaPendienteDto(
    int BovedaMovTempId,
    decimal Monto,
    string OficinaOrigen,
    string? Descripcion,
    string UsuarioReg,
    DateTime FechaReg);
