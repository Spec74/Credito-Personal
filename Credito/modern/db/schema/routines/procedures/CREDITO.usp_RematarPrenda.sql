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

    DECLARE @TipoId INT;
    DECLARE @ModeloId INT;
    DECLARE @MarcaId INT;

    SELECT TOP (1) @TipoId = TipoArticuloId
    FROM ALMACEN.TipoArticulo
    WHERE Estado = CAST(1 AS bit)
    ORDER BY CASE WHEN UPPER(LTRIM(RTRIM(Denominacion))) = N'GENERAL' THEN 0 ELSE 1 END, TipoArticuloId;

    IF @TipoId IS NULL
    BEGIN
        INSERT INTO ALMACEN.TipoArticulo (Denominacion, Descripcion, Estado)
        VALUES (N'GENERAL', N'Remate / catálogo automático', 1);
        SET @TipoId = CONVERT(INT, SCOPE_IDENTITY());
    END;

    SELECT TOP (1) @MarcaId = MarcaId
    FROM ALMACEN.Marca
    WHERE Estado = CAST(1 AS bit)
    ORDER BY CASE WHEN UPPER(LTRIM(RTRIM(Denominacion))) = N'GENERAL' THEN 0 ELSE 1 END, MarcaId;

    IF @MarcaId IS NULL
    BEGIN
        INSERT INTO ALMACEN.Marca (Denominacion, Estado)
        VALUES (N'GENERAL', 1);
        SET @MarcaId = CONVERT(INT, SCOPE_IDENTITY());
    END;

    SELECT TOP (1) @ModeloId = ModeloId
    FROM ALMACEN.Modelo
    WHERE Estado = CAST(1 AS bit)
    ORDER BY CASE WHEN UPPER(LTRIM(RTRIM(Denominacion))) = N'GENERAL' THEN 0 ELSE 1 END, ModeloId;

    IF @ModeloId IS NULL
    BEGIN
        INSERT INTO ALMACEN.Modelo (Denominacion, MarcaId, Estado)
        VALUES (N'GENERAL', @MarcaId, 1);
        SET @ModeloId = CONVERT(INT, SCOPE_IDENTITY());
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
            @ModeloId, @TipoId, @CodArticulo,
            LEFT(@Descripcion, 200),
            LEFT(CONCAT(N'Remate crédito ', @CreditoId, N' prenda ', @CurPrendaId), 250),
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
