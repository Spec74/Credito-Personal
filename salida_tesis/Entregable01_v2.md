# Entregable 01_v2 — Capítulo I (secciones corregidas)

> Texto base del Entregable 01; **amarillo conceptual** = cambio respecto de la versión pegada en el chat. Cifras: corte **2026-10-06**; mes cerrado SCM **setiembre 2026**. Cita institucional: (Inversiones Crediconfiable S.A.C., 2026).

---

## 1.1.1 Contextualización — párrafo local (reemplazo completo)

Finalmente, en el plano local, la problemática se expresa en Inversiones Crediconfiable S.A.C., entidad de microcrédito cuya base de datos transaccional Vendix, con corte al 6 de octubre de 2026, registra desde diciembre de 2019 un total de 6460 clientes, 77 388 expedientes de crédito (77 308 con fecha de desembolso, por S/ 49 253 123,32), 1 609 858 cuotas generadas y 1 655 421 movimientos de caja. Los 5794 clientes con crédito desembolsado residen principalmente en cinco distritos urbanos de Huamanga (78,0 %; 4519 clientes) y en Huanta (14,8 %; 857 clientes); el 6,5 % no tiene distrito registrado y el resto se dispersa en otros distritos del catálogo nacional (Inversiones Crediconfiable S.A.C., 2026). Se trata de créditos de corto ciclo: el monto mediano de desembolso es de S/ 500, el plazo mediano es de 26 cuotas y el 73,6 % de los expedientes desembolsados tiene forma de pago diaria. Según el reporte mensual de saldos del sistema (Saldo Cartera Mensual), el **saldo total por cobrar** de créditos vigentes creció 52,4 % entre julio de 2023 y setiembre de 2026, de S/ 656 611,01 a S/ 1 000 784,16, y la relación entre el saldo en mora (definición interna SCM) y ese saldo representó **18,61 %** en setiembre de 2026, último mes cerrado (Inversiones Crediconfiable S.A.C., 2026). Esta ratio interna **no** es comparable con la morosidad del sistema financiero reportada por la SBS, porque usa otra base de cálculo (véase diccionario de indicadores).

*Nota.* Se omite octubre de 2026 en series mensuales porque el mes no está cerrado a la fecha de corte.

---

## 1.1.2 Diagnóstico (Dx) — reemplazo completo

**Diagnóstico (Dx).** Actualmente, Inversiones Crediconfiable S.A.C. gestiona su información crediticia con el sistema Vendix, orientado principalmente al procesamiento transaccional (OLTP). Su base de datos, de aproximadamente 327 MB, se organiza en 67 tablas relacionales, de las cuales 46 contienen registros; el plan de pagos y los movimientos de caja concentran la mayor parte del volumen transaccional. La empresa **sí dispone** de reportes operativos y de un tablero gerencial, además de un cierre gerencial con exportación; por tanto, no es correcto afirmar que «la gerencia no cuenta con indicadores».

Las limitaciones verificables son otras: (a) coexisten **varias definiciones de mora y de saldo** (Saldo Cartera Mensual, cierre gerencial por cuotas FIFO, reporte de morosidad empresarial y tablero), que producen ratios distintos para el mismo periodo —por ejemplo, en setiembre de 2026 el SCM registra 18,61 % de saldo en mora sobre el saldo total por cobrar, mientras que el cierre de agosto de 2026 reporta 16,2 % bajo definición de cuotas vencidas FIFO, y ambos difieren de forma constante en S/ 3 219,50 y 20 clientes en julio y agosto de 2026 por alcance y fórmula, no por un error aritmético aislado—; (b) la historia consolidada del SCM inicia en julio de 2023 (cerca de 43 meses después del primer desembolso de diciembre de 2019) y el cierre gerencial solo comprende julio y agosto de 2026; (c) las 19 carteras de meta gerencial están activas en filas, pero **sin valores de meta configurados** (campos de meta en NULL); (d) persisten consolidaciones y contrastes manuales paralelos al sistema [SUSTENTAR CON EVIDENCIA EXTERNA: entrevista o hoja de definiciones del cierre]; y (e) **no existe** un módulo predictivo de riesgo crediticio que anticipe el incumplimiento. El catálogo de distritos nacional (1832 registros) y las tablas de módulos no usados no se consideran deficiencias de calidad por sí solas (Inversiones Crediconfiable S.A.C., 2026).

---

## 1.1.2 Pronóstico (Px)

**Pronóstico (Px).** Si la empresa mantiene definiciones no unificadas de mora y saldo, una historia consolidada corta y la ausencia de capacidad predictiva, la asimetría de información para el control de riesgo continuará. Ello puede dificultar el monitoreo oportuno del saldo en mora, la recuperación y el comportamiento por zona o segmento. En el mediano plazo, podría traducirse en decisiones crediticias menos precisas, deterioro de la liquidez institucional y sobrecostos operativos. [SUSTENTAR CON EVIDENCIA EXTERNA cualquier cuantificación de «demora en días hábiles».]

---

