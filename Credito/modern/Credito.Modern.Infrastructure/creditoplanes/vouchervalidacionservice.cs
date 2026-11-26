using System.Globalization;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

public sealed class VoucherValidacionService(IOcrEngine ocr) : IVoucherValidacionService
{
    private static readonly HashSet<string> ExtOk = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".jfif",
    };

    public async Task<VoucherValidacionResponse> ValidarCapturaAsync(
        Stream image,
        string fileName,
        decimal? montoEsperado,
        string? canalEsperado = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (!ocr.IsAvailable)
        {
            return new VoucherValidacionResponse(
                Ok: false,
                CanalDetectado: null,
                Monto: null,
                FechaOperacion: null,
                FechaOperacionIso: null,
                FechaOperacionLocal: null,
                CodigoOperacion: null,
                CoincideMonto: false,
                OcrDisponible: false,
                Advertencias:
                [
                    "OCR no disponible en el servidor. Instale Tesseract (idioma spa) o tipifique la hora manualmente.",
                ],
                TextoOcrPreview: null);
        }

        var name = string.IsNullOrWhiteSpace(fileName) ? "voucher.jpg" : fileName.Trim();
        var ext = Path.GetExtension(name);
        if (string.IsNullOrWhiteSpace(ext) || !ExtOk.Contains(ext))
        {
            name = Path.ChangeExtension(Path.GetFileNameWithoutExtension(name) + ".jpg", ".jpg");
        }

        var text = await ocr.ExtractTextAsync(image, name, cancellationToken).ConfigureAwait(false);
        var expectedCanal = VoucherConstanciaParser.CanalDesdeMarcaOTipo(canalEsperado);
        var parsed = VoucherConstanciaParser.Parse(text, montoEsperado, ahora: null, expectedCanal);

        string? iso = null;
        string? local = null;
        if (parsed.FechaOperacion is { } f)
        {
            iso = f.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
            local = f.ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture);
        }

        return new VoucherValidacionResponse(
            Ok: parsed.Ok,
            CanalDetectado: parsed.CanalDetectado,
            Monto: parsed.Monto,
            FechaOperacion: parsed.FechaOperacion,
            FechaOperacionIso: iso,
            FechaOperacionLocal: local,
            CodigoOperacion: parsed.CodigoOperacion,
            CoincideMonto: parsed.CoincideMonto,
            OcrDisponible: true,
            Advertencias: parsed.Advertencias,
            TextoOcrPreview: parsed.TextoOcrPreview);
    }
}
