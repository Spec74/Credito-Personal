# Diccionario de indicadores (Vendix / Crediconfiable)

**Fecha de corte institucional:** 2026-10-06.  
**Último mes cerrado (SCM):** 2026-09.  
**Advertencia:** la mora interna **no** es comparable con la morosidad SBS (3,3 % a marzo de 2026): bases, umbrales y numeradores distintos. Declarar siempre la fuente.

Cuando el denominador incluye **cuota + cargo − pagos**, se nombra **saldo total por cobrar**, no «cartera SBS».

---

## 1. Saldo Cartera Mensual (SCM)

| Campo | Definición operacional | Código / SP |
|-------|------------------------|-------------|
| Saldo (crédito) | Σ(PlanPago.Cuota+Cargo) − Σ(MovimientoCaja.ImportePago) con `Operacion='CUO'`, `Estado=1`, `ImportePago>0` | `usp_ListarSaldoCartera` |
| Universo | `Credito.Estado = 'DES'` | idem |
| Mora (importe) | Si días atraso > 0: `DiasAtrazo × (Saldo × 0,01)` vía `ufnCalcularMora` | `ufnCalcularMora` |
| **SaldoCartera** | Σ Saldo | SCM |
| **SaldoMoraCartera** | Σ Saldo donde Mora > 0 | SCM |
| **SaldoVencido** | Dentro de mora: atraso ≤ 90 días | SCM |
| **SaldoMorosidad** | Dentro de mora: atraso > 90 días | SCM |
| Identidad | `SaldoMoraCartera = SaldoVencido + SaldoMorosidad` | Verificado sep-2026 |

**Indicador tesis I9 (canónico recomendado):**  
`I9 = SaldoMoraCartera / SaldoCartera × 100` sobre SCM del **último mes cerrado** (sep-2026 → **18,61 %**).  
Fuente: `CREDITO.SaldoCarteraMensual` / `usp_ListarSaldoCartera`.

**I7 (índice de cartera activa / saldo total por cobrar):** `SaldoCartera` SCM del mes cerrado (sep-2026 → **S/ 1 000 784,16**).

---

## 2. Cierre gerencial

| Campo | Definición | Código |
|-------|------------|--------|
| Capital / saldo | Σ saldos de cuota por **FIFO** de pagos CUO válidos (`IndEntrada=1`, corte de fecha) | `ufn_SaldosCuotasGerenciales` |
| Universo | Créditos `DES` de analistas en `MetaGerencialAnalista` (19 carteras) | `usp_GenerarCierreGerencialMensual` |
| MoraGeneral / vencidos | Σ saldo FIFO de cuotas con `FechaVencimiento < FechaCorte` | `ufn_MetricasGerencialesActuales` |
| Especiales | 3 carteras `ESPECIAL` (ago-2026: S/ 142 968,23; 168 clientes) | Detalle cierre |
| Productivas | 16 carteras; vencidos ~2,4 % del capital productivo | Detalle cierre |

**Ago-2026 cerrado:** Capital S/ 1 012 148,27; MoraGeneral S/ 163 507,11 → **16,15 %** (~16,2 %).

---

## 3. SCM vs cierre (discrepancia constante)

| Periodo | Δ Saldo (SCM − Cierre) | Δ Clientes |
|---------|------------------------|------------|
| 2026-07 | S/ 3 219,50 | 20 |
| 2026-08 | S/ 3 219,50 | 20 |

**Causa (definición, no error aritmético):** SCM suma saldo-crédito con mora por `FechaVencimiento` del crédito + `ufnCalcularMora`; cierre suma residual FIFO de cuotas vencidas solo en 19 metas; filtros de pago (`IndEntrada`) y alcance distintos.

---

## 4. Reporte de morosidad empresarial

- Universo: `Estado='DES'`, `IndIrrecuperable=0`, cuotas `PEN` vencidas.  
- Tipologías: SIN_PAGO / NUNCA_PAGO / DEJO_PAGAR / PAGA_CON_ATRASO.  
- SP: `usp_MorosidadEmpresa` (deploy 2026-10-07).  
- **No usar** su stock de S/ como I9 sin declarar que es otra base.

---

## 5. Tablero gerencial (dashboard admin)

| KPI UI | Fórmula | Nota |
|--------|---------|------|
| % clientes en mora (analista) | `ClientesMora / TotalClientes × 100` | Conteo de personas |
| % saldo en mora (SPA) | `saldoMoraCartera / saldoCartera × 100` | Residual PEN vencido |
| SP legado | `usp_DashboardAdminResumen` (semántica cercana a SCM) | Puede coexistir con C# |

Cifras tipo «63,3 %» del tablero **no** son I9 SCM ni mora SBS.

---

## 6. Indicadores tesis Y (alineación)

| Id | Nombre | Definición única para contraste pretest–postest | Fuente |
|----|--------|-----------------------------------------------|--------|
| I7 | Saldo total por cobrar (vigente) | `SUM(SaldoCartera)` SCM mes cerrado | SCM |
| I9 | Ratio saldo en mora (SCM) | `SaldoMoraCartera / SaldoCartera × 100` | SCM |
| I10 | Tasa de recuperación | Monto cobrado del mes / vencido exigible del mes (definir mes cerrado; preferir campos de cierre `MontoCobradoMes` vs vencidos si se usa cierre) | Cierre o ficha operativa — ver 03_PENDIENTES |
| I11 | Detección temprana | Solo postest (módulo X4); pretest = 0 | Modelo ML |
| I13 | Precisión información | Muestra Cochran sobre **N = 77 308** desembolsados; n = 383 | Auditoría de expedientes |

---

## 7. Línea de tiempo de piezas analíticas (código)

| Pieza | Evidencia en repo | ¿Parte de la intervención de tesis? |
|-------|-------------------|-------------------------------------|
| SCM / `usp_ListarSaldoCartera` | Legacy migrado | **Previo** (línea base descriptiva) |
| Tablero admin/gerencial | Commits ~2026-09-16 | **Durante** ventana 2026 — tratar como componente X3 |
| Cierre gerencial | 2026-09-23 cutover | **Durante** 2026 — componente de consolidación |
| Morosidad empresa | 2026-10-07 | Durante / cercano al corte |

El pretest debe describir el estado **antes** de tablero+cierre+datamart BI; si la fecha exacta de «antes» no está en actas, ver `03_PENDIENTES.md`.
