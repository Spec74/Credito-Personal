-- SOLO BD local. Azure ya tiene el modelo bueno:
--   3 = SUCURSAL AYACUCCHO (bóveda 3529, sin cuentas 6-8)
--   9 = SUCURSAL PICHARI
--   10 = HUANTA (solo Yape -13531.10)
-- El script 2026-10-21 había renombrado la 3 a HUANTA y el de sede
-- cerró 3529 metiendo el efectivo de Ayacucho en "Huanta".
-- IDs locales no tienen que coincidir (Pichari 4, Huanta nueva).

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRAN;

UPDATE MAESTRO.Oficina
SET Denominacion = N'SUCURSAL AYACUCCHO',
    Descripcion = N'Sucursal Ayacucho',
    Estado = CAST(1 AS bit),
    IndPrincipal = CAST(0 AS bit)
WHERE OficinaId = 3;

UPDATE MAESTRO.Oficina
SET Denominacion = N'SUCURSAL PICHARI',
    Descripcion = COALESCE(NULLIF(Descripcion, N''), N'Sucursal Pichari'),
    Estado = CAST(1 AS bit),
    IndPrincipal = CAST(0 AS bit)
WHERE Denominacion LIKE N'%PICHARI%';

DECLARE @HuantaId INT = (
    SELECT TOP (1) OficinaId
    FROM MAESTRO.Oficina
    WHERE Denominacion LIKE N'%HUANTA%'
      AND OficinaId <> 3
    ORDER BY OficinaId
);

IF @HuantaId IS NULL
BEGIN
    INSERT INTO MAESTRO.Oficina (
        Denominacion, Descripcion, Telefono, IndPrincipal, Estado, UsuarioAsignadoId)
    VALUES (N'HUANTA', N'Sucursal Huanta', NULL, CAST(0 AS bit), CAST(1 AS bit), 3);
    SET @HuantaId = SCOPE_IDENTITY();
END;

INSERT INTO MAESTRO.UsuarioOficina (UsuarioId, OficinaId, Estado)
SELECT u.UsuarioId, @HuantaId, CAST(1 AS bit)
FROM MAESTRO.Usuario AS u
WHERE u.NombreUsuario IN (N'RMANTILLA', N'YCERVANTES', N'ADMVENDIX')
  AND u.Estado = CAST(1 AS bit)
  AND NOT EXISTS (
      SELECT 1 FROM MAESTRO.UsuarioOficina AS uo
      WHERE uo.UsuarioId = u.UsuarioId AND uo.OficinaId = @HuantaId);

INSERT INTO MAESTRO.UsuarioRol (UsuarioId, RolId, OficinaId)
SELECT ur.UsuarioId, ur.RolId, @HuantaId
FROM MAESTRO.UsuarioRol AS ur
INNER JOIN MAESTRO.Usuario AS u ON u.UsuarioId = ur.UsuarioId
WHERE u.NombreUsuario IN (N'RMANTILLA', N'YCERVANTES', N'ADMVENDIX')
  AND ur.OficinaId = 1
  AND NOT EXISTS (
      SELECT 1 FROM MAESTRO.UsuarioRol AS x
      WHERE x.UsuarioId = ur.UsuarioId
        AND x.RolId = ur.RolId
        AND x.OficinaId = @HuantaId);

DECLARE @BovedaMezcla INT = (
    SELECT TOP (1) b.BovedaId
    FROM CREDITO.Boveda AS b
    WHERE b.OficinaId = 3
      AND b.IndCierre = CAST(0 AS bit)
      AND b.IndTemporal = CAST(0 AS bit)
      AND b.FechaIniOperacion >= '20261024'
    ORDER BY b.BovedaId DESC
);

IF @BovedaMezcla IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM CREDITO.BovedaMov WHERE BovedaId = @BovedaMezcla)
BEGIN
    DELETE FROM CREDITO.BovedaCuenta WHERE BovedaId = @BovedaMezcla;
    DELETE FROM CREDITO.Boveda WHERE BovedaId = @BovedaMezcla;
END;

IF EXISTS (SELECT 1 FROM CREDITO.Boveda WHERE BovedaId = 3529)
BEGIN
    DELETE FROM CREDITO.BovedaCuenta WHERE BovedaId = 3529;
    UPDATE CREDITO.Boveda
    SET OficinaId = 3,
        SaldoInicial = 274852.00,
        IndCierre = CAST(0 AS bit),
        IndTemporal = CAST(0 AS bit),
        FechaFinOperacion = NULL
    WHERE BovedaId = 3529;
    EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = 3529;
END;

IF NOT EXISTS (
    SELECT 1
    FROM CREDITO.Boveda
    WHERE OficinaId = @HuantaId
      AND IndCierre = CAST(0 AS bit)
      AND IndTemporal = CAST(0 AS bit)
)
BEGIN
    INSERT INTO CREDITO.Boveda (
        OficinaId, SaldoInicial, Entradas, Salidas, SaldoFinal,
        FechaIniOperacion, IndCierre, IndTemporal)
    VALUES (
        @HuantaId, -13531.10, 0, 0, -13531.10,
        dbo.ufnFecha(), CAST(0 AS bit), CAST(0 AS bit));
END;

DECLARE @BovedaHuanta INT = (
    SELECT TOP (1) BovedaId
    FROM CREDITO.Boveda
    WHERE OficinaId = @HuantaId
      AND IndCierre = CAST(0 AS bit)
      AND IndTemporal = CAST(0 AS bit)
    ORDER BY BovedaId DESC
);

IF @BovedaHuanta IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM CREDITO.BovedaCuenta
       WHERE BovedaId = @BovedaHuanta AND TipoPagoId = 2)
BEGIN
    UPDATE CREDITO.Boveda
    SET SaldoInicial = -13531.10
    WHERE BovedaId = @BovedaHuanta;
    INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
    VALUES (@BovedaHuanta, 2, -13531.10);
    EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaHuanta;
END;

COMMIT;

SELECT
    o.OficinaId,
    o.Denominacion,
    b.BovedaId,
    b.SaldoInicial,
    b.Entradas,
    b.Salidas,
    b.SaldoFinal,
    b.FechaIniOperacion,
    b.IndCierre
FROM CREDITO.Boveda AS b
INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = b.OficinaId
WHERE b.IndCierre = CAST(0 AS bit)
  AND b.IndTemporal = CAST(0 AS bit)
ORDER BY o.OficinaId;

SELECT
    b.BovedaId,
    o.Denominacion,
    bc.TipoPagoId,
    vt.Denominacion AS Tipo,
    bc.SaldoInicial
FROM CREDITO.BovedaCuenta AS bc
INNER JOIN CREDITO.Boveda AS b ON b.BovedaId = bc.BovedaId
INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = b.OficinaId
LEFT JOIN MAESTRO.ValorTabla AS vt ON vt.TablaId = 13 AND vt.ItemId = bc.TipoPagoId
WHERE b.IndCierre = CAST(0 AS bit)
  AND b.IndTemporal = CAST(0 AS bit)
ORDER BY o.OficinaId, bc.TipoPagoId;
