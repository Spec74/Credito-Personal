#!/usr/bin/env python3
"""Insert tabular PDF export endpoints after matching -csv blocks in program.cs."""

from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROGRAM = ROOT / "Credito.Modern.Api" / "program.cs"

TITLE_BY_ROUTE = {
    "/api/v1/credito/listar-saldo-cartera-pdf": "Saldo cartera",
    "/api/v1/credito/rpt-movimiento-credito-pdf": "Movimiento crédito",
    "/api/v1/credito/rpt-cajas-asignadas-pdf": "Cajas asignadas",
    "/api/v1/credito/rpt-cobro-diario-pdf": "Cobro diario",
    "/api/v1/credito/rpt-cobro-diario-detalle-pdf": "Cobro diario detalle",
    "/api/v1/credito/rpt-clientes-bloqueados-pdf": "Clientes bloqueados",
    "/api/v1/credito/rpt-clientes-tope-credito-pdf": "Clientes tope crédito",
    "/api/v1/credito/rpt-aval-pdf": "Avales",
    "/api/v1/credito/rpt-saldos-caja-pdf": "Saldos caja",
    "/api/v1/credito/rpt-movimiento-boveda-pdf": "Movimiento bóveda",
    "/api/v1/credito/central-riesgo-generar-pdf": "Central de riesgo",
    "/api/v1/credito/rpt-credito-pdf": "Reporte crédito",
    "/api/v1/credito/rpt-credito-morosidad-pdf": "Morosidad crédito",
    "/api/v1/credito/rpt-credito-rentabilidad-pdf": "Rentabilidad crédito",
    "/api/v1/credito/rpt-credito-aprobacion-pdf": "Aprobación crédito",
    "/api/v1/credito/rpt-creditos-activos-pdf": "Créditos activos",
    "/api/v1/credito/rpt-creditos-cierres-pdf": "Créditos cierres",
    "/api/v1/credito/rpt-creditos-morosos-pagados-pdf": "Morosos pagados",
    "/api/v1/credito/rpt-clientes-inactivos-pdf": "Clientes inactivos",
    "/api/v1/credito/rpt-caja-diario-pdf": "Caja diario",
    "/api/v1/credito/rpt-credito-vencido-pdf": "Crédito vencido",
    "/api/v1/credito/rpt-movimiento-caja-anulado-pdf": "Movimiento caja anulado",
    "/api/v1/credito/rpt-saldo-cartera-caja-diario-pdf": "Saldo cartera caja diario",
    "/api/v1/credito/pagos-no-verificados-pdf": "Pagos no verificados",
    "/api/v1/credito/rpt-clientes-nuevos-mes-pdf": "Clientes nuevos del mes",
    "/api/v1/almacen/rpt-stock-anulados-pdf": "Stock anulados",
    "/api/v1/ventas/rpt-lista-precio-pdf": "Lista de precios",
    "/api/v1/ventas/rpt-rentabilidad-venta-pdf": "Rentabilidad venta",
}


def pdf_route_from_csv(csv_route: str) -> str:
    return csv_route.replace("-csv", "-pdf")


def make_pdf_block(csv_block: str, pdf_route: str) -> str:
    title = TITLE_BY_ROUTE.get(pdf_route, pdf_route.rsplit("/", 1)[-1].replace("-", " ").title())
    block = csv_block.replace(csv_route := re.search(r'"/api/v1/[^"]+-csv"', csv_block).group(0).strip('"'), pdf_route)
    block = block.replace("-csv", "-pdf")
    block = re.sub(r'CreateLogger\("([^"]+)Csv"\)', r'CreateLogger("\1Pdf")', block)
    block = block.replace("(CSV)", "(PDF)")
    block = block.replace("CSV de ", "PDF de ")
    block = block.replace("CSV UTF-8 (BOM)", "PDF tabular (QuestPDF)")
    block = block.replace(" el CSV ", " el PDF ")
    block = block.replace("Csv\"", "Pdf\"")
    block = block.replace("Csv\n", "Pdf\n")
    block = block.replace("contentType: \"text/csv\"", "contentType: \"application/pdf\"")
    block = block.replace('"text/csv; charset=utf-8"', '"application/pdf"')
    block = block.replace(".csv\"", ".pdf\"")
    block = block.replace(".csv`", ".pdf`")

    # Wrap CSV bytes -> tabular PDF
    def repl(m: re.Match[str]) -> str:
        formatter = m.group(1)
        args = m.group(2)
        indent = m.group(3)
        return (
            f"var csvBytes = {formatter}.ToUtf8BomCsv({args});\n"
            f"{indent}var bytes = TabularPdfDocument.FromUtf8BomCsv(\"{title}\", csvBytes);\n"
            f"{indent}return TypedResults.File(bytes,"
        )

    block = re.sub(
        r"var bytes = ([A-Za-z0-9_]+)\.ToUtf8BomCsv\(([^)]*)\);\s*\n(\s*)return TypedResults\.File\(bytes,",
        repl,
        block,
        count=1,
    )
    return block


def main() -> None:
    text = PROGRAM.read_text(encoding="utf-8")
    if "TabularPdfDocument" not in text.split("var builder")[0]:
        text = text.replace(
            "using Credito.Modern.Infrastructure;\n",
            "using Credito.Modern.Application.Reportes;\nusing Credito.Modern.Infrastructure;\n",
            1,
        )

    map_pat = re.compile(
        r'app\.MapGet\(\s*\n\s*"/api/v1/[^"]+-csv"[\s\S]*?\.ProducesProblem\(StatusCodes\.Status503ServiceUnavailable\);\s*\n',
        re.MULTILINE,
    )
    blocks = list(map_pat.finditer(text))
    insertions: list[tuple[int, str]] = []
    for m in blocks:
        csv_block = m.group(0)
        csv_route = re.search(r'"/api/v1/[^"]+-csv"', csv_block).group(0).strip('"')
        pdf_route = pdf_route_from_csv(csv_route)
        if pdf_route not in TITLE_BY_ROUTE:
            continue
        if pdf_route in text:
            continue
        pdf_block = make_pdf_block(csv_block, pdf_route)
        insertions.append((m.end(), pdf_block))

    if not insertions:
        print("No PDF blocks to insert.")
        return

    # Insert from end to start to preserve offsets
    for pos, block in reversed(insertions):
        text = text[:pos] + block + text[pos:]

    PROGRAM.write_text(text, encoding="utf-8", newline="\n")
    print(f"Inserted {len(insertions)} tabular PDF endpoint block(s).")


if __name__ == "__main__":
    main()
