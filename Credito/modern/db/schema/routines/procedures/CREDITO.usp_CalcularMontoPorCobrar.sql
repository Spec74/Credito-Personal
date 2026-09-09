
/*  
CREDITO.usp_CalcularMontoPorCobrar 10
*/  
CREATE PROC [CREDITO].[usp_CalcularMontoPorCobrar]  
@UsuarioId INT  
AS  
  
DECLARE @MontoPorCobrar DECIMAL(16,2)
  
;WITH DESEMBOLSOS AS(  
 SELECT   
 --( SELECT SUM(Cuota) FROM CREDITO.PlanPago   
  --  WHERE CreditoId=C.CreditoId AND Estado='PEN' AND FechaVencimiento<=dbo.ufnFecha()) 'CuotaAcumulada',  
    (SELECT TOP 1 Cuota FROM CREDITO.PlanPago   
				WHERE CreditoId=C.CreditoId AND Estado='PEN' ORDER BY Numero) 'CuotaPendiente'    
 FROM CREDITO.Credito C  
 WHERE C.Estado='DES' AND C.UsuarioRegId=@UsuarioId  
)  
SELECT @MontoPorCobrar =SUM(CuotaPendiente)
FROM DESEMBOLSOS   
    
UPDATE CREDITO.CajaDiario SET MontoPorCobrar = ISNULL(@MontoPorCobrar,0)  
WHERE UsuarioAsignadoId=@UsuarioId AND IndCierre=0
