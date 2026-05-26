namespace Credito.Modern.Application.Ventas;

public sealed record EnviarOrdenVentaResponse(int OrdenVentaId, int? CreditoId = null);
