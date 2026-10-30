/*
  Gerencia: compra a proveedores no opera en Credix (vive en ventas).
  Quita el ítem de menú Entrada de almacén y sus RolMenu.
  No elimina ALMACEN.Movimiento / SerieArticulo (salida por venta y kardex).
  No toca Transferencia ni Salida.

  Azure CREDITO de prueba primero. Producción solo si gerencia lo pide.
*/

SET NOCOUNT ON;

DECLARE @Menus TABLE (MenuId int NOT NULL PRIMARY KEY);

INSERT INTO @Menus (MenuId)
SELECT m.MenuId
FROM MAESTRO.Menu AS m
WHERE m.IndPadre = CAST(0 AS bit)
  AND (
        ISNULL(m.Url, N'') LIKE N'%Entrada/Index%'
     OR ISNULL(m.Url, N'') LIKE N'%Almacen/Entrada%'
     OR ISNULL(m.Url, N'') LIKE N'%/Entrada%'
     OR (
            m.Denominacion LIKE N'%entrada%'
        AND m.Denominacion LIKE N'%almacen%'
        )
     OR (
            UPPER(LTRIM(RTRIM(m.Denominacion))) = N'ENTRADA'
        AND ISNULL(m.Modulo, N'') LIKE N'%ALMACEN%'
        )
  )
  AND ISNULL(m.Url, N'') NOT LIKE N'%Transferencia%'
  AND ISNULL(m.Url, N'') NOT LIKE N'%Salida%'
  AND ISNULL(m.Url, N'') NOT LIKE N'%Caja%'
  AND ISNULL(m.Url, N'') NOT LIKE N'%Kardex%';

DELETE rm
FROM MAESTRO.RolMenu AS rm
INNER JOIN @Menus AS x ON x.MenuId = rm.MenuId;

DELETE m
FROM MAESTRO.Menu AS m
INNER JOIN @Menus AS x ON x.MenuId = m.MenuId;

SELECT
    (SELECT COUNT(*) FROM @Menus) AS MenusEntradaQuitados,
    (
        SELECT COUNT(*)
        FROM MAESTRO.Menu
        WHERE IndPadre = CAST(0 AS bit)
          AND (
                ISNULL(Url, N'') LIKE N'%Entrada/Index%'
             OR ISNULL(Url, N'') LIKE N'%Almacen/Entrada%'
             OR (Denominacion LIKE N'%entrada%' AND Denominacion LIKE N'%almacen%')
          )
    ) AS MenusEntradaRestantes;
GO
