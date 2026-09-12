CREATE PROCEDURE [CREDITO].[usp_RegistrarTransferenciaBancos]
    @BovedaId INT,
    @TipoPagoOrigenId SMALLINT,
    @TipoPagoDestinoId SMALLINT,
    @Importe DECIMAL(16,2),
    @Glosa VARCHAR(MAX),
    @UsuarioRegId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @TipoPagoOrigenId = @TipoPagoDestinoId
    BEGIN
        RAISERROR('El banco de origen y el de destino no pueden ser iguales.', 16, 1);
        RETURN;
    END

    IF @Importe <= 0
    BEGIN
        RAISERROR('El importe de la transferencia debe ser mayor a cero.', 16, 1);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @FechaActual DATETIME = GETDATE();
        DECLARE @CodOperacion CHAR(3) = 'TRF';

        INSERT INTO CREDITO.BovedaMov (
            BovedaId, CodOperacion, Glosa, Importe, IndEntrada, Estado,
            CajaDiarioId, UsuarioRegId, FechaReg, TipoPagoId)
        VALUES (
            @BovedaId, @CodOperacion, UPPER('Trf. Salida: ' + ISNULL(@Glosa, '')),
            @Importe, 0, 1, NULL, @UsuarioRegId, @FechaActual, @TipoPagoOrigenId);

        INSERT INTO CREDITO.BovedaMov (
            BovedaId, CodOperacion, Glosa, Importe, IndEntrada, Estado,
            CajaDiarioId, UsuarioRegId, FechaReg, TipoPagoId)
        VALUES (
            @BovedaId, @CodOperacion, UPPER('Trf. Ingreso: ' + ISNULL(@Glosa, '')),
            @Importe, 1, 1, NULL, @UsuarioRegId, @FechaActual, @TipoPagoDestinoId);

        COMMIT TRANSACTION;
        SELECT 1 AS Resultado, 'Transferencia realizada con éxito.' AS Mensaje;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        DECLARE @ErrorMsg VARCHAR(MAX) = ERROR_MESSAGE();
        SELECT 0 AS Resultado, 'Error: ' + @ErrorMsg AS Mensaje;
    END CATCH
END
