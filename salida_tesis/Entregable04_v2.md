# Entregable 04_v2 — Cap. III (3.1 a 3.5) corregido

> Conserva el diseño aprobado (experimental–prospectivo–longitudinal–analítico; nivel aplicativo; preexperimental G: O₁ X O₂; muestra censal de indicadores). Se corrigen coherencias y cifras.

---

## 3.1.1 Tipo — nota de coherencia (añadir tras «experimental»)

El estudio fue **experimental** en el sentido de Supo (2014) —existe intervención del investigador sobre X— y, a la vez, de **diseño preexperimental** en la tipología de Campbell y Stanley (1963) / Hernández-Sampieri y Mendoza (2018), porque hay un solo grupo sin control ni aleatorización (G: O₁ X O₂). No hay contradicción: «experimental» clasifica la intervención; «preexperimental» clasifica el esquema de control de validez interna.

---

## 3.3 Diseño — O₁ / pretest (ajuste)

Se midieron los indicadores en la situación previa a la intervención completa de BI+IA (O₁). Como línea base **descriptiva verificable** de saldos y mora interna se empleó el Saldo Cartera Mensual (serie desde julio de 2023; mes cerrado de referencia setiembre 2026). El tablero gerencial (~setiembre 2026) y el cierre gerencial (julio–agosto 2026) forman parte del ecosistema analítico reciente; su inclusión o exclusión en O₁ debe quedar fijada en el protocolo de medición (véase `03_PENDIENTES.md`). **No** se afirma, sin evidencia, que O₁ consistiera solo en «hojas de cálculo» como único instrumento de la empresa: el sistema Vendix ya generaba reportes RDLC y SCM antes del datamart propuesto. La frase «hojas de cálculo utilizadas antes de la intervención» se reformula como: *fuentes complementarias o consolidaciones paralelas eventuales* [SUSTENTAR CON EVIDENCIA EXTERNA].

Periodo del título («2026») = ventana de implementación y contraste pretest–postest; la base histórica analizada abarca desembolsos desde **2019-12-22** hasta **2026-10-06**.

---

## 3.4.2 Población — cifras

La base de datos Vendix, corte al 6 de octubre de 2026, contenía 6460 clientes, 77 388 expedientes de crédito, **77 308 con fecha de desembolso**, 1 609 858 cuotas y 1 655 421 movimientos de caja (Inversiones Crediconfiable S.A.C., 2026).

---

## 3.4.4 Cálculo I13 — población exacta y n

**Población para la verificación de precisión (I13):** expedientes con `FechaDesembolso IS NOT NULL` y fecha ≤ 2026-10-06 → **N = 77 308** (no 77 388).

Fórmula de Cochran (1977) para población finita, Z = 1,96, p = q = 0,5, e = 0,05:

\[
n = \frac{N Z^{2} p q}{e^{2}(N-1)+Z^{2}pq} \approx 382{,}28 \Rightarrow n = 383
\]

Con N = 77 388 el resultado redondeado también es 383; se documenta N = 77 308 por rigor de definición. Software: Python 3 / SciPy (reproducible).

**Tabla 6 (reemplazo fila I13)**

| Propósito | Parámetros | Resultado |
|-----------|------------|-----------|
| Precisión I13 | N = 77 308 desembolsados; Z = 1,96; p = q = 0,5; e = 0,05 | n = 383 expedientes |

---

## 3.5 Muestreo

Sin cambio de técnica (censal intencional de indicadores; aleatorio simple solo I13). Actualizar N en la justificación de I13.

---

## Tabla 9 — filas actualizadas

| Pregunta | Estado | Evidencia |
|----------|--------|-----------|
| ¿Experimental vs preexperimental coherentes? | SÍ | Nota 3.1.1 |
| ¿Población con N desembolsados? | SÍ | N = 77 308; n = 383 |
| ¿«Solo Excel» en O₁? | Corregido | Reportes/SCM previos; Excel paralelo [SUSTENTAR] |
| ¿Verbo OG vs aplicativo? | OBS. | Confirmar con docente |
