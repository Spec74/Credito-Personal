# INFORME DE PRÁCTICAS PREPROFESIONALES

**Nota tipográfica (APA 7.ª ed.):** al pasar este documento a Microsoft Word, use letra Times New Roman 12 pt, interlineado doble, márgenes de 2,54 cm, sangría de primera línea de 1,27 cm en párrafos del cuerpo y sangría francesa en la lista de referencias. Los corchetes `[…]` marcan datos que debe completar o verificar el practicante antes de la impresión definitiva.

---

## a. Carátula

UNIVERSIDAD NACIONAL DE SAN CRISTÓBAL DE HUAMANGA  
FACULTAD DE INGENIERÍA DE MINAS, GEOLOGÍA Y CIVIL  
ESCUELA PROFESIONAL DE INGENIERÍA DE SISTEMAS  

**INFORME DE PRÁCTICAS PREPROFESIONALES**

**Migración del sistema legado de créditos Credix hacia una arquitectura moderna, con mejoras operativas al límite de paridad y calidad**

Presentado por:  
**Eber Cristian Sulca Paredes**  
Código de estudiante: `[COMPLETAR]`  
Correo institucional: eber.sulca.27@unsch.edu.pe  

Empresa / entidad receptora:  
**Inversiones Crediconfiable S.A.C.**  
Sistema: Credix (crédito personal / multisucursal)  
Ciudad: Ayacucho, Perú  

Asesor de la Escuela Profesional: `[NOMBRE DEL ASESOR]`  
Supervisor en la empresa: `[NOMBRE DEL SUPERVISOR]`  

Periodo de prácticas: `[DD/MM/AAAA]` – `[DD/MM/AAAA]`  
Lugar y fecha de presentación: Ayacucho, `[mes]` de 2026  

---

## b. Índice

1. Resumen  
2. Introducción  
3. Problemas  
4. Objetivos  
   4.1. Objetivo general  
   4.2. Objetivos específicos  
5. Revisión teórica  
   5.1. Sistemas legados y evolución del software  
   5.2. Mantenimiento y migración según normas internacionales  
   5.3. Patrón Strangler Fig y modernización incremental  
   5.4. Capa anticorruption y diseño orientado al dominio  
   5.5. Arquitectura, atributos de calidad y usabilidad  
   5.6. Aplicación al dominio de crédito y tesorería  
6. Resultados  
   6.1. Contexto institucional y del sistema  
   6.2. Estrategia de migración aplicada  
   6.3. Arquitectura resultante Credito.Modern  
   6.4. Paridad funcional por módulos  
   6.5. Mejoras al límite (más allá de la paridad)  
   6.6. Calidad, pruebas y endurecimiento de plataforma  
   6.7. Evidencia de entrega y estado de go-live  
7. Recomendaciones  
8. Bibliografía  
9. Anexos  

---

## c. Resumen

El presente informe documenta las prácticas preprofesionales realizadas en Inversiones Crediconfiable S.A.C., centradas en la **migración incremental** del sistema de créditos **Credix** desde una aplicación monolítica ASP.NET MVC hacia una plataforma moderna integrada por una API en .NET, una aplicación de página única (SPA) en React/TypeScript y la misma base de datos transaccional SQL Server. El enfoque adoptado corresponde al patrón *Strangler Fig* (Fowler, 2004/2024; Microsoft, 2024), que permite sustituir rebanadas funcionales sin detener la operación diaria de las oficinas.

Además de lograr **paridad** con el legado en autenticación, crédito, clientes, caja, bóveda/tesorería, informes y administración, el trabajo empujó **mejoras al límite**: unificación del dominio de medios de pago (efectivo, Yape, Plin, BCP, Interbank, Banco de la Nación), tipado de billeteras de Central (Gerente/René), remesa sucursal→Central con selección de billetera tipada, cobros mixtos con validación al céntimo, endurecimiento de reprogramación y rehabilitación de clientes depurados, documentación *as-built* por módulo (SSD) y endurecimiento de plataforma (CI, cabeceras de seguridad, observabilidad).

