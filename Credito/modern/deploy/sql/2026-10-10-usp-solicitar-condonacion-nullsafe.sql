-- Fix: usp_SolicitarCondonacion fallaba con NULL en TotalPago cuando
-- no hay pagos CUO (SUM → NULL) o Interes/MontoCredito nulos.
-- Sintoma FE: "No se pudo registrar la solicitud de condonación."

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROC [CREDITO].[usp_SolicitarCondonacion]
    @CajaDiarioId INT,
    @CreditoId INT,
    @MoraCondonacion DECIMAL(15, 2)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MontoCredito DECIMAL(15, 2) = (
        SELECT
            ISNULL(MontoCredito, 0)
            + ISNULL(MontoCredito, 0) * ISNULL(Interes, 0) / 100.0
        FROM CREDITO.Credito
        WHERE CreditoId = @CreditoId
    );

    IF @MontoCredito IS NULL
    BEGIN
        RAISERROR(N'No existe el crédito indicado para condonación.', 16, 1);
        RETURN;
    END;

    DECLARE @Pagos DECIMAL(15, 2) = ISNULL((
        SELECT SUM(ImportePago)
        FROM CREDITO.MovimientoCaja
        WHERE CreditoId = @CreditoId
          AND ImportePago > 0
          AND Operacion = N'CUO'
    ), 0);

    INSERT CREDITO.CreditoCondonacion
    (
        CreditoId,
        CajaDiarioId,
        Fecha,
        MoraCondonacion,
        IndAprobado,
        TotalPago
    )
    VALUES
    (
        @CreditoId,
        @CajaDiarioId,
        dbo.ufnFecha(),
        @MoraCondonacion,
        CAST(0 AS bit),
        @MontoCredito - @Pagos + ISNULL(@MoraCondonacion, 0)
    );
END
GO
