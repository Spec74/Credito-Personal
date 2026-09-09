
CREATE PROCEDURE [dbo].[usp_AgregarPuntos]
@CodCliente NUMERIC(9,0), @OrdenVentaId INT

AS
DECLARE @TotalPuntos int = 0


SELECT @TotalPuntos = SUM(LP.Puntos*OVD.Cantidad) 
FROM VENTAS.OrdenVentaDet OVD 
INNER JOIN ALMACEN.Articulo A ON A.ArticuloId = OVD.ArticuloId
INNER JOIN VENTAS.ListaPrecio LP ON LP.ArticuloId = A.ArticuloId
WHERE OrdenVentaId = @OrdenVentaId
