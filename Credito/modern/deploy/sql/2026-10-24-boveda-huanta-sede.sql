-- Multisucursal: Huanta es Oficina, no un TipoPagoId 6-8.
-- 1) Crea HUANTA solo si no existe (no duplica, no renombra Ayacucho).
-- 2) Pasa el saldo abierto de tipos 6/7/8 en la principal a marcas 2/3/4 de Huanta.
-- 3) Ajusta SI de ambas cabeceras y llama usp_ActualizarSaldosBoveda (identidad).
-- 4) Marca 6-8 como obsoletos. No reescribe bóvedas cerradas.
-- Idempotente: si Principal ya no tiene 6-8 abiertos, no mueve.
-- Azure (correcto): 3=AYACUCCHO, 9=PICHARI, 10=HUANTA solo Yape -13531.10.
-- Local no debe tener la 3 llamada HUANTA; si quedó así, correr
-- deploy/sql/local/2026-10-24-alinear-sedes-azure.sql

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROC [CREDITO].[usp_ResumenCuentaBoveda]
@BovedaId INT
AS
DECLARE @SaldoInicial DECIMAL(15,2) = (SELECT SaldoInicial FROM CREDITO.Boveda WHERE BovedaId=@BovedaId)

IF @SaldoInicial>0
BEGIN
  IF NOT EXISTS(SELECT BovedaCuentaId FROM CREDITO.BovedaCuenta WHERE BovedaId=@BovedaId )
	BEGIN
     INSERT CREDITO.BovedaCuenta (BovedaId, TipoPagoId,SaldoInicial)
	 VALUES (@BovedaId, 1, @SaldoInicial  )
	END
END

;WITH CUENTAS AS (
	SELECT MC.BovedaId,TipoPagoId,SUM(IIF(MC.IndEntrada=1,MC.Importe,-MC.Importe) ) 'Importe'
	FROM CREDITO.BovedaMov MC
	WHERE MC.BovedaId=@BovedaId
	AND MC.Estado=1
	GROUP BY MC.BovedaId,MC.TipoPagoId
)
SELECT 'RESUMEN BOVEDA: ' + STRING_AGG(vt.Denominacion + ' = ' + CAST ( ISNULL(BC.SaldoInicial,0) +ISNULL(c.Importe,0)  AS VARCHAR(15)),'  ') 'Total'
FROM MAESTRO.ValorTabla vt
LEFT JOIN CUENTAS C ON  vt.ItemId=c.TipoPagoId
LEFT JOIN CREDITO.BovedaCuenta BC ON BC.BovedaId = @BovedaId and BC.TipoPagoId = vt.ItemId
WHERE vt.TablaId=13 AND vt.ItemId BETWEEN 1 AND 5
GO

BEGIN TRAN;

UPDATE MAESTRO.ValorTabla
SET DesCorta = N'OBSOLETO'
WHERE TablaId = 13
  AND ItemId IN (6, 7, 8)
  AND ISNULL(DesCorta, N'') <> N'OBSOLETO';

DECLARE @HuantaId INT = (
    SELECT TOP (1) OficinaId
    FROM MAESTRO.Oficina
    WHERE Denominacion LIKE N'%HUANTA%'
    ORDER BY IndPrincipal DESC, OficinaId
);

IF @HuantaId IS NULL
BEGIN
    INSERT INTO MAESTRO.Oficina (
        Denominacion, Descripcion, Telefono, IndPrincipal, Estado, UsuarioAsignadoId)
    VALUES (N'HUANTA', N'Sucursal Huanta', NULL, CAST(0 AS bit), CAST(1 AS bit), 3);
    SET @HuantaId = SCOPE_IDENTITY();

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
END
ELSE
BEGIN
    UPDATE MAESTRO.Oficina
    SET Estado = CAST(1 AS bit),
        IndPrincipal = CAST(0 AS bit),
        Descripcion = COALESCE(NULLIF(Descripcion, N''), N'Sucursal Huanta')
    WHERE OficinaId = @HuantaId;
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
        @HuantaId, 0, 0, 0, 0,
        dbo.ufnFecha(), CAST(0 AS bit), CAST(0 AS bit));
END;

