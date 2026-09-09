-- VENTAS.usp_RptRentabilidadVenta '20140201','20141003', 1,1
CREATE PROC [VENTAS].[usp_RptRentabilidadVenta] 
@FechaIni DATE,
@FechaFin DATE,
@IndContado BIT = 1,
@IndCredito BIT = 1,
@OficinaId INT = NULL
AS

DECLARE @Contado CHAR(3)='CON',@Credito CHAR(3)='CRE'

IF @IndContado = 0
	SET @Contado=''
IF @IndCredito = 0
	SET @Credito=''

SELECT  ROW_NUMBER() OVER(ORDER BY A.Denominacion) 'Nro', SA.NumeroSerie 'Codigo',A.Denominacion 'Articulo',
		ME.MovimientoId, ME.Fecha 'FechaEnt',MDE.PrecioUnitario 'PrecioEnt',OV.OrdenVentaId,MS.Fecha 'FechaSal',
		CAST(OVD.Subtotal/OVD.Cantidad AS DECIMAL(15,2)) 'PrecioSal',
		CASE WHEN OV.TipoVenta='CON' THEN 'CONTADO' ELSE 'CREDITO' END 'Modalidad',
		CAST(OVD.Subtotal/OVD.Cantidad AS DECIMAL(15,2)) - MDE.PrecioUnitario 'Rentabilidad', P.NombreCompleto 'Cliente'
FROM ALMACEN.SerieArticulo SA
INNER JOIN ALMACEN.Articulo A ON SA.ArticuloId = A.ArticuloId
INNER JOIN ALMACEN.MovimientoDet MDS ON SA.MovimientoDetSalId = MDS.MovimientoDetId
INNER JOIN ALMACEN.Movimiento MS ON MDS.MovimientoId = MS.MovimientoId
INNER JOIN ALMACEN.MovimientoDet MDE ON SA.MovimientoDetEntId = MDE.MovimientoDetId
INNER JOIN ALMACEN.Movimiento ME ON MDE.MovimientoId = ME.MovimientoId
INNER JOIN VENTAS.OrdenVentaDetSerie OVS ON SA.SerieArticuloId = OVS.SerieArticuloId
INNER JOIN VENTAS.OrdenVentaDet OVD ON OVS.OrdenVentaDetId = OVD.OrdenVentaDetId
INNER JOIN VENTAS.OrdenVenta OV ON  OVD.OrdenVentaId = OV.OrdenVentaId --AND OV.Estado=1
INNER JOIN MAESTRO.Persona P ON OV.PersonaId = P.PersonaId
WHERE	OV.OficinaId=ISNULL(@OficinaId,OV.OficinaId) AND SA.EstadoId = 4 
		--AND (OV.IndContado=@IndContado OR OV.IndCredito=@IndCredito)
		AND OV.TipoVenta IN(@Contado,@Credito)
		AND CAST(MS.Fecha AS DATE) BETWEEN @FechaIni AND @FechaFin
