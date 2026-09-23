namespace Credito.Modern.Application.Dashboard;

/// <summary>
/// Fila de <c>CREDITO.usp_DashboardGestorClientesMora</c>.
/// Tipos válidos: TODOS, SIN_PAGO, NUNCA_PAGO, DEJO_PAGAR, PAGA_CON_ATRASO.
/// </summary>
public sealed record DashboardClienteMoraRowDto(
    int PersonaId,
    string NombreCompleto,
    int CreditosMora,
    decimal SaldoMora,
    DateTime? PrimeraCuotaVencida,
    DateTime? FechaUltimoPago,
    int DiasAtraso,
    string CodigoClasificacion,
    string Clasificacion);

public static class DashboardMoraTipos
{
    public const string Todos = "TODOS";
    public const string SinPago = "SIN_PAGO";
    public const string NuncaPago = "NUNCA_PAGO";
    public const string DejoPagar = "DEJO_PAGAR";
    public const string PagaConAtraso = "PAGA_CON_ATRASO";

    private static readonly HashSet<string> Validos = new(StringComparer.OrdinalIgnoreCase)
    {
        Todos,
        SinPago,
        NuncaPago,
        DejoPagar,
        PagaConAtraso,
    };

    public static string Normalizar(string? tipo)
    {
        var t = string.IsNullOrWhiteSpace(tipo) ? Todos : tipo.Trim().ToUpperInvariant();
        if (!Validos.Contains(t))
        {
            throw new ArgumentOutOfRangeException(
                nameof(tipo),
                "tipo debe ser TODOS, SIN_PAGO, NUNCA_PAGO, DEJO_PAGAR o PAGA_CON_ATRASO.");
        }

        return t;
    }
}
