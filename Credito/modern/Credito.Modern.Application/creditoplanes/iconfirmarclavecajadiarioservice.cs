namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>Paridad <c>CajaDiarioController.ConfirmarClave</c> (usuario ADMVENDIX).</summary>
public interface IConfirmarClaveCajaDiarioService
{
    Task<ConfirmarClaveCajaDiarioResponse> VerificarAsync(
        string clave,
        CancellationToken cancellationToken = default);
}
