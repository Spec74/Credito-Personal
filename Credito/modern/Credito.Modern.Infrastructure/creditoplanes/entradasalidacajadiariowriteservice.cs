using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class EntradaSalidaCajaDiarioWriteService(IOptions<SqlDatabaseOptions> options)
    : IEntradaSalidaCajaDiarioWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<EntradaSalidaCajaDiarioResponse> EjecutarAsync(
        int cajaDiarioId,
        int personaId,
        int tipoOperacionId,
        decimal importe,
        string descripcion,
        int usuarioId,
        int tipoPagoId,
        CancellationToken cancellationToken = default)
    {
        if (cajaDiarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(cajaDiarioId), "cajaDiarioId debe ser >= 1.");
        }

        if (personaId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(personaId), "personaId debe ser >= 1.");
        }

        if (tipoOperacionId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(tipoOperacionId), "tipoOperacionId debe ser >= 1.");
        }

        if (importe <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(importe), "importe debe ser > 0.");
        }

        if (tipoPagoId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(tipoPagoId), "tipoPagoId debe ser >= 1.");
        }

        if (usuarioId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioId), "usuarioId debe ser >= 1.");
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
            var dynamicParams = new DynamicParameters(new
            {
                CajaDiarioId = cajaDiarioId,
                PersonaId = personaId,
                TipoOperacionId = tipoOperacionId,
                Importe = importe,
                Decripcion = descripcion,
                UsuarioId = usuarioId,
                TipoPagoId = tipoPagoId,
            });
            dynamicParams.Add("ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);
            var command = new CommandDefinition(
                "CREDITO.usp_EntradaSalidaCajaDiario",
                dynamicParams,
                transaction: transaction,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command).ConfigureAwait(false);
            var resultCode = dynamicParams.Get<int>("ReturnValue");
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return new EntradaSalidaCajaDiarioResponse(resultCode);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
