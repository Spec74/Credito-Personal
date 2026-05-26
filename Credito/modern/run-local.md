# Ejecutar Credito Modern sin código fuente (solo binarios)

El árbol `src/` y los `.csproj` **no están en disco** (solo quedan `bin/`, `obj/` y `dist/`). Hasta restaurar el repositorio, use estos comandos.

## API (.NET)

`dotnet run --project` falla porque falta `Credito.Modern.Api.csproj`. Use el ejecutable ya compilado:

```powershell
cd D:\GitHub\Credito\modern
.\scripts\run-api.ps1
```

- URL: **http://localhost:5288** (Swagger: `/swagger`)
- HTTPS: **https://localhost:7288**

Si arranca en otro puerto (p. ej. 5000), el script fija `ASPNETCORE_URLS` a 5288/7288.
- Variables: lee `deploy\.env` (cadena SQL, JWT).

Si SQL está en la máquina local (no Docker), edite en `deploy\.env`:

```env
CREDITO_DB_CONNECTION_STRING=Server=localhost,14330;Database=CREDITO;User Id=sa;Password=...;TrustServerCertificate=True;Encrypt=True;
```

## Frontend (SPA)

Sin `package.json` no hay `npm run dev`. Sirva el build en `dist/`:

```powershell
cd D:\GitHub\Credito\modern
.\scripts\run-web.ps1
```

- Abra: **http://localhost:5173/app/**
- El CSS responsive extra está en `dist/assets/credix-responsive-global.css`.

## Probar responsive

DevTools → ancho ~375px → rutas: `/app/credito/aprobar`, `/app/caja/saldos`, `/app/informes/comprobantes-caja-chica`.

## Recuperar código fuente

1. Copia de otro equipo / OneDrive / backup.
2. Rama remota si existió: `git fetch` + buscar rama con `modern/`.
3. **No** hay `modern/` en el stash actual del repo `Credito`.

Cuando vuelva el source, importe en `main.tsx`:

```ts
import './styles/credix-responsive-global.css'
```
