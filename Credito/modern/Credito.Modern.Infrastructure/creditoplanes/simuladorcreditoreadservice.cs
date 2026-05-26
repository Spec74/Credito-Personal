using System.Data;
using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class SimuladorCreditoReadService(IOptions<SqlDatabaseOptions> options)
    : ISimuladorCreditoReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<IReadOnlyList<SimuladorCreditoCuotaDto>> SimularAsync(
        decimal monto,
        string formaPago,
        int nroCuotas,
        decimal interesMensual,
        DateTime fechaPrimerPago,
        decimal gastosAdm,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException(
                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");
        }

        if (monto <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monto), "monto debe ser > 0 para ejecutar el simulador en SQL.");
        }

        if (nroCuotas < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(nroCuotas), "nroCuotas debe ser >= 1.");
        }

        if (string.IsNullOrEmpty(formaPago))
        {
            throw new ArgumentException("formaPago no puede estar vacía.", nameof(formaPago));
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            "CREDITO.usp_SimuladorCredito",
            new
            {
                Monto = monto,
                FormaPago = formaPago,
                NroCuotas = nroCuotas,
                InteresMensual = interesMensual,
                FechaPrimerPago = fechaPrimerPago,
                GastosAdm = gastosAdm,
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<SimuladorCreditoCuotaDto>(command).ConfigureAwait(false);
        return rows.ToList();
    }
}
