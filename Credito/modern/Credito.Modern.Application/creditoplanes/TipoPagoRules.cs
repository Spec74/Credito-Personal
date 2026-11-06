namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Medios de pago (<c>MAESTRO.ValorTabla</c> TablaId = 13).
/// Catálogo: 1 efectivo · 2 Yape (ROLLUP) · 3 Interbank · 4 BCP · 5 Nación · 9 Plin ·
/// 10 Yape Gerente · 11 Yape René. 6–8 sede obsoleta.
/// Postable = no OBSOLETO / no ROLLUP / no 6–8.
/// Digital = TipoPagoId &gt; 1 (voucher + Extension).
/// </summary>
public static class TipoPagoRules
{
    public const int Efectivo = 1;
    public const int Yape = 2;
    public const int Plin = 9;
    public const int YapeGerente = 10;
    public const int YapeRene = 11;

    public static bool IsDigital(int tipoPagoId) => tipoPagoId > Efectivo;

    public static bool IsObsoleteSedeVariant(int tipoPagoId) =>
        tipoPagoId is >= 6 and <= 8;

    public static bool IsYapeFamily(int tipoPagoId) =>
        tipoPagoId is Yape or YapeGerente or YapeRene;

    /// <summary>ItemId 2 es padre rollup: no cobro/fondeo nuevo.</summary>
    public static bool IsRollupParent(int tipoPagoId) => tipoPagoId == Yape;

    public static bool IsPostable(int tipoPagoId) =>
        tipoPagoId >= 1
        && !IsObsoleteSedeVariant(tipoPagoId)
        && !IsRollupParent(tipoPagoId);

    public static string? RequireCobroPermitido(int tipoPagoId, string fieldName = "tipoPagoId")
    {
        if (tipoPagoId < 1)
        {
            return $"{fieldName} debe ser >= 1.";
        }

        if (IsObsoleteSedeVariant(tipoPagoId))
        {
            return $"{fieldName}: no use variantes por sede (6–8). Elija Yape Gerente/René, Plin, Interbank, BCP o Nación.";
        }

        if (IsRollupParent(tipoPagoId))
        {
            return $"{fieldName}: use Yape Gerente o Yape René (el Yape genérico es solo total/rollup).";
        }

        return null;
    }

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
            return $"{fieldName}: los medios digitales/transferencia (Yape Gerente/René, Plin, bancos…) requieren fecha/hora del voucher.";
        }

        return null;
    }
}
