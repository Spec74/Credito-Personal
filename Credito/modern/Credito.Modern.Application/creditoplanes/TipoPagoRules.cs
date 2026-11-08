namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Medios (<c>MAESTRO.ValorTabla</c> TablaId = 13) — multisucursal:
/// <list type="bullet">
/// <item>Sucursal tipa: Yape(2), Interbank(3), BCP, Nación, Plin(9), Efectivo.</item>
/// <item>Central tipa: Yape Gerente/René (10/11), Interbank Gerente/René (12/13) — no el genérico 2/3.</item>
/// <item>Medios de red (vista Central): padres Yape/Interbank = rollup familia (2+10+11 / 3+12+13)
///       con desglose por sede; Gerente/René = detalle de billetera (sin filas de sucursal vacías).</item>
/// <item>Fondeo Central→sucursal con billetera: destino acredita familia (10/11→2, 12/13→3).</item>
/// <item>Remesa sucursal→Central: elige billetera destino (10–13); origen debita familia (2/3).
///       BCP/Nación/Plin/efectivo viajan 1:1. Sin peer entre sucursales.</item>
/// </list>
/// Familia Yape = 2+10+11; Interbank = 3+12+13. 6–8 sede obsoleta. Digital = TipoPagoId &gt; 1.
/// </summary>
public static class TipoPagoRules
{
    public const int Efectivo = 1;
    public const int Yape = 2;
    public const int Interbank = 3;
    public const int Plin = 9;
    public const int YapeGerente = 10;
    public const int YapeRene = 11;
    public const int InterbankGerente = 12;
    public const int InterbankRene = 13;

    public static bool IsDigital(int tipoPagoId) => tipoPagoId > Efectivo;

    public static bool IsObsoleteSedeVariant(int tipoPagoId) =>
        tipoPagoId is >= 6 and <= 8;

    public static bool IsYapeFamily(int tipoPagoId) =>
        tipoPagoId is Yape or YapeGerente or YapeRene;

    public static bool IsInterbankFamily(int tipoPagoId) =>
        tipoPagoId is Interbank or InterbankGerente or InterbankRene;

    /// <summary>Billeteras Gerente/René: solo oficina Central (<c>IndPrincipal</c>).</summary>
    public static bool IsCentralWallet(int tipoPagoId) =>
        tipoPagoId is YapeGerente or YapeRene or InterbankGerente or InterbankRene;

    /// <summary>Genéricos que Central no debe tipar (usa billeteras).</summary>
    public static bool IsCentralReplacedGeneric(int tipoPagoId) =>
        tipoPagoId is Yape or Interbank;

    public static string? RequireCobroPermitido(
        int tipoPagoId,
        bool esCentral,
        string fieldName = "tipoPagoId")
    {
        if (tipoPagoId < 1)
        {
            return $"{fieldName} debe ser >= 1.";
        }

        if (IsObsoleteSedeVariant(tipoPagoId))
        {
            return $"{fieldName}: no use variantes por sede (6–8).";
        }

        if (IsCentralWallet(tipoPagoId) && !esCentral)
        {
            return $"{fieldName}: Yape/Interbank Gerente y René solo aplican en Central. En sucursal use Yape o Interbank.";
        }

        if (esCentral && IsCentralReplacedGeneric(tipoPagoId))
        {
            return tipoPagoId == Yape
                ? $"{fieldName}: en Central use Yape Gerente o Yape René (el Yape genérico es el total)."
                : $"{fieldName}: en Central use Interbank Gerente o Interbank René (el Interbank genérico es el total).";
        }

        return null;
    }

    /// <summary>Compat: sin contexto de sede asume no-Central (bloquea billeteras CENTRAL).</summary>
    public static string? RequireCobroPermitido(int tipoPagoId, string fieldName = "tipoPagoId") =>
        RequireCobroPermitido(tipoPagoId, esCentral: false, fieldName);

    /// <summary>Origen + destino en la misma sede (transferencias internas / bancos).</summary>
    public static string? RequireParCobroPermitido(
        int tipoPagoOrigenId,
        int tipoPagoDestinoId,
        bool esCentral,
        string origenField = "tipoPagoOrigenId",
        string destinoField = "tipoPagoDestinoId") =>
        RequireCobroPermitido(tipoPagoOrigenId, esCentral, origenField)
        ?? RequireCobroPermitido(tipoPagoDestinoId, esCentral, destinoField);

    /// <summary>Billetera Central ↔ genérico de familia (fondeo destino / remesa origen).</summary>
    public static short MapFamiliaGenerica(short tipoPagoBilleteraOId) =>
        tipoPagoBilleteraOId switch
        {
            YapeGerente or YapeRene => (short)Yape,
            InterbankGerente or InterbankRene => (short)Interbank,
            _ => tipoPagoBilleteraOId,
        };

    /// <summary>Fondeo Central→sucursal: destino acredita familia genérica.</summary>
    public static short MapFondeoDestino(short tipoPagoOrigenId) =>
        MapFamiliaGenerica(tipoPagoOrigenId);

    /// <summary>Remesa sucursal→Central: origen debita familia genérica.</summary>
    public static short MapRemesaOrigen(short tipoPagoDestinoCentral) =>
        MapFamiliaGenerica(tipoPagoDestinoCentral);

    public static string? RequireDigitalFecha(
        int tipoPagoId,
        string? fechaPagoTransferencia,
        string fieldName = "fechaPagoTransferencia")
    {
        if (!IsDigital(tipoPagoId))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(fechaPagoTransferencia))
        {
            return $"{fieldName}: los medios digitales/transferencia requieren fecha/hora del voucher.";
        }

        return null;
    }
}
