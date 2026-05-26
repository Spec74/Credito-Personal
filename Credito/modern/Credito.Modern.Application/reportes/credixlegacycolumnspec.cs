namespace Credito.Modern.Application.Reportes;

public enum CredixColumnAlign
{
    Left,
    Center,
    Right,
}

public sealed record CredixLegacyColumnSpec(
    string CsvName,
    string DisplayLabel,
    CredixColumnAlign Align = CredixColumnAlign.Left,
    float RelativeWeight = 1f);
