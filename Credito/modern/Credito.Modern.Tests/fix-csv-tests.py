import re
from pathlib import Path

root = Path(__file__).parent

for path in sorted(root.glob("*CsvFormatterTests.cs")):
    text = path.read_text(encoding="utf-8")
    if "using static Credito.Modern.Tests.CsvTestHelpers" not in text:
        text = text.replace(
            "namespace Credito.Modern.Tests;\n",
            "namespace Credito.Modern.Tests;\n\nusing static Credito.Modern.Tests.CsvTestHelpers;\n",
            1,
        )
    text = re.sub(
        r"Assert\.True\(bytes\.Length >= 3\);\s*"
        r"Assert\.Equal\(0xEF, bytes\[0\]\);\s*"
        r"Assert\.Equal\(0xBB, bytes\[1\]\);\s*"
        r"Assert\.Equal\(0xBF, bytes\[2\]\);",
        "AssertUtf8Bom(bytes);",
        text,
    )

    def repl_starts(m: re.Match[str]) -> str:
        header = m.group(1).replace("\\uFEFF", "")
        return f'Assert.Equal("{header}", HeaderLine(bytes));'

    text = re.sub(
        r"var text = Encoding\.UTF8\.GetString\(bytes\);\s*"
        r'Assert\.StartsWith\(\s*"([^"]+)",\s*text,\s*StringComparison\.Ordinal\);\s*',
        repl_starts,
        text,
        flags=re.DOTALL,
    )
    text = re.sub(
        r'Assert\.StartsWith\(\s*"([^"]+)",\s*text,\s*StringComparison\.Ordinal\);\s*',
        repl_starts,
        text,
        flags=re.DOTALL,
    )

    # multiline StartsWith
    text = re.sub(
        r"var text = Encoding\.UTF8\.GetString\(bytes\);\s*"
        r'Assert\.StartsWith\(\s*\n\s*"([^"]+)",\s*\n\s*text,\s*\n\s*StringComparison\.Ordinal\);\s*',
        repl_starts,
        text,
    )

    path.write_text(text, encoding="utf-8", newline="\r\n")
    print(path.name)

print("ok")
