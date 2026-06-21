from pathlib import Path
import re
import zipfile
import xml.etree.ElementTree as ET

PPTX = Path(r"C:\Users\Artem\Downloads\diplom\outputs\presentation_v4_work\template.pptx")
OUT = Path(r"C:\Users\Artem\Downloads\diplom\outputs\presentation_v4_work\template_text.txt")

NS = {
    "a": "http://schemas.openxmlformats.org/drawingml/2006/main",
    "p": "http://schemas.openxmlformats.org/presentationml/2006/main",
}

def slide_sort(name: str):
    m = re.search(r"slide(\d+)\.xml$", name)
    return int(m.group(1)) if m else 0

lines = []
with zipfile.ZipFile(PPTX) as z:
    slide_names = sorted(
        [n for n in z.namelist() if re.match(r"ppt/slides/slide\d+\.xml$", n)],
        key=slide_sort,
    )
    lines.append(f"slides: {len(slide_names)}")
    for idx, name in enumerate(slide_names, 1):
        root = ET.fromstring(z.read(name))
        lines.append(f"\n--- SLIDE {idx} {name} ---")
        for sp_i, sp in enumerate(root.findall(".//p:sp", NS), 1):
            texts = [t.text or "" for t in sp.findall(".//a:t", NS)]
            text = "\n".join([t for t in texts if t.strip()])
            if text.strip():
                c_nv_pr = sp.find(".//p:cNvPr", NS)
                shape_name = c_nv_pr.get("name") if c_nv_pr is not None else ""
                shape_id = c_nv_pr.get("id") if c_nv_pr is not None else ""
                lines.append(f"shape {shape_id} {shape_name}: {text.strip()}")

OUT.write_text("\n".join(lines), encoding="utf-8")
print(OUT)
