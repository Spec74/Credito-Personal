-- Caja diario: SaldoFinal exacto a 2 decimales (antes ROUND(...,1) perdía/creaba céntimos).
-- Idempotente: CREATE OR ALTER.

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [CREDITO].[usp_RecalcularCajaDiario](@CajaDiarioId INT)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Entradas DECIMAL(16, 2);
    DECLARE @Salidas DECIMAL(16, 2);

    SELECT @Entradas = SUM(ImportePago)
    FROM CREDITO.MovimientoCaja
    WHERE CajaDiarioId = @CajaDiarioId
      AND Estado = 1
      AND IndEntrada = 1;

    SELECT @Salidas = SUM(ImportePago)
    FROM CREDITO.MovimientoCaja
    WHERE CajaDiarioId = @CajaDiarioId
      AND Estado = 1
      AND IndEntrada = 0;

    UPDATE CREDITO.CajaDiario
    SET Entradas = ISNULL(@Entradas, 0),
        Salidas = ISNULL(@Salidas, 0),
        SaldoFinal = CAST(
            SaldoInicial + ISNULL(@Entradas, 0) - ISNULL(@Salidas, 0)
            AS DECIMAL(16, 2)
        )
    WHERE CajaDiarioId = @CajaDiarioId;
END
GO
