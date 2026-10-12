# Matriz de cambios (100 % de modificaciones v2)

| Archivo | Sección | Anterior | Nuevo | Motivo | Evidencia |
|---------|---------|----------|-------|--------|-----------|
| Ent01 | 1.1.1 local | Mora octubre; énfasis Excel | Solo sep-2026 cerrado; sin Excel como eje | Mes en curso prohibido; decisión autor | Q04, Q20 |
| Ent01 | 1.1.1 local | «22 distritos» implícito en versiones previas | Concentración 78 % Huamanga + 14,8 % Huanta | No confundir catálogo con ámbito | Q08–Q09 |
| Ent01 | 1.1.1 | «cartera» ambigua | «saldo total por cobrar» + nota ≠ SBS | Rigor definiciones | Diccionario §1 |
| Ent01 | 1.1.2 Dx | «gerencia no cuenta con indicadores» | Limitaciones: definiciones múltiples, historia corta, metas NULL, sin ML | Tablero/cierre existen | Git 2026-09; Q07; Q15 |
| Ent01 | 1.1.2 Dx | Nulos / tablas vacías / GPS como defectos | Retirados del Dx | Decisión autor + código | — |
| Ent01 | 1.1.2 Dx | Límite Excel central | Retirado (escala opcional) | Excel >1M en modelo datos | Decisión autor |
| Ent01 | 1.1.2 Dx | 365 MB | ~327 MB | Medición allocation_units | Q19 |
| Ent01 | 1.1.2 CPx | Dashboards «en tiempo real» como novedad | Diccionario + datamart + ML; tablero ya existe | Diferenciación real de la tesis | Diccionario §7 |
| Ent01 | 1.1.2 Px | «reportes manuales y tardíos» sin fuente | Marcado [SUSTENTAR…] | Sin evidencia BD | 03_PENDIENTES #3 |
| Ent01 | 1.1.3 espacial | 22 distritos operativos | 2 oficinas; concentración residencia | Oficinas + Q09 | Q16 |
| Ent01 | 1.1.3 muestral | 77 388 sin precisar desembolso | + 77 308 con desembolso | Claridad I13 | Q01 |
| Ent01 | 1.1.1 nacional | Cita Infobae SBS | Primaria SBS o [SUSTENTAR…] | Fuentes primarias | Ent02_v2 |
| Ent01 | Tablas 1–3 | Evidencias antiguas | Actualizadas a corte y SQL | Consistencia | 01_EVIDENCIA.sql |
| Ent02 | Referencias SBS/PRODUCE | Infobae / Andina como primaria | Reemplazo o pendiente | Regla autor | Ent02_v2 |
| Ent02 | Concepto mora | Genérico «cartera atrasada» | SCM vs SBS declarados | Una definición | Diccionario |
| Ent03 | Cap. I completo | Texto antiguo (22 dist., Excel, nulos, oct) | = Ent01_v2 | Sincronización | Ent03_v2 |
| Ent03 | Cap. II | Copia Ent02 | + correcciones fuentes v2 | Sincronización | Ent02_v2 |
| Ent03 | I7 | Cartera activa ambigua | Saldo total por cobrar SCM | Diccionario | Q04 |
| Ent03 | I9 | Capital en mora / capital total | SaldoMoraCartera/SaldoCartera SCM | Una mora | Q04 |
| Ent03 | I10 | Sin fuente única | Provisional + pendiente | Falta decisión | 03_PENDIENTES #5 |
| Ent04 | 3.1.1 | Experimental vs preexperimental sin nexo | Nota de coherencia Supo/Campbell | Rigour metodológico | Ent04_v2 |
| Ent04 | 3.3 O₁ | «solo hojas de cálculo» | SCM/reportes previos; Excel paralelo [SUSTENTAR] | Evidencia código | Diccionario §7 |
| Ent04 | 3.3 periodo | 2026 vs base 2019–2026 | Ventana tesis vs historia BD explícitas | Coherencia | Q01 |
| Ent04 | 3.4.2 | N=77 388 para todo | + N desembolsados 77 308 | Precisión | Q01 |
| Ent04 | 3.4.4 I13 | N=77 388 | N=77 308; n=383 (sin cambio de n) | Definición población | Q21 |
| Anexo1 | Base referencia | Sin desembolso | 77 308 con desembolso; mes cerrado sep | Consistencia | Q01, Q04 |
| Anexo1 | I7/I9 | Redacción antigua | Alineada a diccionario | Consistencia | Ent03_v2 |
| Anexo1 | Metodología | «tipo aplicada» vago | Experimental + preexperimental | Ent04 | Ent04_v2 |

## Cifras maestras (única fecha de corte 2026-10-06)

| Indicador | Valor |
|-----------|-------|
| Clientes | 6460 |
| Expedientes | 77 388 |
| Con desembolso | 77 308 |
| Monto desembolsado | S/ 49 253 123,32 |
| Cuotas | 1 609 858 |
| Movimientos caja | 1 655 421 |
| Clientes con desembolso | 5794 |
| SCM sep-2026 saldo | S/ 1 000 784,16 |
| SCM sep-2026 mora | S/ 186 203,31 (18,61 %) |
| SCM jul-2023 saldo | S/ 656 611,01 |
| Crecimiento jul-23→sep-26 | 52,4 % |
| Cierre ago-2026 capital | S/ 1 012 148,27 |
| Cierre ago-2026 mora FIFO | S/ 163 507,11 (~16,2 %) |
| Δ SCM−Cierre (jul y ago) | S/ 3 219,50 / 20 clientes |
| Metas | 19 activas; valores NULL |
| Tablas / vacías / MB | 67 / 21 / ~327 |
| Mediana monto / cuotas | S/ 500 / 26 |
| Forma pago diaria | 73,6 % |
| n Cochran I13 | 383 |
