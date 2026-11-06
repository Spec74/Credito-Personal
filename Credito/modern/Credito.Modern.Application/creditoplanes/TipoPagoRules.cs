namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Medios de pago (<c>MAESTRO.ValorTabla</c> TablaId = 13).
/// Catálogo operativo: 1 efectivo, 2 Yape, 3 Interbank, 4 BCP, 5 Banco de la Nación, 9 Plin.
/// 6–8 son variantes por sede obsoletas (no usar en cobro).
/// Alineado a los USP: <c>TipoPagoId &gt; 1</c> ⇒ digital/transferencia (voucher + Extension).
/// </summary>
public static class TipoPagoRules
{
    public const int Efectivo = 1;
    public const int Plin = 9;

    public static bool IsDigital(int tipoPagoId) => tipoPagoId > Efectivo;

    public static bool IsObsoleteSedeVariant(int tipoPagoId) =>
        tipoPagoId is >= 6 and <= 8;

    public static string? RequireCobroPermitido(int tipoPagoId, string fieldName = "tipoPagoId")
    {
        if (tipoPagoId < 1)
        {
            return $"{fieldName} debe ser >= 1.";
        }

        if (IsObsoleteSedeVariant(tipoPagoId))
        {
            return $"{fieldName}: no use variantes por sede (6–8). Elija Yape, Plin, Interbank, BCP o Banco de la Nación.";
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
            return $"{fieldName}: los medios digitales/transferencia (Yape, Plin, Interbank, BCP, Nación, …) requieren fecha/hora del voucher.";
        }

        return null;
    }
}
