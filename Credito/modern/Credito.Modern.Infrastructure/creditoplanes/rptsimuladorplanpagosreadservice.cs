using System.Globalization;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.CreditoTasas;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptSimuladorPlanPagosReadService(
    IOptions<SqlDatabaseOptions> options,
    ISimuladorCreditoReadService simulador,
    ICalcularTemService calcularTem)
    : IRptSimuladorPlanPagosReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<RptSimuladorPlanPagosInformeDto?> GenerarAsync(
        RptSimuladorPlanPagosQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Monto <= 0 || query.NroCuotas < 1 || query.ProductoId < 1)
        {
            return null;
        }

        if (!FormaPagoCredito.TryNormalizar(query.FormaPago, out var formaPago, out _))
        {
            return null;
        }

        var ga = string.IsNullOrWhiteSpace(query.Ga) ? "CAP" : query.Ga.Trim().ToUpperInvariant();
        var gastosAdmSp = ga == "CUO" ? query.GastosAdm : 0m;

        var cuotas = await simulador
            .SimularAsync(
                query.Monto,
                formaPago,
                query.NroCuotas,
                query.InteresMensual,
                query.FechaPrimerPago,
                gastosAdmSp,
                cancellationToken)
            .ConfigureAwait(false);

        if (cuotas.Count == 0)
        {
            return null;
        }

        var producto = await ObtenerProductoAsync(query.ProductoId, cancellationToken).ConfigureAwait(false);
        if (producto is null)
        {
            return null;
        }

        var modalidadLabel = MapModalidad(formaPago);
        var desemb = ga == "CAP"
            ? query.Monto - query.GastosAdm
            : query.Monto;
        var tem = await calcularTem
            .CalcularTemAsync(query.InteresMensual, formaPago, cancellationToken)
            .ConfigureAwait(false);
        var temTxt = tem.HasValue ? $"{tem.Value.ToString(CultureInfo.InvariantCulture)}%" : $"{query.InteresMensual}%";

        var inv = CultureInfo.InvariantCulture;
        var cabecera = new RptSimuladorPlanPagosCabeceraDto(
            $"S/. {query.Monto.ToString(inv)}",
            query.NroCuotas.ToString(inv),
            producto,
            query.FechaPrimerPago.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            modalidadLabel,
            query.Cliente ?? string.Empty,
            temTxt,
            $"S/. {desemb.ToString(inv)}",
            $"S/. {Math.Round(query.GastosAdm, 2).ToString("0.00", CultureInfo.InvariantCulture)}");

        return new RptSimuladorPlanPagosInformeDto(cabecera, cuotas);
    }

    private async Task<string?> ObtenerProductoAsync(int productoId, CancellationToken cancellationToken)
    {
        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<string>(
            new CommandDefinition(
                "SELECT Denominacion FROM CREDITO.Producto WHERE ProductoId = @ProductoId;",
                new { ProductoId = productoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);
    }

    private static string MapModalidad(string formaPago) =>
        formaPago switch
        {
            "D" => "DIARIO",
            "S" => "SEMANAL",
            "Q" => "QUINCENAL",
            "M" => "MENSUAL",
            _ => formaPago,
        };

    private void EnsureConnection()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }
    }
}
