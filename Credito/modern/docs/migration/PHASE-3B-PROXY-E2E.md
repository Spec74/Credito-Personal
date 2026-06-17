# Fase 3B — Proxy E2E

## Objetivo

Validar la convivencia MVC legacy + API moderna + SPA usando el proxy strangler.

## Comandos locales

```powershell
cd D:\Ebers\GitHub\Credito\modern
Copy-Item deploy\.env.example deploy\.env
.\deploy\scripts\start-strangler.ps1 -Build
.\deploy\scripts\verify-strangler-proxy.ps1
.\deploy\scripts\smoke-strangler-proxy.ps1
```

## Criterios

- `/api/v1` responde desde la API moderna.
- MVC sigue atendiendo rutas no migradas.
- SPA puede iniciar sesión, leer menú y navegar rutas migradas.
- RDLC legacy abre solo cuando `VITE_LEGACY_ORIGIN` apunta a un MVC válido.
