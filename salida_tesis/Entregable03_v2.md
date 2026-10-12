# Entregable 03_v2 — Caps. I–II + 2.5 / 2.6 (sincronización)

## Regla de sincronización (cumplida en esta versión)

| Bloque | Fuente de verdad | Acción |
|--------|------------------|--------|
| Capítulo I (1.1–1.4) | **Entregable01_v2.md** | Sustituir íntegramente el Cap. I antiguo del Ent03 (texto con «22 distritos», Excel, nulos, mora octubre, «sin indicadores»). |
| Capítulo II (2.1–2.4) | **Entregable02_v2.md** + cuerpo Ent02 | Copiar Cap. II del Ent02 y aplicar correcciones de fuentes de la v2. |
| 2.5 Hipótesis | Ent03 actual | Sin cambio de redacción HG/HE1–HE4. |
| 2.6 Indicadores | Ent03 + diccionario | Actualizar I7, I9, I10 y notas de pretest (abajo). |

---

## 2.6 — Definiciones operacionales corregidas (Y2 / I7)

### I7. Índice / saldo de cartera activa → **Saldo total por cobrar (vigente)**

- **Fórmula:** `SUM(SaldoCartera)` del SCM del mes cerrado.  
- **Pretest línea base institucional (descriptiva):** setiembre 2026 = **S/ 1 000 784,16** (Inversiones Crediconfiable S.A.C., 2026).  
- **No llamar «cartera SBS».**

### I9. Ratio de cartera atrasada (CAR) → **Ratio de saldo en mora (SCM)**

- **Fórmula única:** `SaldoMoraCartera / SaldoCartera × 100` (SCM).  
- **Valor de referencia mes cerrado:** setiembre 2026 = **18,61 %**.  
- **Declarar:** distinta de mora SBS, de MoraGeneral del cierre (16,2 % ago-2026) y del % del tablero (conteo o residual PEN).  
- Sustituir en Tabla 4 el texto «Capital en mora / capital total» por la fórmula SCM anterior.

### I10. Tasa de recuperación de créditos

- Mantener dirección «aumenta».  
- **Definición provisional:** monto cobrado en el mes / vencido exigible del mismo mes cerrado, según ficha o campos de cierre (`MontoCobradoMes`).  
- Confirmación gerencial: `03_PENDIENTES.md` ítem 5.

### Pretest de I4 / I5 / I6 / I14

- Codificar 0 solo si O₁ es **anterior** al tablero y al módulo ML.  
- Si el pretest usa ya el tablero de setiembre 2026, **no** codificar I4 = 0 %; ver pendiente de fecha O₁.

### Retirar del Cap. I embebido (lista de control)

- [ ] «22 distritos» como ámbito  
- [ ] Límite Excel como Dx  
- [ ] % nulos distrito/nacimiento/aval/SBS como defectos  
- [ ] «5828 créditos cancelados con cuotas impagas» (recalcular solo si se redefine con estados; no reintroducir sin SELECT)  
- [ ] Mora de octubre como mes cerrado  
- [ ] «La gerencia no cuenta con indicadores»

---

## Tabla 8 — filas adicionales de registro

| Ubicación | Antes (Ent03 antiguo) | Después |
|-----------|----------------------|---------|
| 1.1.1 | 22 distritos; Excel; mora oct | Texto Ent01_v2 |
| 1.1.2 | Nulos + sin indicadores | Dx de definiciones / historia / metas NULL / sin ML |
| 2.6 I9 | «capital en mora / capital total» ambiguo | Ratio SCM SaldoMoraCartera/SaldoCartera |
| 2.6 I7 | «cartera activa» ambiguo | Saldo total por cobrar SCM |

---

## 2.5 — Sin cambios de fondo

HG y HE1–HE4 se mantienen. La Tabla 2 de dirección esperada actualiza la justificación de I9: «Menor **saldo en mora SCM** respecto del saldo total por cobrar».
