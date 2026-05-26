using Credito.Modern.Application.CajaMaestro;
using Credito.Modern.Application.Maestros;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CajaMaestro;

public sealed class CajaMaestroWriteService(IOptions<SqlDatabaseOptions> options) : ICajaMaestroWriteService
{
    private readonly string _cs = options.Value.ConnectionString;

    public async Task<MaestroOperacionResponse> GuardarAsync(
        GuardarCajaRequest request,
        int usuarioId,
        DateTime fechaServidor,
        CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);

        if (request.CajaId < 1)
        {
            var id = await c.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    INSERT INTO CREDITO.Caja (
                        OficinaId, Denominacion, Estado, UsuarioRegId, FechaReg, IndAbierto, CajeroId)
                    VALUES (
                        @OficinaId, @Denominacion, @Estado, @UsuarioRegId, @FechaReg, CAST(0 AS bit), @CajeroId);
                    SELECT CAST(SCOPE_IDENTITY() AS int);
                    """,
                    new
                    {
                        request.OficinaId,
                        request.Denominacion,
                        request.Estado,
                        UsuarioRegId = usuarioId,
                        FechaReg = fechaServidor,
                        request.CajeroId,
                    },
                    cancellationToken: ct)).ConfigureAwait(false);
            return new MaestroOperacionResponse(true, id, null);
        }

        var n = await c.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE CREDITO.Caja
                SET OficinaId = @OficinaId,
                    Denominacion = @Denominacion,
                    CajeroId = @CajeroId,
                    Estado = @Estado,
                    FechaMod = @FechaMod,
                    UsuarioModId = @UsuarioModId
                WHERE CajaId = @CajaId;
                """,
                new
                {
                    request.CajaId,
                    request.OficinaId,
                    request.Denominacion,
                    request.CajeroId,
                    request.Estado,
                    FechaMod = fechaServidor,
                    UsuarioModId = usuarioId,
                },
                cancellationToken: ct)).ConfigureAwait(false);

        return n > 0
            ? new MaestroOperacionResponse(true, request.CajaId, null)
            : new MaestroOperacionResponse(false, null, "Caja no encontrada.");
    }

    public async Task<MaestroOperacionResponse> ActivarAsync(
        int cajaId,
        int usuarioId,
        DateTime fechaServidor,
        CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var n = await c.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE CREDITO.Caja
                SET Estado = CASE WHEN Estado = CAST(1 AS bit) THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END,
                    FechaMod = @FechaMod,
                    UsuarioModId = @UsuarioModId
                WHERE CajaId = @CajaId;
                """,
                new { CajaId = cajaId, FechaMod = fechaServidor, UsuarioModId = usuarioId },
                cancellationToken: ct)).ConfigureAwait(false);
        return n > 0
            ? new MaestroOperacionResponse(true, cajaId, null)
            : new MaestroOperacionResponse(false, null, "Caja no encontrada.");
    }

    private void Ensure()
    {
        if (string.IsNullOrWhiteSpace(_cs))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }
}
