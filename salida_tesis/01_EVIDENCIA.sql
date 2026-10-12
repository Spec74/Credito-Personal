/*
  Evidencia tesis Crediconfiable / Vendix — SOLO LECTURA
  Base: CREDITO | Fecha de corte única: 2026-10-06
  Mes cerrado para SCM: 2026-09
  No incluye credenciales. Ejecutar: sqlcmd -S <servidor> -d CREDITO -i 01_EVIDENCIA.sql
*/
SET NOCOUNT ON;
DECLARE @FechaCorte date = '2026-10-06';

/* Q01 — Volúmenes */
SELECT
  (SELECT COUNT(*) FROM MAESTRO.Cliente) AS Clientes,
  (SELECT COUNT(*) FROM CREDITO.Credito) AS Expedientes,
  (SELECT COUNT(*) FROM CREDITO.Credito
    WHERE FechaDesembolso IS NOT NULL AND CAST(FechaDesembolso AS date) <= @FechaCorte) AS ConDesembolso,
  (SELECT SUM(CAST(MontoDesembolso AS decimal(18,2))) FROM CREDITO.Credito
    WHERE FechaDesembolso IS NOT NULL AND CAST(FechaDesembolso AS date) <= @FechaCorte) AS MontoDesembolsado,
  (SELECT COUNT(*) FROM CREDITO.PlanPago) AS Cuotas,
  (SELECT COUNT(*) FROM CREDITO.MovimientoCaja) AS Movimientos,
  (SELECT MIN(CAST(FechaDesembolso AS date)) FROM CREDITO.Credito WHERE FechaDesembolso IS NOT NULL) AS PrimerDesemb,
  (SELECT MAX(CAST(FechaDesembolso AS date)) FROM CREDITO.Credito WHERE FechaDesembolso IS NOT NULL) AS UltimoDesemb,
  (SELECT MAX(CAST(FechaReg AS date)) FROM CREDITO.MovimientoCaja) AS UltimoMov;

/* Resultado verificado 2026-10-11 (sesión local):
   Clientes=6460; Expedientes=77388; ConDesembolso=77308; Monto=49253123.32;
   Cuotas=1609858; Movimientos=1655421; Primer=2019-12-22; Ultimo=2026-10-06 */

/* Q02 — Estados (usar Estado, no fechas) */
SELECT Estado, COUNT(*) AS N FROM CREDITO.Credito GROUP BY Estado ORDER BY N DESC;
/* PAG=75800; DES=1495; ANU=58; CRE=33; REP=2 */

/* Q03 — Clientes con desembolso */
SELECT COUNT(DISTINCT PersonaId) AS ClientesConDesembolso
FROM CREDITO.Credito
WHERE FechaDesembolso IS NOT NULL AND CAST(FechaDesembolso AS date) <= @FechaCorte;
/* 5794 */

/* Q04 — SCM setiembre 2026 (último mes cerrado) */
SELECT Anio, Mes,
  SUM(SaldoCartera) AS SaldoTotalPorCobrar,
  SUM(SaldoMoraCartera) AS SaldoMoraCartera,
  CAST(100.0 * SUM(SaldoMoraCartera) / NULLIF(SUM(SaldoCartera),0) AS decimal(10,4)) AS PctMoraSCM,
  SUM(SaldoVencido) AS SaldoVencido_le90,
  SUM(SaldoMorosidad) AS SaldoMorosidad_gt90
FROM CREDITO.SaldoCarteraMensual
WHERE Anio = 2026 AND Mes = 9
GROUP BY Anio, Mes;
/* 1000784.16 | 186203.31 | 18.6057 % | 36826.10 | 149377.21
   Identidad: SaldoMoraCartera = SaldoVencido + SaldoMorosidad */

/* Q05 — SCM julio 2023 (inicio serie) */
SELECT Anio, Mes, SUM(SaldoCartera) AS Saldo
FROM CREDITO.SaldoCarteraMensual WHERE Anio=2023 AND Mes=7 GROUP BY Anio, Mes;
/* 656611.01 → crecimiento a sep-2026: 52,4 % */

/* Q06 — Rango SCM */
SELECT MIN(DATEFROMPARTS(Anio,Mes,1)) AS Primero,
       MAX(DATEFROMPARTS(Anio,Mes,1)) AS Ultimo,
       COUNT(DISTINCT DATEFROMPARTS(Anio,Mes,1)) AS Meses
FROM CREDITO.SaldoCarteraMensual;
/* 2023-07 a 2026-10 = 40 filas-mes; octubre es mes en curso → no usar como cerrado */

/* Q07 — Cierres gerenciales */
SELECT Periodo, FechaCorte, Estado, Version FROM CREDITO.CierreGerencial ORDER BY Periodo;
/* Solo 2026-07 y 2026-08, Estado=CER */

