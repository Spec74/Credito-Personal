
CREATE   PROCEDURE [CREDITO].[usp_CreditoMora_Liquidar]
    @CreditoId INT,
    @CajaDiarioId INT,
    @UsuarioId INT,
    @TipoPagoId INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TotalMoraAPagar DECIMAL(16,2) =
        (SELECT ISNULL(SUM(SaldoMora), 0) FROM [CREDITO].[CreditoMora] WHERE [CreditoId] = @CreditoId AND [MovimientoCajaId] IS NULL);

    IF @TotalMoraAPagar > 0
    BEGIN
        DECLARE @PersonaId INT, @MovimientoCajaId INT;
        SELECT @PersonaId = PersonaId FROM CREDITO.Credito WHERE CreditoId = @CreditoId;

        INSERT INTO CREDITO.MovimientoCaja (
            CajaDiarioId, PersonaId, Operacion, ImportePago, Descripcion,
            IndEntrada, Estado, OrdenVentaId, CreditoId, UsuarioRegId, FechaReg, TipoPagoId
        )
        VALUES (
            @CajaDiarioId, @PersonaId, 'MOR', @TotalMoraAPagar,
            'CREDITO ' + CAST(@CreditoId AS VARCHAR(20)) + ' LIQUIDACION DE MORAS',
            1, 1, NULL, @CreditoId, @UsuarioId, GETDATE(), @TipoPagoId
        );

        -- SCOPE_IDENTITY evita capturar el identity de un trigger, a diferencia de @@IDENTITY.
        SET @MovimientoCajaId = CAST(SCOPE_IDENTITY() AS INT);

        UPDATE [CREDITO].[CreditoMora]
        SET
            [MovimientoCajaId] = @MovimientoCajaId,
            [SaldoMora] = 0.00
        WHERE
            [CreditoId] = @CreditoId
            AND [MovimientoCajaId] IS NULL;

        EXEC CREDITO.usp_RecalcularCajaDiario @CajaDiarioId;

        SELECT @MovimientoCajaId AS MovimientoCajaId;
    END
    ELSE
    BEGIN
        SELECT 0 AS MovimientoCajaId;
    END
END
