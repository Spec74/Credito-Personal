# Roadmap almacenamiento de claves

Spec SSD de login / menú / inicio: [SSD-01-auth-inicio.md](../ssd/SSD-01-auth-inicio.md).

El legacy guarda `MAESTRO.Usuario.ClaveUsuario` en claro. La API moderna verifica en C#
(claro o PBKDF2 con prefijo `$pbk2$`) y puede migrar en el login si se activa
`Auth:MigracionClavePerezosa`.

## Bloqueo resuelto en código

`CreateHash` produce ~85 caracteres. La columna era `nvarchar(50)`: la migración
perezosa fallaba y un alta/reset desde la API moderna truncaría el hash.

Script versionado (no aplica claves; solo ensancha la columna):

`deploy/sql/2026-09-10-usuario-clave-pbkdf2.sql`

El `ALTER` ya está aplicado en CREDITO. `Auth:MigracionClavePerezosa` está en
`true` en Development, Staging, PreProduction y Production. El host de pruebas
(`creditotestwebhost.cs`) lo fuerza a `false`. El token de WhatsApp no forma parte
de este corte.

## Compatibilidad MVC

`Web/Helper/UsuarioPasswordHasherCompat.cs` replica el formato `$pbk2$`.
`HomeController.Autenticar` y `ConfirmarClave` (Home y caja diario) verifican en C#,
no con `ClaveUsuario == clave` en SQL. El plano legado sigue valiendo.

## Orden recomendado

1. Aplicar el `ALTER COLUMN` en la base. Hecho en CREDITO.
2. Activar migración perezosa solo en Development. Hecho.
3. Entrar por la SPA con usuario real (no `dev/token`). Ese login reescribe esa fila a `$pbk2$`.
4. Confirmar que el mismo usuario sigue pudiendo entrar (SPA y, si aplica, MVC).
5. Medir cuántas filas siguen en claro (`ClaveUsuario NOT LIKE '$pbk2$%'`).
6. Flag en preproducción/producción/staging. Hecho (requiere el `ALTER` aplicado en esa base).
7. Cuando no quede ninguna en claro, retirar la rama de comparación plana.
