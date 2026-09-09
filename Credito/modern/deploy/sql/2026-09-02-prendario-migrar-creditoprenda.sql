-- Retira CREDITO.CreditoPrenda en favor de CREDITO.Prenda.
--
-- CreditoPrenda era una tabla provisional del sistema moderno: una prenda por credito y sin
-- marca, modelo, serie, color, foto ni codigo interno. Se adopta CREDITO.Prenda, el diseno de
-- gerencia, porque el MVC legacy va a leer esa tabla y ambos sistemas comparten la base
-- durante la migracion strangler. Ver docs/migration/BITACORA-DESVIACIONES.md.
--
-- Debe ejecutarse despues de 2026-09-01-prendario-modulo.sql. Idempotente.

-- Si la tabla destino no existe pero la de origen si, el script corrio fuera de orden. Cortar
-- aqui en vez de no hacer nada: de lo contrario los bienes se quedarian en la tabla vieja sin
-- que nadie lo note.
IF OBJECT_ID(N'CREDITO.CreditoPrenda', N'U') IS NOT NULL
   AND OBJECT_ID(N'CREDITO.Prenda', N'U') IS NULL
BEGIN
    THROW 51000, 'CREDITO.Prenda no existe: ejecute antes 2026-09-01-prendario-modulo.sql.', 1;
END
GO

IF OBJECT_ID(N'CREDITO.CreditoPrenda', N'U') IS NOT NULL
   AND OBJECT_ID(N'CREDITO.Prenda', N'U') IS NOT NULL
BEGIN
    -- Solo copia los creditos que aun no tienen bienes en la tabla nueva, para que reejecutar
    -- el script no duplique filas.
    INSERT INTO CREDITO.Prenda (
        CreditoId, Descripcion, Serie, ValorTasacion, Observaciones,
        Estado, FechaRegistro, UsuarioRegId, UsuarioModId, FechaMod)
    SELECT cp.CreditoId,
           cp.Descripcion,
           'N/T',
           cp.MontoTasacion,
           cp.Observacion,
           'EN CUSTODIA',
           cp.FechaReg,
           cp.UsuarioRegId,
           cp.UsuarioModId,
           cp.FechaMod
    FROM CREDITO.CreditoPrenda AS cp
    WHERE cp.Estado = CAST(1 AS bit)
      AND NOT EXISTS (
          SELECT 1 FROM CREDITO.Prenda AS p WHERE p.CreditoId = cp.CreditoId);

    -- La fecha de remate y el indicador vivian solo en CreditoPrenda; pasan al credito.
    UPDATE c
    SET EsPrendario = CAST(1 AS bit),
        MontoTasacion = ISNULL((
            SELECT SUM(p.ValorTasacion)
            FROM CREDITO.Prenda AS p
            WHERE p.CreditoId = c.CreditoId), 0),
        NumeroContratoPrendario = ISNULL(
            NULLIF(LTRIM(RTRIM(c.NumeroContratoPrendario)), ''),
            CAST(c.CreditoId AS nvarchar(50))),
        FechaRemate = COALESCE(c.FechaRemate, cp.FechaRemate)
    FROM CREDITO.Credito AS c
    INNER JOIN CREDITO.CreditoPrenda AS cp ON cp.CreditoId = c.CreditoId
    WHERE cp.Estado = CAST(1 AS bit);

    DROP TABLE CREDITO.CreditoPrenda;
END
GO
