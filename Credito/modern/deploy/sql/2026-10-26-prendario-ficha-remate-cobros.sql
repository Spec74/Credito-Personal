-- 005-facturador-smart · gerencia + DBA 2026-10-26
-- Ficha (observaciones/foto), remate → stock, renovar/liberar prendario.
-- No inventa % de interés: Renovar cobra SUM(PlanPago.Interes) ya calculado.
-- Abonar = usp_PagarCuotaPagoLibre (ya existe). Liberar = cancelación + DEVUELTO.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF COL_LENGTH(N'CREDITO.Credito', N'FechaRemateEjecutado') IS NULL
    ALTER TABLE CREDITO.Credito ADD FechaRemateEjecutado datetime NULL;

IF COL_LENGTH(N'CREDITO.Prenda', N'ArticuloRemateId') IS NULL
    ALTER TABLE CREDITO.Prenda ADD ArticuloRemateId int NULL;

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
    @TipoPagoId INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF @CreditoId IS NULL OR @CreditoId < 1
       OR @OficinaId IS NULL OR @OficinaId < 1
       OR @CajaDiarioId IS NULL OR @CajaDiarioId < 1
       OR @UsuarioId IS NULL OR @UsuarioId < 1
    BEGIN
        RAISERROR(N'Datos de renovación incompletos.', 16, 1);
        RETURN;
    END;

    IF @Dias IS NULL OR @Dias < 1
        SET @Dias = 30;
    IF @TipoPagoId IS NULL OR @TipoPagoId < 1
        SET @TipoPagoId = 1;

    IF NOT EXISTS (
        SELECT 1
        FROM CREDITO.Credito AS c
        WHERE c.CreditoId = @CreditoId
          AND c.OficinaId = @OficinaId
          AND c.Estado = N'DES'
          AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
    )
    BEGIN
        RAISERROR(N'Crédito prendario desembolsado no encontrado en la sede.', 16, 1);
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
    @UsuarioId INT
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

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Credito
        WHERE CreditoId = @CreditoId
          AND Estado = N'DES'
    )
    BEGIN
        EXEC CREDITO.usp_PagarCuotasCancelacion
            @CajaDiarioId = @CajaDiarioId,
            @CreditoId = @CreditoId,
            @UsuarioId = @UsuarioId,
            @FechaPago = NULL;
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
    DECLARE @Descripcion NVARCHAR(500);
    DECLARE @Serie NVARCHAR(100);
    DECLARE @Tasacion DECIMAL(18, 2);
    DECLARE @FotoPath NVARCHAR(500);
    DECLARE @AlmacenId INT;
    DECLARE @ArticuloId INT;
    DECLARE @CodArticulo VARCHAR(20);

    SELECT
        @CreditoId = c.CreditoId,
        @Descripcion = p.Descripcion,
        @Serie = p.Serie,
        @Tasacion = p.ValorTasacion,
        @FotoPath = p.FotoPath
    FROM CREDITO.Prenda AS p
    INNER JOIN CREDITO.Credito AS c ON c.CreditoId = p.CreditoId
    WHERE p.PrendaId = @PrendaId
      AND c.OficinaId = @OficinaId
      AND c.Estado = N'DES'
      AND (c.EsPrendario = CAST(1 AS bit) OR c.ProductoId = 2)
      AND p.Estado = N'EN CUSTODIA'
      AND c.FechaRemate IS NOT NULL
      AND c.FechaRemate < @Hoy;

    IF @CreditoId IS NULL
    BEGIN
        RAISERROR(N'Solo se remata un bien en custodia de un crédito en cola de remate (vencimiento + 30 d.).', 16, 1);
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM CREDITO.Credito
        WHERE CreditoId = @CreditoId
          AND FechaRemateEjecutado IS NOT NULL
    )
    BEGIN
        RAISERROR(N'Este crédito ya fue rematado.', 16, 1);
        RETURN;
    END;

    SELECT TOP (1) @AlmacenId = a.AlmacenId
    FROM ALMACEN.Almacen AS a
    WHERE a.OficinaId = @OficinaId
      AND a.Estado = CAST(1 AS bit)
    ORDER BY a.AlmacenId;

    IF @AlmacenId IS NULL
    BEGIN
        RAISERROR(N'La sede no tiene almacén activo. Cree el almacén antes de rematar.', 16, 1);
        RETURN;
    END;

    SET @CodArticulo = LEFT('PRM' + CAST(@PrendaId AS varchar(16)), 20);

    INSERT INTO ALMACEN.Articulo (
        ModeloId, TipoArticuloId, CodArticulo, Denominacion, Descripcion,
        Imagen, IndPerecible, IndImportado, Estado)
    VALUES (
        NULL, NULL, @CodArticulo,
        LEFT(@Descripcion, 200),
        LEFT(CONCAT(N'Remate crédito ', @CreditoId, N' prenda ', @PrendaId), 250),
        @FotoPath, 0, 0, 1);

    SET @ArticuloId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO VENTAS.ListaPrecio (ArticuloId, Monto, Descuento, Estado, Puntos, PuntosCanje)
    VALUES (@ArticuloId, @Tasacion, 0, 1, 0, 0);

    INSERT INTO ALMACEN.SerieArticulo (
        NumeroSerie, AlmacenId, ArticuloId, EstadoId, MovimientoDetEntId, MovimientoDetSalId)
    VALUES (
        LEFT(COALESCE(NULLIF(LTRIM(RTRIM(@Serie)), N''), N'PRM' + CAST(@PrendaId AS varchar(16))), 20),
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
    WHERE PrendaId = @PrendaId
      AND Estado = N'EN CUSTODIA';

    UPDATE CREDITO.Credito
    SET FechaRemateEjecutado = @Fecha,
        IndIrrecuperable = CAST(1 AS bit),
        UsuarioModId = @UsuarioId,
        FechaMod = @Fecha
    WHERE CreditoId = @CreditoId
      AND OficinaId = @OficinaId;

    SELECT
        @PrendaId AS PrendaId,
        @CreditoId AS CreditoId,
        @ArticuloId AS ArticuloId,
        @CodArticulo AS CodArticulo,
        @Tasacion AS PrecioVenta,
        @AlmacenId AS AlmacenId;
END
GO

-- US-5: si el menú de venta rápida / almacén ya existe, se concede a operación.
INSERT INTO MAESTRO.RolMenu (RolId, MenuId)
SELECT r.RolId, m.MenuId
FROM MAESTRO.Rol AS r
CROSS JOIN MAESTRO.Menu AS m
WHERE r.Denominacion IN (N'ANALISTA', N'CAJERO', N'ADMINISTRADOR', N'GESTOR')
  AND (
        m.Url LIKE N'%VentaRapida%'
     OR m.Url LIKE N'%OrdenVenta%'
     OR m.Denominacion IN (N'VENTAS')
     OR (m.Modulo LIKE N'%ALMACEN%' AND m.IndPadre = 0)
     OR m.Url LIKE N'%Almacen/Index%'
  )
  AND NOT EXISTS (
        SELECT 1
        FROM MAESTRO.RolMenu AS rm
        WHERE rm.RolId = r.RolId
          AND rm.MenuId = m.MenuId
  );
GO
