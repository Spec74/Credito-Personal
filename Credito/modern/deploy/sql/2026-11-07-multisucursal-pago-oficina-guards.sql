/*
  Multisucursal — guards de oficina y medios (evitar cobro cruzado / embudo a efectivo).

  Aplica:
  - usp_PagarCuotas / usp_PagarCuotaPagoLibre / usp_PagarCuotasCancelacion:
      Credito.OficinaId = Caja.OficinaId
  - usp_CompletarImpagos(+Validacion): filtro OficinaId + TipoPagoId=1 en impagos
  - usp_TransferirBoveda: TipoPagoId obligatorio (no default a 1)
  - usp_RptCreditoRentabilidad: SumPago sin doble conteo PagoCuota+PagoLibre

  Fuente canónica: db/schema/routines/procedures/CREDITO.usp_*.sql
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* ========== CompletarImpagos ========== */
CREATE OR ALTER PROC [CREDITO].[usp_CompletarImpagos]
    @CajaDiarioId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UsuarioId INT = (
        SELECT UsuarioAsignadoId
        FROM CREDITO.CajaDiario
        WHERE CajaDiarioId = @CajaDiarioId
    );

    DECLARE @OficinaId INT = (
        SELECT cj.OficinaId
        FROM CREDITO.CajaDiario cd
        INNER JOIN CREDITO.Caja cj ON cj.CajaId = cd.CajaId
        WHERE cd.CajaDiarioId = @CajaDiarioId
    );

    DECLARE @Fecha DATETIME = dbo.ufnFecha();

    IF EXISTS(SELECT 1 FROM CREDITO.CajaDiario WHERE CajaDiarioId = @CajaDiarioId AND IndCierre = 1)
    BEGIN
        SET @Fecha = (SELECT FechaFinOperacion FROM CREDITO.CajaDiario WHERE CajaDiarioId = @CajaDiarioId AND IndCierre = 1);
    END

    IF @UsuarioId IS NULL OR @OficinaId IS NULL
    BEGIN
        RAISERROR('CajaDiarioId no válido o sin usuario/oficina asignados.', 16, 1);
        RETURN;
    END

    INSERT INTO CREDITO.MovimientoCaja (
        CajaDiarioId, PersonaId, Operacion, ImportePago,
        Descripcion, IndEntrada, Estado, OrdenVentaId,
        CreditoId, UsuarioRegId, FechaReg, TipoPagoId
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
        @Fecha,
        1
    FROM CREDITO.Credito c
    WHERE c.Estado = 'DES'
      AND c.UsuarioRegId = @UsuarioId
      AND c.OficinaId = @OficinaId
      AND c.FechaPrimerPago <= CAST(@Fecha AS DATE)
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
GO

/* ========== CompletarImpagosValidacion ========== */
CREATE OR ALTER PROC [CREDITO].[usp_CompletarImpagosValidacion]
@CajaDiarioId INT
AS
DECLARE @UsuarioId INT=(SELECT UsuarioAsignadoId FROM CREDITO.CajaDiario WHERE CajaDiarioId=@CajaDiarioId)
DECLARE @OficinaId INT=(
    SELECT cj.OficinaId
    FROM CREDITO.CajaDiario cd
    INNER JOIN CREDITO.Caja cj ON cj.CajaId = cd.CajaId
    WHERE cd.CajaDiarioId = @CajaDiarioId
)
DECLARE @Impagos INT=0
DECLARE @Fecha DATETIME = dbo.ufnFecha()

SELECT @Impagos = COUNT(1)
FROM CREDITO.Credito c
WHERE c.Estado = 'DES'
  AND c.UsuarioRegId = @UsuarioId
  AND c.OficinaId = @OficinaId
  AND c.FechaPrimerPago <= CAST(@Fecha AS DATE)
  AND NOT EXISTS (
      SELECT 1
      FROM CREDITO.MovimientoCaja mc
      WHERE mc.CreditoId = c.CreditoId
        AND mc.CajaDiarioId = @CajaDiarioId
        AND mc.IndEntrada = 1
        AND mc.Estado = 1
        AND mc.Operacion = 'CUO'
  )
  AND EXISTS (
      SELECT 1
      FROM CREDITO.PlanPago pp
      WHERE pp.CreditoId = c.CreditoId
        AND pp.FechaVencimiento = CAST(@Fecha AS DATE)
  );

SELECT @Impagos 'Impagos'
GO

PRINT N'OK: CompletarImpagos(+Validacion) con OficinaId.';
PRINT N'Aplicar también desde schema: usp_PagarCuotas, usp_PagarCuotaPagoLibre, usp_PagarCuotasCancelacion, usp_TransferirBoveda, usp_RptCreditoRentabilidad.';
GO

/* ========== PagarCuotaPagoLibre (guard oficina) ========== */
CREATE OR ALTER PROC [CREDITO].[usp_PagarCuotaPagoLibre]
@CajaDiarioId INT ,
@CreditoId INT,
@ImporteRecibido DECIMAL(16,2) = 0,
@UsuarioId INT,
@TipoPagoId INT=1,
@FechaPagoTransferencia VARCHAR(16)=''
AS
	DECLARE @TotalPago DECIMAL(16,2)=0, @IndImporteLibre BIT = 0
	DECLARE @FechaActual DATETIME=dbo.ufnFecha()
	DECLARE @PersonaId INT, @MovimientoCajaId INT
	DECLARE @OficinaCredito INT, @OficinaCaja INT

	SELECT @OficinaCredito = OficinaId, @PersonaId = PersonaId
	FROM CREDITO.Credito WHERE CreditoId = @CreditoId
	SELECT @OficinaCaja = cj.OficinaId
	FROM CREDITO.CajaDiario cd
	INNER JOIN CREDITO.Caja cj ON cj.CajaId = cd.CajaId
	WHERE cd.CajaDiarioId = @CajaDiarioId

	IF @OficinaCredito IS NULL BEGIN RAISERROR(N'Crédito no existe.', 16, 1); RETURN; END
	IF @OficinaCaja IS NULL BEGIN RAISERROR(N'Caja diario no existe.', 16, 1); RETURN; END
	IF @OficinaCredito <> @OficinaCaja BEGIN RAISERROR(N'El crédito no pertenece a la oficina de la caja (multisucursal).', 16, 1); RETURN; END

	DECLARE @tCuotasPendientes TABLE(Id INT IDENTITY(1,1),PlanPagoId INT,Glosa VARCHAR(MAX),FechaVencimiento DATE,Amortizacion DECIMAL(16,2),
									Interes DECIMAL(16,2), GastosAdm DECIMAL(16,2), Cuota DECIMAL(16,2), DiasAtrazo INT,
									ImporteMora DECIMAL(16,2),Descuento DECIMAL(16,2), Cargo DECIMAL(16,2),
									PagoLibre DECIMAL(16,2),PagoCuota DECIMAL(16,2))
	INSERT INTO @tCuotasPendientes
	EXEC CREDITO.usp_CuotasPendientes @CreditoId,@FechaActual

	DECLARE @MedioPago VARCHAR(80) = (
		SELECT TOP (1) vt.Denominacion FROM MAESTRO.ValorTabla AS vt
		WHERE vt.TablaId = 13 AND vt.ItemId = @TipoPagoId
	)

	INSERT INTO CREDITO.MovimientoCaja
			( CajaDiarioId,PersonaId ,Operacion ,ImportePago ,
			  Descripcion ,IndEntrada ,Estado,OrdenVentaId,CreditoId ,UsuarioRegId ,FechaReg,TipoPagoId)
	VALUES  ( @CajaDiarioId,@PersonaId, 'CUO' , @ImporteRecibido ,
			  'CREDITO ' + CAST(@CreditoId AS VARCHAR(20)) + ' PAGO LIBRE ' + CAST(@ImporteRecibido AS VARCHAR(20))
			  + ISNULL(' (' + @MedioPago + ')', '') ,
			  1, 1,NULL,@CreditoId, @UsuarioId , @FechaActual,@TipoPagoId)
	SET @MovimientoCajaId = @@IDENTITY

	IF @TipoPagoId>1
		INSERT INTO	CREDITO.MovimientoCajaExtension
		(	MovimientoCajaId, FechaTransferencia, IndTransferenciaVerificada )
		VALUES	(   @MovimientoCajaId, @FechaPagoTransferencia, 0 )

	UPDATE PP
	SET PP.DiasAtrazo= CP.DiasAtrazo ,
		PP.ImporteMora=CP.ImporteMora,
		PP.PagoCuota = CP.PagoCuota + cp.PagoLibre,
		PP.MovimientoCajaId=@MovimientoCajaId,
		PP.FechaPagoCuota=@FechaActual,
		PP.UsuarioModId=@UsuarioId,
		PP.FechaMod=@FechaActual
	FROM @tCuotasPendientes CP
	INNER JOIN CREDITO.PlanPago PP ON CP.PlanPagoId = PP.PlanPagoId
	WHERE PP.CreditoId=@CreditoId

	EXEC CREDITO.usp_RegenerarPlanPago @CreditoId = @CreditoId
	EXEC CREDITO.usp_RecalcularCajaDiario @CajaDiarioId = @CajaDiarioId
	SELECT @MovimientoCajaId
GO

PRINT N'OK: usp_PagarCuotaPagoLibre con guard de oficina.';
GO
