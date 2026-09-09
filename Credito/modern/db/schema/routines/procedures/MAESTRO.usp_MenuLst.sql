-- MAESTRO.usp_MenuLst @OficinaId=1,@UsuarioId=4
CREATE PROC [MAESTRO].[usp_MenuLst]
@OficinaId INT,
@UsuarioId INT
AS
BEGIN
	WITH MNU AS(
		SELECT DISTINCT M.* 
		FROM MAESTRO.UsuarioRol UR
		INNER JOIN MAESTRO.RolMenu RM ON UR.RolId = RM.RolId
		INNER JOIN MAESTRO.Menu M ON RM.MenuId = M.MenuId
		WHERE UR.UsuarioId=@UsuarioId AND UR.OficinaId=@OficinaId
	)
	SELECT * FROM MNU
	UNION
	SELECT M.* FROM MAESTRO.Menu M
	INNER JOIN MNU M1 ON M1.Referencia=M.Orden 
	WHERE M.IndPadre=1
	ORDER BY 7
END
