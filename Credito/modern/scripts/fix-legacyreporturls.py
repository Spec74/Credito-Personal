#!/usr/bin/env python3
"""Remove pasted duplicate union/interface tails from legacyreporturls.ts."""

from pathlib import Path
import re

path = Path(__file__).resolve().parents[1] / "Credito.Modern.Web" / "src" / "config" / "legacyreporturls.ts"
text = path.read_text(encoding="utf-8")

orphan_block = """
  | 'credito-vencido'
  | 'credito-observado'
  | 'credito-morosidad'

export interface CreditoMorosidadLegacyParams {
  oficinaId?: number
  hastaFecha: string
  diasAtrazoIni: number
  diasAtrazoFin: number
}
"""
while orphan_block.strip() in text:
    text = text.replace(orphan_block, "\n", 1)

orphan_tail = """
  | 'credito-vencido'
  | 'credito-observado'
  | 'credito-morosidad'
"""
while orphan_tail.strip() in text:
    text = text.replace(orphan_tail, "\n", 1)

while "export type CreditoVencidoLegacyFranja = 'todos' | 'menor60' | 'mayor60' | 'irrecuperable'\n\nexport type CreditoVencidoLegacyFranja" in text:
    text = text.replace(
        "export type CreditoVencidoLegacyFranja = 'todos' | 'menor60' | 'mayor60' | 'irrecuperable'\n\nexport type CreditoVencidoLegacyFranja",
        "export type CreditoVencidoLegacyFranja",
        1,
    )

# Second copy of the creditoPlanes import block
needle = "} from '../api/creditoPlanes'\nimport { legacyDateToApi } from './legacyReportDate'\n\nimport {"
first = text.find(needle)
if first != -1:
    second = text.find(needle, first + 1)
    end = text.find("/** Parámetros para abrir", second if second != -1 else first)
    if second != -1 and end != -1:
        text = text[:second] + text[end:]

text = re.sub(r"\n{3,}", "\n\n", text)
path.write_text(text, encoding="utf-8")
print(f"Fixed {path.name} ({len(text.splitlines())} lines)")
