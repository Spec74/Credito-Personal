# Bitácora de desviaciones respecto al legacy

Registro de cada punto donde el sistema moderno se aparta del comportamiento del MVC legacy,
con el motivo. Existe para que cualquier diferencia de resultado frente al sistema viejo tenga
una respuesta documentada en lugar de una discusión.

## Regla de decisión

Ante una diferencia entre legacy y moderno se aplica este criterio:

- **Regla de negocio deliberada** → se preserva, aunque parezca extraña. Se documenta solo si
  resulta contraintuitiva.
- **Defecto** (pérdida de centavos por redondeo, validación ausente, estado no controlado,
  condición de carrera) → se corrige y se registra aquí.
- **Hueco funcional** (el legacy nunca implementó algo que el negocio necesita) → se implementa
  y se registra aquí.

Nunca se cambia lógica financiera en silencio. Toda corrección que altere un importe, un saldo
o un estado contable debe aparecer en esta bitácora.

## Fuente de verdad

La base de datos es el contrato. La lógica de negocio vive en los procedimientos almacenados
(`CREDITO.usp_*`, `MAESTRO.usp_*`), no en el C# del legacy, que solo los invoca. El snapshot
versionado en `db/schema` refleja la entrega vigente; el legacy en C# se usa como referencia de
flujo de pantallas y experiencia de usuario, no de reglas de cálculo.

Ver `deploy/scripts/export-db-schema.ps1` y `deploy/scripts/restore-db-backup.ps1`.

---

## Registro

### 2026-09-09 — Corrección de esquema en catálogos de almacén

**Tipo:** defecto (moderno, no del legacy)

Cinco tablas se consultaban bajo el esquema `MAESTRO` cuando en la base real viven en `ALMACEN`:
`Marca`, `Modelo`, `TipoArticulo`, `TipoMovimiento` y `Articulo`. Confirmado contra el EDMX del
legacy (`ITB.VENDIX.DA/VENDIXModel.edmx`) y contra el error SQL 208 devuelto por el servidor.

Efecto: los endpoints de marcas, modelos, tipos de artículo y tipos de movimiento respondían 503;
además fallaban la grilla de gestión de Artículos, el reporte de lista de precios general, salidas
y transferencias de almacén, el reporte de stock anulados y las altas y ediciones de Marca, Modelo
y Tipo de Artículo.

Corregidas 25 referencias en 11 archivos de infraestructura. Sin impacto en importes.

### 2026-09-09 — Tabla incorrecta en comprobante de movimiento de bóveda

**Tipo:** defecto (moderno, no del legacy)

`movimientobovedaticketreadservice.cs` consultaba `CREDITO.MovimientoBoveda`, que es el nombre de
la constraint de clave primaria, no de la tabla. La tabla real es `CREDITO.BovedaMov`. El resto de
servicios de bóveda ya usaba el nombre correcto.

Efecto: el comprobante de movimiento de bóveda no se podía emitir. Sin impacto en importes: la
consulta es de solo lectura y las columnas ya coincidían.

Detectado automáticamente por `EsquemaContratoTests` al ejecutarse por primera vez.

### 2026-09-09 — Análisis de la entrega de base `creditodb20260901.bak`

**Tipo:** análisis de entrega

Restaurada en paralelo como `CREDITO_20260901` (respaldo del 2026-09-01) y comparada contra la
base en uso. Resultado:

**Agregado por el cliente (13 objetos):** diez procedimientos de dashboards
(`usp_DashboardAdminResumen`, `AdminAnalistas`, `AdminFlujoCaja`, `AdminHistorico`,
`AdminHistoricoMensual`, `Gestor`, `GestorClientesMora`, `Productividad`, `Ranking`,
`TopAnterior`), `usp_RegistrarTransferenciaBancos`, y el flujo de condonación
(`usp_SolicitarCondonacion` más la tabla `CREDITO.CreditoCondonacion`). Ninguno está
implementado todavía en el sistema moderno.

