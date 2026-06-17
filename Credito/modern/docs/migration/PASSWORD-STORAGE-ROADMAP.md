# Roadmap almacenamiento de claves

El legacy puede tener `MAESTRO.Usuario.ClaveUsuario` en claro. La API moderna soporta PBKDF2 con prefijo `$pbk2$` y migración perezosa controlada por configuración.

## Recomendación

- Mantener compatibilidad durante cutover.
- Activar migración perezosa en entorno controlado.
- Confirmar que MVC legacy acepte el formato hash compatible.
- Retirar comparación de claves en claro cuando todos los usuarios estén migrados.
