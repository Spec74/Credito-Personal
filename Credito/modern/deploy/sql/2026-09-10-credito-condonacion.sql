-- Flujo de solicitud de condonacion del bak 2026-09-01.
-- Tabla CREDITO.CreditoCondonacion + CREDITO.usp_SolicitarCondonacion.
-- El cuerpo del procedimiento se versiona tal cual (sin reescribir la formula).
-- Idempotente: se reaplica tras cada restauracion de base del cliente.

IF OBJECT_ID(N'CREDITO.CreditoCondonacion', N'U') IS NULL
BEGIN
    CREATE TABLE CREDITO.CreditoCondonacion (
        Id int IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_CreditoCondonacion PRIMARY KEY,
        CreditoId int NOT NULL,
        CajaDiarioId int NOT NULL,
        Fecha datetime NOT NULL,
        MoraCondonacion decimal(10,2) NOT NULL,
        TotalPago decimal(15,2) NOT NULL,
        IndAprobado bit NOT NULL,
        CONSTRAINT FK_CreditoCondonacion_Credito FOREIGN KEY (CreditoId)
            REFERENCES CREDITO.Credito (CreditoId),
        CONSTRAINT FK_CreditoCondonacion_CajaDiario FOREIGN KEY (CajaDiarioId)
            REFERENCES CREDITO.CajaDiario (CajaDiarioId)
    );
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROC [CREDITO].[usp_SolicitarCondonacion]
    @CajaDiarioId INT,
    @CreditoId INT,
    @MoraCondonacion DECIMAL(15, 2)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MontoCredito DECIMAL(15, 2) = (
        SELECT
            ISNULL(MontoCredito, 0)
            + ISNULL(MontoCredito, 0) * ISNULL(Interes, 0) / 100.0
        FROM CREDITO.Credito
        WHERE CreditoId = @CreditoId
    );

    IF @MontoCredito IS NULL
    BEGIN
        RAISERROR(N'No existe el crédito indicado para condonación.', 16, 1);
        RETURN;
    END;

    DECLARE @Pagos DECIMAL(15, 2) = ISNULL((
        SELECT SUM(ImportePago)
        FROM CREDITO.MovimientoCaja
        WHERE CreditoId = @CreditoId
          AND ImportePago > 0
          AND Operacion = N'CUO'
    ), 0);

    INSERT CREDITO.CreditoCondonacion
    (
        CreditoId,
        CajaDiarioId,
        Fecha,
        MoraCondonacion,
        IndAprobado,
        TotalPago
    )
    VALUES
    (
        @CreditoId,
        @CajaDiarioId,
        dbo.ufnFecha(),
        @MoraCondonacion,
        CAST(0 AS bit),
        @MontoCredito - @Pagos + ISNULL(@MoraCondonacion, 0)
    );
END
GO
