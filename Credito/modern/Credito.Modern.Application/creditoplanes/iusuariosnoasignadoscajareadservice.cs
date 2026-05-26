namespace Credito.Modern.Application.CreditoPlanes;

public interface IUsuariosNoAsignadosCajaReadService
{
    Task<List<UsuariosNoAsignadosCajaRowDto>> ListarPorOficinaAsync(
        int oficinaId,
        CancellationToken cancellationToken = default);
}
