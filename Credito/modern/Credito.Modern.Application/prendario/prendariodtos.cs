using System.Text.Json.Serialization;

namespace Credito.Modern.Application.Prendario;

/// <summary>
/// Situación de un crédito prendario. Paridad <c>PrendarioController.CalcularCategoria</c>,
/// evaluada en ese orden: un crédito vencido y ya rematado cuenta como rematado.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PrendarioCategoria
{
    /// <summary>Producto prendario sin filas en CREDITO.Prenda (incl. vigentes legacy).</summary>
    SinBienes,

    /// <summary>No está desembolsado, así que las alertas de vencimiento no aplican.</summary>
    Otro,

    Rematado,
    Vencido,
    PorVencer,
    Vigente,
}

/// <summary>Tarjetas del listado: colas de trabajo + exposición de cartera viva.</summary>
public sealed record PrendarioResumenDto(
    int Total,
    int PorVencer,
    int Vencidos,
    int Rematados,
    int Solicitudes,
    decimal MontoCartera);

public sealed record PrendarioCreditoRowDto(
    int CreditoId,
    int PersonaId,
    string? NumeroDocumento,
    string? NombreCompleto,
    string? Celular,
    string? NumeroContratoPrendario,
    decimal MontoTasacion,
    decimal MontoCredito,
    DateTime FechaVencimiento,
    DateTime? FechaRemate,
    string Estado,
    bool EsPrendario,
    int Bienes,
    PrendarioCategoria Categoria,
    int DiasParaVencer);

public sealed record PrendarioListaPageDto(
    IReadOnlyList<PrendarioCreditoRowDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record CrearSolicitudPrendariaRequest(int OficinaId, int PersonaId);

public enum PrendarioDocumentoEstado
{
    NoEncontrado,
    SinBienes,
    Ok,
}

public sealed record PrendarioBienDocumentoDto(
    string Descripcion,
    string? Marca,
    string? Modelo,
    string Serie,
    string? Color,
    decimal ValorTasacion,
    string? CodigoInterno,
    string? Observaciones);

public sealed record PrendarioContratoDto(
    int CreditoId,
    string NumeroContrato,
    DateTime FechaEmision,
    DateTime FechaVencimiento,
    DateTime FechaRemate,
    DateTime FechaDesembolso,
    string PlazoTexto,
    string ApellidosNombres,
    string DniCliente,
    string? ConyugeNombre,
    string? ConyugeDni,
    string? Correo,
    string? Celular,
    string? Domicilio,
    string? Distrito,
    string? Referencia,
    decimal MontoTasacion,
    decimal MontoPrestamo,
    decimal TasaInteres,
    decimal InteresMensual,
    decimal InteresDiario,
    decimal MontoGastosAdm,
    string EjecutivoNombre,
    IReadOnlyList<PrendarioBienDocumentoDto> Bienes);

public sealed record PrendarioActaDto(
    int CreditoId,
    string NumeroContrato,
    DateTime FechaContrato,
    string ApellidosNombres,
    string DniCliente,
    string? Domicilio,
    string? Distrito,
    string? Provincia,
    string? Departamento,
    IReadOnlyList<PrendarioBienDocumentoDto> Bienes);

public sealed record PrendarioDocumentoConsulta<T>(
    PrendarioDocumentoEstado Estado,
    T? Documento)
    where T : class;

public sealed record DevolverPrendaRequest(int OficinaId, long PrendaId);

public sealed record DevolverPrendaResponse(long PrendaId, int CreditoId, string Estado, DateTime? FechaMod);

public sealed record PrendarioDevolucionDto(
    long PrendaId,
    int CreditoId,
    string NumeroContrato,
    DateTime FechaDevolucion,
    string Estado,
    string ApellidosNombres,
    string DniCliente,
    string? Domicilio,
    string? Distrito,
    string? Provincia,
    string? Departamento,
    PrendarioBienDocumentoDto Bien);

/// <summary>Paridad <c>CreditoBL.CreditoPorVencerWhatsapp</c>.</summary>
public sealed record PrendarioAvisoVencimientoDto(
    int CreditoId,
    int OficinaId,
    string NombreCliente,
    string? Celular,
    DateTime FechaVencimiento,
    decimal MontoCredito,
    decimal Interes,
    decimal MontoCancelar);

public sealed record PrendarioAvisoEnvioItemDto(
    int CreditoId,
    bool Exito,
    string Mensaje);

public sealed record PrendarioAvisoEnvioResumenDto(
    int Enviados,
    int Fallidos,
    int Omitidos,
    IReadOnlyList<PrendarioAvisoEnvioItemDto> Detalle,
    string? Advertencia = null);

public sealed record EnviarAvisosPrendarioRequest(int OficinaId, int DiasAntes = 3, int? CreditoId = null);

/// <summary>Última pasada del aviso de 3 días (automática o manual). Sin token ni PhoneNumberId.</summary>
public sealed record PrendarioWhatsAppPasadaDto(
    DateTimeOffset Fecha,
    string Origen,
    int Enviados,
    int Fallidos,
    int Omitidos,
    string? Advertencia);

/// <summary>Estado del canal oficial de vencimiento prendario para la UI del analista.</summary>
public sealed record PrendarioWhatsAppEstadoDto(
    bool Enabled,
    bool Configurado,
    bool AutomaticoActivo,
    int DiasAntes,
    int DailyHourLocal,
    DateTimeOffset ProximaCorrida,
    bool CorreAlArrancar,
    PrendarioWhatsAppPasadaDto? UltimaPasada,
    /// <summary>True si hay VerifyToken (Meta puede verificar el webhook GET).</summary>
    bool WebhookVerifyTokenConfigurado,
    /// <summary>True si hay AppSecret (firma POST). Obligatorio fuera de Development.</summary>
    bool WebhookAppSecretConfigurado);

public sealed record PrendarioFichaRequest(int OficinaId, long PrendaId, string? Observaciones);

public sealed record PrendarioFichaResponse(
    long PrendaId,
    int CreditoId,
    string? Observaciones,
    string? FotoPath,
    string Estado);

public sealed record PrendarioRenovarRequest(
    int OficinaId,
    int CajaDiarioId,
    int CreditoId,
    int Dias = 30,
    int TipoPagoId = 1,
    string? FechaPagoTransferencia = null);

public sealed record PrendarioRenovarResponse(
    int CreditoId,
    decimal InteresCobrado,
    int Dias,
    int? MovimientoCajaId,
    DateTime FechaVencimiento,
    DateTime? FechaRemate);

public sealed record PrendarioLiberarRequest(
    int OficinaId,
    int CajaDiarioId,
    int CreditoId,
    int TipoPagoId = 1,
    string? FechaPagoTransferencia = null);

public sealed record PrendarioLiberarResponse(int CreditoId, string Estado, int BienesDevueltos);

public sealed record RematarPrendaRequest(int OficinaId, long PrendaId);

public sealed record RematarPrendaResponse(
    long PrendaId,
    int CreditoId,
    int ArticuloId,
    string CodArticulo,
    decimal PrecioVenta,
    int AlmacenId);

public sealed record PrendarioFotoArchivoDto(int CreditoId, string RelativePath);

public sealed record PrendarioCobroPreviewDto(
    int CreditoId,
    decimal CapitalPendiente,
    decimal InteresPendiente,
    decimal SaldoAprox,
    DateTime FechaVencimiento,
    DateTime? FechaRemate,
    DateTime? FechaRemateEjecutado,
    bool PuedeRematar,
    int DiasRenovar,
    DateTime NuevoVencimiento);

