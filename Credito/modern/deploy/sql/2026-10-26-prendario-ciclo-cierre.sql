-- 005-facturador-smart ¬∑ cierre 100 % (re-ejecutable)
-- Actualiza USP de cobro/remate, cola FechaRemate, men√∫ venta r√°pida y almac√©n por sede.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'CREDITO.Credito', N'FechaRemateEjecutado') IS NULL
    ALTER TABLE CREDITO.Credito ADD FechaRemateEjecutado datetime NULL;

IF COL_LENGTH(N'CREDITO.Prenda', N'ArticuloRemateId') IS NULL
    ALTER TABLE CREDITO.Prenda ADD ArticuloRemateId int NULL;
GO

UPDATE CREDITO.Credito
SET FechaRemate = DATEADD(DAY, 30, FechaVencimiento)
WHERE FechaRemate IS NULL
  AND FechaVencimiento IS NOT NULL
  AND Estado = N'DES'
  AND (EsPrendario = CAST(1 AS bit) OR ProductoId = 2);
GO

INSERT INTO ALMACEN.Almacen (OficinaId, Denominacion, Descripcion, IndEstadoApertura, FechaApertura, Estado)
SELECT
    o.OficinaId,
    LEFT(CONCAT(N'ALMACEN ', ISNULL(o.Denominacion, CAST(o.OficinaId AS varchar(8)))), 100),
    N'Almac√©n para remate prendario',
    1,
    dbo.ufnFecha(),
    1
FROM MAESTRO.Oficina AS o
WHERE o.Estado = CAST(1 AS bit)
  AND NOT EXISTS (
        SELECT 1
        FROM ALMACEN.Almacen AS a
        WHERE a.OficinaId = o.OficinaId
          AND a.Estado = CAST(1 AS bit)
  );
GO

DECLARE @OrdenVentas DECIMAL(3, 1);
SELECT TOP (1) @OrdenVentas = Orden
FROM MAESTRO.Menu
WHERE Denominacion = N'VENTAS' AND IndPadre = 1
ORDER BY MenuId;

IF @OrdenVentas IS NULL
    SET @OrdenVentas = 4.0;

IF NOT EXISTS (
    SELECT 1 FROM MAESTRO.Menu
    WHERE Url LIKE N'%VentaRapida%' OR Denominacion = N'VENTA RAPIDA'
)
BEGIN
    INSERT INTO MAESTRO.Menu (Denominacion, Modulo, Url, Icono, IndPadre, Orden, Referencia)
    VALUES (
        N'VENTA RAPIDA',
        N'VENTAS',
        N'VentaRapida',
        N'icon-list',
        0,
        @OrdenVentas + 0.1,
        @OrdenVentas
    );
END
GO


CREATE OR ALTER PROC [CREDITO].[usp_PrendarioActualizarFicha]
    @PrendaId BIGINT,
    @OficinaId INT,
    @UsuarioId INT,
    @Observaciones NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @PrendaId IS NULL OR @PrendaId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos de ficha incompletos.', 16, 1);
        RETURN;
    END;

    UPDATE p
    SET p.Observaciones = NULLIF(LTRIM(RTRIM(@Observaciones)), N''),
        p.UsuarioModId = @UsuarioId,
        p.FechaMod = dbo.ufnFecha()
    FROM CREDITO.Prenda AS p
    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = p.CreditoId
    WHERE p.PrendaId = @PrendaId
      AND c.OficinaId = @OficinaId
      AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2);

    IF @@ROWCOUNT = 0
    BEGIN
        RAISERROR(N'Bien no encontrado en la sede.', 16, 1);
        RETURN;
    END;

    SELECT p.PrendaId, p.CreditoId, p.Observaciones, p.FotoPath, p.Estado
    FROM CREDITO.Prenda AS p
    WHERE p.PrendaId = @PrendaId;
END


GO

