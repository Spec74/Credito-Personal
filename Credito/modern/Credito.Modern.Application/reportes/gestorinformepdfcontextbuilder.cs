using System.Globalization;
using Credito.Modern.Application.CajaMaestro;
using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Oficinas;
using Credito.Modern.Application.UsuariosAdmin;

namespace Credito.Modern.Application.Reportes;

/// <summary>Contexto PDF (metadatos RDLC) para informes por gestor.</summary>
public static class GestorInformePdfContextBuilder
{
    public static async Task<CredixLegacyReportContext> BuildCobroDiarioAsync(
        IReadOnlyList<RptCobroDiarioRowDto> items,
        int? usuarioId,
        bool soloMora,
        IUsuarioAdminReadService usuarios,
        ICajaMaestroReadService cajas,
        CancellationToken cancellationToken = default)
    {
        var saldoPendiente = items.Sum(x => x.Saldo ?? 0m);
        var saldoMora = items.Where(x => (x.Mora ?? 0m) > 0).Sum(x => x.Saldo ?? 0m);
        var cult = CultureInfo.CurrentCulture;
        var inv = CultureInfo.InvariantCulture;

        var agente = await ResolveAgenteLabelAsync(usuarioId, soloMora, usuarios, cancellationToken)
            .ConfigureAwait(false);
        var caja = usuarioId is > 0
            ? await cajas.GetDenominacionPorCajeroAsync(usuarioId.Value, cancellationToken).ConfigureAwait(false)
                ?? string.Empty
            : string.Empty;

        return new CredixLegacyReportContext
        {
            Fecha = DateTime.Now.ToString("d", cult),
            Agente = agente,
            Caja = caja,
            NroClientes = items.Count.ToString(inv),
            SaldoVencido = saldoPendiente.ToString("N2", cult),
            SaldoMoroso = saldoMora.ToString("N2", cult),
        };
    }

    public static async Task<CredixLegacyReportContext> BuildGestorOficinaAsync(
        int oficinaId,
        int? usuarioId,
        IUsuarioAdminReadService usuarios,
        IOficinaReadService oficinas,
        CancellationToken cancellationToken = default,
        bool fechaConPrefijoAl = false,
        DateTime? fecha = null)
    {
        var (gestor, oficinaNom) = await CobranzaPagosExportLabels
            .ResolveAsync(usuarioId, oficinaId, usuarios, oficinas, cancellationToken)
            .ConfigureAwait(false);

        var cult = CultureInfo.CurrentCulture;
        var f = (fecha ?? DateTime.Today).ToString("d", cult);
        return new CredixLegacyReportContext
        {
            Fecha = fechaConPrefijoAl ? $" AL {f}" : f,
            Oficina = ToLegacyTodos(oficinaNom),
            Agente = ToLegacyTodos(gestor),
        };
    }

    public static async Task<CredixLegacyReportContext> BuildClientesNuevosMesAsync(
        int oficinaId,
        int? usuarioId,
        DateTime fechaIni,
        DateTime fechaFin,
        IUsuarioAdminReadService usuarios,
        IOficinaReadService oficinas,
        CancellationToken cancellationToken = default)
    {
        var baseCtx = await BuildGestorOficinaAsync(
                oficinaId,
                usuarioId,
                usuarios,
                oficinas,
                cancellationToken)
            .ConfigureAwait(false);
        var cult = CultureInfo.CurrentCulture;
        var titulo =
            $"CLIENTES NUEVOS DEL {fechaIni.ToString("d", cult)} AL {fechaFin.ToString("d", cult)}";

        return baseCtx with
        {
            Titulo = titulo,
            FechaIni = fechaIni.ToString("d", cult),
            FechaFin = fechaFin.ToString("d", cult),
        };
    }

    private static async Task<string> ResolveAgenteLabelAsync(
        int? usuarioId,
        bool permiteTodos,
        IUsuarioAdminReadService usuarios,
        CancellationToken cancellationToken)
    {
        if (usuarioId is null or < 1)
        {
            return permiteTodos ? "TODOS" : "TODOS";
        }

        var detalle = await usuarios.GetDetalleAsync(usuarioId.Value, cancellationToken).ConfigureAwait(false);
        if (detalle is null)
        {
            return $"Gestor #{usuarioId.Value}";
        }

        return string.IsNullOrWhiteSpace(detalle.NombreCompleto)
            ? detalle.NombreUsuario
            : detalle.NombreCompleto;
    }

    private static string ToLegacyTodos(string label) =>
        label.Equals("Todos", StringComparison.OrdinalIgnoreCase)
        || label.Equals("Todas", StringComparison.OrdinalIgnoreCase)
            ? "TODOS"
            : label;
}
