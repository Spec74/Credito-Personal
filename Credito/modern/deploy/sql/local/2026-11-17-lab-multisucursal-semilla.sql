/*
  LAB LOCAL — sucursal + usuario operativo sobre SEMILLA.
  NO producción / NO Azure con datos reales.

  Crea:
    - SUCURSAL HUANTA (IndPrincipal=0)
    - Usuario SUCURSAL / Admin123! (ADMINISTRADOR en esa sede)
    - Caja + bóveda abiertas en 0
    - ADMIN (oficina 1) también puede entrar a la sucursal (UsuarioOficina)

  Login:
    Central:   ADMIN / Admin123!     oficina OFICINA PRINCIPAL
    Sucursal:  SUCURSAL / Admin123!  oficina SUCURSAL HUANTA
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() IN (N'master', N'msdb', N'tempdb', N'model')
BEGIN
    RAISERROR(N'Abortado: base de sistema.', 16, 1);
    RETURN;
END;

DECLARE @CentralId int =
(
    SELECT TOP (1) OficinaId
    FROM MAESTRO.Oficina
    WHERE IndPrincipal = CAST(1 AS bit) AND Estado = CAST(1 AS bit)
    ORDER BY OficinaId
);

IF @CentralId IS NULL
BEGIN
    RAISERROR(N'No hay oficina Central (IndPrincipal=1). Corra primero SEMILLA.', 16, 1);
    RETURN;
END;

DECLARE @RolAdmin int =
(
    SELECT TOP (1) RolId
    FROM MAESTRO.Rol
    WHERE Denominacion IN (N'ADMINISTRADOR', N'ADMIN')
    ORDER BY CASE WHEN Denominacion = N'ADMINISTRADOR' THEN 0 ELSE 1 END, RolId
);

IF @RolAdmin IS NULL
BEGIN
    RAISERROR(N'No existe rol ADMINISTRADOR/ADMIN.', 16, 1);
    RETURN;
END;

DECLARE @AdminUsuarioId int =
(
    SELECT TOP (1) UsuarioId
    FROM MAESTRO.Usuario
    WHERE NombreUsuario = N'ADMIN' AND Estado = CAST(1 AS bit)
);

BEGIN TRAN;

DECLARE @SucursalId int =
(
    SELECT TOP (1) OficinaId
    FROM MAESTRO.Oficina
    WHERE Denominacion = N'SUCURSAL HUANTA'
);

IF @SucursalId IS NULL
BEGIN
    INSERT INTO MAESTRO.Oficina
        (Denominacion, Descripcion, Telefono, IndPrincipal, Estado, UsuarioAsignadoId)
    VALUES
        (N'SUCURSAL HUANTA', N'Sucursal lab multisucursal', NULL, CAST(0 AS bit), CAST(1 AS bit), @AdminUsuarioId);

    SET @SucursalId = CONVERT(int, SCOPE_IDENTITY());
    PRINT N'Oficina creada: SUCURSAL HUANTA id=' + CONVERT(varchar(12), @SucursalId);
END
ELSE
BEGIN
    UPDATE MAESTRO.Oficina
    SET Estado = CAST(1 AS bit),
        IndPrincipal = CAST(0 AS bit)
    WHERE OficinaId = @SucursalId;
    PRINT N'Oficina ya existía: SUCURSAL HUANTA id=' + CONVERT(varchar(12), @SucursalId);
END;

/* Persona + usuario SUCURSAL */
DECLARE @PersonaSuc int =
(
    SELECT TOP (1) p.PersonaId
    FROM MAESTRO.Persona AS p
    INNER JOIN MAESTRO.Usuario AS u ON u.PersonaId = p.PersonaId
    WHERE u.NombreUsuario = N'SUCURSAL'
);

IF @PersonaSuc IS NULL
BEGIN
    INSERT INTO MAESTRO.Persona
        (TipoPersona, Nombre, ApePaterno, ApeMaterno, NombreCompleto, NumeroDocumento, TipoDocumento, Sexo, Estado)
    VALUES
        (N'N', N'SUCURSAL', N'HUANTA', N'LAB', N'HUANTA LAB, SUCURSAL', N'70000099', N'DNI', N'M', CAST(1 AS bit));
    SET @PersonaSuc = CONVERT(int, SCOPE_IDENTITY());
END;

DECLARE @UsuarioSuc int =
(
    SELECT TOP (1) UsuarioId
    FROM MAESTRO.Usuario
    WHERE NombreUsuario = N'SUCURSAL'
);

IF @UsuarioSuc IS NULL
BEGIN
    INSERT INTO MAESTRO.Usuario (PersonaId, NombreUsuario, ClaveUsuario, Estado)
    VALUES (@PersonaSuc, N'SUCURSAL', N'Admin123!', CAST(1 AS bit));
    SET @UsuarioSuc = CONVERT(int, SCOPE_IDENTITY());
    PRINT N'Usuario SUCURSAL creado id=' + CONVERT(varchar(12), @UsuarioSuc);
END
ELSE
BEGIN
    UPDATE MAESTRO.Usuario
    SET Estado = CAST(1 AS bit), PersonaId = @PersonaSuc
    WHERE UsuarioId = @UsuarioSuc;
