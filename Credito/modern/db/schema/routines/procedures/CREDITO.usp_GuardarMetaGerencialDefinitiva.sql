
CREATE   PROCEDURE CREDITO.usp_GuardarMetaGerencialDefinitiva
    @Periodo DATE,
    @UsuarioId INT,
    @MetaCapitalCierre DECIMAL(18,2),
    @MetaClientesActivosCierre INT,
    @MetaVencidosMaximoCierre DECIMAL(18,2),
    @MetaRecuperacionVencidosMes DECIMAL(18,2),
    @UsuarioRegistroId INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @UsuarioRegistroId<>10 THROW 53020, 'Solo BRIGIDA esta autorizada para registrar metas.',1;
    IF @Periodo IS NULL OR @UsuarioId IS NULL THROW 53021, 'Periodo y analista son obligatorios.',1;
    SET @Periodo=DATEFROMPARTS(YEAR(@Periodo),MONTH(@Periodo),1);

    DECLARE @Ahora DATETIME2(0)=CREDITO.ufn_FechaGerencial();
    DECLARE @PeriodoActual DATE=DATEFROMPARTS(YEAR(@Ahora),MONTH(@Ahora),1);
    DECLARE @FechaLimite DATETIME2(0)=DATEADD(SECOND,-1,DATEADD(DAY,5,CAST(@Periodo AS DATETIME2(0))));
    DECLARE @Tipo VARCHAR(20);

    IF @Periodo<>@PeriodoActual OR @Ahora<@Periodo OR @Ahora>@FechaLimite
        THROW 53022, 'Las metas solo pueden modificarse durante los dias 1 al 5 del periodo actual.',1;
    IF EXISTS(SELECT 1 FROM CREDITO.CierreGerencial WHERE Periodo=@Periodo AND Estado='CER')
        THROW 53023, 'No se pueden modificar metas de un periodo cerrado.',1;

    SELECT @Tipo=TipoCartera FROM CREDITO.MetaGerencialAnalista
    WHERE Periodo=@Periodo AND UsuarioId=@UsuarioId AND Activo=1;
    IF @Tipo IS NULL THROW 53024, 'El analista no pertenece a la configuracion activa del periodo.',1;

    IF @MetaVencidosMaximoCierre IS NULL OR @MetaRecuperacionVencidosMes IS NULL
        THROW 53025, 'Las dos metas de vencidos son obligatorias.',1;
    IF @Tipo='PRODUCTIVA' AND (@MetaCapitalCierre IS NULL OR @MetaClientesActivosCierre IS NULL)
        THROW 53026, 'Capital y clientes son obligatorios para una cartera productiva.',1;
    IF ISNULL(@MetaCapitalCierre,0)<0 OR ISNULL(@MetaClientesActivosCierre,0)<0
       OR @MetaVencidosMaximoCierre<0 OR @MetaRecuperacionVencidosMes<0
        THROW 53027, 'Las metas no pueden ser negativas.',1;

    UPDATE CREDITO.MetaGerencialAnalista
       SET MetaCapitalCierre=CASE WHEN @Tipo='ESPECIAL' THEN NULL ELSE @MetaCapitalCierre END,
           MetaClientesActivosCierre=CASE WHEN @Tipo='ESPECIAL' THEN NULL ELSE @MetaClientesActivosCierre END,
           MetaVencidosMaximoCierre=@MetaVencidosMaximoCierre,
           MetaRecuperacionVencidosMes=@MetaRecuperacionVencidosMes,
           UsuarioModificacionId=@UsuarioRegistroId, FechaModificacion=@Ahora
     WHERE Periodo=@Periodo AND UsuarioId=@UsuarioId AND Activo=1;

    SELECT MetaGerencialAnalistaId,Periodo,UsuarioId,MetaCapitalCierre,
           MetaClientesActivosCierre,MetaVencidosMaximoCierre,
           MetaRecuperacionVencidosMes,Activo,UsuarioRegistroId,FechaRegistro,
           UsuarioModificacionId,FechaModificacion
    FROM CREDITO.MetaGerencialAnalista
    WHERE Periodo=@Periodo AND UsuarioId=@UsuarioId;
END;

