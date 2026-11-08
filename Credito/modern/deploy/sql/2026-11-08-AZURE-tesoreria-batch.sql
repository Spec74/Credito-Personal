/*
  AZURE — batch tesorería 2026-11-08
  Incluye:
    - usp_ResumenCuentaBoveda (filtro saldo <> 0 + siempre 1 fila)
    - usp_ReprogramarCredito (NuevoCreditoId + Deuda)
  Ejecutar en la BD CREDITO de Azure con sqlcmd -I (QUOTED_IDENTIFIER ON).
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* ---- CREDITO.usp_ResumenCuentaBoveda ---- */
CREATE OR ALTER PROC [CREDITO].[usp_ResumenCuentaBoveda]
@BovedaId INT
AS

DECLARE @SaldoInicial DECIMAL(15,2) = (SELECT SaldoInicial FROM CREDITO.Boveda WHERE BovedaId=@BovedaId)

IF @SaldoInicial > 0
BEGIN
  IF NOT EXISTS (SELECT BovedaCuentaId FROM CREDITO.BovedaCuenta WHERE BovedaId=@BovedaId)
  BEGIN
     INSERT CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
	 VALUES (@BovedaId, 1, @SaldoInicial)
  END
END

;WITH CUENTAS AS (
	SELECT MC.BovedaId, TipoPagoId, SUM(IIF(MC.IndEntrada=1, MC.Importe, -MC.Importe)) AS Importe
	FROM CREDITO.BovedaMov MC
	WHERE MC.BovedaId = @BovedaId
	  AND MC.Estado = 1
	GROUP BY MC.BovedaId, MC.TipoPagoId
),
SALDOS AS (
	SELECT
		vt.ItemId AS TipoPagoId,
		vt.Denominacion,
		CAST(ISNULL(BC.SaldoInicial, 0) + ISNULL(c.Importe, 0) AS DECIMAL(16, 2)) AS Saldo
	FROM MAESTRO.ValorTabla vt
	LEFT JOIN CUENTAS C ON vt.ItemId = c.TipoPagoId
	LEFT JOIN CREDITO.BovedaCuenta BC
		ON BC.BovedaId = @BovedaId AND BC.TipoPagoId = vt.ItemId
	WHERE vt.TablaId = 13
	  AND vt.ItemId > 0
	  AND vt.ItemId NOT BETWEEN 6 AND 8
	  AND ISNULL(vt.DesCorta, N'') <> N'OBSOLETO'
)
SELECT COALESCE((
	SELECT
		N'RESUMEN BOVEDA: ' + STRING_AGG(Denominacion + ' = ' + CAST(Saldo AS VARCHAR(20)), '  ')
			WITHIN GROUP (ORDER BY TipoPagoId)
	FROM SALDOS
	WHERE Saldo <> 0
), N'RESUMEN BOVEDA: ') AS Total
GO

PRINT N'OK: usp_ResumenCuentaBoveda.';
GO

/* ---- CREDITO.usp_ReprogramarCredito ---- */
CREATE OR ALTER PROC [CREDITO].[usp_ReprogramarCredito]
@CreditoId INT,
@UsuarioId INT
AS

DECLARE @Deuda DECIMAL(16,2)=0, @FechaAct DATE = dbo.ufnFecha()
DECLARE @NuevoCreditoId INT = NULL
DECLARE @tCuotasPendientes TABLE(Id INT IDENTITY(1,1),PlanPagoId INT,Glosa VARCHAR(MAX),FechaVencimiento DATE,Amortizacion DECIMAL(16,2),
								Interes DECIMAL(16,2), GastosAdm DECIMAL(16,2), Cuota DECIMAL(16,2), DiasAtrazo INT,
								ImporteMora DECIMAL(16,2),Descuento DECIMAL(16,2), Cargo DECIMAL(16,2),
								PagoLibre DECIMAL(16,2),PagoCuota DECIMAL(16,2))
INSERT INTO @tCuotasPendientes
EXEC CREDITO.usp_CuotasPendientes @CreditoId,@FechaAct

SET @Deuda = ISNULL((SELECT SUM(PagoCuota) FROM @tCuotasPendientes), 0)

IF @Deuda <= 0
BEGIN
	RAISERROR(N'No hay deuda pendiente para reprogramar.', 16, 1)
	RETURN
END

INSERT INTO	CREDITO.Credito
( OficinaId,PersonaId,ProductoId, Descripcion, MontoProducto, MontoInicial ,MontoGastosAdm ,MontoCredito ,
  FormaPago,NumeroCuotas,Interes,FechaPrimerPago,Observacion,Estado,FechaReg,UsuarioRegId,OrdenVentaId )
SELECT	OficinaId,PersonaId,ProductoId,Descripcion, @Deuda , 0 'Inicial' , 0 'GastoAdm', @Deuda,
		FormaPago,NumeroCuotas,Interes,@FechaAct 'PrimerPago','REPROGRAMADO CREDITO ' + CAST(@CreditoId AS VARCHAR(10)) 'Obs',
		'CRE',@FechaAct,@UsuarioId,OrdenVentaId
FROM CREDITO.Credito WITH(NOLOCK) WHERE CreditoId=@CreditoId

SET @NuevoCreditoId = CAST(SCOPE_IDENTITY() AS INT)

UPDATE CREDITO.Credito SET Estado = 'REP'
WHERE CreditoId = @CreditoId

SELECT @NuevoCreditoId AS NuevoCreditoId, @Deuda AS Deuda
GO

PRINT N'OK: usp_ReprogramarCredito.';
GO

PRINT N'OK 2026-11-08 AZURE tesoreria batch aplicado.';
GO
