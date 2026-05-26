using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;

namespace Credito.Modern.Tests;

public sealed class CobranzaPagosExcelEndpointTests : IClassFixture<CobranzaPagosExcelWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CobranzaPagosExcelEndpointTests(CobranzaPagosExcelWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Rpt_cobranza_pagos_excel_con_token_devuelve_xlsx_abierto_por_closedxml()
    {
        if (string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var tokenRes = await _client.PostAsJsonAsync("/api/v1/dev/token", new { usuarioId = 1, oficinaId = 1 });
        Assert.Equal(HttpStatusCode.OK, tokenRes.StatusCode);
        using var doc = await JsonDocument.ParseAsync(await tokenRes.Content.ReadAsStreamAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/credito/rpt-cobranza-pagos-excel?usuarioId=1&oficinaId=1");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains(
            "spreadsheetml",
            res.Content.Headers.ContentType?.MediaType ?? "",
            StringComparison.OrdinalIgnoreCase);

        var bytes = await res.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 100);
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x4B, bytes[1]);

        using var ms = new MemoryStream(bytes);
        using var wb = new XLWorkbook(ms);
        Assert.Equal("Cobranza", wb.Worksheet(1).Name);
        Assert.Contains("REPORTE DE COBRANZA", wb.Worksheet(1).Cell(1, 1).GetString());
    }
}
