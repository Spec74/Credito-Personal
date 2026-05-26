import base64
import os
import re
import sys

rdlc = sys.argv[1]
out = sys.argv[2]
text = open(rdlc, encoding="utf-8", errors="ignore").read()
m = re.search(
    r'<EmbeddedImage Name="logocredito">.*?<ImageData>([^<]+)</ImageData>',
    text,
    re.S,
)
if not m:
    raise SystemExit("logocredito not found")
data = base64.b64decode(m.group(1))
os.makedirs(os.path.dirname(out), exist_ok=True)
with open(out, "wb") as f:
    f.write(data)
print(len(data))
