#!/usr/bin/env python3
"""Inserta bloques MapGet -pdf tras cada MapGet -csv en Program.cs (si aún no existe -pdf)."""
from __future__ import annotations

import re
from pathlib import Path

PROGRAM = Path(__file__).resolve().parents[1] / "Credito.Modern.Api" / "Program.cs"


def title_from_pdf(filename: str) -> str:
    base = filename.replace(".pdf", "").replace("-", " ")
    return base[:1].upper() + base[1:] if base else "Informe"


def transform_csv_block(block: str) -> str:
    route_m = re.search(r'"/api/v1/([^"]+)-csv"', block)
    if not route_m:
        return ""
    route_base = route_m.group(1)
    pdf_route = f'"/api/v1/{route_base}-pdf"'

    if f'"{pdf_route}"' in block or f"{route_base}-pdf" in block:
        return ""

    b = block.replace(f'"/api/v1/{route_base}-csv"', pdf_route)
    b = re.sub(r'CreateLogger\("([^"]+)Csv"\)', r'CreateLogger("\1Pdf")', b)
    b = b.replace("(CSV)", "(PDF)")
    b = b.replace("generar el CSV", "generar el PDF")
    b = b.replace("el CSV de", "el PDF de")
    b = b.replace(" (CSV)", " (PDF)")

    def file_repl(m: re.Match) -> str:
        formatter = m.group(1)
        csv_name = m.group(2)
        pdf_name = csv_name.replace(".csv", ".pdf")
        title = title_from_pdf(pdf_name)
        return (
            f"var csvBytes = {formatter}.ToUtf8BomCsv(items);\n"
            f"                var bytes = TabularPdfDocument.FromUtf8BomCsv(\"{title}\", csvBytes);\n"
            f'                return TypedResults.File(bytes, "application/pdf", fileDownloadName: "{pdf_name}");'
        )

    b = re.sub(
        r"var bytes = (\w+)\.ToUtf8BomCsv\(items\);\s*"
        r'return TypedResults\.File\(bytes, "text/csv; charset=utf-8", fileDownloadName: "([^"]+)"\);',
        file_repl,
        b,
        count=1,
    )

    b = re.sub(r'\.WithName\("([^"]+)Csv"\)', r'.WithName("\1Pdf")', b)
    b = b.replace("en CSV UTF-8 (BOM)", "en PDF tabular (QuestPDF, columnas = CSV)")
    b = b.replace("UTF-8 (BOM)", "PDF tabular (QuestPDF)")
    b = b.replace('contentType: "text/csv"', 'contentType: "application/pdf"')
    b = re.sub(
        r'\.Produces\(StatusCodes\.Status200OK, contentType: "text/csv"\)',
        '.Produces(StatusCodes.Status200OK, contentType: "application/pdf")',
        b,
    )
      b = b.replace("sin motor RDLC", "PDF tabular sin layout RDLC")
    return b


def main() -> None:
    text = PROGRAM.read_text(encoding="utf-8")

    # Eliminar bloques PDF dedicados (observado/condonado) para unificar patrón CSV→PDF
    text = re.sub(
        r'\napp\.MapGet\(\s*"/api/v1/credito/rpt-credito-observado-pdf",.*?'
        r'\.ProducesProblem\(StatusCodes\.Status503ServiceUnavailable\);\n',
        "\n",
        text,
        count=1,
        flags=re.DOTALL,
    )
    text = re.sub(
        r'\napp\.MapGet\(\s*"/api/v1/credito/rpt-credito-condonado-pdf",.*?'
        r'\.ProducesProblem\(StatusCodes\.Status503ServiceUnavailable\);\n',
        "\n",
        text,
        count=1,
        flags=re.DOTALL,
    )

    pattern = re.compile(
        r'(app\.MapGet\(\s*"/api/v1/[^"]+-csv".*?'
        r'\.ProducesProblem\(StatusCodes\.Status503ServiceUnavailable\);\n)',
        re.DOTALL,
    )

    inserts = 0
    parts: list[str] = []
    last = 0
    for m in pattern.finditer(text):
        block = m.group(1)
        parts.append(text[last : m.end()])
        pdf_block = transform_csv_block(block)
        if pdf_block:
            parts.append("\n" + pdf_block)
            inserts += 1
        last = m.end()
    parts.append(text[last:])
    text = "".join(parts)

    PROGRAM.write_text(text, encoding="utf-8")
    print(f"Inserted {inserts} PDF endpoint blocks into {PROGRAM}")


if __name__ == "__main__":
    main()
