# SSD-05 — Clientes

**Estado:** as-built · 2026-09-11 · GPS dispositivo 2026-10-10 · `ClienteValidacion` / formRules 2026-09-27 ([SSD-12](SSD-12-validacion.md))  
**Código:** `/clientes`, `/clientes/nuevo`, `/clientes/editar/:personaId` · `/api/v1/clientes/*`  
**Fuente legacy:** `ClienteController`, `Views/Cliente/Index.cshtml`, `Mantener.cshtml`  
**Doc de ingeniería:** [clientes_migracion.md](../clientes_migracion.md)

## 1. Propósito y actores

Alta y ficha de persona/cliente para crédito y prendario.

| Rol | Uso |
|-----|-----|
| Gestor / analista | Buscar, editar, nuevo cliente (DNI/RUC, domicilio, SBS, tope) |
| Cajero | Consulta para cobranza |
| Lectura | Listado / ficha sin escrituras de ciclo (según menú) |

## 2. Alcance

**Entra**

- Listado (mis créditos vs catálogo), mantener, activar/bloquear, depurado, cónyuge, persona rápida
- Validación de documento (ya es cliente, no solo persona)
- ApiPerú y mapa (geocode + **GPS del dispositivo**)
- Persistencia `MAESTRO.Cliente.Latitud/Longitud` (no en Persona)

**No entra**

- Captura GPS masiva en cobro en bloque (campo) → SSD-03
- Consulta de créditos de la persona → SSD-02
- Alta prendaria que reutiliza `/clientes/nuevo` → SSD-06
- Informes clientes inactivos / bloqueados / tope → SSD-07

## 3. Paridad MVC

| Legacy | SPA | Observación |
|--------|-----|-------------|
| `/Cliente/Index` | `/clientes` | jqGrid → `CredixCrudPage` |
| `/Cliente/Mantener/{id}` | `/clientes/nuevo`, `/clientes/editar/:id` | Tras guardar → listado |
| `ValidarClienteDNI` | `GET existe-documento` | Cliente, no solo persona |
| Leaflet domicilio | Google Maps + geocode distrito + GPS dispositivo | Clave `VITE_GOOGLE_MAPS_API_KEY`; `navigator.geolocation` |
| Menú CLIENTE (módulo CREDITO) | `/clientes` | Directo, no hub crédito |

## 4. Contrato de datos

| Procedimiento / tabla | Uso |
|-----------------------|-----|
| Persona / Cliente (MAESTRO) | CRUD ficha |
| `ValidarClienteDNI` equivalente | `existe-documento` |
| Ubigeo `distritos-buscar` | Autocomplete (depa 5 Ayacucho en UI) |
| ApiPerú (HTTP externo) | DNI/RUC; token servidor `ApiPeru:Token` |

No hay cálculo de cuota aquí.

## 5. API y SPA

| Método y ruta | Política | Pantalla |
|---------------|----------|----------|
| `GET /api/v1/clientes/listar`, `GET /buscar` | `CreditoUser` | `/clientes` |
| `GET /api/v1/clientes/{id}`, `POST /guardar` | `CreditoUser` | Formulario |
| `GET por-documento`, `GET existe-documento` | `CreditoUser` | Alta / blur DNI |
| `POST .../activar`, `.../bloquear`, `habilitar-depurado` | `CreditoUser` | Ficha |
| `POST /api/v1/clientes/{personaId}/ubicacion` | `CreditoUser` | Ficha Ubicación / cobro-bloque móvil |
| `GET distritos-buscar`, `GET personas-buscar` | `CreditoUser` | Formulario |
| `POST crear-persona-rapida` | `CreditoUser` | Modal |
| `GET /integraciones/apiperu/dni\|ruc` | User | Consulta documento |

Body ubicación: `{ latitud, longitud }` (decimal). Valida rango Perú; rechaza `(0,0)`. Solo actualiza `MAESTRO.Cliente` (404 si no hay fila Cliente).

Listado sin término (&lt; 2 caracteres): clientes distintos de créditos del `UsuarioRegId` (paridad MVC). Con búsqueda: catálogo.

## 6. Seguridad

- JWT de usuario de oficina; no se inventa `oficinaId` de query para guardar.
- Token ApiPerú solo en servidor.
- Menú CLIENTE habilita `/clientes` y `/clientes/*` (hub children).

## 7. Criterios de aceptación

> **Documentación:** criterios redactados al 100%. Las casillas registran **evidencia de ejecución** (Development / preprod), no huecos de spec.

- [ ] Ítem CLIENTE abre listado; doble clic edita.
- [ ] Sin búsqueda se ven «mis créditos»; con 2+ caracteres, catálogo.
- [ ] DNI ya cliente bloquea alta (`existe-documento`).
- [ ] Guardar redirige a `/clientes`.
- [ ] Activar / bloquear cambian estado como el MVC.
- [ ] Prendario nuevo puede ir a `/clientes/nuevo` y volver con `personaId`.
- [ ] Pestaña Ubicación: «Registrar mi ubicación» / «Actualizar GPS» usa el GPS del dispositivo y persiste lat/lng (edición) o pide Guardar (alta).
- [ ] Sin GPS, el mapa/geocode de domicilio sigue funcionando (paridad).

## 8. Desviaciones

Ninguna financiera propia. Mapa Google vs Leaflet es producto. Relatos de menú/iconos en bitácora 2026-09-09 si aplican al shell.

**GPS 2026-10-10:** captura por dispositivo (`Permissions-Policy: geolocation=(self)`). Corrección de pin malo: ficha cliente (oficina o campo con permiso). En cobro-bloque móvil el botón solo aparece si aún no hay coordenadas (SSD-03). Precisión &gt; ~80 m: aviso suave sin bloquear el guardado. Geocode automático de distrito = vista previa (no se persiste como GPS). `guardar` y `/ubicacion` validan rango Perú.

## 9. Pruebas y evidencia

- API: `clienteendpointstests` (listar, documento, buscar, guardar, activar, bloquear, ubicacion 401/400); `validationrulestests` (`ClienteGpsRules`)
- SPA: `resolvespapathfrommenuitem.test.ts` (Cliente → `/clientes`); `clienteMapaNavegacion.test.ts`
- Smoke: buscar DNI, abrir ficha, guardar domicilio; registrar GPS en Ubicación (HTTPS)

## 10. Go-live

Listo en Development. Preprod: ApiPerú y Maps con secretos de ese entorno.