/* Q08–Q09 — Distritos (residencia clientes con desembolso) */
;WITH Cli AS (
  SELECT DISTINCT c.PersonaId, p.DistritoId
  FROM CREDITO.Credito c
  INNER JOIN MAESTRO.Persona p ON p.PersonaId = c.PersonaId
  WHERE c.FechaDesembolso IS NOT NULL AND CAST(c.FechaDesembolso AS date) <= @FechaCorte
)
SELECT COUNT(*) AS Clientes,
       SUM(CASE WHEN DistritoId IS NULL THEN 1 ELSE 0 END) AS SinDistrito,
       COUNT(DISTINCT DistritoId) AS DistritosConClientes
FROM Cli;
/* 5794 | 379 | 22  — 22 = distritos con al menos un cliente, NO ámbito operativo declarado */

;WITH Cli AS (
  SELECT DISTINCT c.PersonaId, ISNULL(d.Denominacion, N'(sin distrito)') AS Distrito
  FROM CREDITO.Credito c
  INNER JOIN MAESTRO.Persona p ON p.PersonaId = c.PersonaId
  LEFT JOIN MAESTRO.Distrito d ON d.idDist = p.DistritoId
  WHERE c.FechaDesembolso IS NOT NULL AND CAST(c.FechaDesembolso AS date) <= @FechaCorte
)
SELECT Distrito, COUNT(*) AS N,
  CAST(100.0*COUNT(*)/SUM(COUNT(*)) OVER() AS decimal(10,2)) AS Pct
FROM Cli GROUP BY Distrito ORDER BY N DESC;

SELECT COUNT(*) AS CatalogoDistritosNacional FROM MAESTRO.Distrito; /* 1832 */

/* Q12–Q14 — Cierre vs SCM (delta constante) */
SELECT 'CIERRE' AS Fuente, SUM(cgd.CapitalCierre) AS Saldo, SUM(cgd.TotalClientesActivos) AS Clientes, SUM(cgd.MoraGeneral) AS Mora
FROM CREDITO.CierreGerencial cg
INNER JOIN CREDITO.CierreGerencialDetalle cgd ON cgd.CierreGerencialId = cg.CierreGerencialId
WHERE cg.Periodo = '2026-07-01' AND cg.Estado = 'CER'
UNION ALL
SELECT 'SCM', SUM(SaldoCartera), SUM(NroClientesSaldoCartera), SUM(SaldoMoraCartera)
FROM CREDITO.SaldoCarteraMensual WHERE Anio=2026 AND Mes=7;
/* Delta saldo = 3219.50; delta clientes = 20 (idem agosto) */

SELECT cgd.TipoCartera, SUM(cgd.CapitalCierre) AS Capital, SUM(cgd.MoraGeneral) AS Mora, SUM(cgd.TotalClientesActivos) AS Clientes
FROM CREDITO.CierreGerencial cg
INNER JOIN CREDITO.CierreGerencialDetalle cgd ON cgd.CierreGerencialId = cg.CierreGerencialId
WHERE cg.Periodo = '2026-08-01' AND cg.Estado = 'CER'
GROUP BY cgd.TipoCartera;
/* ESPECIAL: 142968.23 / 168 cli; PRODUCTIVA: 869180.04 / mora 20538.88 (~2,4 %) */

/* Q15 — Metas: filas activas pero valores NULL */
SELECT COUNT(*) AS FilasActivas,
  SUM(CASE WHEN MetaCapitalCierre IS NULL THEN 1 ELSE 0 END) AS MetaCapitalNull
FROM CREDITO.MetaGerencialAnalista WHERE Activo = 1;
/* 19 activas; 19 con MetaCapitalCierre NULL → metas no parametrizadas */

/* Q17–Q18 — Comportamiento */
/* Mediana MontoDesembolso = 500; mediana NumeroCuotas = 26; FormaPago D = 73,59 % */

/* Q19 — Esquema */
SELECT COUNT(*) AS Tablas FROM sys.tables; /* 67 */
SELECT COUNT(*) AS Vacias FROM sys.tables t
WHERE NOT EXISTS (SELECT 1 FROM sys.partitions p WHERE p.object_id=t.object_id AND p.index_id IN (0,1) AND p.rows>0); /* 21 */
/* Tamaño aprox. 326,7 MB (allocation_units) — no 365 MB */

/* Q21 — Cochran I13 con N = desembolsados */
DECLARE @N int = 77308, @Z float = 1.96, @p float = 0.5, @e float = 0.05;
DECLARE @n float = (@N * @Z*@Z * @p * (1-@p)) / (@e*@e * (@N-1) + @Z*@Z * @p * (1-@p));
SELECT @N AS N_desembolsados, @n AS n_bruto, CEILING(@n) AS n_redondeo_arriba;
/* n ≈ 382,28 → 383 (igual que con N=77388 a este e y Z) */
