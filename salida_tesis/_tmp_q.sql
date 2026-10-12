SET NOCOUNT ON;
DECLARE @FechaCorte date = '2026-10-06';

PRINT '=== Q01 VOLUMENES ===';
SELECT
  (SELECT COUNT(*) FROM MAESTRO.Cliente) AS Clientes,
  (SELECT COUNT(*) FROM CREDITO.Credito) AS Expedientes,
  (SELECT COUNT(*) FROM CREDITO.Credito WHERE FechaDesembolso IS NOT NULL AND CAST(FechaDesembolso AS date) <= @FechaCorte) AS ConDesembolsoHastaCorte,
  (SELECT COUNT(*) FROM CREDITO.Credito WHERE FechaDesembolso IS NOT NULL) AS ConDesembolsoTotal,
  (SELECT SUM(CAST(MontoDesembolso AS decimal(18,2))) FROM CREDITO.Credito WHERE FechaDesembolso IS NOT NULL AND CAST(FechaDesembolso AS date) <= @FechaCorte) AS MontoDesembolsado,
  (SELECT COUNT(*) FROM CREDITO.PlanPago) AS Cuotas,
  (SELECT COUNT(*) FROM CREDITO.MovimientoCaja) AS Movimientos,
  (SELECT MIN(CAST(FechaDesembolso AS date)) FROM CREDITO.Credito WHERE FechaDesembolso IS NOT NULL) AS PrimerDesemb,
  (SELECT MAX(CAST(FechaDesembolso AS date)) FROM CREDITO.Credito WHERE FechaDesembolso IS NOT NULL) AS UltimoDesemb,
  (SELECT MAX(CAST(FechaReg AS date)) FROM CREDITO.MovimientoCaja) AS UltimoMov;

PRINT '=== Q02 ESTADOS ===';
SELECT Estado, COUNT(*) AS N FROM CREDITO.Credito GROUP BY Estado ORDER BY N DESC;

PRINT '=== Q03 CLIENTES DESEMBOLSO ===';
SELECT COUNT(DISTINCT PersonaId) AS ClientesConDesembolso
FROM CREDITO.Credito
WHERE FechaDesembolso IS NOT NULL AND CAST(FechaDesembolso AS date) <= @FechaCorte;

PRINT '=== Q04 SCM SEP 2026 ===';
SELECT Anio, Mes,
  SUM(SaldoCartera) AS SaldoCartera,
  SUM(SaldoMoraCartera) AS SaldoMoraCartera,
  CASE WHEN SUM(SaldoCartera)>0 THEN CAST(100.0*SUM(SaldoMoraCartera)/SUM(SaldoCartera) AS decimal(10,4)) ELSE NULL END AS PctMoraSCM,
  SUM(SaldoVencido) AS SaldoVencido,
  SUM(SaldoMorosidad) AS SaldoMorosidad
FROM CREDITO.SaldoCarteraMensual
WHERE Anio=2026 AND Mes=9
GROUP BY Anio, Mes;

PRINT '=== Q05 SCM JUL 2023 ===';
SELECT Anio, Mes, SUM(SaldoCartera) AS SaldoCartera
FROM CREDITO.SaldoCarteraMensual WHERE Anio=2023 AND Mes=7 GROUP BY Anio, Mes;

PRINT '=== Q06 RANGO SCM ===';
SELECT MIN(DATEFROMPARTS(Anio,Mes,1)) AS Primero, MAX(DATEFROMPARTS(Anio,Mes,1)) AS Ultimo, COUNT(DISTINCT DATEFROMPARTS(Anio,Mes,1)) AS Meses
FROM CREDITO.SaldoCarteraMensual;

PRINT '=== Q07 CIERRE ===';
SELECT Periodo, FechaCorte, Estado, Version FROM CREDITO.CierreGerencial ORDER BY Periodo;

