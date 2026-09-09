--CREDITO.usp_RptMovimientoCredito 12082
CREATE PROC [CREDITO].[usp_RptMovimientoCredito]
@CreditoId INT 
AS

DECLARE @Index INT=2, @Nro INT=0, @Pago DECIMAL(16,2)=0
DECLARE @Saldo DECIMAL(16,2)= (SELECT SUM(Cuota) FROM CREDITO.PlanPago WITH(NOLOCK) WHERE CreditoId= @CreditoId)

DECLARE @tPagos TABLE(Id INT IDENTITY(1,1),MovimientoCajaId INT,Fecha DATETIME, Operacion CHAR(3),
								Glosa VARCHAR(MAX),ImportePago DECIMAL(16,2), Saldo DECIMAL(16,2))

INSERT INTO @tPagos
SELECT 0,FechaDesembolso,'---','SALDO INICIAL',0.0 'Pago',@Saldo 'Saldo' FROM CREDITO.Credito WITH(NOLOCK) WHERE CreditoId=@CreditoId

INSERT INTO @tPagos
SELECT MovimientoCajaId,FechaReg,Operacion,Descripcion,ImportePago,0.0 'Saldo'
FROM CREDITO.MovimientoCaja WITH(NOLOCK)
WHERE CreditoId=@CreditoId AND Estado=1 AND Operacion='CUO' --AND ImportePago>0

--INSERT INTO @tPagos
--SELECT MovimientoCajaId,FechaPagoCuota,'MOR','MORA / CARGO CUOTA ' + CAST(Numero AS VARCHAR(10)),-(ImporteMora),0.0 'Saldo'
--FROM CREDITO.PlanPago WITH(NOLOCK)
--WHERE CreditoId=@CreditoId AND ImporteMora>0 AND Estado='PAG'

--INSERT INTO @tPagos
--SELECT MovimientoCajaId,FechaPagoCuota,'DES','DESCUENTO CUOTA ' + CAST(Numero AS VARCHAR(10)),Descuento,0.0 'Saldo'
--FROM CREDITO.PlanPago WITH(NOLOCK)
--WHERE CreditoId=@CreditoId AND Descuento>0 AND Estado='PAG'

--SELECT * FROM @tPagos
--SELECT  @Saldo
SET @Nro = (SELECT COUNT(1) FROM @tPagos)
WHILE @Index<=@Nro
BEGIN
	SELECT @Pago=ImportePago
	FROM @tPagos WHERE Id=@Index
	
	SET @Saldo=@Saldo - @Pago
	--SELECT  @Pago, @Saldo

	UPDATE @tPagos SET	 Saldo=@Saldo WHERE Id=@Index

	SET @Index = @Index + 1
END

SELECT MovimientoCajaId,Fecha,Operacion,Glosa,ImportePago,Saldo FROM @tPagos
ORDER BY MovimientoCajaId ASC
