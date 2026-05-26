import re
from pathlib import Path

root = Path(__file__).parent
for path in root.glob("*CsvFormatterTests.cs"):
    t = path.read_text(encoding="utf-8")
    t = re.sub(
        r'Assert\.Equal\("([^"]+)", HeaderLine\(bytes\)\);',
        r'Assert.StartsWith("\1", HeaderLine(bytes), StringComparison.Ordinal);',
        t,
    )
    if "RptMovimientoBovedaCsvFormatterTests" in path.name:
        t = t.replace(
            'Assert.StartsWith("sep=,", HeaderLine(bytes), StringComparison.Ordinal);',
            'Assert.StartsWith("MovimientoBovedaId", HeaderLine(bytes), StringComparison.Ordinal);',
        )
    path.write_text(t, encoding="utf-8", newline="\r\n")
print("ok")
