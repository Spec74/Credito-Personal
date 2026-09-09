namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Cabecera de cliente en consulta crédito — paridad <c>CreditoController.Creditos(pPersonaId)</c> / <c>Creditos.cshtml</c>.
/// </summary>
public sealed record PersonaCreditoFichaDto(
    int PersonaId,
    string NumeroDocumento,
    string NombreCompleto,
    string? Codigo,
    string Calificacion,
    string CalificacionLabel,
    bool ClienteActivo,
    string EstadoCliente,
    bool Bloqueado,
    int? ClasificacionRiesgoSbsItemId,
    string? ClasificacionRiesgoSbsCodigo,
    string? ClasificacionRiesgoSbsLabel,
    string? ClasificacionRiesgoSbsObs,
    decimal TopeCredito,
    int TotalCreditos,
    int CreditosPendientes,
    int? SolicitudCreditoId,
    string? DepuradoDescripcion,
    bool PuedeCrearSolicitud);

public sealed record CreditoContextoDto(
    int CreditoId,
    int PersonaId,
    string? PersonaNombre,
    string? Observacion,
    bool IndCondonacion,
    decimal MontoCondonacion,
    bool IndIrrecuperable,
    decimal MontoGastosAdm,
    decimal CentralRiesgo,
    int? PersonaAvalId,
    string? PersonaAvalNombre,
    bool EsPrendario,
    decimal? MontoTasacion,
    string? NumeroContratoPrendario,
    DateTime? FechaRemate,
    DateTime FechaVencimiento);

public sealed record SolicitudCreditoDetalleDto(
    int SolicitudCreditoId,
    int PersonaId,
    string Cliente,
    int? ProductoId,
    decimal MontoCredito,
    string? FormaPago,
    int NumeroCuotas,
    decimal Interes,
    DateTime? FechaPrimerPago,
    decimal MontoGastosAdm,
    string? Observacion,
    decimal CentralRiesgo);

public sealed record CondonarCreditoRequest(
    int OficinaId,
    int CreditoId,
    decimal MontoCxc,
    decimal MontoCondonacion,
    string Observacion);

public sealed record ObservarCreditoRequest(int OficinaId, int CreditoId, string Observacion);

public sealed record CargoCreditoRowDto(
    int CargoId,
    string TipoCargo,
    int NumCuota,
    string Descripcion,
    decimal Importe,
    string UsuarioCargo,
    string Estado);

public sealed record GuardarCargoCreditoRequest(
    int OficinaId,
    int CreditoId,
    int TipoCargoId,
    decimal Monto,
    string Descripcion,
    bool Final);

public sealed record CreditoEvidenciaDto(int Id, int CreditoId, string Imagen, string? Url);

public sealed record CreditoGrillaPersonaRowDto(
    int CreditoId,
    int PersonaId,
    string Estado,
    decimal MontoCredito,
    DateTime? FechaPrimerPago,
    string? Descripcion);

public sealed record CreditoGrillaPersonaPageDto(
    IReadOnlyList<CreditoGrillaPersonaRowDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record CreditoAvalRelacionDto(
    string Grupo,
    int CreditoId,
    int PersonaId,
    int? PersonaRelacionadaId,
    decimal MontoCredito,
    string Estado,
    string? Persona,
    string? Dni,
    string? Celular);

public sealed record CambiarAnalistaCreditoRequest(int OficinaId, int CreditoId, int AnalistaId);

public sealed record ActualizarTopeCreditoRequest(int OficinaId, int PersonaId, decimal TopeCredito);

public sealed record DepurarPersonaCreditoRequest(int OficinaId, int PersonaId, string Observacion);

public sealed record ActualizarIrrecuperableRequest(int OficinaId, int CreditoId, bool IndIrrecuperable);

public sealed record ModificarTramiteAdmCreditoRequest(int OficinaId, int CreditoId, decimal Valor);

public sealed record ModificarCentralRiesgoCreditoRequest(int OficinaId, int CreditoId, decimal Valor);

public sealed record ActualizarDescuentoPlanPagoRequest(
    int OficinaId,
    int CreditoId,
    int PlanPagoId,
    decimal Descuento);

public sealed record ActualizarAvalCreditoRequest(int OficinaId, int CreditoId, int? PersonaAvalId);

public sealed record EliminarEvidenciaRequest(int OficinaId, int CreditoImagenId);

/// <summary>Bien en custodia de un crédito prendario (<c>CREDITO.Prenda</c>).</summary>
public sealed record PrendaDto(
    long PrendaId,
    int CreditoId,
    string Descripcion,
    string? Marca,
    string? Modelo,
    string? Serie,
    string? Color,
    decimal ValorTasacion,
    string? Observaciones,
    string? FotoPath,
    string Estado,
    DateTime FechaRegistro,
    string? CodigoInterno);

/// <summary>Renglón del formulario de bienes (paridad <c>ITB.VENDIX.BL.PrendaDto</c>).</summary>
public sealed record PrendaItemRequest(
    string Descripcion,
    string? Marca,
    string? Modelo,
    string? Serie,
    string? Color,
    decimal ValorTasacion,
    string? Observaciones,
    string? CodigoInterno);

/// <summary>
/// Cuerpo del guardado de bienes. El formulario envía siempre el detalle completo: la operación
/// reemplaza las prendas del crédito, no las acumula (paridad <c>CreditoBL.GuardarPrendario</c>).
/// </summary>
public sealed record GuardarPrendasRequest(
    int OficinaId,
    int CreditoId,
    IReadOnlyList<PrendaItemRequest> Prendas,
    DateTime? FechaRemate);

public sealed record CreditoGestionOperacionResponse(bool Success, string? Mensaje);
