
/*

SELECT * FROM CREDITO.PlanPago WHERE CreditoId=45
EXEC CREDITO.usp_RptCredito 1,NULL,'ANU','20210101','20220501'

*/
CREATE PROC [CREDITO].[usp_RptCredito]
@OficinaId INT ,
@GestorId INT ,
@Estado CHAR(3),
@FechaDesIni DATE ,
@FechaDesFin DATE 
AS


IF @Estado='CRE' OR @Estado='PEN'
BEGIN
	SELECT	UPPER(PR.Denominacion) 'Producto',P.NombreCompleto 'Cliente',C.CreditoId,FechaDesembolso,
			(SELECT MAX(FechaVencimiento) FROM CREDITO.PlanPago WHERE CreditoId=C.CreditoId) 'FechaVcto',
			C.FormaPago,NumeroCuotas,Interes,C.Estado,
			C.MontoProducto,C.MontoInicial,MontoCredito,TipoGastoAdm,MontoGastosAdm,MontoDesembolso,C.Observacion				
	FROM CREDITO.Credito C
	INNER JOIN CREDITO.Producto PR ON C.ProductoId = PR.ProductoId
	INNER JOIN MAESTRO.Persona P ON C.PersonaId = P.PersonaId
	WHERE	C.OficinaId = ISNULL(@OficinaId,C.OficinaId) 
			AND C.UsuarioRegId = ISNULL(@GestorId,C.UsuarioRegId) 
			AND C.Estado=@Estado 
			AND	CAST(C.FechaReg AS DATE) BETWEEN @FechaDesIni AND @FechaDesFin
END

IF @Estado='DES' 
BEGIN
	SELECT	UPPER(PR.Denominacion) 'Producto',P.NombreCompleto 'Cliente',C.CreditoId,FechaDesembolso,
			(SELECT MAX(FechaVencimiento) FROM CREDITO.PlanPago WHERE CreditoId=C.CreditoId) 'FechaVcto',
			C.FormaPago,NumeroCuotas,Interes,C.Estado,
			C.MontoProducto,C.MontoInicial,MontoCredito,TipoGastoAdm,MontoGastosAdm,MontoDesembolso,C.Observacion				
	FROM CREDITO.Credito C
	INNER JOIN CREDITO.Producto PR ON C.ProductoId = PR.ProductoId
	INNER JOIN MAESTRO.Persona P ON C.PersonaId = P.PersonaId
	WHERE	C.OficinaId = ISNULL(@OficinaId,C.OficinaId) 
			AND C.UsuarioRegId = ISNULL(@GestorId,C.UsuarioRegId) 
			AND C.Estado=@Estado 
			AND	CAST(C.FechaDesembolso AS DATE) BETWEEN @FechaDesIni AND @FechaDesFin
END

IF @Estado='PAG' 
BEGIN
	SELECT	UPPER(PR.Denominacion) 'Producto',P.NombreCompleto 'Cliente',C.CreditoId,FechaDesembolso,
			(SELECT MAX(FechaVencimiento) FROM CREDITO.PlanPago WHERE CreditoId=C.CreditoId) 'FechaVcto',
			C.FormaPago,NumeroCuotas,Interes,C.Estado,
			C.MontoProducto,C.MontoInicial,MontoCredito,TipoGastoAdm,MontoGastosAdm,MontoDesembolso,C.Observacion				
	FROM CREDITO.Credito C
	INNER JOIN CREDITO.Producto PR ON C.ProductoId = PR.ProductoId
	INNER JOIN MAESTRO.Persona P ON C.PersonaId = P.PersonaId
	WHERE	C.OficinaId = ISNULL(@OficinaId,C.OficinaId) 
			AND C.UsuarioRegId = ISNULL(@GestorId,C.UsuarioRegId) 
			AND C.Estado=@Estado 
			AND	CAST(C.FechaPagado AS DATE) BETWEEN @FechaDesIni AND @FechaDesFin
END

IF @Estado='ANU' or @Estado='REP' 
BEGIN
	SELECT	UPPER(PR.Denominacion) 'Producto',P.NombreCompleto 'Cliente',C.CreditoId,FechaDesembolso,
			(SELECT MAX(FechaVencimiento) FROM CREDITO.PlanPago WHERE CreditoId=C.CreditoId) 'FechaVcto',
			C.FormaPago,NumeroCuotas,Interes,C.Estado,
			C.MontoProducto,C.MontoInicial,MontoCredito,TipoGastoAdm,MontoGastosAdm,MontoDesembolso, C.Observacion			
	FROM CREDITO.Credito C
	INNER JOIN CREDITO.Producto PR ON C.ProductoId = PR.ProductoId
	INNER JOIN MAESTRO.Persona P ON C.PersonaId = P.PersonaId
	WHERE	C.OficinaId = ISNULL(@OficinaId,C.OficinaId) 
			AND C.UsuarioRegId = ISNULL(@GestorId,C.UsuarioRegId) 
			AND C.Estado=@Estado 
			AND	CAST(C.FechaMod AS DATE) BETWEEN @FechaDesIni AND @FechaDesFin
END
