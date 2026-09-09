-- EXEC [CREDITO].[usp_CompletarImpagosValidacion] 26080

CREATE PROC [CREDITO].[usp_CompletarImpagosValidacion]
@CajaDiarioId INT 
AS

DECLARE @UsuarioId INT=(select UsuarioAsignadoId from CREDITO.CajaDiario where CajaDiarioId=@CajaDiarioId)
DECLARE @Impagos INT=0
DECLARE @Fecha DATETIME = dbo.ufnFecha()

SELECT	@Impagos = COUNT(1)
FROM CREDITO.Credito c
    WHERE c.Estado = 'DES' 
      AND c.UsuarioRegId = @UsuarioId
      -- Condición 1: No existe movimiento de caja relacionado a caja diario
      AND NOT EXISTS (
          SELECT 1 
          FROM CREDITO.MovimientoCaja mc
          WHERE mc.CreditoId = c.CreditoId
		    AND mc.CajaDiarioId = @CajaDiarioId
            AND mc.IndEntrada = 1 
            AND mc.Estado = 1
		    AND mc.Operacion = 'CUO'
      )
	  -- Condición 2: La fecha actual NO coincide con ninguna fecha de vencimiento del plan de pago
      AND EXISTS (
          SELECT 1
          FROM CREDITO.PlanPago pp
          WHERE pp.CreditoId = c.CreditoId
            AND pp.FechaVencimiento = CAST(@Fecha AS DATE)
      );

Select @Impagos 'Impagos'
