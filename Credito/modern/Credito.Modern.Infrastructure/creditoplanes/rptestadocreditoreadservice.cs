using Credito.Modern.Application.CreditoPlanes;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class RptEstadoCreditoReadService(
    IOptions<SqlDatabaseOptions> options,
    IEstadoPlanPagoReadService estadoPlanPago)
    : IRptEstadoCreditoReadService
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<RptEstadoCreditoInformeDto?> ObtenerAsync(
        int creditoId,
        CancellationToken cancellationToken = default)
    {
        if (creditoId < 1)
        {
            return null;
        }

        EnsureConnection();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var cab = await connection.QueryFirstOrDefaultAsync<CabeceraRow>(
            new CommandDefinition(
                """
                SELECT
                    c.CreditoId,
                    c.PersonaId,
                    pr.Denominacion AS Producto,
                    c.FechaPrimerPago,
                    c.FechaVencimiento,
                    c.MontoCredito,
                    c.FormaPago,
                    c.NumeroCuotas,
                    c.Interes,
                    c.Estado,
                    p.Codigo AS CodigoPersona,
                    p.NumeroDocumento,
                    p.NombreCompleto,
                    u.NombreUsuario AS Analista,
                    c.MontoGastosAdm
                FROM CREDITO.Credito AS c
                INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId
                INNER JOIN CREDITO.Producto AS pr ON pr.ProductoId = c.ProductoId
                INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.UsuarioRegId
                WHERE c.CreditoId = @CreditoId;
                """,
                new { CreditoId = creditoId },
                cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (cab is null)
        {
            return null;
        }

        var cuotas = await estadoPlanPago.ListarPorCreditoAsync(creditoId, cancellationToken).ConfigureAwait(false);
        var total = cab.MontoCredito + cuotas.Sum(x => x.Interes);

        var cabecera = new RptEstadoCreditoCabeceraDto(
            cab.CreditoId,
            cab.PersonaId,
            cab.Producto,
            cab.FechaPrimerPago,
            cab.FechaVencimiento,
            cab.MontoCredito,
            MapModalidad(cab.FormaPago),
            cab.NumeroCuotas,
            cab.Interes,
            cab.Estado,
            cab.CodigoPersona ?? string.Empty,
            $"{cab.NumeroDocumento} {cab.NombreCompleto}".Trim(),
            cab.Analista ?? string.Empty,
            cab.MontoGastosAdm,
            total);

        return new RptEstadoCreditoInformeDto(cabecera, cuotas);
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

    private sealed class CabeceraRow
    {
        public int CreditoId { get; init; }
        public int PersonaId { get; init; }
        public string Producto { get; init; } = string.Empty;
        public DateTime FechaPrimerPago { get; init; }
        public DateTime FechaVencimiento { get; init; }
        public decimal MontoCredito { get; init; }
        public string FormaPago { get; init; } = string.Empty;
        public int NumeroCuotas { get; init; }
        public decimal Interes { get; init; }
        public string Estado { get; init; } = string.Empty;
        public string? CodigoPersona { get; init; }
        public string NumeroDocumento { get; init; } = string.Empty;
        public string NombreCompleto { get; init; } = string.Empty;
        public string? Analista { get; init; }
        public decimal MontoGastosAdm { get; init; }
    }
}
