from pathlib import Path

root = Path(__file__).resolve().parents[1] / "src" / "pages" / "informes"
needle = "import { Link } from 'react-router-dom'\n"

for path in root.glob("*.tsx"):
    text = path.read_text(encoding="utf-8")
    if needle not in text:
        continue
    new = text.replace(needle, "", 1)
    if "Link" in new and "<Link" in new:
        continue
    path.write_text(new, encoding="utf-8")
    print("removed Link import:", path.name)

path = root / "MorosidadGestorPage.tsx"
text = path.read_text(encoding="utf-8")
block = """  const exportarCsv = () => {
    if (filas.length === 0) {
      message.warning('Consulte primero; no hay filas con mora > 0')
      return
    }
    downloadTableCsv('morosidad-gestor.csv', [
      ['CreditoId', 'Cliente', 'Celular', 'Saldo', 'Mora', 'DiasAtrazo', 'FormaPago'],
      ...filas.map((r) => [
        String(r.creditoId),
        r.cliente ?? '',
        r.celular ?? '',
        String(r.saldo ?? ''),
        String(r.mora ?? ''),
        String(r.diasAtrazo ?? ''),
        r.formaPago ?? '',
      ]),
    ])
    message.success('CSV descargado (solo mora > 0)')
  }

"""
if block in text:
    text = text.replace(block, "")
    text = text.replace("import { downloadTableCsv } from '../../utils/downloadTableCsv'\n", "")
    path.write_text(text, encoding="utf-8")
    print("cleaned MorosidadGestorPage")
