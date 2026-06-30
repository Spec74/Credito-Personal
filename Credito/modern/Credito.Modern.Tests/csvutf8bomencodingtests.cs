using Credito.Modern.Application;

namespace Credito.Modern.Tests;

public sealed class CsvUtf8BomEncodingTests
{
    [Theory]
    [InlineData("=2+2", "'=2+2")]
    [InlineData("+SUM(A1:A2)", "'+SUM(A1:A2)")]
    [InlineData("-10+20", "'-10+20")]
    [InlineData("@cmd", "'@cmd")]
    [InlineData("   =2+2", "'   =2+2")]
    public void EscapeField_neutraliza_formulas_de_excel(string value, string expected)
    {
        Assert.Equal(expected, CsvUtf8BomEncoding.EscapeField(value));
    }

    [Fact]
    public void EscapeField_neutraliza_formula_y_mantiene_escape_csv()
    {
        Assert.Equal("\"'=1,2\"\"3\"", CsvUtf8BomEncoding.EscapeField("=1,2\"3"));
    }
}
