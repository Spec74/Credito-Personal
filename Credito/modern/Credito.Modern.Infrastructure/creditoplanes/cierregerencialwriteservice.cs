using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class CierreGerencialWriteService(IOptions<SqlDatabaseOptions> options)
    : ICierreGerencialWriteService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task GuardarMetasAsync(
        DateTime periodo,
        IReadOnlyList<MetaGerencialGuardarItemDto> metas,
        int usuarioRegistroId,
        CancellationToken cancellationToken = default)
    {
        EnsureConnection();

        if (metas is null || metas.Count == 0)
        {
            throw new ArgumentException("Debe enviar al menos una meta.", nameof(metas));
        }

        if (usuarioRegistroId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(usuarioRegistroId), "La sesión del usuario ha expirado.");
        }

        if (metas.Select(x => x.UsuarioId).Distinct().Count() != metas.Count)
        {
            throw new ArgumentException("La solicitud contiene analistas duplicados.", nameof(metas));
        }

        foreach (var meta in metas)
        {
            ValidarMeta(meta);
        }

        var periodoNormalizado = new DateTime(periodo.Year, periodo.Month, 1);

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = (SqlTransaction)await connection
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            foreach (var meta in metas)
            {
                var p = new DynamicParameters();
                p.Add("@Periodo", periodoNormalizado, DbType.DateTime);
                p.Add("@UsuarioId", meta.UsuarioId, DbType.Int32);
                p.Add("@MetaCapitalCierre", meta.MetaCapitalCierre, DbType.Decimal);
                p.Add("@MetaClientesActivosCierre", meta.MetaClientesActivosCierre, DbType.Int32);
                p.Add("@MetaVencidosMaximoCierre", meta.MetaVencidosMaximoCierre, DbType.Decimal);
                p.Add("@MetaRecuperacionVencidosMes", meta.MetaRecuperacionVencidosMes, DbType.Decimal);
                p.Add("@UsuarioRegistroId", usuarioRegistroId, DbType.Int32);

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "CREDITO.usp_GuardarMetaGerencialDefinitiva",
                        p,
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure,
                        commandTimeout: 180,
                        cancellationToken: cancellationToken)).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }

    private static void ValidarMeta(MetaGerencialGuardarItemDto meta)
    {
        if (meta.UsuarioId < 1)
        {
            throw new ArgumentException("Existe una meta sin analista válido.", nameof(meta));
        }

        var tipo = (meta.TipoCartera ?? string.Empty).Trim().ToUpperInvariant();
        if (tipo is not ("PRODUCTIVA" or "ESPECIAL"))
        {
            throw new ArgumentException("Existe una cartera con tipo no válido.", nameof(meta));
        }

        if (meta.MetaVencidosMaximoCierre is null || meta.MetaRecuperacionVencidosMes is null)
        {
            throw new ArgumentException("Las dos metas de vencidos son obligatorias.", nameof(meta));
        }

        if (tipo == "PRODUCTIVA" &&
            (meta.MetaCapitalCierre is null || meta.MetaClientesActivosCierre is null))
        {
            throw new ArgumentException(
                "Capital y clientes son obligatorios para las carteras productivas.",
                nameof(meta));
        }

        if ((meta.MetaCapitalCierre is < 0) ||
            (meta.MetaClientesActivosCierre is < 0) ||
            meta.MetaVencidosMaximoCierre < 0 ||
            meta.MetaRecuperacionVencidosMes < 0)
        {
            throw new ArgumentException("Las metas no pueden contener valores negativos.", nameof(meta));
        }
    }
}
