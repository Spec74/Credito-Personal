
CREATE PROC [CREDITO].[usp_EntradaSalidaCajaChicaDiario]
@CajaChicaDiarioId INT ,
@PersonaId INT ,
@TipoOperacionId INT,
@Importe DECIMAL(16,2) = 0,
@Decripcion VARCHAR(MAX),
@UsuarioId INT
AS

DECLARE @IndEntrada BIT,@TipoOperacion CHAR(3)

SELECT @IndEntrada=IndEntrada , @TipoOperacion=Codigo
FROM MAESTRO.TipoOperacion 
WHERE TipoOperacionId=@TipoOperacionId

INSERT INTO CREDITO.MovimientoCajaChica
        ( CajaChicaDiarioId,PersonaId ,Operacion ,Importe ,
          Descripcion ,IndEntrada ,Estado ,UsuarioRegId ,FechaReg
        )
VALUES  ( @CajaChicaDiarioId,@PersonaId, @TipoOperacion , @Importe, 
          @Decripcion , @IndEntrada, 1, @UsuarioId , dbo.ufnFecha())
        
/*actualizar caja chica diario*/
DECLARE @entradas DECIMAL(16,2)=0, @salidas DECIMAL(16,2)=0
SELECT @entradas=SUM(Importe) 
FROM CREDITO.MovimientoCajaChica
WHERE CajaChicaDiarioId=@CajaChicaDiarioId AND Estado=1 AND IndEntrada=1

SELECT @salidas=SUM(Importe) 
FROM CREDITO.MovimientoCajaChica
WHERE CajaChicaDiarioId=@CajaChicaDiarioId AND Estado=1 AND IndEntrada=0

UPDATE CREDITO.CajaChicaDiario 
SET Entradas=ISNULL(@entradas,0) , Salidas = ISNULL(@salidas,0) , 
	SaldoFinal = SaldoInicial + ISNULL(@entradas,0) - ISNULL(@salidas,0)
WHERE Id=@CajaChicaDiarioId
