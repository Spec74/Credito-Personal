using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CajaChicaEntradaSalidaWriteService(IOptions<SqlDatabaseOptions> options)
    : ICajaChicaEntradaSalidaWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<(string? Error, EntradaSalidaCajaDiarioResponse? Result)> EjecutarAsync(
        int oficinaId,
        int personaId,
        int tipoOperacionId,
        decimal importe,
        string descripcion,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (personaId < 1 || tipoOperacionId < 1 || usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(personaId), "Parámetros inválidos.");
        }

        if (importe <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(importe), "importe debe ser > 0.");
        }

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            return ("Ingrese Descripción", null);
        }

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var cajaChica = await connection.QueryFirstOrDefaultAsync<CajaChicaRow>(
                new CommandDefinition(
                    """
                    SELECT Id, SaldoFinal
                    FROM CREDITO.CajaChicaDiario
                    WHERE UsuarioId = @UsuarioId
                      AND IndCierre = CAST(0 AS bit);
                    """,
                    new { UsuarioId = usuarioId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (cajaChica is null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("No existe caja chica abierta para el usuario.", null);
            }

            var tipoOp = await connection.QueryFirstOrDefaultAsync<TipoOperacionRow>(
                new CommandDefinition(
                    """
                    SELECT IndEntrada
                    FROM MAESTRO.TipoOperacion
                    WHERE TipoOperacionId = @TipoOperacionId;
                    """,
                    new { TipoOperacionId = tipoOperacionId },
                    transaction: transaction,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            if (tipoOp is null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("Tipo de operación no válido.", null);
            }

            if (!tipoOp.IndEntrada && importe > cajaChica.SaldoFinal)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return ("Saldo Insuficiente!", null);
            }

            var dynamicParams = new DynamicParameters(new
            {
                CajaChicaDiarioId = cajaChica.Id,
                PersonaId = personaId,
                TipoOperacionId = tipoOperacionId,
                Importe = importe,
                Decripcion = descripcion,
                UsuarioId = usuarioId,
            });
            dynamicParams.Add("ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "CREDITO.usp_EntradaSalidaCajaChicaDiario",
                    dynamicParams,
                    transaction: transaction,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken)).ConfigureAwait(false);

            var resultCode = dynamicParams.Get<int>("ReturnValue");
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            if (resultCode < 0)
            {
                return ($"El procedimiento devolvió código {resultCode}.", new EntradaSalidaCajaDiarioResponse(resultCode));
            }

            return (null, new EntradaSalidaCajaDiarioResponse(resultCode));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private sealed class CajaChicaRow
    {
        public int Id { get; init; }
        public decimal SaldoFinal { get; init; }
    }

    private sealed class TipoOperacionRow
    {
        public bool IndEntrada { get; init; }
    }
}
