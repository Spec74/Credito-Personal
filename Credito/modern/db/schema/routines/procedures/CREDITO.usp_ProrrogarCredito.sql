Create proc CREDITO.usp_ProrrogarCredito
@CreditoId INT,
@Dias INT=7
AS

UPDATE PP
SET PP.FechaVencimiento= DATEADD(day,@Dias,PP.FechaVencimiento)
--select C.* 
FROM CREDITO.Credito C
INNER JOIN CREDITO.PlanPago PP on C.CreditoId=PP.CreditoId
WHERE C.CreditoId=@CreditoId and 
C.Estado='DES' AND PP.Estado='PEN'

UPDATE C
SET C.FechaVencimiento= DATEADD(day,@Dias,C.FechaVencimiento)
--select C.* 
from CREDITO.Credito C
where C.CreditoId=@CreditoId AND
C.Estado='DES'
