using Credito.Modern.Application.CreditoPlanes;
using Credito.Modern.Application.Maestros;
using Credito.Modern.Application.Oficinas;
using Credito.Modern.Application.UsuariosAdmin;

namespace Credito.Modern.Tests.Fakes;

internal sealed class FakeRptCobroDiarioDetalleReadService : IRptCobroDiarioDetalleReadService
{
    private static readonly List<RptCobroDiarioDetalleRowDto> SampleRows =
    [
        new()
        {
            Nro = 1,
            Cliente = "CLIENTE PRUEBA",
            FormaPago = "D",
            MontoCredito = 500m,
            Interes = 10m,
            MontoTotal = 510m,
            FechaPrimerPago = new DateTime(2024, 1, 15),
            FechaVencimiento = new DateTime(2024, 6, 15),
            TotalPago = 200m,
            Saldo = 310m,
            Pagos = "10.00 (15/01/2024),0.00 (15/02/2024)",
        },
    ];

    public Task<List<RptCobroDiarioDetalleRowDto>> ListarAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(SampleRows);

    public Task<List<RptCobroDiarioDetalleRowDto>> ListarCobranzaAsync(
        int? usuarioId,
        int? oficinaId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(SampleRows);
}

internal sealed class FakeUsuarioAdminReadService : IUsuarioAdminReadService
{
    public Task<UsuarioGestionPageDto> ListGestionAsync(
        string? buscar,
        int page,
        int pageSize,
        bool incluirInactivos,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<UsuarioReporteGestorDto>> ListReporteGestoresAsync(
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<UsuarioPersonaDetalleDto?> GetDetalleAsync(int usuarioId, CancellationToken cancellationToken = default) =>
        Task.FromResult<UsuarioPersonaDetalleDto?>(
            new UsuarioPersonaDetalleDto(
                usuarioId,
                "gestor.test",
                true,
                1,
                "Apellido",
                "Materno",
                "Nombre",
                "Gestor Test",
                "00000000",
                null,
                null,
                null,
                null,
                null,
                []));

    public Task<PersonaPorDniDto?> GetPersonaPorDniAsync(
        string numeroDocumento,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<ValidarDniResponse> ValidarDniAsync(
        string numeroDocumento,
        int? usuarioId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<List<RolAsignacionDto>> GetRolesAsignacionAsync(
        int usuarioId,
        int oficinaId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

internal sealed class FakeOficinaReadService : IOficinaReadService
{
    public Task<List<OficinaListItemDto>> GetActivasAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(
            new List<OficinaListItemDto>
            {
                new() { OficinaId = 1, Denominacion = "Oficina Test", Estado = true },
            });

    public Task<List<OficinaAdminListItemDto>> ListGestionAsync(
        bool incluirInactivos,
        string? buscar = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
