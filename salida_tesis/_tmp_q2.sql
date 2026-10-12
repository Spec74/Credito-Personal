SET NOCOUNT ON;
DECLARE @FechaCorte date = '2026-10-06';

PRINT '=== Q08 DISTRITOS CLIENTES DESEMBOLSO ===';
;WITH Cli AS (
  SELECT DISTINCT c.PersonaId, p.DistritoId
  FROM CREDITO.Credito c
  INNER JOIN MAESTRO.Persona p ON p.PersonaId = c.PersonaId
  WHERE c.FechaDesembolso IS NOT NULL AND CAST(c.FechaDesembolso AS date) <= @FechaCorte
)
SELECT
  COUNT(*) AS Clientes,
  SUM(CASE WHEN DistritoId IS NULL THEN 1 ELSE 0 END) AS SinDistrito,
  COUNT(DISTINCT DistritoId) AS DistritosDistintos
FROM Cli;

PRINT '=== Q09 TOP DISTRITOS ===';
;WITH Cli AS (
  SELECT DISTINCT c.PersonaId, ISNULL(d.Denominacion, N'(sin distrito)') AS Distrito
  FROM CREDITO.Credito c
  INNER JOIN MAESTRO.Persona p ON p.PersonaId = c.PersonaId
  LEFT JOIN MAESTRO.Distrito d ON d.idDist = p.DistritoId
  WHERE c.FechaDesembolso IS NOT NULL AND CAST(c.FechaDesembolso AS date) <= @FechaCorte
),
Agg AS (
  SELECT Distrito, COUNT(*) AS N,
    CAST(100.0 * COUNT(*) / SUM(COUNT(*)) OVER() AS decimal(10,2)) AS Pct
  FROM Cli GROUP BY Distrito
)
SELECT TOP 20 Distrito, N, Pct FROM Agg ORDER BY N DESC;

PRINT '=== Q11 CATALOGO DISTRITO ===';
SELECT COUNT(*) AS DistritosCatalogo FROM MAESTRO.Distrito;

PRINT '=== Q12 CIERRE DETALLE AGO ===';
SELECT cg.Periodo,
  SUM(cgd.CapitalCierre) AS CapitalCierre,
  SUM(cgd.MoraGeneral) AS MoraGeneral,
  SUM(cgd.VencidosCuotasCierre) AS VencidosCuotas,
  SUM(cgd.TotalClientesActivos) AS ClientesActivos,
  SUM(cgd.ClientesVencidosCierre) AS ClientesVencidos
FROM CREDITO.CierreGerencial cg
INNER JOIN CREDITO.CierreGerencialDetalle cgd ON cgd.CierreGerencialId = cg.CierreGerencialId
WHERE cg.Periodo = '2026-08-01' AND cg.Estado = 'CER'
GROUP BY cg.Periodo;

PRINT '=== Q13 CIERRE JUL vs SCM JUL ===';
SELECT 'CIERRE' AS Fuente, SUM(cgd.CapitalCierre) AS Saldo, SUM(cgd.TotalClientesActivos) AS Clientes, SUM(cgd.MoraGeneral) AS Mora
FROM CREDITO.CierreGerencial cg
INNER JOIN CREDITO.CierreGerencialDetalle cgd ON cgd.CierreGerencialId = cg.CierreGerencialId
WHERE cg.Periodo = '2026-07-01' AND cg.Estado = 'CER'
UNION ALL
SELECT 'SCM', SUM(SaldoCartera), SUM(NroClientesSaldoCartera), SUM(SaldoMoraCartera)
FROM CREDITO.SaldoCarteraMensual WHERE Anio=2026 AND Mes=7;

PRINT '=== Q14 CIERRE AGO vs SCM AGO ===';
SELECT 'CIERRE' AS Fuente, SUM(cgd.CapitalCierre) AS Saldo, SUM(cgd.TotalClientesActivos) AS Clientes, SUM(cgd.MoraGeneral) AS Mora
FROM CREDITO.CierreGerencial cg
INNER JOIN CREDITO.CierreGerencialDetalle cgd ON cgd.CierreGerencialId = cg.CierreGerencialId
WHERE cg.Periodo = '2026-08-01' AND cg.Estado = 'CER'
UNION ALL
SELECT 'SCM', SUM(SaldoCartera), SUM(NroClientesSaldoCartera), SUM(SaldoMoraCartera)
FROM CREDITO.SaldoCarteraMensual WHERE Anio=2026 AND Mes=8;

