# SSD-XX — {Nombre del módulo}

**Estado:** as-built · {fecha}  
**Código:** {rutas SPA} · {prefijo API}  
**Fuente legacy:** {controller / vistas}  
**Doc de ingeniería (si existe):** {archivo `*_migracion.md`}

## 1. Propósito y actores

Quién usa esta rebanada y para qué (una frase por rol: gestor, cajero, encargado, analista, administrador).

## 2. Alcance

**Entra**

- …

**No entra**

- … (otra rebanada, puente RDLC, etc.)

## 3. Paridad MVC

| Legacy (URL / acción) | SPA | Observación |
|-----------------------|-----|-------------|
| | | |

Menú `MAESTRO.usp_MenuLst`: denominación, módulo, URL.

## 4. Contrato de datos

Lógica de negocio: solo `usp_*` / tablas versionadas en `db/schema`. El C# no inventa importes.

| Procedimiento o tabla | Uso |
|-----------------------|-----|
| | |

## 5. API y SPA

| Método y ruta | Política JWT | Pantalla |
|---------------|--------------|----------|
| | | |

## 6. Seguridad

Oficina y usuario del JWT. Roles que habilitan la ruta (`EXACT_MENU_ROUTES` si aplica).

## 7. Criterios de aceptación

- [ ] Login con rol X abre la pantalla.
- [ ] La operación Y produce el mismo efecto que el MVC (mismo SP).
- [ ] Sin permiso: 403 / menú ausente.

## 8. Desviaciones

Solo id + enlace. El relato vive en `docs/migration/BITACORA-DESVIACIONES.md`.

- …

## 9. Pruebas y evidencia

- Tests: `{archivo}`  
- Smoke manual: …

## 10. Go-live

Pendiente / listo en Development / listo en preprod.
