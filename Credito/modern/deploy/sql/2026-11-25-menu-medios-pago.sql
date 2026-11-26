/*
  Menú: Medios de pago (bajo MANTENIMIENTO → Referencia 40).
  La SPA resuelve Url=MediosPago / etiqueta a /maestros/medios-pago.
  Idempotente.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @MenuId int;
DECLARE @Orden decimal(10, 1) = 40.7;

SELECT @MenuId = MenuId
FROM MAESTRO.Menu
WHERE Url = N'MediosPago'
   OR Denominacion IN (N'MEDIOS DE PAGO', N'Medios de pago');

IF @MenuId IS NULL
BEGIN
    INSERT INTO MAESTRO.Menu (
        Denominacion, Modulo, Url, Icono, IndPadre, Orden, Referencia
    )
    VALUES (
        N'MEDIOS DE PAGO',
        N'MANTENIMIENTO',
        N'MediosPago',
        NULL,
        0,
        @Orden,
        40.0
    );

    SET @MenuId = SCOPE_IDENTITY();
END;

;WITH RolesDestino AS (
    SELECT DISTINCT rm.RolId
    FROM MAESTRO.RolMenu AS rm
    WHERE rm.MenuId IN (1, 11, 12, 13, 14, 32)
    UNION
    SELECT RolId
    FROM MAESTRO.Rol
    WHERE Denominacion IN (N'ADMINISTRADOR', N'ENCARGADO', N'CAJA', N'CAJA CENTRAL')
)
INSERT INTO MAESTRO.RolMenu (RolId, MenuId)
SELECT r.RolId, @MenuId
FROM RolesDestino AS r
WHERE NOT EXISTS (
    SELECT 1
    FROM MAESTRO.RolMenu AS rm
    WHERE rm.RolId = r.RolId
      AND rm.MenuId = @MenuId
);

PRINT CONCAT('Menu Medios de pago Id=', @MenuId);
GO