Se concluye que la migración incremental, gobernada por contrato de datos (`usp_*`), criterios de aceptación por módulo y pruebas automatizadas, reduce el riesgo operativo frente a un reemplazo “big bang”, al tiempo que eleva mantenibilidad, usabilidad y control monetario. El estado técnico al cierre del periodo es de **candidato a go-live**, pendiente de smoke con datos reales de producción y firma de negocio para el retiro gradual del MVC.

*Palabras clave:* migración de software, Strangler Fig, sistemas legados, arquitectura de software, crédito, tesorería, paridad funcional, ISO/IEC 25010.

---

## d. Introducción

Las organizaciones que dependen de software crítico de negocio enfrentan un dilema recurrente: el sistema legado sostiene la operación, pero su stack tecnológico, acoplamiento y experiencia de usuario dificultan la evolución (Sommerville, 2016). En el caso de Inversiones Crediconfiable S.A.C., el núcleo operativo —otorgamiento y cobranza de créditos, caja diaria, bóveda multisucursal e informes gerenciales— residía en una aplicación web MVC con lógica repartida entre controladores, capa de negocio y procedimientos almacenados SQL Server.

Durante las prácticas preprofesionales se participó en la rama de modernización **Credito.Modern / Credix**, con el mandato explícito de **migrar sin romper la operación** y, en paralelo, **llevar las mejoras al límite** de lo que el negocio exige hoy: medios de pago digitales reales en Ayacucho y sedes, trazabilidad de fondeo y remesa entre Central y sucursales, y flujos de crédito (reprogramar, depurar/rehabilitar) más seguros y claros para el usuario.

Este informe sigue la estructura reglamentaria de prácticas preprofesionales (carátula, índice, resumen, introducción, problemas, objetivos, revisión teórica, resultados, recomendaciones, bibliografía y anexos). La revisión teórica se apoya en fuentes académicas y normas internacionales verificables; los resultados se contrastan con la evidencia técnica del repositorio, la documentación SSD y las entregas realizadas en el periodo de prácticas.

---

## e. Problemas

A partir del diagnóstico del legado y de la operación multisucursal (Central en Huamanga y sedes como Pichari, Huanta y Andahuaylas), se identificaron los siguientes problemas:

**P1. Dependencia de un monolito MVC difícil de evolucionar.**  
La interfaz y parte de la orquestación estaban acopladas a patrones web antiguos; incorporar cambios de UX o de API pública implicaba alto costo y riesgo de regresión (Sommerville, 2016; Bass et al., 2021).

**P2. Riesgo de un reemplazo total (“big bang”).**  
Apagar el legado de un día para otro amenazaba caja, desembolso y cierres diarios. Se requería coexistencia controlada entre MVC y la nueva plataforma (Fowler, 2004/2024; Newman, 2015).

**P3. Paridad incompleta o frágil en módulos críticos.**  
No bastaba “parecer” al legado: saldos, cuotas, mora, transferencias de bóveda y permisos por oficina debían respetar los mismos `usp_*` y reglas de negocio; cualquier recálculo improvisado en C# o en el frontend era inaceptable.

**P4. Medios de pago y tesorería por debajo de la realidad operativa.**  
La operación ya usaba Yape, Plin, transferencias BCP/Interbank/Banco de la Nación y billeteras tipadas en Central (p. ej., Gerente/René), pero el software no modelaba de forma unificada el catálogo, el fondeo, la remesa ni el cobro mixto con **integridad al céntimo**.

**P5. Experiencia de usuario opaca en operaciones sensibles.**  
Flujos como reprogramación de crédito o rehabilitación de cliente depurado carecían de preview, confirmación clara o visibilidad de estado, vulnerando principios de visibilidad del estado del sistema y prevención de errores (Nielsen, 1994/2024).

**P6. Déficit de especificación y de calidad de plataforma.**  
Sin especificaciones *as-built*, bitácora de desviaciones, CI y endurecimiento (headers, observabilidad, smoke), la migración no era auditable ni segura para cutover (ISO/IEC/IEEE, 2022; ISO/IEC, 2011).

---

## f. Objetivos

### 4.1. Objetivo general

