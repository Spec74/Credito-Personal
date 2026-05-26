namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Cuerpo de <c>POST /api/v1/credito/asignar-caja</c> (paridad <c>SaldosController.AsignarCaja</c>).</summary>
public sealed record AsignarCajaRequest(int OficinaId, int CajaId, decimal SaldoInicial = 0m);
