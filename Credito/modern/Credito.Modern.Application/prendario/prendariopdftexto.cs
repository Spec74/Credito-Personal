using System.Globalization;

namespace Credito.Modern.Application.Prendario;

/// <summary>Textos y formatos de los reportes prendarios (paridad RDLC).</summary>
public static class PrendarioPdfTexto
{
    public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-PE");

    public const string EmpresaContrato = "GRUPO CREDICONFIANZA S.A.C.";
    public const string EmpresaActa = "INVERSIONES CREDICONFIABLE S.A.C.";
    public const string EjecutivoCargo = "ANALISTA DE CRÉDITOS";
    public const decimal RgAdmPorcentaje = 1m;
    public const decimal IgvPorcentaje = 18m;
    public const decimal ComisionVentaPorcentaje = 5m;

    public static string FechaCorta(DateTime fecha) =>
        fecha.ToString("dd/MM/yyyy", Cultura);

    public static string FechaLarga(DateTime fecha) =>
        fecha.ToString("dd 'de' MMMM 'de' yyyy", Cultura);

    public static string FechaLargaConDia(DateTime fecha)
    {
        var diaSemana = fecha.ToString("dddd", Cultura);
        var mes = MesNombre(fecha);
        return $"{diaSemana}, {fecha.Day.ToString(CultureInfo.InvariantCulture)} de {mes} de {fecha.Year.ToString(CultureInfo.InvariantCulture)}";
    }

    public static string FechaCiudad(DateTime fecha) =>
        "Ayacucho, " + FechaLarga(fecha);

    public static string PorcentajeFijo(decimal valor) =>
        valor.ToString("0.00", Cultura) + "%";

    public static string PorcentajeMensualEnLetras(decimal tasa)
    {
        var letras = NumeroALetras.EnSoles(Math.Round(tasa, 0, MidpointRounding.AwayFromZero));
        var entero = letras.Replace(" CON 00/100 SOLES", string.Empty, StringComparison.Ordinal);
        return entero + " POR CIENTO MENSUAL";
    }

    public static string Valor(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? string.Empty : valor.Trim();

    public static string Dia(DateTime fecha) =>
        fecha.Day.ToString(CultureInfo.InvariantCulture);

    public static string MesNombre(DateTime fecha) =>
        Cultura.TextInfo.ToTitleCase(fecha.ToString("MMMM", Cultura));

    public static string PorcentajeEntero(decimal valor) =>
        valor.ToString("0", Cultura) + "%";

    public static (string Numero, string Unidad) PartesPlazo(string plazoTexto)
    {
        var t = (plazoTexto ?? string.Empty).Trim();
        var partes = t.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return partes.Length == 2
            ? (partes[0], partes[1].ToUpperInvariant())
            : (t, string.Empty);
    }

    public static string Anio(DateTime fecha) =>
        fecha.Year.ToString(CultureInfo.InvariantCulture);

    public static string Money(decimal valor) =>
        "S/ " + valor.ToString("N2", Cultura);

    public static string Porcentaje(decimal valor, string formato = "0.##") =>
        valor.ToString(formato, Cultura) + " %";

    public static string Texto(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? "—" : valor.Trim();
}
