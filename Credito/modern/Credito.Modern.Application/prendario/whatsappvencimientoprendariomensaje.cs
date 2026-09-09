using System.Globalization;
using System.Text.Json;

namespace Credito.Modern.Application.Prendario;

/// <summary>
/// Cuerpo de plantilla <c>aviso_vencimiento_prendario</c>: {{1}} nombre, {{2}} fecha, {{3}} importe.
/// </summary>
public static class WhatsAppVencimientoPrendarioMensaje
{
    public static string? NormalizarCelularPeru(string? celular)
    {
        if (string.IsNullOrWhiteSpace(celular))
        {
            return null;
        }

        var limpio = new string([.. celular.Where(char.IsDigit)]);
        if (limpio.Length == 9)
        {
            return "51" + limpio;
        }

        if (limpio.Length == 11 && limpio.StartsWith("51", StringComparison.Ordinal))
        {
            return limpio;
        }

        return limpio.Length >= 9 ? limpio : null;
    }

    public static string FormatearFecha(DateTime fecha) =>
        fecha.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-PE"));

    public static string FormatearImporte(decimal monto) =>
        monto.ToString("0.00", CultureInfo.InvariantCulture);

    public static string CrearJson(
        string celularE164,
        string nombreCliente,
        DateTime fechaVencimiento,
        decimal montoCancelar,
        string templateName,
        string templateLang)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to = celularE164,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = templateLang },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = new object[]
                        {
                            new { type = "text", text = Truncar(nombreCliente, 60) },
                            new { type = "text", text = FormatearFecha(fechaVencimiento) },
                            new { type = "text", text = FormatearImporte(montoCancelar) },
                        },
                    },
                },
            },
        };
        return JsonSerializer.Serialize(payload);
    }

    private static string Truncar(string valor, int max)
    {
        var limpio = string.IsNullOrWhiteSpace(valor) ? "cliente" : valor.Trim();
        return limpio.Length <= max ? limpio : limpio[..max];
    }
}
