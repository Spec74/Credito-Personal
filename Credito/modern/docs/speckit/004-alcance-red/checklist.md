# Checklist — 004-alcance-red

| Ruta | Criterio | OK |
|------|----------|----|
| `/reportes/cobranza` en sucursal (ADMIN) | No hay combo Oficina; API ignora otra `oficinaId` | [x] |
| `/reportes/cobranza` en Central | Combo Oficina + “todas”; filas de esa sede o red | [x] |
| `/mantenimiento/cajas` sucursal | Solo cajas de la sesión; no crear en otra oficina | [x] |
| `/mantenimiento/cajas` Central | Ve la red; puede crear en una sede | [x] |
| `/inicio` Central | Selector Sede cambia KPIs sin re-login | [x] |
| `/inicio` sucursal | Sin selector; datos de su JWT | [x] |
| Caja diario / crédito (escritura) | Sigue exigiendo JWT = oficina | [x] |
| `/maestros/almacenes` sucursal | Solo almacenes de la sesión; no crear en otra oficina | [x] |
| `/maestros/almacenes` Central | Selector de sede o red; puede crear en una sede | [x] |
