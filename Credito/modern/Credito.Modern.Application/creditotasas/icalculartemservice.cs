namespace Credito.Modern.Application.CreditoTasas;

public interface ICalcularTemService
{
    /// <summary>Ejecuta <c>CREDITO.usp_CalcularTEM</c> (misma firma que el EDMX legado).</summary>
    Task<decimal?> CalcularTemAsync(decimal tea, string formaPago, CancellationToken cancellationToken = default);
}
