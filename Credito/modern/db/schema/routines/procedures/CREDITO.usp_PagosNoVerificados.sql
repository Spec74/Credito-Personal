-- exec CREDITO.usp_PagosNoVerificados
CREATE PROC CREDITO.usp_PagosNoVerificados
AS
SELECT mc.MovimientoCajaId,p.NombreCompleto 'Cliente',mc.Descripcion 'Movimiento',mc.ImportePago,
		vt.Denominacion 'TipoPago',mce.FechaTransferencia,u.NombreUsuario 'Registro' 
FROM CREDITO.CajaDiario cd
INNER JOIN CREDITO.MovimientoCaja mc ON mc.CajaDiarioId = cd.CajaDiarioId 
INNER JOIN MAESTRO.Persona p ON p.PersonaId = mc.PersonaId
INNER JOIN CREDITO.MovimientoCajaExtension mce ON mce.MovimientoCajaId = mc.MovimientoCajaId
INNER JOIN MAESTRO.ValorTabla vt ON vt.TablaId=13 AND mc.TipoPagoId=vt.ItemId
INNER JOIN MAESTRO.Usuario u ON u.UsuarioId = mc.UsuarioRegId
WHERE cd.IndCierre=0 AND mc.TipoPagoId>1 AND mc.Estado=1 AND mce.IndTransferenciaVerificada=0
ORDER BY u.NombreUsuario