Diseñar, implementar y documentar la **migración incremental** del sistema Credix hacia la arquitectura Credito.Modern, preservando la paridad de negocio con el legado y **extendiéndola con mejoras operativas al límite** en medios de pago, tesorería multisucursal, flujos de crédito y calidad de plataforma.

### 4.2. Objetivos específicos

1. Aplicar una estrategia tipo *Strangler Fig* con fachada/proxy, retiro gradual de rutas MVC y convivencia con la misma base `CREDITO`.  
2. Exponer los módulos operativos vía API JWT (`.NET`) y SPA (React + TypeScript + Ant Design), con capas Application/Infrastructure/Domain.  
3. Garantizar paridad monetaria mediante invocación de procedimientos almacenados existentes y validación previa de entradas.  
4. Unificar el dominio de medios de pago y completar fondeo/remesa Central↔sucursal con billeteras tipadas.  
5. Endurecer cobros (mixto, al céntimo) y operaciones de crédito (reprogramar con preview; depurar/rehabilitar).  
6. Formalizar documentación SSD, pruebas automatizadas y endurecimiento de plataforma orientados a go-live.  
7. Elaborar el presente informe de prácticas conforme a la estructura institucional y al estilo APA.

---

## g. Revisión teórica

### 5.1. Sistemas legados y evolución del software

Sommerville (2016) define los *legacy systems* como sistemas antiguos que siguen siendo críticos para el negocio, a menudo construidos con tecnologías obsoletas y degradados estructuralmente por años de mantenimiento. Las estrategias típicas son: desechar el sistema, continuar el mantenimiento, reingeniería o reemplazo. La elección depende del **valor de negocio** y de la **calidad técnica** del software.

Pressman y Maxim (2019) enfatizan que la evolución del software es inevitable: cambian requisitos, entorno, hardware y expectativas de usabilidad. En ese marco, modernizar un core bancario/crediticio no es un “proyecto cosmético”, sino un proceso de ingeniería con gestión de riesgo, configuración y calidad.

### 5.2. Mantenimiento y migración según normas internacionales

La norma **ISO/IEC/IEEE 14764:2022** detalla el proceso de mantenimiento del software alineado con ISO/IEC/IEEE 12207, incluyendo planificación, análisis de problemas/modificaciones, implementación, revisión/aceptación y disposición (*disposal*) del software (ISO/IEC/IEEE, 2022). Aunque el foco es mantenimiento, la migración a nuevos entornos aparece como actividad que exige plan documentado, participación de usuarios, verificación y control de configuración.

Complementariamente, **ISO/IEC 25010:2011** (familia SQuaRE) modela la calidad del producto en características como idoneidad funcional, eficiencia de desempeño, compatibilidad, usabilidad, fiabilidad, seguridad, mantenibilidad y portabilidad (ISO/IEC, 2011). En una migración de créditos, la idoneidad funcional (paridad de reglas), la seguridad (JWT, oficina, roles), la usabilidad (caja/tesorería) y la mantenibilidad (capas, tests, SSD) son atributos explícitos de diseño, no “extras”.

### 5.3. Patrón Strangler Fig y modernización incremental

Fowler (2004/2024) describe el patrón *Strangler Fig* por analogía con la higuera estranguladora: se construye lo nuevo alrededor del legado, se traslada comportamiento por partes y, eventualmente, el legado se retira. Microsoft (2024) formaliza el mismo patrón en su Architecture Center: una fachada/proxy enruta peticiones al legado o al nuevo servicio; la migración es iterativa; al final se decommissiona el sistema antiguo.

Newman (2015) sitúa el *Strangler Pattern* en el contexto de integración y descomposición de monolitos: coexistir endpoints, evitar cambios rompientes y retirar capacidad antigua cuando el tráfico ya no la necesita. AWS (n.d.) resume el ciclo en transformar, coexistir y eliminar. Estas fuentes convergen en un mensaje operativo: **modernizar sin big bang**.

### 5.4. Capa anticorruption y diseño orientado al dominio

