# Módulo Clientes — Legacy → Modern

Spec SSD (actores, aceptación, go-live): [docs/ssd/SSD-05-clientes.md](ssd/SSD-05-clientes.md). Este archivo conserva el mapa fino de pantallas y fases.

Paridad con `Web/Controllers/ClienteController.cs`, `Views/Cliente/Index.cshtml` y `Mantener.cshtml`.

## Mapa

| Legacy | Modern | API |
|--------|--------|-----|
| `/Cliente/Index` (jqGrid) | `/clientes` | `GET /clientes/listar` |
| Búsqueda autocomplete | Debounce en listado + `buscar` | `GET /clientes/buscar` |
| `/Cliente/Mantener/{id}` | `/clientes/nuevo`, `/clientes/editar/:personaId` | `GET /clientes/{id}`, `POST /guardar` |
| `ObtenerClienteDNI` | Blur DNI en alta | `GET /clientes/por-documento` |
| Activar / Bloquear | Botones en formulario | `POST .../activar`, `.../bloquear` |
| Menú **Cliente** (módulo Crédito) | Directo `/clientes` | `resolveSpaPathFromMenuItem` |

## Listado (jqGrid)

- Sin búsqueda (o &lt; 2 caracteres): clientes distintos de **créditos del usuario** (`UsuarioRegId`), como legacy.
- Con búsqueda: catálogo por nombre, DNI, código, celular, email.
- Paginación 15/30/45, orden por columna, doble clic → editar.

## Mantener (2026)

- Google Maps (`GoogleMapLocationPicker`, `VITE_GOOGLE_MAPS_API_KEY`).
- Geocodificación: al elegir distrito, al cargar sin GPS, botón «Ubicar domicilio» (paridad Leaflet legacy).
- ApiPeru vía API (`GET /integraciones/apiperu/dni|ruc`) — token en `ApiPeru:Token` (servidor).
- `existe-documento` = ya es **cliente** (paridad `ValidarClienteDNI`, no solo persona).
- Distrito autocomplete (`distritos-buscar`, depa 5 Ayacucho).
- Cónyuge, estado civil, vivienda, SBS, tope, negocio, depurado, crear persona rápida.
- Tras guardar → listado `/clientes` (paridad redirect legacy).
- UI marca `#114885`; código dividido en `clienteMantenerConstants`, `components/`.

### Diseño Credix (UI profesional)

- `styles/clientes-module.css` — toolbar, tabla, tabs card, footer sticky, vacíos.
- Listado: `CredixCrudPage`, tags Mis créditos / Catálogo, empty state con CTA.
- Ficha: pestañas card, skeleton de carga, barra de acciones fija.

### Estructura frontend mantenible

```
pages/clientes/
  ClientesPage.tsx
  ClienteFormPage.tsx
  ClienteMantenerForm.tsx
  clienteMantenerConstants.ts
  components/
    ClientesTableEmpty.tsx
    ConyugueAutoComplete.tsx
    CrearPersonaRapidaModal.tsx
```

Configurar desarrollo:

```bash
# Web (.env.development)
VITE_GOOGLE_MAPS_API_KEY=...

# API (user-secrets)
dotnet user-secrets set "ApiPeru:Token" "su-token-apiperu" --project Credito.Modern.Api
```

## Archivos

- Web: `ClientesPage.tsx`, `ClienteFormPage.tsx`, `api/clientes.ts`
- API: `ClienteEndpoints.cs` (listar, por-documento) + rutas en `Program.cs`
- Infra: `ClienteListadoReadService`, `ClienteBuscarReadService`, `ClienteWriteService`
