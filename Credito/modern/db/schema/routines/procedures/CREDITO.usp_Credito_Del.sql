CREATE PROC [CREDITO].[usp_Credito_Del]
@CreditoId INT,
@Observacion VARCHAR(MAX),
@UsuarioId INT
AS

DECLARE @OrdenVentaId INT,@EstadoEnAlmacen INT=2

SELECT @OrdenVentaId = OrdenVentaId FROM CREDITO.Credito
WHERE CreditoId = @CreditoId

UPDATE CREDITO.Credito SET Estado='ANU',Observacion=@Observacion , UsuarioModId=@UsuarioId,FechaMod=dbo.ufnFecha()
WHERE CreditoId=@CreditoId

UPDATE	CREDITO.CuentaxCobrar
SET		Estado = 'ANU'
WHERE CreditoId=@CreditoId		
		
UPDATE VENTAS.OrdenVenta SET Estado=0,MovimientoAlmacenId=NULL ,UsuarioModId=@UsuarioId,FechaMod=dbo.ufnFecha()
WHERE OrdenVentaId=@OrdenVentaId 

UPDATE	SA
SET		SA.EstadoId=@EstadoEnAlmacen, 
		SA.MovimientoDetSalId = NULL
FROM VENTAS.OrdenVentaDet OVD
INNER JOIN VENTAS.OrdenVentaDetSerie OVDS ON OVD.OrdenVentaDetId = OVDS.OrdenVentaDetId
INNER JOIN ALMACEN.SerieArticulo SA ON SA.SerieArticuloId=OVDS.SerieArticuloId
WHERE OVD.OrdenVentaId=@OrdenVentaId

DELETE MD
FROM VENTAS.OrdenVenta OV
INNER JOIN ALMACEN.Movimiento M ON OV.MovimientoAlmacenId=M.MovimientoId
INNER JOIN ALMACEN.MovimientoDet MD ON M.MovimientoId = MD.MovimientoId
WHERE OV.OrdenVentaId=@OrdenVentaId

DELETE M
FROM VENTAS.OrdenVenta OV
INNER JOIN ALMACEN.Movimiento M ON OV.MovimientoAlmacenId=M.MovimientoId
WHERE OV.OrdenVentaId=@OrdenVentaId
