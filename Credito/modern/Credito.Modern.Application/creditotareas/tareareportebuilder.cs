using System.Globalization;

namespace Credito.Modern.Application.CreditoTareas;

/// <summary>
/// Construye filas de reporte ordenadas por nombre de cliente (agrupación en PDF).
/// </summary>
public static class TareaReporteBuilder
{
    public static IReadOnlyList<TareaReporteRowDto> ConstruirFilasOrdenadas(
        IReadOnlyList<TareaListItemDto> tareas,
        IReadOnlyDictionary<int, IReadOnlyList<SubtareaDto>> subtareasPorTarea,
        string? estadoFiltro)
    {
        var ordenadas = tareas
            .OrderBy(t => t.ClienteNombre.Trim(), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(t => t.ClienteDni.Trim(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(t => t.TareaId)
            .ToList();

        var resultado = new List<TareaReporteRowDto>(ordenadas.Count);
        var nro = 1;
        foreach (var t in ordenadas)
        {
            subtareasPorTarea.TryGetValue(t.TareaId, out var subs);
            var lineas = (subs ?? Array.Empty<SubtareaDto>())
                .Select(s => (s.Completada ? "[Ok] " : "[X ] ") + s.Titulo)
                .ToList();

            resultado.Add(
                new TareaReporteRowDto
                {
                    Nro = nro++,
                    TareaId = t.TareaId,
                    CreditoId = t.CreditoId,
                    ClienteNombre = t.ClienteNombre,
                    ClienteDni = t.ClienteDni,
                    Cliente =
                        $"{t.ClienteDni} - {t.ClienteNombre} (Crédito #{t.CreditoId} - S/. {t.MontoCredito.ToString("N2", CultureInfo.InvariantCulture)})",
                    Analista = t.NombreUsuario ?? string.Empty,
                    SubtareasResumen = $"{t.SubtareasCompletadas}/{t.TotalSubtareas}",
                    DetalleSubtareas = string.Join(Environment.NewLine, lineas),
                    Estado = t.Estado,
                });
        }

        return resultado;
    }

    public static IReadOnlyList<TareaReporteClienteGrupo> AgruparPorCliente(
        IReadOnlyList<TareaReporteRowDto> filas)
    {
        return filas
            .GroupBy(f => TareaBusquedaTerminos.ClaveOrdenCliente(f.ClienteNombre, f.ClienteDni))
            .OrderBy(g => g.First().ClienteNombre, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(g => g.First().ClienteDni, StringComparer.CurrentCultureIgnoreCase)
            .Select(g =>
            {
                var first = g.First();
                return new TareaReporteClienteGrupo(
                    first.ClienteNombre,
                    first.ClienteDni,
                    first.Cliente,
                    g.OrderBy(r => r.CreditoId).ThenBy(r => r.TareaId).ToList());
            })
            .ToList();
    }

    public static string TituloPdf(string? estado)
    {
        var e = string.IsNullOrWhiteSpace(estado) ? "PEN" : estado.Trim().ToUpperInvariant();
        return e switch
        {
            "COM" => "REPORTE DE CRÉDITOS CON TAREAS COMPLETADAS",
            "" or "TODAS" => "REPORTE DE CRÉDITOS CON TAREAS",
            _ => "REPORTE DE CRÉDITOS CON TAREAS PENDIENTES",
        };
    }
}

public sealed record TareaReporteClienteGrupo(
    string ClienteNombre,
    string ClienteDni,
    string ClienteEtiqueta,
    IReadOnlyList<TareaReporteRowDto> Tareas);