**Modificado por el cliente (17 objetos):** tres índices nuevos de rendimiento en
`CREDITO.Credito` y `CREDITO.MovimientoCaja`; reescrituras importantes de
`usp_RptCobroDiario` (92 a 298 líneas), `usp_RptClientesInactivos`, `usp_RptCreditosActivos` y
`usp_RptAval`; y cambios menores en `usp_CompletarImpagos`, `usp_PagarCuotaPagoLibre`,
`usp_CerrarCajasDiarios`, `usp_RecalcularCajaDiario`, `usp_RptCreditosCierres` y `dbo.ufnFecha`.
`CREDITO.Tarea.FechaCreacion` perdió su valor por defecto.

**Ausente en la entrega:** las adiciones propias del sistema moderno. Se documentaron como
migraciones idempotentes en `deploy/sql` y se reaplican con `deploy/scripts/apply-migrations.ps1`.

### 2026-09-09 — Mora postergada frente a `MovimientoCajaId NOT NULL`

**Tipo:** divergencia deliberada, pendiente de confirmación con el negocio

La entrega del cliente define `CREDITO.CreditoMora.MovimientoCajaId` como `NOT NULL`. El flujo
moderno de mora postergada registra la mora de una cuota vencida antes de cobrarla, y usa
`MovimientoCajaId NULL` para marcarla como pendiente hasta que `usp_CreditoMora_Liquidar` genere
el movimiento de caja.

`deploy/sql/2026-06-credito-mora-postergada.sql` vuelve la columna nullable. Es una divergencia
respecto al esquema del cliente y debe confirmarse: la alternativa es rediseñar el estado
pendiente sin usar `NULL` como centinela.

### 2026-09-09 — `@@IDENTITY` reemplazado por `SCOPE_IDENTITY()` en liquidación de mora

**Tipo:** defecto con impacto financiero

`usp_CreditoMora_Liquidar` capturaba el id del movimiento de caja recién insertado con
`@@IDENTITY`, que devuelve el último identity generado en la conexión **incluyendo el de
cualquier trigger**. Si algún trigger inserta en otra tabla con identity, la mora se vincularía
a un movimiento de caja equivocado.

Corregido a `SCOPE_IDENTITY()`, que se limita al ámbito actual. Afecta la trazabilidad del
dinero cobrado por mora.

### 2026-09-09 — Mensajes de error técnicos ocultos al usuario

**Tipo:** endurecimiento de seguridad

El legacy y las primeras versiones de la API exponían el texto de la excepción en la respuesta
HTTP. Se reemplazó por mensajes genéricos en producción, conservando el detalle en el log del
servidor y en entorno de desarrollo.

Efecto en negocio: ninguno. Cambia solo el texto visible ante error.

### 2026-09-09 — Correcciones al script de prendario entregado por gerencia

**Tipo:** defectos en el script entregado

Gerencia entregó el DDL del módulo de crédito prendario. Se aplicó en
`deploy/sql/2026-09-prendario-modulo.sql` con cuatro correcciones:

1. **Permisos concedidos al módulo equivocado.** El script hacía
   `INSERT INTO MAESTRO.RolMenu (RolId, MenuId) VALUES (6, 38), (6, 39)` con los ids fijos. Los
   MenuId 38 y 39 ya existen en la entrega vigente y corresponden a **CONDONACION** y
   **DASHBOARD**; los menús de prendario reciben 40, 41 y 42 porque `MenuId` es `IDENTITY`. Tal
   cual, el script habría dado al rol ANALISTA acceso a condonación y al dashboard, y ningún
   acceso a prendario. Ahora los MenuId se resuelven por `Denominacion`.

2. **`CREATE TABLE` inválido.** Faltaba la coma entre `CodigoInterno nvarchar(50) NULL` y
   `CONSTRAINT FK_Prenda_Credito`, por lo que el lote fallaba con error de sintaxis.

3. **Tipos incompatibles.** `Credito.PrendaId` se declaraba `INT` mientras
   `Prenda.PrendaId` es `BIGINT IDENTITY`. Se unificó a `bigint`.

