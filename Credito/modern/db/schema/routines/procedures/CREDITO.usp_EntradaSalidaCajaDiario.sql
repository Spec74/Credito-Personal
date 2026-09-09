
/*


*/
CREATE PROC [CREDITO].[usp_EntradaSalidaCajaDiario]
@CajaDiarioId INT ,
@PersonaId INT ,
@TipoOperacionId INT,
@Importe DECIMAL(16,2) = 0,
@Decripcion VARCHAR(MAX),
@UsuarioId INT,
@TipoPagoId INT = 1
AS

DECLARE @IndEntrada BIT,@TipoOperacion CHAR(3)

SELECT @IndEntrada=IndEntrada , @TipoOperacion=Codigo
FROM MAESTRO.TipoOperacion 
WHERE TipoOperacionId=@TipoOperacionId

INSERT INTO CREDITO.MovimientoCaja
        ( CajaDiarioId,PersonaId ,Operacion ,ImportePago ,
          Descripcion ,IndEntrada ,Estado ,UsuarioRegId ,FechaReg,TipoPagoId
        )
VALUES  ( @CajaDiarioId,@PersonaId, @TipoOperacion , @Importe ,
          @Decripcion , @IndEntrada, 1, @UsuarioId , dbo.ufnFecha(),@TipoPagoId)
        
/*actualizar caja diario*/
DECLARE @entradas DECIMAL(16,2)=0, @salidas DECIMAL(16,2)=0
SELECT @entradas=SUM(ImportePago) 
FROM CREDITO.MovimientoCaja
WHERE CajaDiarioId=@CajaDiarioId AND Estado=1 AND IndEntrada=1

SELECT @salidas=SUM(ImportePago) 
FROM CREDITO.MovimientoCaja
WHERE CajaDiarioId=@CajaDiarioId AND Estado=1 AND IndEntrada=0

UPDATE CREDITO.CajaDiario 
SET Entradas=ISNULL(@entradas,0) , Salidas = ISNULL(@salidas,0) , 
	SaldoFinal = SaldoInicial + ISNULL(@entradas,0) - ISNULL(@salidas,0)
WHERE CajaDiarioId=@CajaDiarioId
