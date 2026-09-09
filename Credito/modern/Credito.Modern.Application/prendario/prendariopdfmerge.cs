using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Credito.Modern.Application.Prendario;

/// <summary>
/// Anexa el PDF fijo de cláusulas al contrato (paridad <c>ReporteController.MergePdf</c>)
/// y estampa fecha, nombre y DNI sobre los blancos de la última página.
/// </summary>
public static class PrendarioPdfMerge
{
    /// <summary>
    /// Coordenadas medidas sobre <c>ClausulasPrendario.pdf</c> (A4, origen superior).
    /// Cubren «Ayacucho, __ de __ de ____», NOMBRE y DNI.
    /// </summary>
    private const float FirmaTop = 735f;

    public static byte[] ConClausulas(
        byte[] contrato,
        DateTime fechaEmision,
        string cliente,
        string dni)
    {
        var clausulas = Reportes.CredixReportAssets.LoadClausulasPrendario();
        if (clausulas is null || clausulas.Length < 5)
        {
            return contrato;
        }

        var dir = Path.Combine(Path.GetTempPath(), "credito-prendario-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var principal = Path.Combine(dir, "contrato.pdf");
            var anexo = Path.Combine(dir, "clausulas.pdf");
            var sello = Path.Combine(dir, "sello.pdf");
            var salida = Path.Combine(dir, "final.pdf");
            File.WriteAllBytes(principal, contrato);
            File.WriteAllBytes(anexo, clausulas);
            File.WriteAllBytes(sello, BuildSello(fechaEmision, cliente, dni));
            DocumentOperation
                .LoadFile(principal)
                .MergeFile(anexo)
                .OverlayFile(new DocumentOperation.LayerConfiguration
                {
                    FilePath = sello,
                    TargetPages = "z",
                    SourcePages = "1",
                })
                .Save(salida);
            return File.ReadAllBytes(salida);
        }
        finally
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    public static byte[] BuildSello(DateTime fechaEmision, string cliente, string dni)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var fecha = PrendarioPdfTexto.FechaCiudad(fechaEmision);
        var nombre = PrendarioPdfTexto.Texto(cliente);
        var documento = PrendarioPdfTexto.Texto(dni);

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);
                page.Content().Layers(layers =>
                {
                    Tap(layers, 16, 622, 340, 22);
                    Tap(layers, 76, 736, 240, 16);
                    Tap(layers, 380, 742, 200, 16);
                    Tap(layers, 52, 750, 170, 16);

                    layers.PrimaryLayer().Column(col =>
                    {
                        col.Item().Height(624);
                        col.Item().Height(18).PaddingLeft(20).AlignMiddle()
                            .Text(fecha).FontSize(10).SemiBold();
                        col.Item().Height(FirmaTop - 624 - 18);
                        col.Item().Height(16).PaddingLeft(80).AlignMiddle()
                            .Text(nombre).FontSize(9);
                        col.Item().Height(14).PaddingLeft(54).AlignMiddle()
                            .Text(documento).FontSize(9);
                    });

                    layers.Layer().Unconstrained().TranslateX(380).TranslateY(742)
                        .Width(200).Height(16).AlignMiddle().AlignCenter()
                        .Text(PrendarioPdfTexto.EmpresaContrato).FontSize(7);
                });
            });
        }).GeneratePdf();
    }

    private static void Tap(LayersDescriptor layers, float x, float y, float width, float height) =>
        layers.Layer().Unconstrained().TranslateX(x).TranslateY(y)
            .Width(width).Height(height).Image(WhitePixel).FitUnproportionally();

    // PNG 1×1 blanco: tapa los guiones del PDF legal sin reescribir las cláusulas.
    private static readonly byte[] WhitePixel = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGP4//8/AAX+Av4N70a4AAAAAElFTkSuQmCC");
}
