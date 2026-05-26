import re
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "src" / "pages" / "informes"
pattern = re.compile(
    r"breadcrumb=\{\[\s*"
    r"\{ title: <Link to=\"/inicio\">Inicio</Link> \},\s*"
    r"\{ title: <Link to=\"/credito\">Crédito</Link> \},\s*"
    r"\{ title: ('|\")(.+?)\1 \},\s*"
    r"\]\}",
    re.MULTILINE,
)

for path in sorted(root.glob("*.tsx")):
    text = path.read_text(encoding="utf-8")
    if 'Link to="/credito">Crédito' not in text:
        continue

    def repl(m: re.Match[str]) -> str:
        q, title = m.group(1), m.group(2)
        return f"breadcrumb={{reportesCreditoBreadcrumb({q}{title}{q})}}"

    new_text, n = pattern.subn(repl, text)
    if n == 0:
        print("SKIP", path.name)
        continue

    imp = "import { reportesCreditoBreadcrumb } from '../../utils/reportesBreadcrumbs'"
    if imp not in new_text:
        marker = "from '../../components/credix'"
        if marker in new_text:
            new_text = new_text.replace(marker, marker + "\n" + imp, 1)
        else:
            lines = new_text.splitlines()
            last_imp = 0
            for i, line in enumerate(lines):
                if line.startswith("import "):
                    last_imp = i
            lines.insert(last_imp + 1, imp)
            new_text = "\n".join(lines)

    path.write_text(new_text, encoding="utf-8")
    print("OK", path.name, n)