## 1.1.2 Control de pronóstico (CPx) — reemplazo del cierre del párrafo

**Control de pronóstico (CPx).** Frente a esta situación, se propone el diseño e implementación de un modelo de Inteligencia de Negocios e Inteligencia Artificial sustentado en el enfoque de modelado dimensional de Kimball y Ross (2013), complementado con un módulo predictivo de riesgo crediticio basado en Machine Learning. La propuesta se diferencia de los reportes y del tablero ya existentes por: (1) un **diccionario único de indicadores** alineado a una sola definición operacional por métrica; (2) un **datamart histórico** que unifique saldos, mora, cobranzas y colocaciones con trazabilidad ETL; (3) dashboards analíticos que consumen ese diccionario (sin presentarlos como la primera visualización de la empresa); y (4) el entrenamiento, validación y explicabilidad de un modelo de clasificación supervisada que estima la probabilidad de incumplimiento por cliente (Mathibela y Maposa, 2026). Con ello se busca elevar la trazabilidad, anticipar el riesgo crediticio y optimizar las decisiones financieras y operativas con base en evidencia empírica.

---

## 1.1.3 Delimitación

**Dimensión espacial.** El estudio se desarrolla en Inversiones Crediconfiable S.A.C., con operaciones registradas en dos oficinas (Oficina Principal y Sucursal Ayacucho). La cartera de clientes con crédito desembolsado se concentra en cinco distritos urbanos de Huamanga y en Huanta (92,8 % de esos clientes), departamento de Ayacucho; no se afirma que la empresa «opere en 22 distritos», cifra que solo describe la dispersión residual de residencias en el catálogo geográfico.

**Dimensión temporal.** La investigación utiliza información histórica disponible en la base de datos institucional hasta el 6 de octubre de 2026; el diseño, desarrollo, validación e implementación del modelo se ejecutaron durante los semestres académicos 2026-I y 2026-II. Los indicadores mensuales de línea base usan el último mes cerrado (setiembre de 2026).

**Dimensión muestral.** El universo de análisis está conformado por la base de datos histórica de la empresa, integrada por 6460 clientes, 77 388 expedientes de crédito (77 308 con desembolso), 1 609 858 cuotas generadas y 1 655 421 movimientos de caja, con corte al 6 de octubre de 2026 (Inversiones Crediconfiable S.A.C., 2026).

*(Dimensiones social y conceptual: sin cambio de fondo respecto del Entregable 01, salvo coherencia con X1–X4.)*

---

## 1.4.3 Justificación práctica y social (ajuste)

En el plano práctico, la investigación busca resolver un problema real de la empresa: la **dispersión de definiciones** y la limitada historia consolidada para el seguimiento del saldo total por cobrar, la mora interna y las cobranzas, pese a contar ya con reportes y tablero. La implementación del modelo de Inteligencia de Negocios e Inteligencia Artificial permite unificar indicadores críticos, reduce la dependencia de consolidaciones paralelas [SUSTENTAR CON EVIDENCIA EXTERNA el tiempo exacto] y facilita una supervisión más oportuna del desempeño financiero y operativo.

*(Párrafo social + Reglamento UE: se mantienen.)*

---

## 1.1.1 Ámbito nacional — cita SBS

Sustituir la cita (Infobae, 2026) por la fuente primaria SBS cuando esté disponible; mientras tanto:

> …según la Superintendencia de Banca, Seguros y AFP (SBS), a marzo de 2026 el sistema financiero registró una morosidad general de 3,3 %… [SUSTENTAR CON EVIDENCIA EXTERNA: boletín oficial SBS marzo 2026].

---

## Tabla 1 — ajustes de evidencia (filas 1 y 7)

| N.° | Cambio en columna Evidencia |
|-----|-----------------------------|
| 1 | Cifras locales: corte 2026-10-06; SCM sep-2026 18,61 %; sin «22 distritos» como ámbito; sin límite Excel como Dx central. Fuentes: Q01–Q09 de `01_EVIDENCIA.sql`. |
| 7 | Retirar Infobae como fuente primaria SBS; Microsoft s. f. deja de sustentar el Dx (escala opcional en resultados). |

## Tabla 2 — filas nuevas (además de tiempos verbales)

| Ubicación | Antes | Después |
|-----------|-------|---------|
| 1.1.1 local | 22 distritos; mora octubre; límite Excel | Concentración Huamanga/Huanta; solo mes cerrado; sin Excel como Dx |
| 1.1.2 Dx | «gerencia no cuenta con indicadores»; nulos como defectos | Limitaciones: definiciones múltiples, historia corta, metas NULL, sin predictivo |
| 1.1.2 CPx | «dashboards en tiempo real» como novedad absoluta | Diccionario único + datamart + ML; tablero ya existente |
| 1.1.3 espacial | 22 distritos operativos | 2 oficinas; concentración de residencia |

## Tabla 3

Sin cambio de fondo en X4; actualizar nota de verificación institucional a corte 2026-10-06 y a `01_EVIDENCIA.sql`.
