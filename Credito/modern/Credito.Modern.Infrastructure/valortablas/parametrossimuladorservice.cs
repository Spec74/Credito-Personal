using Credito.Modern.Application.ValorTablas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.ValorTablas;

public sealed class ParametrosSimuladorService(IOptions<SqlDatabaseOptions> options) : IParametrosSimuladorService
{
    private const int TablaId = 3;
    private const int ItemFactorVariable = 1;
    private const int ItemFactorFijo = 2;
    private readonly string _cs = options.Value.ConnectionString;

    public async Task<ParametrosSimuladorDto> ObtenerAsync(CancellationToken ct = default)
    {
        Ensure();
        const string sql = """
            SELECT ItemId, Valor
            FROM MAESTRO.ValorTabla
            WHERE TablaId = @TablaId AND ItemId IN (@ItemVar, @ItemFijo);
            """;
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        var rows = (await c.QueryAsync<(int ItemId, string? Valor)>(
            new CommandDefinition(
                sql,
                new { TablaId, ItemVar = ItemFactorVariable, ItemFijo = ItemFactorFijo },
                cancellationToken: ct))
            .ConfigureAwait(false)).ToDictionary(x => x.ItemId, x => x.Valor ?? string.Empty);

        return new ParametrosSimuladorDto(
            rows.GetValueOrDefault(ItemFactorVariable, string.Empty),
            rows.GetValueOrDefault(ItemFactorFijo, string.Empty));
    }

    public async Task<bool> ActualizarAsync(ActualizarParametrosSimuladorRequest request, CancellationToken ct = default)
    {
        Ensure();
        await using var c = new SqlConnection(_cs);
        await c.OpenAsync(ct).ConfigureAwait(false);
        await using var tx = await c.BeginTransactionAsync(ct).ConfigureAwait(false);
        try
        {
            await UpdateItemAsync(c, tx, ItemFactorVariable, request.FactorVariable, ct).ConfigureAwait(false);
            await UpdateItemAsync(c, tx, ItemFactorFijo, request.FactorFijo, ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch
        {
            await tx.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task UpdateItemAsync(
        SqlConnection c,
        System.Data.Common.DbTransaction tx,
        int itemId,
        string valor,
        CancellationToken ct)
    {
        var n = await c.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE MAESTRO.ValorTabla
                SET Valor = @Valor
                WHERE TablaId = @TablaId AND ItemId = @ItemId;
                """,
                new { TablaId, ItemId = itemId, Valor = valor ?? string.Empty },
                transaction: tx,
                cancellationToken: ct)).ConfigureAwait(false);
        if (n < 1)
            throw new InvalidOperationException($"No existe ValorTabla TablaId={TablaId} ItemId={itemId}.");
    }

    private void Ensure()
    {
        if (string.IsNullOrWhiteSpace(_cs))
            throw new InvalidOperationException("Configure CreditoDatabase:ConnectionString.");
    }
}
