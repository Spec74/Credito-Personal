using Credito.Modern.Application.CreditoPlanes;

using Dapper;

using Microsoft.Data.SqlClient;

using Microsoft.Extensions.Options;



namespace Credito.Modern.Infrastructure.CreditoPlanes;



public sealed class CreditosPorAprobarReadService(IOptions<SqlDatabaseOptions> options)

    : ICreditosPorAprobarReadService

{

    private static readonly HashSet<string> SortColumns = new(StringComparer.OrdinalIgnoreCase)

    {

        "CreditoId",

        "Codigo",

        "Cliente",

        "Documento",

        "Monto",

        "Interes",

        "Agente",

    };



    private readonly string _connectionString = options.Value.ConnectionString;



    public async Task<CreditosPorAprobarListResponse> ListarAsync(

        string? buscar,

        int page,

        int pageSize,

        string sortField,

        string sortOrder,

        CancellationToken cancellationToken = default)

    {

        if (page < 1)

        {

            throw new ArgumentOutOfRangeException(nameof(page), "page debe ser >= 1.");

        }



        if (pageSize is < 1 or > 200)

        {

            throw new ArgumentOutOfRangeException(nameof(pageSize), "pageSize debe estar entre 1 y 200.");

        }



        if (string.IsNullOrWhiteSpace(_connectionString))

        {

            throw new InvalidOperationException(

                "Configure CreditoDatabase:ConnectionString (appsettings, variables de entorno o dotnet user-secrets).");

        }



        var sortCol = SortColumns.Contains(sortField) ? sortField : "Agente";

        var desc = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        var orderSql = desc ? $"{sortCol} DESC" : $"{sortCol} ASC";



        var tokens = CreditoBandejaBusquedaSql.ParseTokens(buscar);

        var offset = (page - 1) * pageSize;



        var parameters = new DynamicParameters();

        parameters.Add("Offset", offset);

        parameters.Add("PageSize", pageSize);

        var whereBusqueda = CreditoBandejaBusquedaSql.BuildWhereClause(tokens, parameters);



        var countSql = $"""

            SELECT COUNT(1)

            FROM CREDITO.Credito AS c

            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId

            INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.UsuarioRegId

            INNER JOIN MAESTRO.Persona AS up ON up.PersonaId = u.PersonaId

            WHERE c.Estado = 'PEN'

              AND ({whereBusqueda});

            """;



        var dataSql = $"""

            SELECT

                c.CreditoId,

                c.PersonaId,

                p.Codigo,

                p.NombreCompleto AS Cliente,

                RTRIM(ISNULL(p.TipoDocumento, '') + ' ' + ISNULL(p.NumeroDocumento, '')) AS Documento,

                c.MontoCredito AS Monto,

                c.Interes,

                up.NombreCompleto AS Agente

            FROM CREDITO.Credito AS c

            INNER JOIN MAESTRO.Persona AS p ON p.PersonaId = c.PersonaId

            INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = c.UsuarioRegId

            INNER JOIN MAESTRO.Persona AS up ON up.PersonaId = u.PersonaId

            WHERE c.Estado = 'PEN'

              AND ({whereBusqueda})

            ORDER BY {orderSql}

            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            """;



        await using var connection = new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);



        var total = await connection.ExecuteScalarAsync<int>(

            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken))

            .ConfigureAwait(false);



        var rows = await connection.QueryAsync<CreditoPorAprobarRowDto>(

            new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken))

            .ConfigureAwait(false);



        return new CreditosPorAprobarListResponse(rows.ToList(), total);

    }

}


