namespace Credito.Modern.Application.CreditoPlanes;

public interface IRutaCobrosService
{
    Task<GenerarRutaCobrosResponse> GenerarAsync(
        int usuarioId,
        int oficinaId,
        IReadOnlyList<int> creditoIds,
        CancellationToken cancellationToken = default);

    string? ObtenerTextoRuta(string cacheId);
}