4. **Sin trazabilidad.** La prenda es un bien físico en custodia y la tabla no registraba quién
   la dio de alta ni quién la modificó. Se agregaron `UsuarioRegId`, `UsuarioModId` y `FechaMod`,
   nullables para no romper inserciones existentes.

El menú padre no lleva fila en `RolMenu` a propósito: `usp_MenuLst` lo deriva uniendo
`Menu.Orden` con la `Referencia` de los hijos concedidos. Verificado ejecutando
`MAESTRO.usp_MenuLst` con un usuario analista real sobre la base restaurada.

### 2026-09-09 — `CREDITO.Prenda` sustituye a `CREDITO.CreditoPrenda`

**Tipo:** decisión de diseño

El sistema moderno había creado una tabla provisional `CREDITO.CreditoPrenda` (una prenda por
crédito, campos mínimos). El diseño de gerencia usa `CREDITO.Prenda`, admite varias prendas por
crédito y agrega marca, modelo, serie, color, foto y código interno.

Se adopta `CREDITO.Prenda` porque el MVC legacy modificado va a leer esa tabla y ambos sistemas
comparten la base durante la migración strangler.

`CreditoPrenda` ya se retiró: `deploy/sql/2026-09-prendario-migrar-creditoprenda.sql` copia sus
filas a `Prenda` (con `Serie = 'N/T'`, que es el centinela del legacy), traslada el indicador y la
fecha de remate al crédito, y elimina la tabla.

### 2026-09-09 — Retirado el DDL en caliente

**Tipo:** endurecimiento de seguridad y operación

`CreditoPrendaSchema.EnsureAsync` ejecutaba `CREATE TABLE` en cada lectura y escritura de prenda.
Eso obliga a que la cuenta de la aplicación tenga permisos de DDL en producción, y hacía que el
esquema dependiera del código en lugar de las migraciones.

Eliminado. El esquema se toca solo desde `deploy/sql`, y la prueba de contrato
(`EsquemaContratoTests`) verifica que el SQL del código coincida con el snapshot.

### 2026-09-09 — `menutree.ts` duplicado tapaba los iconos del menú

**Tipo:** defecto por código redundante

Existían `src/utils/menutree.ts` y `src/utils/menutree.tsx` con el mismo contenido salvo que el
`.tsx` renderiza iconos. Vite resuelve `.ts` antes que `.tsx`, así que la aplicación cargaba
siempre la versión sin iconos y el trabajo del `.tsx` era código muerto.

Se eliminó el `.ts`. El menú ahora muestra los iconos que ya estaban implementados.

### 2026-09-09 — Eliminada la inyección artificial del menú de prendario

**Tipo:** endurecimiento de seguridad

`ensureCreditoPrendarioMenuItem` agregaba un ítem de menú sintético de prendario cuando el menú
del usuario no traía ninguno. Era un parche para cuando la base no tenía el menú, pero con la
ruta ya restringida habría concedido prendario a cualquier usuario con algún ítem de crédito.

Eliminada: el acceso sale de `MAESTRO.RolMenu`, que es la fuente real.

### 2026-09-09 — Prendario restringido al rol ANALISTA en la SPA

**Tipo:** endurecimiento de seguridad

`/credito/prendario` se resolvía por prefijo del hub `/credito`, así que cualquier usuario con
algún ítem del menú de crédito podía entrar. Se agregó a `EXACT_MENU_ROUTES`: ahora exige que el
menú del usuario conceda esa ruta en concreto, que es lo que hace `RolMenu` solo para ANALISTA.

### 2026-09-09 — `MontoTasacion` no cuadraba con los bienes guardados

**Tipo:** defecto con impacto financiero

En `CreditoBL.GuardarPrendario` (rama `Prendario-Eber` del legacy) el guardado descarta las
prendas sin descripción:

```csharp
foreach (var p in pPrendas.Where(x => !string.IsNullOrWhiteSpace(x.Descripcion)))
```

pero el total se calcula sobre la lista completa:

```csharp
credito.MontoTasacion = pPrendas.Sum(x => x.ValorTasacion);
```

