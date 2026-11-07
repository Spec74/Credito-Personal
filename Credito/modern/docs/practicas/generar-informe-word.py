# -*- coding: utf-8 -*-
"""Genera el informe de prácticas en .docx con tipografía cercana a APA 7."""
from pathlib import Path

from docx import Document
from docx.enum.text import WD_ALIGN_PARAGRAPH, WD_LINE_SPACING
from docx.oxml.ns import qn
from docx.shared import Pt, Cm, Twips

ROOT = Path(__file__).resolve().parent
MD = ROOT / "INFORME-PRACTICAS-PREPROFESIONALES-MIGRACION-CREDIX.md"
OUT = ROOT / "INFORME-PRACTICAS-PREPROFESIONALES-MIGRACION-CREDIX.docx"


def set_run_font(run, size=12, bold=False, italic=False):
    run.font.name = "Times New Roman"
    run._element.rPr.rFonts.set(qn("w:eastAsia"), "Times New Roman")
    run.font.size = Pt(size)
    run.bold = bold
    run.italic = italic


def style_paragraph(p, *, first_line=True, center=False, space_after=0):
    pf = p.paragraph_format
    pf.line_spacing_rule = WD_LINE_SPACING.DOUBLE
    pf.space_after = Pt(space_after)
    pf.space_before = Pt(0)
    if center:
        p.alignment = WD_ALIGN_PARAGRAPH.CENTER
        pf.first_line_indent = Cm(0)
    elif first_line:
        p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
        pf.first_line_indent = Cm(1.27)
    else:
        p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
        pf.first_line_indent = Cm(0)


def add_heading_custom(doc, text, level=1):
    p = doc.add_paragraph()
    style_paragraph(p, first_line=False, center=(level == 0))
    run = p.add_run(text)
    set_run_font(run, size=12, bold=True)
    return p


def add_body(doc, text, *, first_line=True, center=False, italic=False, bold=False):
    p = doc.add_paragraph()
    style_paragraph(p, first_line=first_line, center=center)
    run = p.add_run(text)
    set_run_font(run, italic=italic, bold=bold)
    return p


def add_ref(doc, text):
    p = doc.add_paragraph()
    pf = p.paragraph_format
    pf.line_spacing_rule = WD_LINE_SPACING.DOUBLE
    pf.first_line_indent = Cm(-1.27)
    pf.left_indent = Cm(1.27)
    pf.space_after = Pt(0)
    p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
    run = p.add_run(text)
    set_run_font(run)
    return p


def parse_and_write(doc, lines):
    """Convierte un subconjunto de Markdown al docx."""
    in_code = False
    in_refs = False
    table_buffer = []

    def flush_table():
        nonlocal table_buffer
        if not table_buffer:
            return
        rows = [r for r in table_buffer if not set(r.replace("|", "").strip()) <= set("-: ")]
        parsed = []
        for r in rows:
            cells = [c.strip() for c in r.strip().strip("|").split("|")]
            parsed.append(cells)
        table_buffer = []
        if not parsed:
            return
        cols = max(len(r) for r in parsed)
        t = doc.add_table(rows=len(parsed), cols=cols)
        t.style = "Table Grid"
        for i, row in enumerate(parsed):
            for j in range(cols):
                cell_text = row[j] if j < len(row) else ""
                cell = t.rows[i].cells[j]
                cell.text = ""
                p = cell.paragraphs[0]
                run = p.add_run(cell_text)
                set_run_font(run, size=11, bold=(i == 0))
        doc.add_paragraph()

    for raw in lines:
        line = raw.rstrip("\n")

        if line.startswith("```"):
            flush_table()
            in_code = not in_code
            continue
        if in_code:
            add_body(doc, line, first_line=False)
            continue

        if line.startswith("|") and "|" in line[1:]:
            table_buffer.append(line)
            continue
        else:
            flush_table()

        if not line.strip():
            continue

        if line.startswith("---"):
            continue

        # Skip the tipografía note at top as separate italic note once
        if line.startswith("**Nota tipográfica"):
            add_body(doc, line.replace("**", ""), first_line=False, italic=True)
            continue

        if line.startswith("# "):
            add_heading_custom(doc, line[2:].strip(), level=0)
            in_refs = False
            continue
        if line.startswith("## "):
            title = line[3:].strip()
            add_heading_custom(doc, title, level=1)
            in_refs = title.lower().startswith("j.") or "bibliografía" in title.lower()
            continue
        if line.startswith("### "):
            add_heading_custom(doc, line[4:].strip(), level=2)
            continue

        # bold-only short lines (carátula fields)
        if line.startswith("**") and line.endswith("**") and line.count("**") == 2:
            add_body(doc, line.strip("*"), first_line=False, center=True, bold=True)
            continue

        if in_refs and line and not line.startswith("#") and not line.startswith("###"):
            # reference entries under bibliografía (not headings)
            if line.startswith("Inversiones") or (
                line[0].isalpha() or line[0] == "A" or line.startswith("ISO") or line.startswith("Amazon")
                or line.startswith("American") or line.startswith("Bass") or line.startswith("Evans")
                or line.startswith("Fowler") or line.startswith("Microsoft") or line.startswith("Newman")
                or line.startswith("Nielsen") or line.startswith("Pressman") or line.startswith("Sommerville")
                or line.startswith("Thoughtworks")
            ):
                add_ref(doc, line)
                continue

        # strip simple markdown bold/italic for body
        text = line
        # keep readable: remove single-pair ** for rendering as plain with some bold segments simplified
        text = text.replace("**", "")
        text = text.replace("*", "")
        text = text.replace("`", "")

        if text.startswith("- "):
            p = doc.add_paragraph(style="List Bullet")
            pf = p.paragraph_format
            pf.line_spacing_rule = WD_LINE_SPACING.DOUBLE
            run = p.add_run(text[2:])
            set_run_font(run)
            continue

        if len(text) >= 2 and text[0].isdigit() and text[1] in ".)":
            add_body(doc, text, first_line=False)
            continue

        # carátula-ish short centered lines without markdown
        if text.isupper() and len(text) > 10:
            add_body(doc, text, first_line=False, center=True, bold=True)
            continue

        add_body(doc, text, first_line=True)

    flush_table()


def main():
    doc = Document()
    for section in doc.sections:
        section.top_margin = Cm(2.54)
        section.bottom_margin = Cm(2.54)
        section.left_margin = Cm(2.54)
        section.right_margin = Cm(2.54)

    style = doc.styles["Normal"]
    style.font.name = "Times New Roman"
    style.font.size = Pt(12)
    style._element.rPr.rFonts.set(qn("w:eastAsia"), "Times New Roman")

    lines = MD.read_text(encoding="utf-8").splitlines()
    # Drop the leading H1 duplicate handling — full file
    parse_and_write(doc, lines)
    doc.save(OUT)
    print(f"OK -> {OUT}")


if __name__ == "__main__":
    main()
