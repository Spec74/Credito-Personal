# SSD-09 — Maestros (catálogos)

**Estado:** as-built · 2026-09-11  
**Código:** `/maestros/*` · `/api/v1/marcas`, `/modelos`, `/tipos-articulo`, `/articulos`, `/almacenes`  
**Fuente legacy:** `MarcaController`, `ModeloController`, `TipoArticuloController`, `ArticuloController`, `AlmacenController` (mantenimiento)  
**Doc de ingeniería:** [ui_modernizacion_modulos.md](../ui_modernizacion_modulos.md)

## 1. Propósito y actores

Mantener catálogos de producto (marca → modelo → tipo → artículo → almacén) para ventas y stock.

| Rol | Uso |
|-----|-----|
| Administrador | Guardar y activar |
| Operación (menú MAESTRO) | Consultar gestión |
| Ventas / almacén | Consumen el catálogo (SSD-10 / SSD-11) |

## 2. Alcance

**Entra**

- Hub `/maestros` y CRUD: marcas, modelos, tipos de artículo, artículos, almacenes
- Lecturas de combo usadas por otras pantallas (`GET /marcas`, `/modelos`, … en `maestroreadendpoints`)

**No entra**

- Oficinas → SSD-08 (`/mantenimiento/oficinas`)
- Maestro de cajas → SSD-03
- Lista de precios (CRUD e informe) → SSD-10 (`/ventas/lista-precios`)
- Movimientos de almacén (entrada/salida/kardex) → SSD-11
- Clientes / personas → SSD-05

## 3. Paridad MVC

| Legacy | SPA | Observación |
|--------|-----|-------------|
| `/Marca` | `/maestros/marcas` | Módulo MAESTRO |
| `/Modelo` | `/maestros/modelos` | |
| `/TipoArticulo` | `/maestros/tipos-articulo` | |
| `/Articulo` | `/maestros/articulos` | |
| `/Almacen` (maestro) | `/maestros/almacenes` | Distinto de Reportes → Almacén |
| Padre MANTENIMIENTO / MAESTRO | — | No se mapea a hubs |

## 4. Contrato de datos

Tablas `MAESTRO.Marca`, `Modelo`, `TipoArticulo`, `Articulo`, `Almacen` (y SPs de guardar/activar del MVC). El C# no inventa precios ni existencias aquí.

Artículos: gestión con filtro modelo/tipo; activar. Detalle de series/kardex es operación de almacén.

## 5. API y SPA

Patrón por entidad: `GET .../gestion` (`CreditoUser`), `POST .../guardar` y `POST .../{id}/activar` (`CreditoRolAdministrador`).

| Recurso | Pantalla |
|---------|----------|
| `/api/v1/marcas/*` | `/maestros/marcas` |
| `/api/v1/modelos/*` | `/maestros/modelos` |
| `/api/v1/tipos-articulo/*` | `/maestros/tipos-articulo` |
| `/api/v1/articulos/gestion` + guardar/activar | `/maestros/articulos` |
| `/api/v1/almacenes/*` (maestro) | `/maestros/almacenes` |

Combos de solo lectura (`GET /marcas` sin `/gestion`) alimentan ventas e informes.

## 6. Seguridad

- Hub `/maestros` habilita `/maestros/*`.
- Escritura admin; listados con JWT de usuario.
- Ítem Reportes → Almacén no es este CRUD (va a `/reportes/almacen`, SSD-07 / SSD-11).

## 7. Criterios de aceptación

- [ ] Ítem Marcas abre `/maestros/marcas`; alta y activar persisten como el MVC.
- [ ] Modelo exige marca; artículo exige tipo/modelo según validación del legado.
- [ ] Sin menú de catálogo, el padre MANTENIMIENTO no abre `/maestros/articulos`.
- [ ] Almacén maestro no es kardex ni entrada/salida.
- [ ] Lista de precios no vive bajo `/maestros`.

## 8. Desviaciones

Ninguna financiera propia. Relatos de menú/iconos (2026-09-09) en [BITACORA](../migration/BITACORA-DESVIACIONES.md) si el ítem no resolvía SPA.

## 9. Pruebas y evidencia

- API: `maestroscrudendpointtests`, `articuloscrudendpointtests`, `marcaendpointtests`, `modeloendpointtests`, `tipoarticuloendpointtests`, `articuloendpointtests`, `almacenendpointtests`
- SPA: `resolvespapathfrommenuitem.test.ts` (Marcas → `/maestros/marcas`)
- Smoke: crear marca de prueba, usarla en un artículo, verla en combo de venta

## 10. Go-live

Listo en Development. Cutover: un alta de marca/artículo en preprod y comprobar que venta rápida (SSD-10) la lista.