Un renglón con tasación y sin descripción no se guarda como bien pero sí suma al
`MontoTasacion` del crédito. El monto garantizado queda por encima del valor de los bienes que
la empresa realmente tiene en custodia, y es la cifra que se imprime en el contrato.

En el moderno la suma se calcula sobre las prendas efectivamente insertadas. Ver
`docs/migration/PARIDAD-PRENDARIO.md`.

### 2026-09-09 — El listado prendario del legacy no filtra por oficina

**Tipo:** hueco de aislamiento de datos

`PrendarioController.ListarPrendarioGrd` consulta `Credito` con el único filtro
`EsPrendario = 1 OR ProductoId = 2`. Un analista de cualquier oficina ve los créditos prendarios
de todas, con nombre, documento y celular del cliente. El resto del sistema moderno acota cada
consulta de crédito a la oficina del token (`ValidateJwtOficina`), así que replicar el legacy
aquí abriría un hueco que ya está cerrado en los demás módulos.

`/api/v1/prendario/resumen` y `/api/v1/prendario/creditos` exigen `oficinaId`, lo validan contra
el token y filtran por `Credito.OficinaId`.

**A confirmar con el cliente:** si gerencia necesita una vista consolidada de todas las
oficinas, corresponde un endpoint aparte con su propia autorización, no relajar este.

### 2026-09-09 — La categoría del listado se calcula en SQL, no en memoria

**Tipo:** rendimiento

El legacy trae las filas y luego llama a `CalcularCategoria` por cada una en C#, de modo que no
puede paginar ni contar por categoría en el servidor. Con 2414 créditos prendarios en la base
del cliente eso significa traer la tabla completa en cada carga del listado.

En el moderno el `CASE` de la categoría y los días para vencer viven en la consulta, sobre
`dbo.ufnFecha()` para no depender del reloj del servidor de aplicación. Así el `OFFSET/FETCH`
pagina sobre el conjunto ya clasificado. El orden de evaluación es el mismo que el del legacy.

### 2026-09-09 — Alta de solicitud prendaria repetida

**Tipo:** defecto de datos

`CreditoBL.CrearSolicitudCreditoPrendario` inserta un crédito en estado `CRE` en cada llamada,
sin comprobar si la persona ya tiene una solicitud abierta. Su método hermano,
`CrearSolicitudCredito`, sí lo comprueba. Dos clics en el botón dejan dos solicitudes muertas en
`CRE` para la misma persona.

`CrearSolicitudPrendariaAsync` reutiliza la solicitud prendaria en `CRE` de la persona en esa
oficina, igual que la ordinaria.

Como efecto del indicador nuevo, la reutilización de la solicitud ordinaria pasó a exigir
`EsPrendario = 0`: sin eso, pedir un crédito normal para una persona con una solicitud prendaria
abierta devolvía la prendaria.

### 2026-09-09 — `EsPrendario` se rellena en la cartera existente

**Tipo:** migración de datos

La columna nace con `DEFAULT 0`, así que los 2414 créditos del producto CREDI PRENDARIO que ya
existían en la base del cliente quedaban con el indicador apagado. El listado los recuperaba por
`ProductoId = 2`, pero el resumen (que filtra por `EsPrendario = 1`) marcaba cero créditos
prendarios vigentes teniendo 114 desembolsados, y el índice filtrado
`IX_Credito_EsPrendario` quedaba vacío.

`2026-09-01-prendario-modulo.sql` rellena `EsPrendario = 1` donde `ProductoId = 2`. El producto
es la fuente de verdad; el indicador es una denormalización para el índice filtrado.

### 2026-09-09 — Orden de las migraciones de prendario

**Tipo:** defecto de despliegue

`apply-migrations.ps1` ejecuta los `.sql` en orden alfabético, y
`2026-09-prendario-migrar-creditoprenda.sql` ordenaba antes de
`2026-09-prendario-modulo.sql`. Como la migración de datos está guardada con
`IF OBJECT_ID('CREDITO.Prenda') IS NOT NULL`, sobre una base recién restaurada no habría hecho
nada y los bienes se habrían quedado en la tabla vieja sin aviso.

