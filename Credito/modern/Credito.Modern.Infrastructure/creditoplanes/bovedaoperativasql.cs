namespace Credito.Modern.Infrastructure.CreditoPlanes;

/// <summary>
/// Selección canónica de bóveda abierta por oficina.
/// <para>
/// <b>Retorno de cajas / cierre:</b> si hay temporal abierta, esa es la destino
/// (misma regla que <c>usp_CerrarCajasDiarios</c>). Así el turno del encargado
/// no parte el dinero entre principal y temporal.
/// </para>
/// <para>
/// <b>Fondeo a cajas (asignar / TRS / analista / chica):</b> misma preferencia que el
/// retorno — temporal abierta si existe; si no, principal. Evita partir el ciclo.
/// </para>
/// <para>
/// <b>Bóveda UI / I-E / bancos:</b> siempre la principal abierta (IndTemporal=0).
/// Financiar temporal es vía <c>asignar-boveda-temporal</c>.
/// </para>
/// </summary>
internal static class BovedaOperativaSql
{
    /// <summary>
    /// Destino de TRE al devolver dinero de caja(s) y fuente de fondeo a cajas:
    /// temporal si existe, si no principal. Exige IndCierre=0.
    /// </summary>
    internal const string SelectIdDestinoRetornoCajas = """
        SELECT TOP 1 BovedaId
        FROM CREDITO.Boveda WITH (UPDLOCK, HOLDLOCK)
        WHERE OficinaId = @OficinaId
          AND IndCierre = CAST(0 AS bit)
        ORDER BY CASE WHEN IndTemporal = CAST(1 AS bit) THEN 0 ELSE 1 END, BovedaId;
        """;

    /// <summary>Alias semántico: misma regla que el retorno (ciclo temporal cerrado).</summary>
    internal const string SelectIdFuenteFondeoCajas = SelectIdDestinoRetornoCajas;

    /// <summary>Sin UPDLOCK (lecturas de monto / UI).</summary>
    internal const string SelectIdFuenteFondeoCajasRead = """
        SELECT TOP 1 BovedaId
        FROM CREDITO.Boveda
        WHERE OficinaId = @OficinaId
          AND IndCierre = CAST(0 AS bit)
        ORDER BY CASE WHEN IndTemporal = CAST(1 AS bit) THEN 0 ELSE 1 END, BovedaId;
        """;

    /// <summary>True si la fuente de fondeo es temporal.</summary>
    internal const string ExisteTemporalAbierta = """
        SELECT CAST(CASE WHEN EXISTS (
            SELECT 1 FROM CREDITO.Boveda
            WHERE OficinaId = @OficinaId
              AND IndCierre = CAST(0 AS bit)
              AND IndTemporal = CAST(1 AS bit)
        ) THEN 1 ELSE 0 END AS bit);
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