Evans (2003) introduce la *Anti-Corruption Layer* (ACL) para proteger el modelo nuevo de las semánticas del sistema externo o legado. Microsoft (n.d.) la documenta como fachada/adaptador/traductor entre subsistemas. En Credito.Modern, aunque se reutiliza el ledger SQL (`usp_*`), la API y la Application layer actúan como frontera: validan entradas, traducen errores a `ProblemDetails`, aplican guards de oficina/JWT y evitan que el frontend “entienda” el legado en crudo. Ello se alinea con Bass et al. (2021): la arquitectura existe para satisfacer atributos de calidad (modificabilidad, seguridad, testabilidad) bajo restricciones del negocio.

### 5.5. Arquitectura, atributos de calidad y usabilidad

Bass, Clements y Kazman (2021) sostienen que la arquitectura es el vehículo para razonar sobre atributos de calidad y para evolucionar sistemas complejos. En interfaces, Nielsen (1994/2024) propone diez heurísticas; resultan particularmente relevantes en caja y crédito:

- visibilidad del estado del sistema (preview de deuda al reprogramar; saldos de bóveda);  
- prevención de errores (validación al céntimo; 422 con mensaje de negocio);  
- consistencia y estándares (catálogo único de medios de pago FE/BE);  
- reconocimiento antes que recuerdo (chips/marcas de Yape, Plin, bancos).

### 5.6. Aplicación al dominio de crédito y tesorería

En microfinanzas / crédito personal, el software es el libro mayor operativo: un error de un céntimo en cobro mixto o en remesa de bóveda no es un “bug de UI”, sino un incidente de caja. Por ello la literatura de modernización debe combinarse con una regla de dominio local: **la base de datos es el contrato**; el código moderno orquesta y endurece, pero no inventa cuotas ni saldos. Esta decisión es coherente con ACL + strangler: se moderniza la periferia (UX, API, seguridad, catálogos) sin corromper el core transaccional hasta que el negocio firme cada retiro.

---

## h. Resultados

### 6.1. Contexto institucional y del sistema

Inversiones Crediconfiable S.A.C. opera créditos personales con red de oficinas bajo un único ledger SQL Server (`CREDITO`). La oficina Central (indicador de principal) fondea y consolida; las sucursales operan caja y bóveda filtradas por `OficinaId`. El producto moderno se conoce como **Credix** / **Credito.Modern**.

Durante las prácticas se trabajó en el repositorio de modernización (rama `Eber-migracion`), participando en implementación, depuración, documentación y cierre de huecos de UX/negocio en crédito y tesorería.

### 6.2. Estrategia de migración aplicada

Se implementó el strangler documentado en `STRANGLER-MIGRATION.md` y `SSD-00-cutover`:

| Capa | Destino |
|------|---------|
| `/api/v1/*`, `/health` | API moderna (.NET) |
| `/app/*` | SPA React |
| Rutas aún no firmadas | MVC legado |

Regla de retiro: una ruta legacy solo se apaga cuando la rebanada moderna pasa smoke funcional, seguridad y validación de negocio. El cutover productivo permanece **no firmado** hasta smoke con datos reales; el estado técnico es **candidato a go-live** (`MIGRATION-CLOSURE.md`).

### 6.3. Arquitectura resultante Credito.Modern

Arquitectura en capas (as-built):

1. **Credito.Modern.Web** — React + Vite + TypeScript + Ant Design; JWT; menú desde `usp_MenuLst`.  
2. **Credito.Modern.Api** — Minimal APIs .NET, políticas JWT, `ProblemDetails`.  
3. **Application** — casos de uso, DTOs, validación transversal (SSD-12).  
4. **Infrastructure** — Dapper/SqlClient, procedimientos `usp_*`, integraciones.  
5. **Domain** — contratos mínimos.  
6. **Tests** — batería automatizada backend + pruebas de permisos/frontend.

El reloj de negocio permanece en `CREDITO.usp_FechaBD`. Una sola base sirve a todas las sedes; no se fragmentó el ledger por oficina.

### 6.4. Paridad funcional por módulos

Con base en el catálogo SSD, al momento del informe la paridad operativa principal cubre:

