-- Geolocalizacion de clientes y oficinas (adicion del sistema moderno).
-- Requerida por el selector de mapa (GoogleMapLocationPicker) y por la ruta de cobros.
-- No existe en las entregas de base del cliente: reaplicar tras cada restauracion.

IF COL_LENGTH(N'MAESTRO.Cliente', N'Latitud') IS NULL
BEGIN
    ALTER TABLE MAESTRO.Cliente ADD Latitud decimal(11,8) NULL;
END
GO

IF COL_LENGTH(N'MAESTRO.Cliente', N'Longitud') IS NULL
BEGIN
    ALTER TABLE MAESTRO.Cliente ADD Longitud decimal(11,8) NULL;
END
GO

IF COL_LENGTH(N'MAESTRO.Oficina', N'Latitud') IS NULL
BEGIN
    ALTER TABLE MAESTRO.Oficina ADD Latitud decimal(11,8) NULL;
END
GO

IF COL_LENGTH(N'MAESTRO.Oficina', N'Longitud') IS NULL
BEGIN
    ALTER TABLE MAESTRO.Oficina ADD Longitud decimal(11,8) NULL;
END
GO