Los scripts pasan a llamarse `2026-09-01-prendario-modulo.sql` y
`2026-09-02-prendario-migrar-creditoprenda.sql`, y el segundo lanza `THROW 51000` si encuentra
`CreditoPrenda` sin `Prenda` en lugar de no hacer nada.

### 2026-09-09 — Contrato y acta prendarios en QuestPDF

**Tipo:** hueco funcional cubierto

El legacy genera `rptContratoPrendario.rdlc` y `rptActaEntregaPrendario.rdlc`, y anexa el PDF
fijo `ClausulasPrendario.pdf` al contrato. El moderno los sustituye por QuestPDF
(`/api/v1/prendario/contrato-pdf` y `/acta-entrega-pdf`). Ambos exigen al menos un bien
guardado (409 si no hay). El departamento y la provincia salen del catálogo
(`MAESTRO.Distrito` → `Provincia` → `Departamento`), no del literal `AYACUCHO` del legacy.

Las cláusulas fijas se anexan con `PrendarioPdfMerge` sobre el PDF oficial de la rama
`Prendario-Eber` (`Web/Reporte/ClausulasPrendario.pdf`), embebido en
`ReportAssets/ClausulasPrendario.pdf`. El moderno estampa fecha, nombre y DNI sobre
los blancos de la última página (`Ayacucho, __ de __ de ____`) para no rellenar a
mano; el texto legal no se transcribe.

El importe en letras corrige el "VEINTIuno" del legacy y emite `VEINTIUN`.

Anexo A / Anexo B y el acta reproducen los campos y el texto legal del RDLC
(`rptContratoPrendario.rdlc`, `rptActaEntregaPrendario.rdlc`). No se transcriben las
19 cláusulas: el PDF oficial se anexa y se le estampa la fecha de generación.

### 2026-09-09 — Aviso WhatsApp de vencimiento prendario

**Tipo:** paridad con desviación deliberada

El legado envía plantillas de WhatsApp Business (Meta) al arrancar IIS
(`Global.asax`) y con `TareasProgramadas/ProbarWhatsapp?pClave=`. El moderno usa un
`BackgroundService` diario a las 08:00 (zona Lima) y opcionalmente una pasada al
arrancar. La plantilla es `aviso_vencimiento_prendario` (`{{1}}` nombre, `{{2}}`
fecha `dd/MM/yyyy`, `{{3}}` importe `MontoCredito + MontoCredito * Interes / 100`).
El token vive en user-secrets, no en el repositorio. Con credenciales de prueba Meta
solo entrega a números agregados como testers.

### 2026-09-09 — `usp_Credito_Ins` y el índice filtrado de prendario

**Tipo:** defecto de despliegue

`IX_Credito_EsPrendario` es un índice filtrado (`WHERE EsPrendario = 1`). Cualquier
`UPDATE`/`INSERT` sobre `CREDITO.Credito` desde un módulo creado con
`QUOTED_IDENTIFIER OFF` falla con el error 1934. `usp_Credito_Ins` (generar crédito,
prendario y ordinario) nació así en la base del cliente; la solicitud en `CRE` sí
insertaba porque Dapper abre la sesión con `QUOTED_IDENTIFIER ON`.

`2026-09-03-usp-credito-ins-quoted-identifier.sql` recrea el procedimiento con las
opciones correctas y deja el cuerpo intacto. `2026-06-usp-credito-ins-saldo-compat.sql`
pasa a emitir `SET QUOTED_IDENTIFIER ON` antes del `CREATE OR ALTER` para que una
restauración posterior no vuelva a dejarlo apagado.

### 2026-09-09 — CTE `Base` en el listado prendario

**Tipo:** defecto

`ListarAsync` usaba un CTE `Base` y luego dos `SELECT` (conteo y página). Un CTE solo
vive para la sentencia inmediata, así que el segundo `SELECT` respondía
«El nombre de objeto 'Base' no es válido». El conjunto intermedio pasa a una tabla
temporal `#PrendarioListado`.

