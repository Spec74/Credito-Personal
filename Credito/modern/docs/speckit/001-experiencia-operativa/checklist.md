# Checklist de calidad — 001-experiencia-operativa

**Spec Kit:** `/speckit.checklist` · Usar en cada PR de Oleada A/B.

## Requisitos (spec)

Aplica a **cualquier** ruta del moderno (caja, crédito, clientes, informes, ventas, almacén, admin), no solo prendario.

- [ ] La pantalla tiene **una** acción primaria
- [ ] La sucursal activa se ve sin abrir un menú
- [ ] No hay párrafo de ayuda permanente ni doble alerta
- [ ] Error junto al campo; banner solo si bloquea el submit
- [ ] Estados (CRE/DES/mora/custodia) tienen texto, no solo color
- [ ] Oficina JWT respeta la sede; consolidado solo con permiso
- [ ] No se recalcula cuota/mora/LTV tope en el cliente (solo se muestra lo del contrato)
- [ ] Responsive del **puesto** (mostrador / tablet / cobrador), no “se ve chico”

## Accesibilidad (WCAG 2.2 AA, flujos P0)

- [ ] Contraste de texto y de estados
- [ ] Foco visible (`--credix-focus-ring`)
- [ ] Target ≥ 24 px (táctil ≥ 40)
- [ ] Teclado: tab order = orden de lectura; Enter no dispara irreversibles
- [ ] Labels en inputs; tablas con encabezados

## Multisucursal

- [ ] Probado mentalmente en oficina ≠ 1
- [ ] Alta de sede no termina en “oficina creada” sin cajas/roles/fondeo
- [ ] Transferencia muestra oficina destino y monto grande (MoneyConfirm)

## Constitución

- [ ] No se regeneró EDMX
- [ ] No hay secretos nuevos en Git
- [ ] Tokens, no hex sueltos en la página

## Review 2026-10-21 (P0 actual)

Evidencia: T-A9, T-E2 (Docker 9080, RMANTILLA / YCERVANTES) + código de esta oleada.

- [x] Una acción primaria por pantalla P0 (Hoy, cobro, bóveda, fondeo, prendario nuevo/gestión)
- [x] Sucursal visible en `CredixContextBar` (más «Cambiar sede» → re-login)
- [x] Sin párrafo de ayuda permanente en P0 restantes (se quitó la nota de vencimiento/remate en gestión)
- [x] Error de campo en formularios P0; banner solo si bloquea
- [x] Estados CRE/DES/mora/custodia con texto
- [x] JWT `vendix:oficina_id`; consolidado solo con rol/permiso
- [x] Cuota/mora/LTV no se recalculan en el cliente (LTV observado; tope 100%)
- [x] Puesto mostrador/tablet verificado en cobro 390px (T-A9)
- [x] Contraste + foco `--credix-focus-ring` + targets ≥ 24 px en chrome
- [x] Labels en login, prórroga, filtros; tablas Credix con encabezados
- [x] Oficina ≠ 1 ensayada (Pichari/Huanta T-B9); cambio de sede ya no rebota a `/inicio`
- [x] Wizard sede no termina en “oficina creada” suelta
- [x] Transferencia/fondeo usa MoneyConfirm
- [x] Sin EDMX nuevo; sin secretos nuevos; tokens `--credix-*`
