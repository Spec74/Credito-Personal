namespace Credito.Modern.Application.ValorTablas;

public sealed record ParametrosSimuladorDto(string FactorVariable, string FactorFijo);

public sealed record ActualizarParametrosSimuladorRequest(string FactorVariable, string FactorFijo);