END;

UPDATE MAESTRO.Oficina
SET UsuarioAsignadoId = @UsuarioSuc
WHERE OficinaId = @SucursalId;

IF NOT EXISTS (
    SELECT 1 FROM MAESTRO.UsuarioOficina
    WHERE UsuarioId = @UsuarioSuc AND OficinaId = @SucursalId
)
    INSERT INTO MAESTRO.UsuarioOficina (UsuarioId, OficinaId, Estado)
    VALUES (@UsuarioSuc, @SucursalId, CAST(1 AS bit));

IF @AdminUsuarioId IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM MAESTRO.UsuarioOficina
        WHERE UsuarioId = @AdminUsuarioId AND OficinaId = @SucursalId
   )
    INSERT INTO MAESTRO.UsuarioOficina (UsuarioId, OficinaId, Estado)
    VALUES (@AdminUsuarioId, @SucursalId, CAST(1 AS bit));

IF NOT EXISTS (
    SELECT 1 FROM MAESTRO.UsuarioRol
    WHERE UsuarioId = @UsuarioSuc AND RolId = @RolAdmin AND OficinaId = @SucursalId
)
    INSERT INTO MAESTRO.UsuarioRol (UsuarioId, RolId, OficinaId)
    VALUES (@UsuarioSuc, @RolAdmin, @SucursalId);

IF NOT EXISTS (SELECT 1 FROM CREDITO.Caja WHERE OficinaId = @SucursalId)
BEGIN
    INSERT INTO CREDITO.Caja
        (OficinaId, Denominacion, Estado, UsuarioRegId, FechaReg, IndAbierto, CajeroId)
    VALUES
        (@SucursalId, N'CAJA HUANTA', CAST(1 AS bit), @UsuarioSuc, SYSUTCDATETIME(), CAST(0 AS bit), @UsuarioSuc);
END
ELSE
BEGIN
    UPDATE CREDITO.Caja
    SET CajeroId = @UsuarioSuc, Estado = CAST(1 AS bit)
    WHERE OficinaId = @SucursalId AND CajeroId IS NULL;
END;

IF NOT EXISTS (
    SELECT 1 FROM CREDITO.Boveda
    WHERE OficinaId = @SucursalId AND IndCierre = CAST(0 AS bit) AND IndTemporal = CAST(0 AS bit)
)
BEGIN
    INSERT INTO CREDITO.Boveda
        (OficinaId, SaldoInicial, Entradas, Salidas, SaldoFinal, FechaIniOperacion, IndCierre, IndTemporal)
    VALUES
        (@SucursalId, 0, 0, 0, 0, SYSUTCDATETIME(), CAST(0 AS bit), CAST(0 AS bit));
END;

DECLARE @BovedaSuc int =
(
    SELECT TOP (1) BovedaId
    FROM CREDITO.Boveda
    WHERE OficinaId = @SucursalId AND IndCierre = 0 AND IndTemporal = 0
    ORDER BY BovedaId
);

IF @BovedaSuc IS NOT NULL
   AND OBJECT_ID(N'CREDITO.BovedaCuenta', N'U') IS NOT NULL
BEGIN
    INSERT INTO CREDITO.BovedaCuenta (BovedaId, TipoPagoId, SaldoInicial)
    SELECT @BovedaSuc, vt.ItemId, 0
    FROM MAESTRO.ValorTabla AS vt
    WHERE vt.TablaId = 13
      AND vt.ItemId > 0
      AND vt.ItemId NOT BETWEEN 6 AND 8
      AND ISNULL(vt.DesCorta, N'') <> N'OBSOLETO'
      AND NOT EXISTS (
            SELECT 1 FROM CREDITO.BovedaCuenta AS bc
            WHERE bc.BovedaId = @BovedaSuc AND bc.TipoPagoId = vt.ItemId);
END;

COMMIT;

SELECT
    o.OficinaId,
    o.Denominacion,
    o.IndPrincipal,
    (SELECT COUNT(*) FROM CREDITO.Caja c WHERE c.OficinaId = o.OficinaId) AS Cajas,
    (SELECT COUNT(*) FROM CREDITO.Boveda b WHERE b.OficinaId = o.OficinaId AND b.IndCierre = 0) AS BovedasAbiertas
FROM MAESTRO.Oficina AS o
WHERE o.OficinaId IN (@CentralId, @SucursalId)
ORDER BY o.IndPrincipal DESC, o.OficinaId;

SELECT u.NombreUsuario, uo.OficinaId, o.Denominacion
FROM MAESTRO.Usuario AS u
INNER JOIN MAESTRO.UsuarioOficina AS uo ON uo.UsuarioId = u.UsuarioId
INNER JOIN MAESTRO.Oficina AS o ON o.OficinaId = uo.OficinaId
WHERE u.NombreUsuario IN (N'ADMIN', N'SUCURSAL')
ORDER BY u.NombreUsuario, uo.OficinaId;

PRINT N'OK lab multisucursal. Central=ADMIN · Sucursal=SUCURSAL / Admin123!';
GO