| Módulo | Contenido migrado (síntesis) |
|--------|------------------------------|
| Auth / menú | Login JWT, refresh, menú, guards de oficina |
| Crédito | Consulta, solicitud, simulador, aprobación, tareas, gestión, reprogramar/depurar |
| Clientes | Listado, mantenimiento, estados (incl. depurado) |
| Caja | Diario, saldos, cierre, caja chica, verificar |
| Bóveda / tesorería | Estado, movimientos, transferencias, fondeo, remesa, cierre |
| Informes | JSON/CSV/PDF tabular Credix; puente RDLC opcional |
| Admin / maestros | Oficinas, usuarios, catálogos |
| Ventas / almacén | Implementados; alcance de menú según oficina |

La constitución del proyecto —no negociable por pantalla— establece que la lógica monetaria vive en SQL; el moderno valida y orquesta.

### 6.5. Mejoras al límite (más allá de la paridad)

Las prácticas no se limitaron a “clonar pantallas”. Se impulsaron mejoras que el legado no resolvía de forma profesional:

**a) Dominio unificado de medios de pago.**  
Catálogo tipado compartido FE/BE (`TipoPago` / `tipoPago.ts`): efectivo y digitales de primera clase (Yape, Plin, BCP, Interbank, Banco de la Nación), con reglas centralizadas en lugar de condicionales dispersos.

**b) Billeteras de Central tipadas (Gerente / René).**  
Modelado explícito de billeteras Yape/Interbank asociadas a responsables en Central, visibles en rollups y seleccionables en operaciones de red, sin confundirlas con medios genéricos de sucursal.

**c) Fondeo y remesa Central ↔ sucursal.**  
Fondeo mapea familias tipadas de Central hacia medios de sucursal; remesa inversa debita familia en sucursal y acredita billetera tipada en Central. El USP de transferencia de bóveda se alineó y documentó (`deploy/sql/…usp-transferir-boveda-map-billeteras.sql`).

**d) Cobro mixto al céntimo.**  
Permite combinar efectivo y digitales en un mismo cobro, con validación estricta de suma y glosa por medio, reduciendo pérdida o sobrante de dinero por redondeo/UI.

**e) Crédito: reprogramar con preview y rehabilitar depurado.**  
El modal de reprogramación muestra deuda/plan antes de confirmar la secuencia REP→CRE. La rehabilitación de cliente depurado se expone en cabecera de crédito y ficha de cliente (roles autorizados), con confirmación. El backend endurece depurar (sin duplicados) y la lectura del estado depurado.

**f) UX de marcas y a11y en chips de cobro.**  
Corrección de logos/nombres (Plin, Banco de la Nación, René) y presentación consistente en selects/chips.

Estas mejoras materializan heurísticas de Nielsen (estado visible, prevención de error, consistencia) y atributos ISO/IEC 25010 (idoneidad funcional, usabilidad, fiabilidad operativa).

### 6.6. Calidad, pruebas y endurecimiento de plataforma

Resultados de ingeniería asociados al periodo:

- Compilación de solución .NET y build de SPA verificados en ciclos de entrega.  
- Suite de tests backend histórica del orden de cientos de casos (`Credito.Modern.Tests`); tests unitarios de permisos/frontend para operaciones de crédito.  
- CI en GitHub Actions (calidad + gate antes de publish API).  
- Cabeceras de seguridad, correlation id / OpenTelemetry, ACL de shell para evitar flash de pantallas sin permiso (`PLATFORM-HARDENING.md`).  
- Documentación: `ARCHITECTURE.md`, `DOCUMENTACION.md`, SSD 00–19, guías de migración por módulo, bitácora de desviaciones, manual por rol.

### 6.7. Evidencia de entrega y estado de go-live

Entregables observables del trabajo de prácticas incluyen commits en `Eber-migracion` sobre tesorería (medios, fondeo, remesa), caja (cobro mixto, céntimo) y crédito (preview reprogramar, rehabilitar), más sincronización de schema/manual USP.

**Estado al cierre documentado:** candidato a go-live; falta smoke E2E con base real de producción, decisión de negocio sobre retiro RDLC y aplicación en Azure de scripts SQL pendientes (p. ej. mapeo de billeteras en USP de transferencia). El MVC no se declara retirado.

