# Plan técnico — 004-alcance-red

**Spec Kit:** `/speckit.plan` · 2026-10-24

## Enfoque

Un helper (`SesionOficina.OficinaLectura`) + el resolver de reportes + caja maestro + tablero. Sin USP.

```
1. Spec + research (esta carpeta)
2. CobroDiarioReportAccess: sucursal siempre JWT
3. Caja maestro list/write por sede
4. Dashboard admin ?sedeId=
5. SPA: useEsCentral, cobranza, maestro cajas, /inicio
6. SSD-01/03/07 + bitácora
```

## Archivos

| Pieza | Archivo |
|-------|---------|
| Lectura | `Api/auth/sesionoficina.cs` |
| Informes cobro | `Api/credito/cobrodiarioreportaccess.cs` + callers cobranza/cobro-diario/ruta |
| Caja | `cajamaestro*`, `Api/caja/cajamaestroendpoints.cs` |
| Tablero | `dashboardadminendpoints.cs`, `api/dashboard.ts`, `AdminDashboardPage.tsx` |
| SPA | `hooks/useEsCentral.ts`, `CobranzaPagosPage.tsx`, `CajaMaestroPage.tsx` |

## Riesgos

- Central con rol de reporte sigue pudiendo `oficinaId` null = red (paridad MVC, solo Central).
- Informes con oficina oculta no ganan selector en este PR.
