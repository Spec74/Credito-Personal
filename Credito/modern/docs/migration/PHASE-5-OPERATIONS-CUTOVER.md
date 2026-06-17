# Fase 5 — Operaciones y cutover

## Cutover recomendado

1. Desplegar API moderna y SPA en preproducción.
2. Mantener MVC legacy detrás del proxy.
3. Ejecutar smoke por rol y módulo.
4. Activar rutas SPA por rebanada.
5. Monitorear errores y tiempos de respuesta.
6. Retirar rutas MVC solo después de validación de negocio.

## Rollback

El rollback consiste en reenrutar la rebanada afectada al MVC legacy y conservar API/SPA para las rutas ya validadas.
