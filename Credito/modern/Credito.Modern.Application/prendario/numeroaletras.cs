using System.Globalization;

namespace Credito.Modern.Application.Prendario;

/// <summary>
/// Convierte un importe a texto en soles. Paridad <c>CreditoBL.NumeroALetras</c>, con la
/// corrección de que 21–29 salen en mayúsculas (el legacy concatenaba "VEINTI" + unidad en
/// minúsculas: "VEINTIuno").
/// </summary>
public static class NumeroALetras
{
    private static readonly string[] Unidades =
    [
        "", "UN", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE",
        "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISEIS", "DIECISIETE",
        "DIECIOCHO", "DIECINUEVE",
    ];

    private static readonly string[] Decenas =
    [
        "", "DIEZ", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA",
        "OCHENTA", "NOVENTA",
    ];

    private static readonly string[] Centenas =
    [
        "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS",
        "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS",
    ];

    public static string EnSoles(decimal monto)
    {
        var entero = (long)Math.Floor(monto);
        var centavos = (int)Math.Round((monto - entero) * 100m, MidpointRounding.AwayFromZero);
        if (centavos == 100)
        {
            entero += 1;
            centavos = 0;
        }

        var letras = entero == 0 ? "CERO" : ConvertirEntero(entero);
        return $"{letras} CON {centavos.ToString("00", CultureInfo.InvariantCulture)}/100 SOLES";
    }

    private static string ConvertirEntero(long n)
    {
        if (n == 0)
        {
            return "";
        }

        if (n < 20)
        {
            return Unidades[n];
        }

        if (n < 30)
        {
            var resto = n % 10;
            return resto == 0 ? "VEINTE" : "VEINTI" + Unidades[resto];
        }

        if (n < 100)
        {
            var resto = n % 10;
            return Decenas[n / 10] + (resto > 0 ? " Y " + Unidades[resto] : "");
        }

        if (n < 1000)
        {
            if (n == 100)
            {
                return "CIEN";
            }

            return Centenas[n / 100] + (n % 100 > 0 ? " " + ConvertirEntero(n % 100) : "");
        }

        if (n < 1_000_000)
        {
            var miles = n / 1000;
            var txtMiles = miles == 1 ? "MIL" : ConvertirEntero(miles) + " MIL";
            return txtMiles + (n % 1000 > 0 ? " " + ConvertirEntero(n % 1000) : "");
        }

        var millones = n / 1_000_000;
        var txtMillones = millones == 1 ? "UN MILLON" : ConvertirEntero(millones) + " MILLONES";
        return txtMillones + (n % 1_000_000 > 0 ? " " + ConvertirEntero(n % 1_000_000) : "");
    }
}
