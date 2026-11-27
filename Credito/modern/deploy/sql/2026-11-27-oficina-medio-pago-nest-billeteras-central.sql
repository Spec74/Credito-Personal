/*
  Jerarquía UI en Central: billeteras 10/11 bajo Yape (2); 12/13 bajo Interbank (3).

  - NO cambia TipoPagoId (ledger / BovedaCuenta / movimientos intactos).
  - Solo IndPrincipal=1.
  - Idempotente.

  Requiere CREDITO.OficinaMedioPago (2026-11-25-oficina-medio-pago.sql).
*/

SET NOCOUNT ON;
GO

IF OBJECT_ID(N'CREDITO.OficinaMedioPago', N'U') IS NULL
BEGIN
    RAISERROR(N'Falta CREDITO.OficinaMedioPago. Ejecute 2026-11-25-oficina-medio-pago.sql primero.', 16, 1);
    RETURN;
END
GO

UPDATE child
SET child.ParentOficinaMedioPagoId = parent.OficinaMedioPagoId,
    child.UpdatedAtUtc = SYSUTCDATETIME()
FROM CREDITO.OficinaMedioPago AS child
INNER JOIN MAESTRO.Oficina AS o
    ON o.OficinaId = child.OficinaId
   AND ISNULL(o.IndPrincipal, 0) = 1
INNER JOIN CREDITO.OficinaMedioPago AS parent
    ON parent.OficinaId = child.OficinaId
   AND parent.TipoPagoId = CASE child.TipoPagoId
        WHEN 10 THEN CAST(2 AS smallint)
        WHEN 11 THEN CAST(2 AS smallint)
        WHEN 12 THEN CAST(3 AS smallint)
        WHEN 13 THEN CAST(3 AS smallint)
   END
   AND ISNULL(parent.ParentOficinaMedioPagoId, 0) = 0
WHERE child.TipoPagoId IN (10, 11, 12, 13)
  AND (
        child.ParentOficinaMedioPagoId IS NULL
        OR child.ParentOficinaMedioPagoId = 0
        OR child.ParentOficinaMedioPagoId <> parent.OficinaMedioPagoId
      );
GO

/* Verificación: billeteras central deben tener padre; genéricos 2/3 sin padre. */
SELECT
    o.OficinaId,
    o.Denominacion,
    child.TipoPagoId AS BilleteraTipo,
    child.Nombre AS Billetera,
    parent.TipoPagoId AS PadreTipo,
    parent.Nombre AS Padre
FROM CREDITO.OficinaMedioPago AS child
INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = child.OficinaId
LEFT JOIN CREDITO.OficinaMedioPago AS parent
    ON parent.OficinaMedioPagoId = child.ParentOficinaMedioPagoId
WHERE ISNULL(o.IndPrincipal, 0) = 1
  AND child.TipoPagoId IN (10, 11, 12, 13)
ORDER BY child.TipoPagoId;
GO
