# Anexo 1_v2 — Matriz de consistencia

*Tema: Implementación de Inteligencia de Negocios e Inteligencia Artificial para optimizar decisiones financieras y operativas en Inversiones Crediconfiable S.A.C., Ayacucho, 2026*

*Sincronizada con Entregable01_v2 (1.2–1.3), Entregable03_v2 (2.5–2.6) y Entregable04_v2 (metodología).*

## Problemas / objetivos / hipótesis

| N.° | Problema | Objetivo | Hipótesis |
|-----|----------|----------|-----------|
| GENERAL | ¿En qué medida influye, mediante la implementación de un modelo de Inteligencia de Negocios e Inteligencia Artificial, en las decisiones financieras y operativas en Inversiones Crediconfiable S.A.C., Ayacucho, durante el 2026? | Determinar la influencia, mediante la implementación de un modelo de Inteligencia de Negocios e Inteligencia Artificial, en las decisiones financieras y operativas en Inversiones Crediconfiable S.A.C., Ayacucho, durante el 2026. | La implementación de un modelo de Inteligencia de Negocios e Inteligencia Artificial mejora significativamente las decisiones financieras y operativas en Inversiones Crediconfiable S.A.C., Ayacucho, durante el 2026. |
| PE1 / OE1 / HE1 | ¿En qué medida influye, mediante la implementación de la integración de datos (procesos ETL), …? | Determinar la influencia, mediante la implementación de la integración de datos (procesos ETL), … | La implementación de la integración de datos mediante procesos ETL mejora significativamente … |
| PE2 / OE2 / HE2 | ¿En qué medida mejora, con el uso del modelado dimensional (datamart), …? | Determinar la mejora, con el uso del modelado dimensional (datamart), … | La implementación del modelado dimensional mediante un datamart mejora significativamente … |
| PE3 / OE3 / HE3 | ¿Cuál es el efecto, al aplicar la visualización analítica (dashboards), …? | Determinar el efecto, al aplicar la visualización analítica (dashboards), … | La implementación de la visualización analítica mediante dashboards mejora significativamente … |
| PE4 / OE4 / HE4 | ¿En qué medida influye, mediante la implementación de un módulo predictivo de riesgo crediticio basado en Machine Learning, …? | Determinar la influencia, mediante la implementación de un módulo predictivo de riesgo crediticio basado en Machine Learning, … | La implementación de un módulo predictivo de riesgo crediticio mediante Machine Learning mejora significativamente … |

*(Enunciados completos idénticos a Ent01/Ent03; omitidos aquí solo por espacio en PE1–PE4 — copiar literal desde Entregable01_v2.)*

## Variables, dimensiones e indicadores

| Variable | Dimensión | Indicadores |
|----------|-----------|-------------|
| X — BI e IA | X1 ETL | I1 tiempo carga; I2 tasa éxito ETL |
| | X2 Datamart | I3 fragmentación remanente |
| | X3 Dashboards | I4 disponibilidad analítica |
| | X4 ML riesgo | I5 AUC; I6 F1 |
| Y — Decisiones fin./op. | Y1 Gestión de cartera | I7 **saldo total por cobrar (SCM)**; I8 tiempo consolidación |
| | Y2 Control de morosidad | I9 **ratio saldo en mora SCM**; I10 recuperación; I11 detección temprana |
| | Y3 Desempeño institucional | I12 tiempo reportes; I13 precisión (N=77 308, n=383); I14 explicabilidad IA |

## Metodología

- Tipo: experimental (intervención) / diseño **preexperimental** pretest–postest (G: O₁ X O₂); nivel aplicativo (Supo).  
- **Base de datos de referencia (Vendix, corte 2026-10-06):** 6460 clientes; 77 388 expedientes; **77 308 con desembolso**; 1 609 858 cuotas; 1 655 421 movimientos.  
- Mes cerrado de referencia para I7/I9: **setiembre 2026**.  
- I9 ≠ morosidad SBS.
