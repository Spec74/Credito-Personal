namespace Credito.Modern.Application.Ventas;

public interface IVentaRapidaWriteService
{
    Task<(string? Error, RealizarPedidoResponse? Result)> RealizarPedidoAsync(
        int oficinaId,
        int cajaDiarioId,
        int personaId,
        int usuarioId,
        DateTime fechaReg,
        IReadOnlyList<PedidoLineaRequest> pedidos,
        CancellationToken cancellationToken = default);
}