En síntesis, los objetivos específicos 1–6 se cumplen en el ámbito técnico del repositorio y de la operación piloto/local; el objetivo 7 se cumple con este informe.

---

## i. Recomendaciones

1. **Ejecutar el checklist de cutover (SSD-00)** con oficina piloto, smoke de login/caja/crédito/bóveda/informe y plan de rollback por rebanada.  
2. **Aplicar en Azure** los scripts SQL versionados de `deploy/sql/` antes de confiar en remesa/fondeo tipado en producción.  
3. **Firmar retiro gradual del MVC** solo módulo por módulo; no desmantelar el proxy hasta que negocio acepte PDF tabular Credix.  
4. **Institucionalizar el catálogo TipoPago** como única fuente de verdad (evitar reintroducir `if` por pantalla).  
5. **Ampliar pruebas E2E Playwright** a cobro mixto, remesa tipada y reprogramar/rehabilitar con datos realistas.  
6. **Capacitar cajeros y tesorería** con el manual por rol, enfatizando céntimos y selección de billeteras Central.  
7. **Mantener la bitácora de desviaciones** ante cualquier excepción a la constitución “DB = contrato”.  
8. **Planificar fase BFF/httpOnly** para tokens si el visor PDF lo permite, reforzando seguridad perimetral.  
9. **Medir calidad en uso** (ISO/IEC 25010) tras go-live: tiempo de cobro, incidencias de descuadre, tickets de medios digitales.  
10. **Conservar el strangler como capacidad organizacional** (Fowler, 2024): la modernización no termina en un release; es una práctica continua.

---

## j. Bibliografía

American Psychological Association. (2020). *Publication manual of the American Psychological Association* (7.ª ed.). https://apastyle.apa.org/

Amazon Web Services. (n.d.). *Strangler fig pattern*. AWS Prescriptive Guidance. https://docs.aws.amazon.com/prescriptive-guidance/latest/modernization-decomposing-monoliths/strangler-fig.html

Bass, L., Clements, P., & Kazman, R. (2021). *Software architecture in practice* (4.ª ed.). Addison-Wesley Professional.

Evans, E. (2003). *Domain-driven design: Tackling complexity in the heart of software*. Addison-Wesley Professional.

Fowler, M. (2004, junio 29). *Strangler fig application*. https://martinfowler.com/bliki/StranglerFigApplication.html

Fowler, M. (2024). *Rewriting strangler fig*. https://martinfowler.com/articles/2024-strangler-fig-rewrite.html

ISO/IEC. (2011). *Systems and software engineering — Systems and software Quality Requirements and Evaluation (SQuaRE) — System and software quality models* (ISO/IEC 25010:2011). International Organization for Standardization.

ISO/IEC/IEEE. (2022). *Software engineering — Software life cycle processes — Maintenance* (ISO/IEC/IEEE 14764:2022). International Organization for Standardization. https://www.iso.org/standard/80710.html

Microsoft. (n.d.). *Anti-corruption layer pattern*. Azure Architecture Center. https://learn.microsoft.com/en-us/azure/architecture/patterns/anti-corruption-layer

Microsoft. (2024). *Strangler Fig pattern*. Azure Architecture Center. https://learn.microsoft.com/en-us/azure/architecture/patterns/strangler-fig

Newman, S. (2015). *Building microservices*. O’Reilly Media.

Nielsen, J. (1994). Enhancing the explanatory power of usability heuristics. En *Proceedings of the SIGCHI Conference on Human Factors in Computing Systems* (pp. 152–158). Association for Computing Machinery. https://doi.org/10.1145/191666.191729

Nielsen, J. (2024, 30 de enero). *10 usability heuristics for user interface design*. Nielsen Norman Group. https://www.nngroup.com/articles/ten-usability-heuristics/ (Trabajo original publicado en 1994; revisión vigente)

Pressman, R. S., & Maxim, B. R. (2019). *Software engineering: A practitioner’s approach* (9.ª ed.). McGraw-Hill Education.

Sommerville, I. (2016). *Software engineering* (10.ª ed.). Pearson.

Thoughtworks. (n.d.). *Embracing the Strangler Fig pattern for legacy modernization*. https://www.thoughtworks.com/en-us/insights/articles/embracing-strangler-fig-pattern-legacy-modernization-part-one

