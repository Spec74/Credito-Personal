-- Alinea CompletarImpagosValidacion con CompletarImpagos:
-- no contar créditos cuyo 1.er pago aún no llega (pago adelantado el día del desembolso es voluntario).
-- Idempotente: recrea el procedimiento.

SET NOCOUNT ON;
GO

CREATE OR ALTER PROC [CREDITO].[usp_CompletarImpagosValidacion]
    @CajaDiarioId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UsuarioId INT = (
        SELECT UsuarioAsignadoId
        FROM CREDITO.CajaDiario
        WHERE CajaDiarioId = @CajaDiarioId
    );
    DECLARE @Impagos INT = 0;
    DECLARE @Fecha DATETIME = dbo.ufnFecha();

    SELECT @Impagos = COUNT(1)
    FROM CREDITO.Credito AS c
    WHERE c.Estado = N'DES'
      AND c.UsuarioRegId = @UsuarioId
      AND c.FechaPrimerPago <= CAST(@Fecha AS DATE)
      AND NOT EXISTS (
          SELECT 1
          FROM CREDITO.MovimientoCaja AS mc
          WHERE mc.CreditoId = c.CreditoId
            AND mc.CajaDiarioId = @CajaDiarioId
            AND mc.IndEntrada = 1
            AND mc.Estado = 1
            AND mc.Operacion = N'CUO'
      )
      AND EXISTS (
          SELECT 1
          FROM CREDITO.PlanPago AS pp
          WHERE pp.CreditoId = c.CreditoId
            AND pp.FechaVencimiento = CAST(@Fecha AS DATE)
      );

    SELECT @Impagos AS Impagos;
END
GO
