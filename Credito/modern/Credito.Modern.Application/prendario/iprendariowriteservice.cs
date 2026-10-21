namespace Credito.Modern.Application.Prendario;

public interface IPrendarioWriteService
{
    Task<DevolverPrendaResponse> DevolverAsync(
        DevolverPrendaRequest request,
        int usuarioId,
        CancellationToken cancellationToken = default);
}