### Fuentes técnicas del proyecto (literatura gris / documentación interna)

Inversiones Crediconfiable S.A.C. / Spec74. (2026). *ARCHITECTURE.md* [Documentación técnica]. Repositorio Credito.Modern.

Inversiones Crediconfiable S.A.C. / Spec74. (2026). *MIGRATION-CLOSURE.md* [Documentación técnica]. Repositorio Credito.Modern.

Inversiones Crediconfiable S.A.C. / Spec74. (2026). *STRANGLER-MIGRATION.md* [Documentación técnica]. Repositorio Credito.Modern.

Inversiones Crediconfiable S.A.C. / Spec74. (2026). *SSD-00-cutover.md* y catálogo SSD [Especificaciones as-built]. Repositorio Credito.Modern.

---

## k. Anexos

### Anexo A. Glosario operativo

| Término | Significado |
|---------|-------------|
| Credix / Credito.Modern | Plataforma moderna (API + SPA) |
| MVC legado | Aplicación ASP.NET MVC previa |
| Strangler | Convivencia y retiro gradual del legado |
| `usp_*` | Procedimientos almacenados de negocio |
| Central | Oficina principal (fondeo / red) |
| Remesa | Transferencia de liquidez sucursal → Central |
| Fondeo | Transferencia Central → sucursal |
| DEP / REP / CRE | Operaciones de depuración / reprogramación / crédito según flujo interno |

### Anexo B. Mapa simplificado de arquitectura

```
Usuario (navegador)
        │
        ▼
 SPA Credito.Modern.Web  (/app/)
        │  HTTPS /api/v1
        ▼
 Proxy strangler (nginx/IIS)
   ├─ /api/v1 → Credito.Modern.Api (.NET + JWT)
   ├─ /app/   → SPA
   └─ resto   → MVC legado (hasta firma de retiro)
        │
        ▼
 SQL Server CREDITO  (usp_*, una base, muchas oficinas)
```

### Anexo C. Tabla de problemas → objetivos → resultados

| Problema | Objetivo específico | Resultado principal |
|----------|---------------------|---------------------|
| P1 Monolito | 2 | API + SPA en capas |
| P2 Big bang | 1 | Proxy strangler + cutover documentado |
| P3 Paridad | 3 | Constitución DB + SSD por módulo |
| P4 Medios/tesorería | 4 | Catálogo unificado + fondeo/remesa tipada |
| P5 UX sensible | 5 | Preview reprogramar + rehabilitar + cobro al céntimo |
| P6 Calidad | 6 | CI, hardening, tests, documentación |

### Anexo D. Evidencias sugeridas para el CD / expediente

1. Capturas de login SPA, menú por rol, cobro mixto, bóveda (fondeo/remesa), reprogramar con preview, rehabilitar.  
2. Export de `git log` de `Eber-migracion` filtrado por tesorería/caja/crédito.  
3. Resultado de `dotnet test` y `npm run build`.  
4. PDF/impresiones de SSD-00, ARCHITECTURE y manual por rol (extractos).  
5. Constancia de prácticas y evaluación del supervisor empresarial.  
6. Este informe en PDF + versión editable.

### Anexo E. Datos pendientes de completar por el practicante

- Código de matrícula, fechas exactas de inicio/fin, horas diarias.  
- Nombres del asesor universitario y supervisor de empresa.  
- Razón social completa según constancia, RUC y dirección de la sede de prácticas.  
- Ajuste de facultad/escuela si la carátula institucional difiere del modelo usado aquí.  
- Firmas y sellos según reglamento (tres ejemplares físicos + CD, si sigue vigente).

### Anexo F. Declaración de originalidad (borrador)

El suscrito declara que el presente informe es producto de las prácticas preprofesionales realizadas y de la documentación técnica del proyecto Credito.Modern / Credix, citando conforme al estilo APA las fuentes teóricas utilizadas. Los datos entre corchetes serán completados antes de la presentación oficial.

_______________________________  
Eber Cristian Sulca Paredes  
DNI: `[COMPLETAR]`

---

*Fin del informe.*
