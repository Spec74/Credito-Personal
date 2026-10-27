namespace Credito.Modern.Application.Prendario;

public interface IPrendarioWriteService
{
    Task<DevolverPrendaResponse> DevolverAsync(
        DevolverPrendaRequest request,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<PrendarioFichaResponse> ActualizarFichaAsync(
        PrendarioFichaRequest request,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<PrendarioFichaResponse> SubirFotoAsync(
        int oficinaId,
        long prendaId,
        int usuarioId,
        string extension,
        Stream contenido,
        CancellationToken cancellationToken = default);

    Task<PrendarioRenovarResponse> RenovarAsync(
        PrendarioRenovarRequest request,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<PrendarioLiberarResponse> LiberarAsync(
        PrendarioLiberarRequest request,
        int usuarioId,
        CancellationToken cancellationToken = default);

    Task<RematarPrendaResponse> RematarAsync(
        RematarPrendaRequest request,
        int usuarioId,
        CancellationToken cancellationToken = default);
}
