
/* Llamado seguro desde el cierre de boveda. */
CREATE   PROCEDURE CREDITO.usp_IntentarGenerarCierreGerencialMensual
    @OficinaId INT,
    @UsuarioCierreId INT
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Ahora DATETIME2(0)=CREDITO.ufn_FechaGerencial();
    DECLARE @Hoy DATE=CAST(@Ahora AS DATE);
    DECLARE @Periodo DATE=DATEFROMPARTS(YEAR(@Ahora),MONTH(@Ahora),1);
    DECLARE @PeriodoAnterior DATE=DATEADD(MONTH,-1,@Periodo);

    IF @Hoy=EOMONTH(@Hoy)
        EXEC CREDITO.usp_GenerarCierreGerencialMensual
            @Periodo=@Periodo,@OficinaId=@OficinaId,@UsuarioCierreId=@UsuarioCierreId;
    ELSE IF DAY(@Hoy)<=2 AND NOT EXISTS
        (SELECT 1 FROM CREDITO.CierreGerencial
         WHERE Periodo=@PeriodoAnterior AND Estado='CER')
        EXEC CREDITO.usp_GenerarCierreGerencialMensual
            @Periodo=@PeriodoAnterior,@OficinaId=@OficinaId,
            @UsuarioCierreId=@UsuarioCierreId;
END;

