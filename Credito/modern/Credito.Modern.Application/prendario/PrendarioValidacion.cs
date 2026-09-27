using System.Globalization;
using System.Text.RegularExpressions;
using Credito.Modern.Application.CreditoPlanes;

namespace Credito.Modern.Application.Prendario;

/// <summary>
/// Reglas de validación de crédito prendario (paridad legacy + buenas prácticas de entrada).
/// Mensajes listos para ProblemDetails / toast de UI.
/// </summary>
public static partial class PrendarioValidacion
{
    public const int ProductoIdPrendario = 2;
    public const int MaxDescripcion = 200;
    public const int MaxMarca = 50;
    public const int MaxModelo = 50;
    public const int MaxSerie = 50;
    public const int MaxColor = 50;
    public const int MaxCodigoInterno = 50;
    public const int MaxObservaciones = 500;
    public const decimal MaxValorTasacion = 9_999_999.99m;

    /// <summary>Filtra renglones vacíos del formulario (sin descripción).</summary>
    public static IReadOnlyList<PrendaItemRequest> FiltrarPrendasConDescripcion(
        IEnumerable<PrendaItemRequest>? prendas) =>
        (prendas ?? Array.Empty<PrendaItemRequest>())
            .Where(p => !string.IsNullOrWhiteSpace(p.Descripcion))
            .ToList();

    /// <summary>
    /// Valida el detalle de bienes. Devuelve null si es válido; si no, el primer mensaje de error.
    /// </summary>
    public static string? ValidarPrendas(IReadOnlyList<PrendaItemRequest> prendas)
    {
        if (prendas.Count == 0)
        {
            return "Registre al menos un bien con descripción y tasación mayor a cero.";
        }

        for (var i = 0; i < prendas.Count; i++)
        {
            var n = i + 1;
            var p = prendas[i];
            var desc = p.Descripcion?.Trim() ?? string.Empty;
            if (desc.Length == 0)
            {
                return $"Bien #{n}: la descripción es obligatoria.";
            }

            if (desc.Length > MaxDescripcion)
            {
                return $"Bien #{n}: la descripción no puede superar {MaxDescripcion} caracteres.";
            }

            if (p.ValorTasacion <= 0)
            {
                return $"Bien #{n}: el valor de tasación debe ser mayor a cero.";
            }

            if (p.ValorTasacion > MaxValorTasacion)
            {
                return $"Bien #{n}: el valor de tasación supera el máximo permitido.";
            }

            if (TieneMasDeDosDecimales(p.ValorTasacion))
            {
                return $"Bien #{n}: el valor de tasación admite como máximo 2 decimales.";
            }

            var largo = ValidarLargoOpcional(p.Marca, MaxMarca, n, "marca")
                ?? ValidarLargoOpcional(p.Modelo, MaxModelo, n, "modelo")
                ?? ValidarLargoOpcional(p.Serie, MaxSerie, n, "serie")
                ?? ValidarLargoOpcional(p.Color, MaxColor, n, "color")
                ?? ValidarLargoOpcional(p.CodigoInterno, MaxCodigoInterno, n, "código interno")
                ?? ValidarLargoOpcional(p.Observaciones, MaxObservaciones, n, "observaciones");
            if (largo is not null)
            {
                return largo;
            }
        }

        return null;
    }

    /// <summary>Valida prenda enviada al generar el crédito desde el simulador.</summary>
    public static string? ValidarPrendaGeneracion(CrearCreditoPrendaRequest prenda)
    {
        if (string.IsNullOrWhiteSpace(prenda.Descripcion))
        {
            return "La descripción de la prenda es obligatoria.";
        }

        if (prenda.Descripcion.Trim().Length > MaxDescripcion)
        {
            return $"La descripción de la prenda no puede superar {MaxDescripcion} caracteres.";
        }

        if (prenda.MontoTasacion <= 0)
        {
            return "El monto de tasación debe ser mayor a cero.";
        }

        if (prenda.MontoTasacion > MaxValorTasacion)
        {
            return "El monto de tasación supera el máximo permitido.";
        }

        if (TieneMasDeDosDecimales(prenda.MontoTasacion))
        {
            return "El monto de tasación admite como máximo 2 decimales.";
        }

        if (prenda.FechaRemate == default)
        {
            return "La fecha de remate es obligatoria.";
        }

        return ValidarLargoOpcional(prenda.Observacion, MaxObservaciones, 1, "observación");
    }

    /// <summary>
    /// Reglas al generar/simular crédito prendario: mensual, 1 cuota, monto ≤ tasación, fechas.
    /// </summary>
    public static string? ValidarGeneracionPrendaria(
        string formaPago,
        int numeroCuotas,
        decimal montoCredito,
        decimal montoTasacion,
        DateTime fechaPrimerPago,
        DateTime? fechaServidor = null)
    {
        var modalidad = (formaPago ?? string.Empty).Trim().ToUpperInvariant();
        if (modalidad is not "M")
        {
            return "El crédito prendario solo admite modalidad mensual (M).";
        }

        if (numeroCuotas != 1)
        {
            return "El crédito prendario solo admite 1 cuota.";
        }

        if (montoCredito <= 0)
        {
            return "El monto del crédito debe ser mayor a cero.";
        }

        if (TieneMasDeDosDecimales(montoCredito))
        {
            return "El monto del crédito admite como máximo 2 decimales.";
        }

        if (montoTasacion <= 0)
        {
            return "Debe registrar bienes con tasación antes de generar el crédito.";
        }

        if (montoCredito > montoTasacion)
        {
            return $"El monto del crédito (S/ {montoCredito.ToString("N2", CulturaPe)}) no puede superar la tasación (S/ {montoTasacion.ToString("N2", CulturaPe)}).";
        }

        if (fechaPrimerPago == default)
        {
            return "La fecha del primer pago es obligatoria.";
        }

        var hoy = (fechaServidor ?? DateTime.Today).Date;
        if (fechaPrimerPago.Date < hoy)
        {
            return "La fecha del primer pago no puede ser anterior a hoy.";
        }

        return null;
    }

    /// <summary>Celular móvil peruano: 9 dígitos que empiezan con 9.</summary>
    public static bool EsCelularPeruano(string? celular)
    {
        if (string.IsNullOrWhiteSpace(celular))
        {
            return false;
        }

        var digits = SoloDigitos().Replace(celular, string.Empty);
        return digits.Length == 9 && digits[0] == '9';
    }

    public static string? ValidarCelularObligatorio(string? celular) =>
        EsCelularPeruano(celular)
            ? null
            : "El celular del cliente es obligatorio (9 dígitos que empiezan con 9).";

    private static string? ValidarLargoOpcional(string? valor, int max, int n, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        if (valor.Trim().Length > max)
        {
            return $"Bien #{n}: {campo} no puede superar {max} caracteres.";
        }

        return null;
    }

    private static bool TieneMasDeDosDecimales(decimal valor)
    {
        var scaled = decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
        return scaled != valor;
    }

    private static CultureInfo CulturaPe { get; } = CultureInfo.GetCultureInfo("es-PE");

    [GeneratedRegex(@"\D")]
    private static partial Regex SoloDigitos();
}
