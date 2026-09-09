
CREATE PROC [CREDITO].[usp_CompletarImpagos]
    @CajaDiarioId INT 
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UsuarioId INT = (
        SELECT UsuarioAsignadoId 
        FROM CREDITO.CajaDiario 
        WHERE CajaDiarioId = @CajaDiarioId
    );

    DECLARE @Fecha DATETIME = dbo.ufnFecha() 
    
    IF EXISTS(SELECT 1 FROM CREDITO.CajaDiario WHERE CajaDiarioId = @CajaDiarioId AND IndCierre = 1)
    BEGIN
        SET @Fecha = (SELECT FechaFinOperacion FROM CREDITO.CajaDiario WHERE CajaDiarioId = @CajaDiarioId AND IndCierre = 1)
    END
		
    -- Validar que existe la caja y tiene usuario asignado
    IF @UsuarioId IS NULL
    BEGIN
        RAISERROR('CajaDiarioId no válido o sin usuario asignado.', 16, 1);
        RETURN;
    END

    -- Inserción masiva de impagos (Monto 0.00)
    INSERT INTO CREDITO.MovimientoCaja (
        CajaDiarioId, PersonaId, Operacion, ImportePago,
        Descripcion, IndEntrada, Estado, OrdenVentaId, 
        CreditoId, UsuarioRegId, FechaReg
    )
    SELECT 
        @CajaDiarioId, 
        c.PersonaId, 
        'CUO', 
        0, 
        'CRED ' + CAST(c.CreditoId AS VARCHAR(20)) + ' PAGO LIBRE 0', 
        1, 1, NULL, 
        c.CreditoId, 
        @UsuarioId, 
        @Fecha
    FROM CREDITO.Credito c
    WHERE c.Estado = 'DES' 
      AND c.UsuarioRegId = @UsuarioId
      
      -- CONDICIÓN OPTIMIZADA: Solo procesar si ya llegó o ya pasó la fecha de su primer pago
      AND c.FechaPrimerPago <= CAST(@Fecha AS DATE)
      
      -- Condición 1: No existe movimiento de caja relacionado a caja diario (no ha pagado hoy)
      AND NOT EXISTS (
          SELECT 1 
          FROM CREDITO.MovimientoCaja mc
          WHERE mc.CreditoId = c.CreditoId
            AND mc.CajaDiarioId = @CajaDiarioId
            AND mc.IndEntrada = 1 
            AND mc.Estado = 1
            AND mc.Operacion = 'CUO'
      );
END
