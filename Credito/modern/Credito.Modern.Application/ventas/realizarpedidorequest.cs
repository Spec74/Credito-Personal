namespace Credito.Modern.Application.Ventas;

public sealed record RealizarPedidoRequest(
    int OficinaId,
    int CajaDiarioId,
    int PersonaId,
    IReadOnlyList<PedidoLineaRequest> Pedidos);
