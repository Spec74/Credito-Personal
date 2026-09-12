CREATE PROC [CREDITO].[usp_SolicitarCondonacion]
@CajaDiarioId INT ,
@CreditoId INT ,
@MoraCondonacion DECIMAL(15,2)
 AS

 DECLARE @MontoCredito DECIMAL(15,2) = (SELECT MontoCredito + (MontoCredito * Interes / 100)
 FROM CREDITO.Credito WHERE CreditoId=@CreditoId)

 DECLARE @Pagos DECIMAL(15,2) = (SELECT SUM(ImportePago) FROM CREDITO.MovimientoCaja
 WHERE CreditoId=@CreditoId AND ImportePago>0 AND Operacion='CUO')

 INSERT CREDITO.CreditoCondonacion
 (
     CreditoId,     CajaDiarioId,     Fecha,     MoraCondonacion,     IndAprobado, TotalPago
 )
 VALUES
 (   @CreditoId,  @CajaDiarioId, dbo.ufnFecha(), @MoraCondonacion,       0  , @MontoCredito - @Pagos + @MoraCondonacion)
