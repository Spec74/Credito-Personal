namespace Credito.Modern.Application.CreditoPlanes;

public interface IEntradaSalidaCajaDiarioWriteService
{
    Task<EntradaSalidaCajaDiarioResponse> EjecutarAsync(
        int cajaDiarioId,
        int personaId,
        int tipoOperacionId,
        decimal importe,
        string descripcion,
        int usuarioId,
        int tipoPagoId,
        CancellationToken cancellationToken = default);
}
