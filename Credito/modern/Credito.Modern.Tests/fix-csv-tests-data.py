import re
from pathlib import Path

root = Path(__file__).parent

for path in sorted(root.glob("*CsvFormatterTests.cs")):
    text = path.read_text(encoding="utf-8")

    text = re.sub(
        r"var text = Encoding\.UTF8\.GetString\(([^;]+)\);\s*"
        r"var line = text\.Split\('\\n', StringSplitOptions\.RemoveEmptyEntries\)\[1\]\.TrimEnd\('\\r'\);",
        r"var line = DataLine(\1);",
        text,
    )

    text = re.sub(
        r"var text = Encoding\.UTF8\.GetString\(([^;]+)\);\s*"
        r"var lines = text\.Split\('\\n', StringSplitOptions\.RemoveEmptyEntries\);\s*"
        r"Assert\.Equal\(2, lines\.Length\);\s*"
        r"var line = lines\[1\]\.TrimEnd\('\\r'\);",
        r"var line = DataLine(\1);",
        text,
    )

    text = re.sub(
        r"var text = Encoding\.UTF8\.GetString\(([^;]+)\);\s*"
        r"var lines = text\.Split\('\\n', StringSplitOptions\.RemoveEmptyEntries\);\s*"
        r"var line = lines\.Length >= 3 \? lines\[2\]\.TrimEnd\('\\r'\) : lines\[\^1\]\.TrimEnd\('\\r'\);",
        r"var line = DataLine(\1);",
        text,
    )

    text = text.replace(
        'HeaderLine(bytes));}',
        'HeaderLine(bytes));\n    }',
    )

    path.write_text(text, encoding="utf-8", newline="\r\n")

print("ok")
