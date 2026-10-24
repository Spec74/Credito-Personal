-- Rechazo de solicitud PEN. NO toca CREDITO.usp_Credito_Del (eso es ANULAR
-- del legacy: UPDATE Estado='ANU', no DELETE).
--
-- Legacy:
--   RechazarCredito → usp_OrdenVenta_Del (si hay OV) o usp_SolicitudCredito_Del
--   AnularCredito   → usp_Credito_Del
--
-- Este lote SOLO completa usp_SolicitudCredito_Del (hijos que bloquean
-- DELETE Credito: Aprobacion, Prenda, imagen, tarea, cargo, central).
-- NO reemplaza VENTAS.usp_OrdenVenta_Del: es USP de ventas del MVC; si
-- producción divergió, un CREATE OR ALTER entero rompe borrar OV.
-- La API moderna ya limpia esos hijos en C# antes de llamar a cualquiera
-- de los dos USP.
--
-- No se tocan CxC / caja / mora / condonación: esos créditos se anulan.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROC CREDITO.[usp_SolicitudCredito_Del]
    @CreditoId INT = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @CreditoId IS NULL OR @CreditoId < 1
        RETURN;

    IF EXISTS (
        SELECT 1 FROM CREDITO.MovimientoCaja WHERE CreditoId = @CreditoId
        UNION ALL
        SELECT 1 FROM CREDITO.CuentaxCobrar WHERE CreditoId = @CreditoId
        UNION ALL
        SELECT 1 FROM CREDITO.CreditoMora WHERE CreditoId = @CreditoId
        UNION ALL
        SELECT 1 FROM CREDITO.CreditoCondonacion WHERE CreditoId = @CreditoId
    )
    BEGIN
        RAISERROR(N'No se puede rechazar: el crédito ya tiene cuentas por cobrar, pagos de caja o mora. Use anular.', 16, 1);
        RETURN;
    END

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Credito
        WHERE CreditoId = @CreditoId
          AND Estado <> 'PEN'
    )
    BEGIN
        RAISERROR(N'Solo se puede rechazar un crédito pendiente de aprobación (PEN). Si ya está aprobado o desembolsado, use anular.', 16, 1);
        RETURN;
    END

    IF OBJECT_ID(N'CREDITO.PlaInusual', N'U') IS NOT NULL
        UPDATE CREDITO.PlaInusual SET CreditoId = NULL WHERE CreditoId = @CreditoId;

    DELETE FROM CREDITO.Aprobacion WHERE CreditoId = @CreditoId;
    DELETE FROM CREDITO.CreditoImagen WHERE CreditoId = @CreditoId;
    DELETE FROM CREDITO.CentralRiesgo WHERE CreditoId = @CreditoId;
    DELETE FROM CREDITO.Cargo WHERE CreditoId = @CreditoId;

    DELETE st
    FROM CREDITO.Subtarea st
    INNER JOIN CREDITO.Tarea t ON t.TareaId = st.TareaId
    WHERE t.CreditoId = @CreditoId;

    DELETE FROM CREDITO.Tarea WHERE CreditoId = @CreditoId;
    DELETE FROM CREDITO.Prenda WHERE CreditoId = @CreditoId;
    DELETE FROM CREDITO.PlanPago WHERE CreditoId = @CreditoId;
    DELETE FROM CREDITO.Credito WHERE CreditoId = @CreditoId;
END
GO
