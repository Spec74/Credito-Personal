# Checklist — 004-alcance-red

| Ruta | Criterio | OK |
|------|----------|----|
| `/reportes/cobranza` en sucursal (ADMIN) | No hay combo Oficina; API ignora otra `oficinaId` | [ ] |
| `/reportes/cobranza` en Central | Combo Oficina + “todas”; filas de esa sede o red | [ ] |
| `/mantenimiento/cajas` sucursal | Solo cajas de la sesión; no crear en otra oficina | [ ] |
| `/mantenimiento/cajas` Central | Ve la red; puede crear en una sede | [ ] |
| `/inicio` Central | Selector Sede cambia KPIs sin re-login | [ ] |
| `/inicio` sucursal | Sin selector; datos de su JWT | [ ] |
| Caja diario / crédito (escritura) | Sigue exigiendo JWT = oficina | [ ] |
