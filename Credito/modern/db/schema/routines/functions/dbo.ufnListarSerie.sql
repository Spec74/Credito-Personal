-- SELECT dbo.ufnListarSerie(111111)
CREATE FUNCTION [dbo].[ufnListarSerie] ( @OrdenVentaId INT)
RETURNS VARCHAR(MAX)
AS 
    BEGIN
	    RETURN	LTRIM(ISNULL(
					STUFF(
						(SELECT ', ' + rtrim(convert(char(15),NumeroSerie))
							FROM VENTAS.OrdenVentaDet OVD
							INNER JOIN VENTAS.OrdenVentaDetSerie OVS ON OVD.OrdenVentaDetId = OVS.OrdenVentaDetId
							INNER JOIN ALMACEN.SerieArticulo SA ON OVS.SerieArticuloId = SA.SerieArticuloId
							WHERE OVD.OrdenVentaId = @OrdenVentaId
							FOR XML PATH(''))
						,1,1,'')
					,'')) 
    END
