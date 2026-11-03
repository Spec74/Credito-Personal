/*
  Azure CREDITO de prueba. NO producción.

  Tras el vaciado OPERATIVO se borró BovedaMov y se puso SaldoInicial=0
  en BovedaCuenta, pero CREDITO.Boveda conservó SI / Entradas / Salidas /
  SaldoFinal viejos. Por eso la pantalla muestra -929k y no deja asignar caja.

  Este script pone en 0 las cabeceras abiertas, cierra temporales y deja
  medios en 0. Luego alimentas con Bóveda → Registrar → Movimiento (ingreso).
*/

SET NOCOUNT ON;

PRINT N'Base: ' + DB_NAME();
PRINT N'Servidor: ' + @@SERVERNAME;

IF DB_NAME() IN (N'master', N'msdb', N'tempdb', N'model')
BEGIN
    RAISERROR(N'Abortado: elige la base CREDITO de prueba.', 16, 1);
    RETURN;
END;

SELECT
    BovedaId,
    OficinaId,
    IndTemporal,
    IndCierre,
    SaldoInicial,
    Entradas,
    Salidas,
    SaldoFinal
FROM CREDITO.Boveda
WHERE IndCierre = CAST(0 AS bit)
ORDER BY OficinaId, IndTemporal, BovedaId;

UPDATE CREDITO.Boveda
SET SaldoInicial = 0,
    Entradas = 0,
    Salidas = 0,
    SaldoFinal = 0
WHERE IndCierre = CAST(0 AS bit)
  AND (
        SaldoInicial <> 0
     OR Entradas <> 0
     OR Salidas <> 0
     OR SaldoFinal <> 0
  );

PRINT N'Cabeceras abiertas en 0: ' + CONVERT(varchar(12), @@ROWCOUNT);

IF COL_LENGTH(N'CREDITO.BovedaCuenta', N'SaldoInicial') IS NOT NULL
BEGIN
    UPDATE CREDITO.BovedaCuenta
    SET SaldoInicial = 0
    WHERE SaldoInicial <> 0;
    PRINT N'BovedaCuenta SaldoInicial en 0: ' + CONVERT(varchar(12), @@ROWCOUNT);
END;

UPDATE CREDITO.Boveda
SET IndCierre = CAST(1 AS bit),
    SaldoInicial = 0,
    Entradas = 0,
    Salidas = 0,
    SaldoFinal = 0
WHERE IndTemporal = CAST(1 AS bit)
  AND IndCierre = CAST(0 AS bit);

PRINT N'Temporales cerradas: ' + CONVERT(varchar(12), @@ROWCOUNT);

SELECT
    BovedaId,
    OficinaId,
    IndTemporal,
    IndCierre,
    SaldoInicial,
    Entradas,
    Salidas,
    SaldoFinal
FROM CREDITO.Boveda
WHERE IndCierre = CAST(0 AS bit)
ORDER BY OficinaId, BovedaId;

PRINT N'Listo. En SPA: Bóveda → actualizar. Luego Registrar → Movimiento (Ingreso).';
GO
