

-- exec CREDITO.usp_RptAval 38
CREATE PROC	[CREDITO].[usp_RptAval]
@PersonaId INT
AS

-- 1. CORREGIDO: Se cambió LEFT JOIN por INNER JOIN para evitar filas vacías si el crédito no tiene Aval
SELECT 
    'AVAL' 'Grupo', 
    C.CreditoId,
    C.MontoCredito, 
    C.Estado, 
    P.PersonaId,
    p.NombreCompleto 'Persona',
    p.NumeroDocumento 'Dni',
    p.Celular1 'Celular'
FROM CREDITO.Credito C
INNER JOIN MAESTRO.Persona P ON P.PersonaId = C.PersonaAvalId
WHERE C.PersonaId=@PersonaId 
  AND C.Estado IN('CRE','PEN','APR','DES')

UNION ALL

-- 2. SE MANTIENE: Muestra a quién avala el cliente actual (Funciona correctamente)
SELECT 
    'AVALADOS' 'Grupo', 
    C.CreditoId,
    C.MontoCredito, 
    C.Estado, 
    P.PersonaId,
    p.NombreCompleto 'Persona',
    p.NumeroDocumento 'Dni',
    p.Celular1 'Celular'
FROM CREDITO.Credito C
LEFT JOIN MAESTRO.Persona P ON P.PersonaId = C.PersonaId
WHERE C.PersonaAvalId=@PersonaId 
  AND C.Estado IN('CRE','PEN','APR','DES')
