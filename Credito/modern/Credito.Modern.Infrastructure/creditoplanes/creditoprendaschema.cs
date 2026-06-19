using Dapper;
using Microsoft.Data.SqlClient;

namespace Credito.Modern.Infrastructure.CreditoPlanes;

internal static class CreditoPrendaSchema
{
    public static Task EnsureAsync(SqlConnection connection, CancellationToken cancellationToken) =>
        connection.ExecuteAsync(
            new CommandDefinition(
                """
                IF OBJECT_ID(N'CREDITO.CreditoPrenda', N'U') IS NULL
                BEGIN
                    CREATE TABLE CREDITO.CreditoPrenda (
                        CreditoPrendaId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_CreditoPrenda PRIMARY KEY,
                        CreditoId int NOT NULL,
                        Descripcion varchar(500) NOT NULL,
                        MontoTasacion decimal(16,2) NOT NULL,
                        FechaRemate date NOT NULL,
                        Observacion varchar(max) NULL,
                        Estado bit NOT NULL CONSTRAINT DF_CreditoPrenda_Estado DEFAULT (1),
                        UsuarioRegId int NOT NULL,
                        FechaReg datetime NOT NULL CONSTRAINT DF_CreditoPrenda_FechaReg DEFAULT (GETDATE()),
                        UsuarioModId int NULL,
                        FechaMod datetime NULL,
                        CONSTRAINT UQ_CreditoPrenda_Credito UNIQUE (CreditoId),
                        CONSTRAINT FK_CreditoPrenda_Credito FOREIGN KEY (CreditoId)
                            REFERENCES CREDITO.Credito (CreditoId)
                    );
                END;
                """,
                cancellationToken: cancellationToken));
}