PRINT '=== Q14b ESPECIALES AGO ===';
SELECT cgd.TipoCartera,
  SUM(cgd.CapitalCierre) AS Capital,
  SUM(cgd.MoraGeneral) AS Mora,
  SUM(cgd.TotalClientesActivos) AS Clientes,
  COUNT(*) AS Filas
FROM CREDITO.CierreGerencial cg
INNER JOIN CREDITO.CierreGerencialDetalle cgd ON cgd.CierreGerencialId = cg.CierreGerencialId
WHERE cg.Periodo = '2026-08-01' AND cg.Estado = 'CER'
GROUP BY cgd.TipoCartera;

PRINT '=== Q15 METAS ===';
SELECT COUNT(*) AS Total, SUM(CASE WHEN Activo=1 THEN 1 ELSE 0 END) AS Activas,
  SUM(CASE WHEN TipoCartera='PRODUCTIVA' AND Activo=1 THEN 1 ELSE 0 END) AS Productivas,
  SUM(CASE WHEN TipoCartera='ESPECIAL' AND Activo=1 THEN 1 ELSE 0 END) AS Especiales
FROM CREDITO.MetaGerencialAnalista;

PRINT '=== Q16 OFICINAS ===';
SELECT OficinaId, Denominacion FROM MAESTRO.Oficina ORDER BY OficinaId;

PRINT '=== Q17 MEDIANAS ===';
;WITH D AS (
  SELECT MontoDesembolso,
    ROW_NUMBER() OVER (ORDER BY MontoDesembolso) AS rn,
    COUNT(*) OVER() AS n
  FROM CREDITO.Credito
  WHERE FechaDesembolso IS NOT NULL AND CAST(FechaDesembolso AS date) <= @FechaCorte
)
SELECT AVG(1.0*MontoDesembolso) AS MedianaMonto FROM D WHERE rn IN ((n+1)/2, (n+2)/2);
;WITH D2 AS (
  SELECT NumeroCuotas,
    ROW_NUMBER() OVER (ORDER BY NumeroCuotas) AS rn,
    COUNT(*) OVER() AS n
  FROM CREDITO.Credito
  WHERE FechaDesembolso IS NOT NULL AND CAST(FechaDesembolso AS date) <= @FechaCorte
)
SELECT AVG(1.0*NumeroCuotas) AS MedianaCuotas FROM D2 WHERE rn IN ((n+1)/2, (n+2)/2);

PRINT '=== Q18 FORMA PAGO ===';
SELECT FormaPago, COUNT(*) AS N,
  CAST(100.0*COUNT(*)/SUM(COUNT(*)) OVER() AS decimal(10,2)) AS Pct
FROM CREDITO.Credito
WHERE FechaDesembolso IS NOT NULL AND CAST(FechaDesembolso AS date) <= @FechaCorte
GROUP BY FormaPago ORDER BY N DESC;

PRINT '=== Q19 TABLAS / SIZE ===';
SELECT COUNT(*) AS Tablas FROM sys.tables;
SELECT COUNT(*) AS Vacias
FROM sys.tables t
WHERE NOT EXISTS (SELECT 1 FROM sys.partitions p WHERE p.object_id=t.object_id AND p.index_id IN (0,1) AND p.rows>0);
SELECT CAST(SUM(a.total_pages)*8.0/1024 AS decimal(10,1)) AS MB
FROM sys.tables t
JOIN sys.indexes i ON t.object_id=i.object_id
JOIN sys.partitions p ON i.object_id=p.object_id AND i.index_id=p.index_id
JOIN sys.allocation_units a ON p.partition_id=a.container_id;

PRINT '=== Q20 SCM OCT (mes en curso - NO usar como cerrado) ===';
SELECT Anio, Mes, SUM(SaldoCartera) AS Saldo, SUM(SaldoMoraCartera) AS Mora
FROM CREDITO.SaldoCarteraMensual WHERE Anio=2026 AND Mes=10 GROUP BY Anio, Mes;