CREATE OR ALTER PROC [CREDITO].[usp_PrendarioRenovar]
    @CreditoId INT,
    @OficinaId INT,
    @CajaDiarioId INT,
    @UsuarioId INT,
    @Dias INT = 30,
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
        RAISERROR(N'Datos de renovaciÛn incompletos.', 16, 1);
        RETURN;
    END;

    IF @Dias IS NULL OR @Dias < 1
        SET @Dias = 30;
    IF @TipoPagoId IS NULL OR @TipoPagoId < 1
        SET @TipoPagoId = 1;
    IF @FechaPagoTransferencia IS NULL
        SET @FechaPagoTransferencia = '';

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.Credito AS c
        WHERE c.CreditoId = @CreditoId
          AND c.OficinaId = @OficinaId
          AND c.Estado = N'DES'
          AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
    )
    BEGIN
        RAISERROR(N'CrÈdito prendario desembolsado no encontrado en la sede.', 16, 1);
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
        RAISERROR(N'La caja no est· abierta en esta sede.', 16, 1);
        RETURN;
    END;

    DECLARE @Interes DECIMAL(16, 2) = 0;
    DECLARE @PersonaId INT;
    DECLARE @Fecha DATETIME = dbo.ufnFecha();
    DECLARE @MovimientoCajaId INT;

    SELECT @Interes = ISNULL(SUM(pp.Interes), 0)
    FROM CREDITO.PlanPago AS pp
    WHERE pp.CreditoId = @CreditoId
      AND pp.Estado = N'PEN';

    SELECT @PersonaId = c.PersonaId
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;

    IF @Interes > 0
    BEGIN
        INSERT INTO CREDITO.MovimientoCaja (
            CajaDiarioId, PersonaId, Operacion, ImportePago,
            Descripcion, IndEntrada, Estado, OrdenVentaId, CreditoId,
            UsuarioRegId, FechaReg, TipoPagoId)
        VALUES (
            @CajaDiarioId, @PersonaId, N'CUO', @Interes,
            N'RENOVACION PRENDARIA INTERES CREDITO ' + CAST(@CreditoId AS varchar(20)),
            1, 1, NULL, @CreditoId, @UsuarioId, @Fecha, @TipoPagoId);

        SET @MovimientoCajaId = CONVERT(INT, SCOPE_IDENTITY());

        IF @TipoPagoId > 1
            INSERT INTO CREDITO.MovimientoCajaExtension (
                MovimientoCajaId, FechaTransferencia, IndTransferenciaVerificada)
            VALUES (@MovimientoCajaId, @FechaPagoTransferencia, 0);

        EXEC CREDITO.usp_RecalcularCajaDiario @CajaDiarioId = @CajaDiarioId;
    END;

    DECLARE @NuevaRemate DATE;
    SELECT @NuevaRemate = DATEADD(
        DAY,
        @Dias,
        COALESCE(c.FechaRemate, DATEADD(DAY, 30, c.FechaVencimiento)))
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;

    EXEC CREDITO.usp_ProrrogarCredito @CreditoId = @CreditoId, @Dias = @Dias;

    UPDATE CREDITO.Credito
    SET FechaRemate = @NuevaRemate,
        UsuarioModId = @UsuarioId,
        FechaMod = @Fecha
    WHERE CreditoId = @CreditoId
      AND OficinaId = @OficinaId;

    SELECT
        @CreditoId AS CreditoId,
        @Interes AS InteresCobrado,
        @Dias AS Dias,
        @MovimientoCajaId AS MovimientoCajaId,
        c.FechaVencimiento,
        c.FechaRemate
    FROM CREDITO.Credito AS c
    WHERE c.CreditoId = @CreditoId;
END


GO

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
        RAISERROR(N'Datos de liberaciÛn incompletos.', 16, 1);
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
        RAISERROR(N'CrÈdito prendario no encontrado en la sede.', 16, 1);
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
        RAISERROR(N'La caja no est· abierta en esta sede.', 16, 1);
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
        RAISERROR(N'Quedan cuotas pendientes. No se liberÛ la prenda.', 16, 1);
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


GO

