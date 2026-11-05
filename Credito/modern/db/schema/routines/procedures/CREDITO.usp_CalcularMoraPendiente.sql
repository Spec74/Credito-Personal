-- Retirado: fórmula consolidada saldo×días divergía de la mora por cuota
-- (cobro / grilla / reportes usan usp_CuotasPendientes + ufnCalcularMora por cuota).
-- Ejecutar en cada ambiente (local / Azure / sucursales) al desplegar.
IF OBJECT_ID(N'CREDITO.usp_CalcularMoraPendiente', N'P') IS NOT NULL
    DROP PROCEDURE CREDITO.usp_CalcularMoraPendiente;
GO
