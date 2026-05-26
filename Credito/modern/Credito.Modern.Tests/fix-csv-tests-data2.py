import re
from pathlib import Path

root = Path(__file__).parent
for path in root.glob("*CsvFormatterTests.cs"):
    t = path.read_text(encoding="utf-8")
    t = re.sub(
        r"var text = Encoding\.UTF8\.GetString\(([^;]+)\);\s*"
        r"var inv = CultureInfo\.InvariantCulture;\s*"
        r"var line = text\.Split\('\\n', StringSplitOptions\.RemoveEmptyEntries\)\[1\]\.TrimEnd\('\\r'\);",
        r"var bytes = \1;\n        var inv = CultureInfo.InvariantCulture;\n        var line = DataLine(bytes);",
        t,
    )
    t = re.sub(
        r"var text = Encoding\.UTF8\.GetString\(([^;]+)\);\s*"
        r"var line = text\.Split\('\\n', StringSplitOptions\.RemoveEmptyEntries\)\[1\]\.TrimEnd\('\\r'\);\s*"
        r"Assert\.Equal\(",
        r"var line = DataLine(\1);\n        Assert.Equal(",
        t,
    )
    path.write_text(t, encoding="utf-8", newline="\r\n")
print("ok")
