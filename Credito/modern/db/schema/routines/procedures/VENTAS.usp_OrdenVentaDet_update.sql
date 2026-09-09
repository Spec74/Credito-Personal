
/*

EXEC VENTAS.usp_OrdenVentaDet_update @OrdenVentaDetId = 120, @Descuento = 100

*/

CREATE PROC [VENTAS].[usp_OrdenVentaDet_update]
@OrdenVentaDetId INT, 
@Descuento DECIMAL(16,4)
AS
BEGIN
	
	DECLARE  @IGV DECIMAL(16,2), @OrdenVentaId INT
	SET @IGV = 0.18

	SELECT @OrdenVentaId = OrdenVentaId FROM VENTAS.OrdenVentaDet
	WHERE OrdenVentaDetId = @OrdenVentaDetId

	UPDATE VENTAS.OrdenVentaDet 
	SET Descuento = @Descuento, Subtotal = (ValorVenta-@Descuento)*Cantidad
	WHERE OrdenVentaDetId=@OrdenVentaDetId

	;WITH OrdenDetalle AS(
		SELECT OrdenVentaId, SUM(Subtotal) 'Subtotal' , SUM(Descuento) 'Descuento'
		FROM VENTAS.OrdenVentaDet WHERE OrdenVentaId=@OrdenVentaId AND Estado=1
		GROUP BY OrdenVentaId
	)
	UPDATE OV
	SET OV.TotalNeto = OD.Subtotal,
	OV.TotalDescuento = OD.Descuento,
	OV.Subtotal = OD.Subtotal / (1 + @IGV),
	OV.TotalImpuesto =  OD.Subtotal * ( @IGV/(1+@IGV) ) 
	FROM VENTAS.OrdenVenta OV
	INNER JOIN OrdenDetalle OD ON OV.OrdenVentaId = OD.OrdenVentaId
	WHERE OV.OrdenVentaId = @OrdenVentaId
END
