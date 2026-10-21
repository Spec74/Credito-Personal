CREATE OR ALTER PROC [CREDITO].[usp_PlaGuardarParametro]
    @TcVentaMesAnterior decimal(12, 4) = NULL,
    @TcPeriodo char(7) = NULL,
    @UmbralIndividualMenorUsd decimal(18, 2) = NULL,
    @UmbralMultipleMenorUsd decimal(18, 2) = NULL,
    @UsuarioId int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT PlaParametroId FROM CREDITO.PlaParametro WHERE PlaParametroId = 1;
END
