namespace Credito.Modern.Application.Time;

/// <summary>
/// Hora de referencia expuesta por SQL Server (proc <c>usp_FechaBD</c> en el legado VENDIX).
/// </summary>
public interface IDatabaseTimeProvider
{
    Task<DateTime?> GetServerTimeAsync(CancellationToken cancellationToken = default);
}
