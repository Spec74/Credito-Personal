using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CuentaPorCobrarPagoWriteService(IOptions<SqlDatabaseOptions> options)
    : ICuentaPorCobrarPagoWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<PagoCajaResultResponse> PagarAsync(
        int ordenVentaId,
        int cuentaxCobrarId,
        int cajaDiarioId,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (cajaDiarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cajaDiarioId), "cajaDiarioId debe ser >= 1.");
        }

        if (usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "usuarioId debe ser >= 1.");
        }

        if (cuentaxCobrarId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(cuentaxCobrarId), "cuentaxCobrarId debe ser >= 0.");
        }

        if (cuentaxCobrarId == 0 && ordenVentaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(ordenVentaId), "ordenVentaId debe ser >= 1 cuando cuentaxCobrarId es 0.");
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
            var command = new CommandDefinition(
                "CREDITO.usp_PagarCuentaxCobrar",
                new
                {
                    OrdenVentaId = ordenVentaId,
                    CuentaxCobrarId = cuentaxCobrarId,
                    CajaDiarioId = cajaDiarioId,
                    UsuarioId = usuarioId,
                },
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);
            var resultId = await connection.QueryFirstOrDefaultAsync<int?>(command).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new PagoCajaResultResponse(resultId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
