namespace Credito.Modern.Application.CreditoPlanes;

public interface ICajaChicaEntradaSalidaWriteService
{
    /// <summary>Mensaje de error MVC o null si OK.</summary>
    Task<(string? Error, EntradaSalidaCajaDiarioResponse? Result)> EjecutarAsync(
        int oficinaId,
        int personaId,
        int tipoOperacionId,
        decimal importe,
        string descripcion,
        int usuarioId,
        CancellationToken cancellationToken = default);
}
