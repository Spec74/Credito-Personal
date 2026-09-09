using System.Text.Json.Serialization;

namespace Credito.Modern.Application.Prendario;

/// <summary>
/// Situación de un crédito prendario. Paridad <c>PrendarioController.CalcularCategoria</c>,
/// evaluada en ese orden: un crédito vencido y ya rematado cuenta como rematado.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PrendarioCategoria
{
    /// <summary>Marcado como prendario pero sin bienes registrados.</summary>
    SinBienes,

    /// <summary>No está desembolsado, así que las alertas de vencimiento no aplican.</summary>
    Otro,

    Rematado,
    Vencido,
    PorVencer,
    Vigente,
}

/// <summary>Tarjetas del listado, sobre cartera desembolsada (<c>Estado = 'DES'</c>).</summary>
public sealed record PrendarioResumenDto(
    int Total,
    int PorVencer,
    int Vencidos,
    int Rematados);

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
    string? CodigoInterno);

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
