namespace Credito.Modern.Infrastructure.CreditoPlanes;

/// <summary>
/// Selección canónica de bóveda abierta por oficina.
/// <para>
/// <b>Retorno de cajas / cierre:</b> si hay temporal abierta, esa es la destino
/// (misma regla que <c>usp_CerrarCajasDiarios</c>). Así el turno del encargado
/// no parte el dinero entre principal y temporal.
/// </para>
/// <para>
/// <b>Bóveda UI / ops de sede:</b> la principal abierta (IndTemporal=0 primero).
/// Financiar temporal es vía <c>asignar-boveda-temporal</c>.
/// </para>
/// </summary>
internal static class BovedaOperativaSql
{
    /// <summary>
    /// Destino de TRE al devolver dinero de caja(s): temporal si existe, si no principal.
    /// Exige IndCierre=0.
    /// </summary>
    internal const string SelectIdDestinoRetornoCajas = """
        SELECT TOP 1 BovedaId
        FROM CREDITO.Boveda WITH (UPDLOCK, HOLDLOCK)
        WHERE OficinaId = @OficinaId
          AND IndCierre = CAST(0 AS bit)
        ORDER BY CASE WHEN IndTemporal = CAST(1 AS bit) THEN 0 ELSE 1 END, BovedaId;
        """;

    /// <summary>
    /// Igual que <see cref="SelectIdDestinoRetornoCajas"/> pero también IndCierre (para validar).
    /// </summary>
    internal const string SelectRowDestinoRetornoCajas = """
        SELECT TOP 1 BovedaId, IndCierre, IndTemporal
        FROM CREDITO.Boveda WITH (UPDLOCK, HOLDLOCK)
        WHERE OficinaId = @OficinaId
          AND IndCierre = CAST(0 AS bit)
        ORDER BY CASE WHEN IndTemporal = CAST(1 AS bit) THEN 0 ELSE 1 END, BovedaId;
        """;

    /// <summary>Principal abierta (pantalla bóveda / fondeo sede).</summary>
    internal const string SelectIdPrincipalAbierta = """
        SELECT TOP 1 BovedaId
        FROM CREDITO.Boveda
        WHERE OficinaId = @OficinaId
          AND IndCierre = CAST(0 AS bit)
          AND IndTemporal = CAST(0 AS bit)
        ORDER BY BovedaId;
        """;
}
