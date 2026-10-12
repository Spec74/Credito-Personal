# Prompt adaptado (fuente de verdad operativa)

## Cambios respecto del prompt original

| Punto | Ajuste |
|-------|--------|
| Entregables | **5** archivos (01–04 + Anexo1), no «4». |
| Formato de salida | **Markdown** (`.md`) con sufijo `_v2`, a petición del autor. |
| Fecha de corte | **Única:** `2026-10-06` (máximo `FechaDesembolso` y `MovimientoCaja.FechaReg` en BD `CREDITO` local). |
| Mes cerrado para indicadores mensuales | **Setiembre 2026** (nunca octubre 2026: mes en curso en SCM). |
| Población I13 (Cochran) | Expedientes con `FechaDesembolso IS NOT NULL` → **N = 77 308** (no 77 388). |
| Mora / «cartera» | Una definición canónica por indicador; se declara diferencia vs SBS; «saldo total por cobrar» cuando incluye cuota+cargo. |
| Credenciales | No se documentan en ningún entregable (solo usadas en sesión local de verificación). |
| G1–G5 GPS | **Fuera de alcance** de este paquete (acordado: después). |

## Motor verificado

- SQL Server 2025 · base `CREDITO` · solo `SELECT`.
- Código: Vendix / Credito.Modern (SPs SaldoCartera, CierreGerencial, MorosidadEmpresa, dashboards).

## Orden de sincronización (cumplido)

1. Cifras y definiciones → evidencia SQL + diccionario.  
2. Entregable01 + Entregable02.  
3. Propagación a Entregable03.  
4. Entregable04 + Anexo1.  
5. Matriz de cambios + pendientes.
