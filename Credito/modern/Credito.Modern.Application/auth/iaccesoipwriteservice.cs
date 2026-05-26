namespace Credito.Modern.Application.Auth;

public interface IAccesoIpWriteService
{
    /// <summary>Paridad <c>HomeController.CrearAcceso</c> — inserta IP si no existe.</summary>
    Task<bool> RegistrarSiNoExisteAsync(string direccionIp, CancellationToken cancellationToken = default);
}