CREATE OR ALTER PROC [CREDITO].[usp_RematarPrenda]
    @PrendaId BIGINT,
    @OficinaId INT,
    @UsuarioId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF @PrendaId IS NULL OR @PrendaId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos de remate incompletos.', 16, 1);
        RETURN;
    END;

    DECLARE @Hoy DATE = CONVERT(date, dbo.ufnFecha());
    DECLARE @Fecha DATETIME = dbo.ufnFecha();
    DECLARE @CreditoId INT;
    DECLARE @AlmacenId INT;
    DECLARE @ArticuloId INT;
    DECLARE @CodArticulo VARCHAR(20);
    DECLARE @TasacionSolicitada DECIMAL(18, 2);

    SELECT
        @CreditoId = c.CreditoId,
        @TasacionSolicitada = p.ValorTasacion
    FROM CREDITO.Prenda AS p
    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = p.CreditoId
    WHERE p.PrendaId = @PrendaId
      AND c.OficinaId = @OficinaId
      AND c.Estado = N'DES'
      AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
      AND p.Estado = N'EN CUSTODIA'
      AND (
            (c.FechaRemate IS NOT NULL AND c.FechaRemate < @Hoy)
         OR (c.FechaRemate IS NULL AND DATEADD(DAY, 30, c.FechaVencimiento) < @Hoy)
      );

    IF @CreditoId IS NULL
    BEGIN
        RAISERROR(N'Solo se remata un bien en custodia de un crÈdito en cola de remate (vencimiento + 30 d.).', 16, 1);
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Credito
        WHERE CreditoId = @CreditoId
          AND FechaRemateEjecutado IS NOT NULL
    )
    BEGIN
        RAISERROR(N'Este crÈdito ya fue rematado.', 16, 1);
        RETURN;
    END;

    SELECT TOP (1) @AlmacenId = a.AlmacenId
    FROM ALMACEN.Almacen AS a
    WHERE a.OficinaId = @OficinaId
      AND a.Estado = CAST(1 AS bit)
    ORDER BY a.AlmacenId;

    IF @AlmacenId IS NULL
    BEGIN
        RAISERROR(N'La sede no tiene almacÈn activo. Cree el almacÈn antes de rematar.', 16, 1);
        RETURN;
    END;

    DECLARE @CurPrendaId BIGINT;
    DECLARE @Descripcion NVARCHAR(500);
    DECLARE @Serie NVARCHAR(100);
    DECLARE @Tasacion DECIMAL(18, 2);
    DECLARE @FotoPath NVARCHAR(500);
    DECLARE @ArticuloPedido INT;

    WHILE EXISTS (
        SELECT 1
        FROM CREDITO.Prenda
        WHERE CreditoId = @CreditoId
          AND Estado = N'EN CUSTODIA'
    )
    BEGIN
        SELECT TOP (1)
            @CurPrendaId = PrendaId,
            @Descripcion = Descripcion,
            @Serie = Serie,
            @Tasacion = ValorTasacion,
            @FotoPath = FotoPath
        FROM CREDITO.Prenda
        WHERE CreditoId = @CreditoId
          AND Estado = N'EN CUSTODIA'
        ORDER BY CASE WHEN PrendaId = @PrendaId THEN 0 ELSE 1 END, PrendaId;

        SET @CodArticulo = LEFT('PRM' + CAST(@CurPrendaId AS varchar(16)), 20);

        INSERT INTO ALMACEN.Articulo (
            ModeloId, TipoArticuloId, CodArticulo, Denominacion, Descripcion,
            Imagen, IndPerecible, IndImportado, Estado)
        VALUES (
            NULL, NULL, @CodArticulo,
            LEFT(@Descripcion, 200),
            LEFT(CONCAT(N'Remate crÈdito ', @CreditoId, N' prenda ', @CurPrendaId), 250),
            @FotoPath, 0, 0, 1);

        SET @ArticuloId = CONVERT(INT, SCOPE_IDENTITY());

        INSERT INTO VENTAS.ListaPrecio (ArticuloId, Monto, Descuento, Estado, Puntos, PuntosCanje)
        VALUES (@ArticuloId, @Tasacion, 0, 1, 0, 0);

        INSERT INTO ALMACEN.SerieArticulo (
            NumeroSerie, AlmacenId, ArticuloId, EstadoId, MovimientoDetEntId, MovimientoDetSalId)
        VALUES (
            LEFT(COALESCE(NULLIF(LTRIM(RTRIM(@Serie)), N''), N'PRM' + CAST(@CurPrendaId AS varchar(16))), 20),
            @AlmacenId,
            @ArticuloId,
            2,
            NULL,
            NULL);

        UPDATE CREDITO.Prenda
        SET Estado = N'REMATADO',
            ArticuloRemateId = @ArticuloId,
            UsuarioModId = @UsuarioId,
            FechaMod = @Fecha
        WHERE PrendaId = @CurPrendaId
          AND Estado = N'EN CUSTODIA';

        IF @CurPrendaId = @PrendaId
            SET @ArticuloPedido = @ArticuloId;
    END;

    UPDATE CREDITO.Credito
    SET FechaRemateEjecutado = @Fecha,
        FechaRemate = COALESCE(FechaRemate, DATEADD(DAY, 30, FechaVencimiento)),
        IndIrrecuperable = CAST(1 AS bit),
        UsuarioModId = @UsuarioId,
        FechaMod = @Fecha
    WHERE CreditoId = @CreditoId
      AND OficinaId = @OficinaId;

    SELECT
        @PrendaId AS PrendaId,
        @CreditoId AS CreditoId,
        @ArticuloPedido AS ArticuloId,
        LEFT('PRM' + CAST(@PrendaId AS varchar(16)), 20) AS CodArticulo,
        @TasacionSolicitada AS PrecioVenta,
        @AlmacenId AS AlmacenId;
END


GO


INSERT INTO MAESTRO.RolMenu (RolId, MenuId)
SELECT r.RolId, m.MenuId
FROM MAESTRO.Rol AS r
CROSS JOIN MAESTRO.Menu AS m
WHERE r.Denominacion IN (N'ANALISTA', N'CAJERO', N'ADMINISTRADOR', N'GESTOR')
  AND (
        m.Url LIKE N'%VentaRapida%'
     OR m.Url LIKE N'%OrdenVenta%'
     OR m.Denominacion IN (N'VENTAS', N'VENTA RAPIDA')
     OR (m.Modulo LIKE N'%ALMACEN%' AND m.IndPadre = 0)
     OR m.Url LIKE N'%Almacen/Index%'
     OR m.Url LIKE N'%Almacen/Entrada%'
  )
  AND NOT EXISTS (
        SELECT 1 FROM MAESTRO.RolMenu AS rm
        WHERE rm.RolId = r.RolId AND rm.MenuId = m.MenuId
  );
GO
