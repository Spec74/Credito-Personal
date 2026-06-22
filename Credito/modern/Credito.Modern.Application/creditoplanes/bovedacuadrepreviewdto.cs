namespace Credito.Modern.Application.CreditoPlanes;

public sealed record BovedaCuadrePreviewDto(
    BovedaAbiertaDto Boveda,
    BovedaEstadoDineroDto EstadoDinero,
    BovedaCuadreTotalesDto Totales,
    IReadOnlyList<BovedaCuadreResponsableDto> Responsables,
    IReadOnlyList<BovedaCuadreMedioDto> MediosBoveda,
    IReadOnlyList<BovedaCuadreValidacionDto> Validaciones,
    IReadOnlyList<BovedaCuadreDenominacionDto> Denominaciones,
    IReadOnlyList<string> Pendientes);

public sealed record BovedaCuadreTotalesDto(
    decimal EfectivoBoveda,
    decimal EfectivoCajas,
    decimal EfectivoSistema,
    decimal DigitalBancosSistema,
    decimal TotalMediosSistema,
    decimal TotalFondo,
    decimal MontoALlevarSugerido,
    decimal SaldoPorLlevarSugerido,
    decimal DiferenciaSistema);

public sealed record BovedaCuadreResponsableDto(
    int CajaDiarioId,
    string Caja,
    string? Responsable,
    DateTime FechaIniOperacion,
    DateTime? FechaFinOperacion,
    bool IndCierre,
    bool TransBoveda,
    decimal SaldoInicial,
    decimal Entradas,
    decimal Salidas,
    decimal SaldoFinal,
    decimal Efectivo,
    decimal DigitalBancos,
    decimal TotalSistema,
    IReadOnlyList<BovedaCuadreMedioDto> Medios);

public sealed record BovedaCuadreMedioDto(
    int TipoPagoId,
    string TipoPago,
    string Grupo,
    decimal Monto,
    bool EsEfectivo,
    bool EsDigitalOBanco,
    bool RequiereVerificacion,
    int PagosNoVerificados);

public sealed record BovedaCuadreValidacionDto(
    string Codigo,
    string Concepto,
    decimal Sistema,
    decimal? Contraste,
    decimal? Diferencia,
    string Severidad,
    string Mensaje);

public sealed record BovedaCuadreDenominacionDto(
    string Codigo,
    string Etiqueta,
    decimal Valor,
    string Grupo);
