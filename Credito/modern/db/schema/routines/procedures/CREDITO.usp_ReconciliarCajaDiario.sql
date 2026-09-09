-- exec CREDITO.usp_ReconciliarCajaDiario 14350
CREATE	PROC [CREDITO].[usp_ReconciliarCajaDiario]
(
	@CajaDiarioId INT
)
AS

DECLARE @Glosa VARCHAR(MAX), @PersonaIdAgente INT, @UsuarioIdAgente INT
DECLARE @tMovReconciliar TABLE(MovimientoCajaId INT,CajaDiarioId INT,CreditoId INT,ImportePago DECIMAL(16,2),PersonaId INT, AgenteDestinoId INT,CajaDiarioDestinoId INT)

SELECT @PersonaIdAgente= U.PersonaId, @UsuarioIdAgente = CD.UsuarioAsignadoId
FROM CREDITO.CajaDiario CD 
INNER JOIN MAESTRO.Usuario U ON U.UsuarioId = CD.UsuarioAsignadoId
WHERE CD.CajaDiarioId=@CajaDiarioId

INSERT INTO @tMovReconciliar
			(MovimientoCajaId,CajaDiarioId,CreditoId,ImportePago,PersonaId,AgenteDestinoId,CajaDiarioDestinoId)
SELECT MC.MovimientoCajaId,MC.CajaDiarioId,MC.CreditoId,MC.ImportePago,C.PersonaId,C.UsuarioRegId,CDD.CajaDiarioId
FROM CREDITO.MovimientoCaja MC
INNER JOIN CREDITO.CajaDiario CD ON CD.CajaDiarioId = MC.CajaDiarioId
INNER JOIN CREDITO.Credito C ON C.CreditoId = MC.CreditoId
LEFT JOIN CREDITO.CajaDiario CDD ON C.UsuarioRegId=CDD.UsuarioAsignadoId AND CDD.IndCierre=0 
WHERE MC.CajaDiarioId=@CajaDiarioId AND MC.CreditoId IS NOT NULL AND CD.UsuarioAsignadoId <> C.UsuarioRegId


DELETE FROM @tMovReconciliar WHERE CajaDiarioDestinoId IS NULL

----actualizar movimientos
UPDATE MC 
SET MC.CajaDiarioId=R.CajaDiarioDestinoId
FROM @tMovReconciliar R
INNER JOIN CREDITO.MovimientoCaja MC ON MC.MovimientoCajaId = R.MovimientoCajaId
 
-- actualizar saldos caja diario origen y destino
 DECLARE @Index INT=1,@NroCajaDiario INT,@CajaDiarioDestinoId INT
 DECLARE @tCajaDiarioDestino TABLE (Id INT IDENTITY(1,1),CajaDiarioDestinoId INT)

 INSERT INTO @tCajaDiarioDestino (CajaDiarioDestinoId)
 SELECT DISTINCT CajaDiarioDestinoId FROM @tMovReconciliar

SET @NroCajaDiario = (SELECT COUNT(1) FROM @tCajaDiarioDestino)
IF	@NroCajaDiario>0
BEGIN
    EXEC CREDITO.usp_RecalcularCajaDiario @CajaDiarioId

	WHILE @Index<=@NroCajaDiario
	BEGIN
		SELECT @CajaDiarioDestinoId=CajaDiarioDestinoId 
		FROM @tCajaDiarioDestino WHERE Id=@Index
		
		DECLARE @UsuarioCajaDiarioDestino VARCHAR(50) =( SELECT U.NombreUsuario FROM CREDITO.CajaDiario CD
															INNER JOIN MAESTRO.Usuario U ON U.UsuarioId = CD.UsuarioAsignadoId
															Where CajaDiarioId=@CajaDiarioDestinoId)

		SELECT @Glosa = STRING_AGG(P.NombreCompleto + ' ' + MC.Descripcion + ' ' + VT.Denominacion  , char(10) + char(13)) 
		FROM @tMovReconciliar R
		INNER JOIN MAESTRO.Persona P ON P.PersonaId = R.PersonaId
		INNER JOIN CREDITO.MovimientoCaja MC ON MC.MovimientoCajaId = R.MovimientoCajaId	
		LEFT JOIN MAESTRO.ValorTabla VT ON VT.TablaId=13 AND MC.TipoPagoId = VT.ItemId
		WHERE R.CajaDiarioDestinoId=@CajaDiarioDestinoId

		SET @Glosa =@Glosa + char(10) + CHAR(13) + 'TOTAL CONCILIADO ' + @UsuarioCajaDiarioDestino + ' = ' 
						+ (SELECT CAST(SUM(ImportePago) AS VARCHAR(30)) 
							FROM @tMovReconciliar WHERE CajaDiarioDestinoId=@CajaDiarioDestinoId)

		-- crear movimiento conciliacion
		INSERT INTO CREDITO.MovimientoCaja
			(
			  CajaDiarioId,Operacion,ImportePago,PersonaId,TipoPagoId,Descripcion,IndEntrada,Estado,UsuarioRegId,FechaReg
			)
		VALUES
			( @CajaDiarioId,'TRS',0, @PersonaIdAgente,1,@Glosa ,0,1,@UsuarioIdAgente,GETDATE() )
	
		EXEC CREDITO.usp_RecalcularCajaDiario @CajaDiarioDestinoId

		SET @Index = @Index + 1
	END

END
