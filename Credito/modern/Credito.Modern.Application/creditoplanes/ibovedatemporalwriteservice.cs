namespace Credito.Modern.Application.CreditoPlanes;

public interface IBovedaTemporalWriteService
{
    /// <summary>Mensaje de error MVC o null si OK.</summary>
    Task<(string? Error, AsignarBovedaTemporalResponse? Result)> AsignarOTransferirAsync(
        int oficinaId,
        decimal importe,
        string descripcion,
        int usuarioAsignadoId,
        int usuarioRegId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default);
}
