namespace Credito.Modern.Application.CreditoPlanes;

public sealed record CrearRendicionCajaChicaRequest(
    int MovimientoCajaChicaId,
    int TipoDocumentoId,
    DateTime Fecha,
    string Serie,
    string Numero,
    string Ruc,
    string RazonSocial,
    string DetalleGasto,
    decimal Importe);
