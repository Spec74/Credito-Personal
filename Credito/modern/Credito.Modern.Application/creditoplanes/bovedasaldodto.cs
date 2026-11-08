namespace Credito.Modern.Application.CreditoPlanes;

/// <summary>
/// Saldo de un medio para remesa/fondeo.
/// <list type="bullet">
/// <item><see cref="Disponible"/> / <see cref="DisponibleBoveda"/>: ledger de bóveda
/// (SI cuenta + neto mov) — misma fórmula que <c>usp_TransferirBoveda</c>. Es lo remesable.</item>
/// <item><see cref="PendienteCaja"/>: cobros en caja diaria aún no transferidos a bóveda
/// (<c>TransBoveda=0</c>). Aparecen en Medios pero <b>no</b> se pueden remesar hasta el TRE.</item>
/// <item><see cref="DisponibleFamiliaBoveda"/>: suma vault de la familia (Yape 2+10+11 /
/// Interbank 3+12+13). Si supera <see cref="Disponible"/> hay tipado legado en sede.</item>
/// </list>
/// </summary>
public sealed record BovedaSaldoMedioDto(
    int BovedaId,
    int OficinaId,
    short TipoPagoId,
    decimal Disponible,
    decimal DisponibleBoveda,
    decimal PendienteCaja,
    decimal DisponibleFamiliaBoveda);
