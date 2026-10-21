namespace Credito.Modern.Application.Pla;

public sealed record PlaParametroDto(
    int PlaParametroId,
    decimal IndividualUsd,
    decimal MultipleUsd,
    decimal? UmbralIndividualMenorUsd,
    decimal? UmbralMultipleMenorUsd,
    decimal IndividualEfectivoUsd,
    decimal MultipleEfectivoUsd,
    decimal? TcVentaMesAnterior,
    string? TcPeriodo,
    string FuenteNorma,
    bool TieneTipoCambio);

public sealed record GuardarPlaParametroRequest(
    decimal? TcVentaMesAnterior,
    string? TcPeriodo,
    decimal? UmbralIndividualMenorUsd,
    decimal? UmbralMultipleMenorUsd);

public sealed record PlaCandidatoRoDto(
    string Tipo,
    int? CreditoId,
    int? PersonaId,
    string? NumeroDocumento,
    string? NombreCompleto,
    decimal Monto,
    DateTime FechaOp,
    bool SuperaUmbral,
    string MotivoRo,
    bool FaltaDocumento,
    bool FaltaDireccion,
    bool FaltaOcupacion,
    decimal IndividualEfectivoUsd,
    decimal MultipleEfectivoUsd,
    decimal? TcVentaMesAnterior,
    decimal? UmbralIndividualPen,
    decimal? UmbralMultiplePen,
    bool TieneTipoCambio,
    decimal? MontoMes);

public sealed record PlaInusualDto(
    int PlaInusualId,
    int OficinaId,
    int PersonaId,
    string? NumeroDocumento,
    string? NombreCompleto,
    int? CreditoId,
    string Calificacion,
    string Motivo,
    int UsuarioRegId,
    string? NombreUsuario,
    DateTime FechaReg);

public sealed record RegistrarPlaInusualRequest(
    int OficinaId,
    int PersonaId,
    int? CreditoId,
    string Calificacion,
    string Motivo);

public sealed record RegistrarPlaInusualResponse(
    int PlaInusualId,
    string Calificacion);
