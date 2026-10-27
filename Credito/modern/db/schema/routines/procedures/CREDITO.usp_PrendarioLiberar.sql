CREATE OR ALTER PROC [CREDITO].[usp_PrendarioLiberar]
    @CreditoId INT,
    @OficinaId INT,
    @CajaDiarioId INT,
    @UsuarioId INT,
    @TipoPagoId INT = 1,
    @FechaPagoTransferencia VARCHAR(16) = ''
AS
BEGIN
    SET NOCOUNT ON;

    IF @CreditoId IS NULL OR @CreditoId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
       OR @CajaDiarioId IS NULL OR @CajaDiarioId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos de liberación incompletos.', 16, 1);
        RETURN;
    END;

    IF @TipoPagoId IS NULL OR @TipoPagoId < 1
        SET @TipoPagoId = 1;
    IF @FechaPagoTransferencia IS NULL
        SET @FechaPagoTransferencia = '';

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.Credito AS c
        WHERE c.CreditoId = @CreditoId
          AND c.OficinaId = @OficinaId
          AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
    )
    BEGIN
        RAISERROR(N'Crédito prendario no encontrado en la sede.', 16, 1);
        RETURN;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.CajaDiario AS cd
        INNER JOIN CREDITO.Caja AS cj ON cj.CajaId = cd.CajaId
        WHERE cd.CajaDiarioId = @CajaDiarioId
          AND cj.OficinaId = @OficinaId
          AND cd.IndCierre = CAST(0 AS bit)
          AND cd.FechaFinOperacion IS NULL
    )
    BEGIN
        RAISERROR(N'La caja no está abierta en esta sede.', 16, 1);
        RETURN;
    END;

    DECLARE @Fecha DATETIME = dbo.ufnFecha();
    DECLARE @ListaPlanPagoId VARCHAR(MAX) = '';
    DECLARE @SumaPagoCuota DECIMAL(16, 2) = 0;
    DECLARE @NroCuotas INT = 0;
    DECLARE @Index INT = 1;
    DECLARE @PagoCuota DECIMAL(16, 2) = 0;
    DECLARE @PlanPagoId INT;

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Credito
        WHERE CreditoId = @CreditoId
          AND Estado = N'DES'
    )
    BEGIN
        DECLARE @tCuotasPendientes TABLE (
            Id INT IDENTITY(1, 1),
            PlanPagoId INT,
            Glosa VARCHAR(MAX),
            FechaVencimiento DATE,
            Amortizacion DECIMAL(16, 2),
            Interes DECIMAL(16, 2),
            GastosAdm DECIMAL(16, 2),
            Cuota DECIMAL(16, 2),
            DiasAtrazo INT,
            ImporteMora DECIMAL(16, 2),
            Descuento DECIMAL(16, 2),
            Cargo DECIMAL(16, 2),
            PagoLibre DECIMAL(16, 2),
            PagoCuota DECIMAL(16, 2)
        );

        INSERT INTO @tCuotasPendientes
        EXEC CREDITO.usp_CuotasPendientes @CreditoId, @Fecha, 1;

        SET @NroCuotas = (SELECT COUNT(1) FROM @tCuotasPendientes);
        WHILE @Index <= @NroCuotas
        BEGIN
            SELECT @PagoCuota = PagoCuota, @PlanPagoId = PlanPagoId
            FROM @tCuotasPendientes
            WHERE Id = @Index;

            SET @SumaPagoCuota = @SumaPagoCuota + @PagoCuota;
            SET @ListaPlanPagoId = @ListaPlanPagoId + CAST(@PlanPagoId AS varchar(10)) + ',';
            SET @Index = @Index + 1;
        END;

        IF LEN(@ListaPlanPagoId) > 0
        BEGIN
            SET @ListaPlanPagoId = SUBSTRING(@ListaPlanPagoId, 1, LEN(@ListaPlanPagoId) - 1);
            EXEC CREDITO.usp_PagarCuotas
                @CajaDiarioId = @CajaDiarioId,
                @CreditoId = @CreditoId,
                @ListaPlanPagoId = @ListaPlanPagoId,
                @ImporteRecibido = @SumaPagoCuota,
                @UsuarioId = @UsuarioId,
                @FechaPago = NULL,
                @TipoPagoId = @TipoPagoId,
                @FechaPagoTransferencia = @FechaPagoTransferencia;
        END;
    END;

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Credito AS c
        INNER JOIN CREDITO.PlanPago AS pp ON pp.CreditoId = c.CreditoId
        WHERE c.CreditoId = @CreditoId
          AND c.Estado = N'DES'
          AND pp.Estado = N'PEN'
    )
    BEGIN
        RAISERROR(N'Quedan cuotas pendientes. No se liberó la prenda.', 16, 1);
        RETURN;
    END;

    UPDATE p
    SET p.Estado = N'DEVUELTO',
        p.UsuarioModId = @UsuarioId,
        p.FechaMod = @Fecha
    FROM CREDITO.Prenda AS p
    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = p.CreditoId
    WHERE p.CreditoId = @CreditoId
      AND c.OficinaId = @OficinaId
      AND p.Estado = N'EN CUSTODIA';

    SELECT
        @CreditoId AS CreditoId,
        c.Estado,
        (SELECT COUNT(*) FROM CREDITO.Prenda WHERE CreditoId = @CreditoId AND Estado = N'DEVUELTO') AS BienesDevueltos
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;
END
