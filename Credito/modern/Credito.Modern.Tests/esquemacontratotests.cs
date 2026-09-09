using System.Text.RegularExpressions;

namespace Credito.Modern.Tests;

/// <summary>
/// Contrasta el SQL embebido en Infrastructure contra el snapshot de esquema en <c>db/schema</c>.
/// Detecta referencias a tablas o procedimientos inexistentes (por ejemplo un esquema equivocado)
/// sin necesidad de conexion a SQL Server, para que corra en CI.
/// Regenerar el snapshot con <c>deploy/scripts/export-db-schema.ps1</c> tras cada entrega de base.
/// </summary>
public sealed class EsquemaContratoTests
{
    private static readonly Regex TablaReferenciada = new(
        @"\b(?:FROM|JOIN|INTO|UPDATE)\s+\[?(?<esquema>ALMACEN|CREDITO|MAESTRO|VENTAS)\]?\.\[?(?<tabla>\w+)\]?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RutinaReferenciada = new(
        @"\[?(?<esquema>ALMACEN|CREDITO|MAESTRO|VENTAS)\]?\.\[?(?<rutina>usp_\w+|fn_\w+|udf_\w+)\]?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [Fact]
    public void TablasReferenciadasExistenEnElSnapshot()
    {
        var snapshot = CargarSnapshot("tables");
        var faltantes = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (archivo, contenido) in LeerFuentesInfrastructure())
        {
            foreach (Match match in TablaReferenciada.Matches(contenido))
            {
                var nombre = $"{match.Groups["esquema"].Value.ToUpperInvariant()}.{match.Groups["tabla"].Value}";
                if (!snapshot.Contains(nombre))
                {
                    faltantes.Add($"{nombre}  ({Path.GetFileName(archivo)})");
                }
            }
        }

        Assert.True(
            faltantes.Count == 0,
            "Hay tablas referenciadas que no existen en el snapshot de esquema:" +
            Environment.NewLine + string.Join(Environment.NewLine, faltantes));
    }

    [Fact]
    public void RutinasReferenciadasExistenEnElSnapshot()
    {
        var snapshot = CargarSnapshot(Path.Combine("routines", "procedures"));
        snapshot.UnionWith(CargarSnapshot(Path.Combine("routines", "functions")));
        var faltantes = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (archivo, contenido) in LeerFuentesInfrastructure())
        {
            foreach (Match match in RutinaReferenciada.Matches(contenido))
            {
                var nombre = $"{match.Groups["esquema"].Value.ToUpperInvariant()}.{match.Groups["rutina"].Value}";
                if (!snapshot.Contains(nombre))
                {
                    faltantes.Add($"{nombre}  ({Path.GetFileName(archivo)})");
                }
            }
        }

        Assert.True(
            faltantes.Count == 0,
            "Hay procedimientos o funciones referenciados que no existen en el snapshot de esquema:" +
            Environment.NewLine + string.Join(Environment.NewLine, faltantes));
    }

    private static HashSet<string> CargarSnapshot(string subcarpeta)
    {
        var carpeta = Path.Combine(RaizModern(), "db", "schema", subcarpeta);
        Assert.True(
            Directory.Exists(carpeta),
            $"Falta el snapshot de esquema en '{carpeta}'. Ejecute deploy/scripts/export-db-schema.ps1.");

        return Directory
            .EnumerateFiles(carpeta, "*.sql")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<(string Archivo, string Contenido)> LeerFuentesInfrastructure()
    {
        var carpeta = Path.Combine(RaizModern(), "Credito.Modern.Infrastructure");
        foreach (var archivo in Directory.EnumerateFiles(carpeta, "*.cs", SearchOption.AllDirectories))
        {
            if (archivo.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                || archivo.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            {
                continue;
            }

            yield return (archivo, File.ReadAllText(archivo));
        }
    }

    private static string RaizModern()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null)
        {
            if (File.Exists(Path.Combine(directorio.FullName, "credito.modern.sln")))
            {
                return directorio.FullName;
            }

            directorio = directorio.Parent;
        }

        throw new InvalidOperationException("No se ubico la raiz de Credito/modern (credito.modern.sln).");
    }
}
