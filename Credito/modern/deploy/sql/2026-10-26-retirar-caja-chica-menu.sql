/*
  Gerencia: caja chica no se usa.
  Quita el ítem del menú y sus permisos de rol.
  No elimina CREDITO.CajaChicaDiario ni movimientos (historial).
*/

SET NOCOUNT ON;

DECLARE @Menus TABLE (MenuId int NOT NULL PRIMARY KEY);

INSERT INTO @Menus (MenuId)
SELECT m.MenuId
FROM MAESTRO.Menu AS m
WHERE m.Denominacion LIKE N'%caja%chica%'
   OR m.Denominacion LIKE N'%cajachica%'
   OR ISNULL(m.Url, N'') LIKE N'%CajaChica%'
   OR ISNULL(m.Url, N'') LIKE N'%caja/chica%'
   OR ISNULL(m.Modulo, N'') LIKE N'%CAJACHICA%';

DELETE rm
FROM MAESTRO.RolMenu AS rm
INNER JOIN @Menus AS x ON x.MenuId = rm.MenuId;

DELETE m
FROM MAESTRO.Menu AS m
INNER JOIN @Menus AS x ON x.MenuId = m.MenuId;

SELECT
    (SELECT COUNT(*) FROM @Menus) AS MenusCajaChicaQuitados,
    (SELECT COUNT(*) FROM MAESTRO.Menu WHERE Denominacion LIKE N'%caja%chica%' OR Denominacion LIKE N'%cajachica%') AS MenusCajaChicaRestantes;
GO
