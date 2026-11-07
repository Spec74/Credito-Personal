
CREATE OR ALTER PROC [CREDITO].[usp_ReprogramarCredito]
@CreditoId INT,
@UsuarioId INT
AS

DECLARE @Deuda DECIMAL(16,2)=0, @FechaAct DATE = dbo.ufnFecha()
DECLARE @NuevoCreditoId INT = NULL
DECLARE @tCuotasPendientes TABLE(Id INT IDENTITY(1,1),PlanPagoId INT,Glosa VARCHAR(MAX),FechaVencimiento DATE,Amortizacion DECIMAL(16,2),
								Interes DECIMAL(16,2), GastosAdm DECIMAL(16,2), Cuota DECIMAL(16,2), DiasAtrazo INT,
								ImporteMora DECIMAL(16,2),Descuento DECIMAL(16,2), Cargo DECIMAL(16,2),
								PagoLibre DECIMAL(16,2),PagoCuota DECIMAL(16,2))
INSERT INTO @tCuotasPendientes
EXEC CREDITO.usp_CuotasPendientes @CreditoId,@FechaAct

SET @Deuda = ISNULL((SELECT SUM(PagoCuota) FROM @tCuotasPendientes), 0)

IF @Deuda <= 0
BEGIN
	RAISERROR(N'No hay deuda pendiente para reprogramar.', 16, 1)
	RETURN
END

INSERT INTO	CREDITO.Credito
( OficinaId,PersonaId,ProductoId, Descripcion, MontoProducto, MontoInicial ,MontoGastosAdm ,MontoCredito ,
  FormaPago,NumeroCuotas,Interes,FechaPrimerPago,Observacion,Estado,FechaReg,UsuarioRegId,OrdenVentaId )
SELECT	OficinaId,PersonaId,ProductoId,Descripcion, @Deuda , 0 'Inicial' , 0 'GastoAdm', @Deuda,
		FormaPago,NumeroCuotas,Interes,@FechaAct 'PrimerPago','REPROGRAMADO CREDITO ' + CAST(@CreditoId AS VARCHAR(10)) 'Obs',
		'CRE',@FechaAct,@UsuarioId,OrdenVentaId
FROM CREDITO.Credito WITH(NOLOCK) WHERE CreditoId=@CreditoId

SET @NuevoCreditoId = CAST(SCOPE_IDENTITY() AS INT)

UPDATE CREDITO.Credito SET Estado = 'REP'
WHERE CreditoId = @CreditoId

SELECT @NuevoCreditoId AS NuevoCreditoId, @Deuda AS Deuda
