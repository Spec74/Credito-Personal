namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/verificar-pago-transferencia</c> (paridad <c>VerificarPagosController.Verificar</c>).</summary>
public sealed record VerificarPagoTransferenciaRequest(int OficinaId, int MovimientoCajaId);