DECLARE @PrincipalId INT = (
    SELECT TOP (1) OficinaId
    FROM MAESTRO.Oficina
    WHERE IndPrincipal = CAST(1 AS bit) AND Estado = CAST(1 AS bit)
    ORDER BY OficinaId
);
DECLARE @BovedaPrincipal INT = (
    SELECT TOP (1) BovedaId
    FROM CREDITO.Boveda
    WHERE OficinaId = @PrincipalId
      AND IndCierre = CAST(0 AS bit)
      AND IndTemporal = CAST(0 AS bit)
    ORDER BY BovedaId DESC
);
DECLARE @BovedaHuanta INT = (
    SELECT TOP (1) BovedaId
    FROM CREDITO.Boveda
    WHERE OficinaId = @HuantaId
      AND IndCierre = CAST(0 AS bit)
      AND IndTemporal = CAST(0 AS bit)
    ORDER BY BovedaId DESC
);

IF @BovedaPrincipal IS NULL OR @BovedaHuanta IS NULL
BEGIN
    ROLLBACK;
    RAISERROR(N'No hay bóveda principal abierta en Central o Huanta.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM CREDITO.BovedaCuenta WHERE BovedaId = @BovedaHuanta)
BEGIN
    DECLARE @SiHuanta DECIMAL(16, 2) = (
        SELECT SaldoInicial FROM CREDITO.Boveda WHERE BovedaId = @BovedaHuanta);
    IF ISNULL(@SiHuanta, 0) <> 0
    BEGIN
        INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
        VALUES (@BovedaHuanta, 1, @SiHuanta);
    END
END;

DECLARE @Neto DECIMAL(16, 2) = 0;
DECLARE @Marca SMALLINT;
DECLARE @Tipo SMALLINT = 6;

WHILE @Tipo <= 8
BEGIN
    SET @Marca = CASE @Tipo WHEN 6 THEN 2 WHEN 7 THEN 3 WHEN 8 THEN 4 END;
    SET @Neto = ISNULL((
        SELECT ISNULL(bc.SaldoInicial, 0) + ISNULL((
            SELECT SUM(IIF(bm.IndEntrada = 1, bm.Importe, -bm.Importe))
            FROM CREDITO.BovedaMov AS bm
            WHERE bm.BovedaId = @BovedaPrincipal
              AND bm.Estado = CAST(1 AS bit)
              AND bm.TipoPagoId = @Tipo
        ), 0)
        FROM CREDITO.BovedaCuenta AS bc
        WHERE bc.BovedaId = @BovedaPrincipal AND bc.TipoPagoId = @Tipo
    ), 0);

    IF @Neto <> 0
    BEGIN
        IF EXISTS (
            SELECT 1 FROM CREDITO.BovedaCuenta
            WHERE BovedaId = @BovedaHuanta AND TipoPagoId = @Marca)
            UPDATE CREDITO.BovedaCuenta
            SET SaldoInicial = SaldoInicial + @Neto
            WHERE BovedaId = @BovedaHuanta AND TipoPagoId = @Marca;
        ELSE
            INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
            VALUES (@BovedaHuanta, @Marca, @Neto);

        DELETE FROM CREDITO.BovedaCuenta
        WHERE BovedaId = @BovedaPrincipal AND TipoPagoId = @Tipo;

        UPDATE CREDITO.Boveda
        SET SaldoInicial = SaldoInicial - @Neto
        WHERE BovedaId = @BovedaPrincipal;

        UPDATE CREDITO.Boveda
        SET SaldoInicial = SaldoInicial + @Neto
        WHERE BovedaId = @BovedaHuanta;
    END

    SET @Tipo = @Tipo + 1;
END;

DELETE FROM CREDITO.BovedaCuenta
WHERE BovedaId = @BovedaPrincipal
  AND TipoPagoId IN (6, 7, 8);

EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaPrincipal;
EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaHuanta;

COMMIT;

DECLARE @UsuarioCierre INT = ISNULL((
    SELECT TOP (1) UsuarioAsignadoId FROM MAESTRO.Oficina WHERE OficinaId = @HuantaId
), 3);

IF EXISTS (
    SELECT 1
    FROM CREDITO.Boveda
    WHERE BovedaId = @BovedaHuanta
      AND FechaIniOperacion < '20230101'
)
BEGIN
    EXEC CREDITO.usp_CerrarBoveda @OficinaId = @HuantaId, @UsuarioRegId = @UsuarioCierre;
END

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
ORDER BY o.IndPrincipal DESC, o.OficinaId;

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
ORDER BY o.IndPrincipal DESC, b.BovedaId, bc.TipoPagoId;
