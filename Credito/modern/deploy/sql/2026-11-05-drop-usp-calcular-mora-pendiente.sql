/*
  Retira CREDITO.usp_CalcularMoraPendiente (saldo×días).
  Mora operativa = suma ImporteMora por cuota (usp_CuotasPendientes / ufnCalcularMora).
  Ejecutar en cada BD CREDITO (local, Azure prueba, prod, réplicas de sucursal).
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'CREDITO.usp_CalcularMoraPendiente', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE CREDITO.usp_CalcularMoraPendiente;
    SELECT N'DROPPED' AS Resultado, N'CREDITO.usp_CalcularMoraPendiente' AS Objeto;
END
ELSE
BEGIN
    SELECT N'ALREADY_ABSENT' AS Resultado, N'CREDITO.usp_CalcularMoraPendiente' AS Objeto;
END
GO
