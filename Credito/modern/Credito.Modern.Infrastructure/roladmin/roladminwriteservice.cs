using Credito.Modern.Application.Maestros;
using Credito.Modern.Application.RolAdmin;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.RolAdmin;

public sealed class RolAdminWriteService(IOptions<SqlDatabaseOptions> options) : IRolAdminWriteService
{
    private readonly string _cs = options.Value.ConnectionString;

    public async Task<MaestroOperacionResponse> GuardarAsync(GuardarRolRequest request, CancellationToken ct = default)
    {
        Ensure();
        var denominacion = (request.Denominacion ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(denominacion))
            return new MaestroOperacionResponse(false, null, "La denominación es obligatoria.");

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);

        if (request.RolId < 1)
        {
            var id = await c.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO MAESTRO.Rol (Denominacion, Estado)
                    VALUES (@Denominacion, @Estado);
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """,
                    new { Denominacion = denominacion, request.Estado },
                    cancellationToken: ct)).ConfigureAwait(false);
            return new MaestroOperacionResponse(true, id, null);
        }

        var n = await c.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE MAESTRO.Rol
                SET Denominacion = @Denominacion,
                    Estado = @Estado
                WHERE RolId = @RolId;
                """,
                new { request.RolId, Denominacion = denominacion, request.Estado },
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0
            ? new MaestroOperacionResponse(true, request.RolId, null)
            : new MaestroOperacionResponse(false, null, "Rol no encontrado.");
    }

    public async Task<MaestroOperacionResponse> ActivarAsync(int rolId, CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var n = await c.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE MAESTRO.Rol
                SET Estado = CASE WHEN Estado = CAST(1 AS bit) THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END
                WHERE RolId = @RolId;
                """,
                new { RolId = rolId },
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0
            ? new MaestroOperacionResponse(true, rolId, null)
            : new MaestroOperacionResponse(false, null, "Rol no encontrado.");
    }

    public async Task<MaestroOperacionResponse> AsignarMenusAsync(
        int rolId,
        IReadOnlyList<int> menuIds,
        CancellationToken ct = default)
    {
        Ensure();
        if (rolId < 1)
            return new MaestroOperacionResponse(false, null, "rolId inválido.");

        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        await using var tx = await c.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            await c.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM MAESTRO.RolMenu WHERE RolId = @RolId;",
                    new { RolId = rolId },
                    transaction: tx,
                    cancellationToken: ct)).ConfigureAwait(false);

            foreach (var menuId in menuIds.Distinct().Where(id => id > 0))
            {
                await c.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO MAESTRO.RolMenu (RolId, MenuId)
                        VALUES (@RolId, @MenuId);
                        """,
                        new { RolId = rolId, MenuId = menuId },
                        transaction: tx,
                        cancellationToken: ct)).ConfigureAwait(false);
            }

            await tx.CommitAsync(ct).ConfigureAwait(false);
            return new MaestroOperacionResponse(true, rolId, null);
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    private void Ensure()
    {
        if (string.IsNullOrWhiteSpace(_cs))
            throw new InvalidOperationException("Configure CreditoDatabase:ConnectionString.");
    }
}
