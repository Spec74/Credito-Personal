namespace Credito.Modern.Application.CreditoPlanes;

public interface ICajaAsignacionWriteService
{
    /// <summary>
    /// Asigna caja y abre turno. Devuelve mensaje de error de negocio (paridad MVC) o <c>null</c> si OK.
    /// </summary>
    Task<(string? Error, AsignarCajaResponse? Result)> AsignarAsync(
        int oficinaId,
        int cajaId,
        decimal saldoInicial,
        int usuarioRegId,
        DateTime fechaOperacion,
        CancellationToken cancellationToken = default);
}
