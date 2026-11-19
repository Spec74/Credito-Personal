# Deuda técnica — Credito.Modern

**Fecha:** 2026-11-18 · rama `Eber-migracion`  
**Índice:** [DOCUMENTACION.md](DOCUMENTACION.md)

Este archivo lista **solo** lo que falta para excelencia de mantenimiento/escala.  
El **paquete documental as-built** y la arquitectura por capas están cerrados; esto no bloquea go-live de negocio.

---

## 1. Evidencia operativa (no es hueco de docs)

Las casillas `[ ]` en cada SSD §7 son **criterios escritos al 100%**.  
Marcarlas `[x]` requiere smoke firmado en preprod/prod (humano + ambiente), no más prosa.

Pista: [GUIA-PRUEBAS-GO-LIVE.md](GUIA-PRUEBAS-GO-LIVE.md) · [SMOKE-SIGN-OFF.md](SMOKE-SIGN-OFF.md) · [SSD-00](ssd/SSD-00-cutover.md).

---

## 2. Archivos grandes (partir en sprints)

| Archivo | ~líneas | Acción sugerida |
|---------|---------|-----------------|
| `Api/credito/creditobovedaoperacionendpoints.cs` | ~2800 | `partial`: lecturas / transferencias / caja-chica |
| `Api/credito/creditoreportecreditocarteraendpoints.cs` | ~2200 | Ya hay familia `creditoreporte*`; seguir partiendo por informe |
| `Api/credito/creditocajadiarioendpoints.cs` | ~1800 | Separar cobro / cierre / saldos si crece |
| `Api/ventas/ventasendpoints.cs` | ~1700 | Lecturas vs escrituras |
| `Web/.../CobroBloquePage.tsx` | grande | Extraer hooks / subpaneles |
| `Infrastructure/dashboard/dashboardadminreadservice.cs` | ~900 | Ya partial + `.red.cs` (hecho 2026-11-18) |

---

## 3. Hecho recientemente (no reabrir)

| Ítem | Estado |
|------|--------|
| Tablero Red escalable (1 SQL multi-sede) | Hecho — `dashboardadminreadservice.red.cs` |
| FE Red en archivo propio | Hecho — `AdminRedDashboard.tsx` |
| Guía de lectura del código | Hecho — `COMO-LEER-EL-CODIGO.md` |
| `readme.md` adelgazado | Hecho — inventario en `API-ENDPOINTS-INVENTARIO.md` |
| Ranking/podio/meta analista | Hecho |

---

## 4. Fase C producto (opcional)

- Alertas persistentes / umbrales configurables en tablero
- Mini-comparativo sede vs promedio red en tablero operativo
- Partir el resto de endpoints >1500 líneas
