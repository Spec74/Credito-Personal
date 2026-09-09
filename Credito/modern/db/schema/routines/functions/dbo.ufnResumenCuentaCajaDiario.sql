

/*
SELECT dbo.ufnResumenCuentaCajaDiario(19036)
*/

CREATE FUNCTION [dbo].[ufnResumenCuentaCajaDiario] ( @CajaDiarioId INT)
RETURNS VARCHAR(500)
AS 
    BEGIN
	    DECLARE @SaldoInicial DECIMAL(15,2) = (SELECT SaldoInicial FROM CREDITO.CajaDiario WHERE CajaDiarioId=@CajaDiarioId)
		DECLARE	@Retorno VARCHAR(500)

		;WITH SALDOINICIAL AS(	
			SELECT ItemId 'TipoPagoId',Denominacion, IIF(ItemId=1,@SaldoInicial,0) 'Importe'
			FROM MAESTRO.ValorTabla 
			WHERE TablaId=13 AND ItemId>0
		),CUENTAS AS (
			SELECT TipoPagoId,SUM(IIF(MC.IndEntrada=1,MC.ImportePago,-MC.ImportePago) ) 'Importe'
			FROM CREDITO.MovimientoCaja MC  
			WHERE MC.CajaDiarioId=@CajaDiarioId 
			AND MC.Estado=1	
			GROUP BY MC.TipoPagoId	
		), SALDOS AS(
			SELECT S.TipoPagoId, S.Denominacion, S.Importe + ISNULL(c.Importe,0) 'Saldo'
			FROM SALDOINICIAL S
			LEFT JOIN CUENTAS C ON  c.TipoPagoId= S.TipoPagoId
		)
		SELECT @Retorno=( STRING_AGG(Denominacion + ' = ' + CAST (Saldo AS VARCHAR(15)),'  ') )
		FROM SALDOS
		WHERE Saldo>0
        
		RETURN @Retorno
    END
