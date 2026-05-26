#!/usr/bin/env python3
"""Post-dedupe manual fixes for known duplicate patterns."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Credito.Modern.Web" / "src"

# legacyRoutes duplicate MODULO_HUB block
lr_path = ROOT / "utils" / "legacyroutes.ts"
lr = lr_path.read_text(encoding="utf-8")
dup_hub = """
/** Hub SPA cuando el ítem de menú no trae URL mapeable (paridad módulo MVC). */
const MODULO_HUB: Record<string, string> = {
  CREDITO: '/credito',
  REPORTES: '/informes',
  REPORTE: '/informes',
  CAJA: '/caja',
  VENTAS: '/ventas',
  ALMACEN: '/almacen',
  ADMINISTRACION: '/admin',
  TESORERIA: '/tesoreria',
  CLIENTE: '/clientes',
  CLIENTES: '/clientes',
  MAESTRO: '/maestros',
  MAESTROS: '/maestros',
}

export function resolveSpaPathFromModulo(modulo: string | null | undefined): string | null {
  if (!modulo?.trim()) {
    return null
  }
  const key = modulo
    .trim()
    .toUpperCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
  return MODULO_HUB[key] ?? null
}

"""
if lr.count(dup_hub.strip()) > 1:
    lr = lr.replace(dup_hub, "\n", 1)
    lr_path.write_text(lr, encoding="utf-8")

# menutree
mt_path = ROOT / "utils" / "menutree.ts"
mt = mt_path.read_text(encoding="utf-8")
mt = mt.replace(
    "import { formatMenuLabel } from './formatMenuLabel'\nimport { formatMenuLabel } from './formatMenuLabel'",
    "import { formatMenuLabel } from './formatMenuLabel'",
)
mt_path.write_text(mt, encoding="utf-8")

# routes index
ri_path = ROOT / "routes" / "index.tsx"
ri = ri_path.read_text(encoding="utf-8")
ri = ri.replace(
    "import { CreditoOperacionRoute } from '../components/credito/CreditoOperacionRoute'\n"
    "import { CreditoOperacionRoute } from '../components/credito/CreditoOperacionRoute'",
    "import { CreditoOperacionRoute } from '../components/credito/CreditoOperacionRoute'",
)
block = (
    '        <Route path="/reportes/credito" element={<Pages.ReporteCreditoIndexPage />} />\n'
    '        <Route path="/reportes/almacen" element={<Pages.ReporteAlmacenIndexPage />} />\n'
    '        <Route path="/reportes/cobranza" element={<Pages.CobranzaPagosPage />} />\n'
)
if block * 2 in ri:
    ri = ri.replace(block * 2, block)
ri_path.write_text(ri, encoding="utf-8")

# resolveSpaPathFromMenuItem
rp_path = ROOT / "utils" / "resolvespapathfrommenuitem.ts"
rp = rp_path.read_text(encoding="utf-8")
dup_alm = """  if (
    (label === 'almacen' || label.includes('almacen') || label.includes('stock')) &&
    (mod.includes('REPORTE') || mod === 'REPORTES')
  ) {
    return '/reportes/almacen'
  }

"""
if dup_alm * 2 in rp:
    rp = rp.replace(dup_alm * 2, dup_alm)
    rp_path.write_text(rp, encoding="utf-8")

# BovedaPage: remove unused planPago block and duplicate oficinaLabel if present
bp_path = ROOT / "pages" / "tesoreria" / "bovedapage.tsx"
if bp_path.exists():
    bp = bp_path.read_text(encoding="utf-8")
    bp = bp.replace(
        "  const oficinaLabel = getLoginProfile().oficinaLabel ?? `Oficina ${oficinaId}`\n"
        "  const oficinaLabel = getLoginProfile().oficinaLabel ?? `Oficina ${oficinaId}`\n",
        "  const oficinaLabel = getLoginProfile().oficinaLabel ?? `Oficina ${oficinaId}`\n",
    )
    bp = bp.replace(
        "import { BovedaResumenCuenta } from './components/BovedaResumenCuenta'\n"
        "import { BovedaSaldosGrid } from './components/BovedaSaldosGrid'\n"
        "import { BovedaResumenCuenta } from './components/BovedaResumenCuenta'\n"
        "import { BovedaSaldosGrid } from './components/BovedaSaldosGrid'\n",
        "import { BovedaResumenCuenta } from './components/BovedaResumenCuenta'\n"
        "import { BovedaSaldosGrid } from './components/BovedaSaldosGrid'\n",
    )
    plan = """  const planPago = useQuery({
    queryKey: ['monto-pendiente-plan-pago', oficinaId],
    queryFn: () => fetchMontoPendientePlanPago(oficinaId),
    enabled: oficinaId > 0,
  })

"""
    if plan in bp:
        bp = bp.replace(plan, "")
    bp_path.write_text(bp, encoding="utf-8")

print("Post-fixes applied.")
