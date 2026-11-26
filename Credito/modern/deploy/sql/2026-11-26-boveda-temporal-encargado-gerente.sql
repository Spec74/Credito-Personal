-- ENCARGADO = gerente permanente de sede.
-- Tracking de quién recibió ENCARGADO al crear la temporal.
-- Al cerrar: solo se revoca si OtorgoEncargado=1 (el gerente que ya lo tenía se conserva).
-- Fuente de cierre: 2026-11-13-usp-cerrar-boveda-temporal-centimos.sql + revocación selectiva.
-- Idempotente.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'CREDITO.BovedaTemporalResponsable', N'U') IS NULL
BEGIN
    CREATE TABLE CREDITO.BovedaTemporalResponsable (
        BovedaId int NOT NULL CONSTRAINT PK_BovedaTemporalResponsable PRIMARY KEY,
        OficinaId int NOT NULL,
        UsuarioId int NOT NULL,
        OtorgoEncargado bit NOT NULL CONSTRAINT DF_BovedaTemporalResponsable_Otorgo DEFAULT (0),
        FechaReg datetime NOT NULL CONSTRAINT DF_BovedaTemporalResponsable_Fecha DEFAULT (dbo.ufnFecha()),
        CONSTRAINT FK_BovedaTemporalResponsable_Boveda
            FOREIGN KEY (BovedaId) REFERENCES CREDITO.Boveda (BovedaId),
        CONSTRAINT FK_BovedaTemporalResponsable_Oficina
            FOREIGN KEY (OficinaId) REFERENCES MAESTRO.Oficina (OficinaId),
        CONSTRAINT FK_BovedaTemporalResponsable_Usuario
            FOREIGN KEY (UsuarioId) REFERENCES MAESTRO.Usuario (UsuarioId)
    );
END
GO

CREATE OR ALTER PROCEDURE [CREDITO].[usp_CerrarBovedaTemporal]
    @OficinaId INT,
    @UsuarioRegId INT
AS
DECLARE @BovedaId INT, @BovedaIdTemporal INT, @ImporteBovedaTemporal DECIMAL(15,2), @RolEncargado INT

SELECT @BovedaIdTemporal=BovedaId, @ImporteBovedaTemporal=SaldoFinal
FROM CREDITO.Boveda
WHERE OficinaId=@OficinaId AND IndTemporal=1 AND IndCierre=0

SELECT @BovedaId=BovedaId
FROM CREDITO.Boveda
WHERE OficinaId=@OficinaId AND IndTemporal=0 AND IndCierre=0

IF @BovedaIdTemporal IS NULL
BEGIN
    RAISERROR(N'No hay bóveda temporal abierta en la oficina.', 16, 1)
    RETURN
END

IF @BovedaId IS NULL
BEGIN
    RAISERROR(N'No hay bóveda principal abierta para devolver el saldo temporal.', 16, 1)
    RETURN
END

EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaIdTemporal

;WITH CUENTAS AS (
    SELECT MC.BovedaId,TipoPagoId,SUM(IIF(MC.IndEntrada=1,MC.Importe,-MC.Importe) ) 'Importe'
    FROM CREDITO.BovedaMov MC
    WHERE MC.BovedaId=@BovedaIdTemporal
    AND MC.Estado=1
    GROUP BY MC.BovedaId,MC.TipoPagoId
)
INSERT INTO CREDITO.BovedaMov(BovedaId,CodOperacion,Glosa,TipoPagoId,Importe,IndEntrada, Estado, CajaDiarioId, UsuarioRegId, FechaReg)
SELECT @BovedaId,'TRE','CIERRE BOVEDA TEMPORAL',vt.ItemId,
       ROUND(ISNULL(BC.SaldoInicial,0) + ISNULL(c.Importe,0), 2),1,1,0,@UsuarioRegId,dbo.ufnFecha()
FROM MAESTRO.ValorTabla vt
LEFT JOIN CUENTAS C ON vt.ItemId = C.TipoPagoId
LEFT JOIN CREDITO.BovedaCuenta BC ON BC.BovedaId = @BovedaIdTemporal AND BC.TipoPagoId = vt.ItemId
WHERE vt.TablaId=13
  AND vt.ItemId > 0
  AND vt.ItemId NOT BETWEEN 6 AND 8
  AND ISNULL(vt.DesCorta, N'') <> N'OBSOLETO'
  AND ROUND(ISNULL(BC.SaldoInicial,0) + ISNULL(c.Importe,0), 2) <> 0

UPDATE CREDITO.Boveda
SET IndCierre=1, FechaFinOperacion=dbo.ufnFecha()
WHERE BovedaId= @BovedaIdTemporal

EXEC CREDITO.usp_ActualizarSaldosBoveda @BovedaId = @BovedaId

-- Solo revoca ENCARGADO otorgado por ESTA temporal. Gerente permanente se conserva.
SELECT @RolEncargado=r.RolId
FROM MAESTRO.Rol AS r
WHERE r.Denominacion ='ENCARGADO'

IF @RolEncargado IS NOT NULL AND OBJECT_ID(N'CREDITO.BovedaTemporalResponsable', N'U') IS NOT NULL
BEGIN
    DELETE ur
    FROM MAESTRO.UsuarioRol AS ur
    INNER JOIN CREDITO.BovedaTemporalResponsable AS tr
        ON tr.UsuarioId = ur.UsuarioId
       AND tr.OficinaId = ur.OficinaId
       AND tr.BovedaId = @BovedaIdTemporal
    WHERE ur.RolId = @RolEncargado
      AND ur.OficinaId = @OficinaId
      AND tr.OtorgoEncargado = CAST(1 AS bit)
END
GO
