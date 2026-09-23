
/* El formulario de BRIGIDA: base heredada y ventana de edicion dias 1 al 5. */
CREATE   PROCEDURE CREDITO.usp_ListarMetasGerencialesDefinitivas
    @Periodo DATE
AS
BEGIN
    SET NOCOUNT ON;
    IF @Periodo IS NULL THROW 53010, 'El periodo es obligatorio.', 1;

    SET @Periodo = DATEFROMPARTS(YEAR(@Periodo),MONTH(@Periodo),1);
    DECLARE @Ahora DATETIME2(0) = CREDITO.ufn_FechaGerencial();
    DECLARE @PeriodoActual DATE = DATEFROMPARTS(YEAR(@Ahora),MONTH(@Ahora),1);
    DECLARE @FechaLimite DATETIME2(0) = DATEADD(SECOND,-1,DATEADD(DAY,5,CAST(@Periodo AS DATETIME2(0))));
    DECLARE @PeriodoCerrado BIT = CASE WHEN EXISTS
        (SELECT 1 FROM CREDITO.CierreGerencial WHERE Periodo=@Periodo AND Estado='CER')
        THEN 1 ELSE 0 END;

    ;WITH Anterior AS
    (
        SELECT D.*
        FROM CREDITO.CierreGerencial C
        INNER JOIN CREDITO.CierreGerencialDetalle D
            ON D.CierreGerencialId=C.CierreGerencialId
        WHERE C.Periodo=DATEADD(MONTH,-1,@Periodo) AND C.Estado='CER'
    ),
    Cartera AS
    (
        SELECT M.*
        FROM CREDITO.MetaGerencialAnalista M
        WHERE M.Periodo=@Periodo AND M.Activo=1
    )
    SELECT M.UsuarioId, CAST(U.NombreUsuario AS VARCHAR(100)) AS NombreUsuario,
           CAST(P.NombreCompleto AS VARCHAR(250)) AS NombreCompleto, @Periodo AS Periodo,
           CAST(ISNULL(A.CapitalCierre,0) AS DECIMAL(18,2)) AS CapitalBase,
           ISNULL(A.TotalClientesActivos,0) AS ClientesActivosBase,
           CAST(A.VencidosCuotasCierre AS DECIMAL(18,2)) AS VencidosBaseComparable,
           CAST(ISNULL(A.MoraGeneral,0) AS DECIMAL(18,2)) AS VencidosBaseLegacy,
           CAST(A.ClientesVencidosCierre AS INT) AS ClientesVencidosBase,
           M.MetaCapitalCierre, M.MetaClientesActivosCierre,
           M.MetaVencidosMaximoCierre, M.MetaRecuperacionVencidosMes,
           CAST(CASE WHEN M.TipoCartera='ESPECIAL'
                          AND M.MetaVencidosMaximoCierre IS NOT NULL
                          AND M.MetaRecuperacionVencidosMes IS NOT NULL THEN 1
                     WHEN M.TipoCartera='PRODUCTIVA'
                          AND M.MetaCapitalCierre IS NOT NULL
                          AND M.MetaClientesActivosCierre IS NOT NULL
                          AND M.MetaVencidosMaximoCierre IS NOT NULL
                          AND M.MetaRecuperacionVencidosMes IS NOT NULL THEN 1
                     ELSE 0 END AS BIT) AS Configurada,
           @PeriodoCerrado AS PeriodoCerrado,
           CAST(CASE WHEN @Periodo=@PeriodoActual AND @Ahora>=@Periodo
                          AND @Ahora<=@FechaLimite AND @PeriodoCerrado=0
                     THEN 1 ELSE 0 END AS BIT) AS PuedeEditar,
           @FechaLimite AS FechaLimiteEdicion,
           M.Asesor,M.Supervisor,M.Mercado,M.Orden,M.TipoCartera
    FROM Cartera M
    INNER JOIN MAESTRO.Usuario U ON U.UsuarioId=M.UsuarioId
    LEFT JOIN MAESTRO.Persona P ON P.PersonaId=U.PersonaId
    LEFT JOIN Anterior A ON A.UsuarioId=M.UsuarioId
    ORDER BY M.Orden;
END;

