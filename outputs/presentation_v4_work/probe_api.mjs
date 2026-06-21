import { FileBlob, PresentationFile } from "@oai/artifact-tool";
const p = await PresentationFile.importPptx(await FileBlob.load("C:/Users/Artem/Downloads/diplom/outputs/presentation_v4_work/template.pptx"));
console.log(Object.keys(p));
console.log("slides keys", Object.keys(p.slides));
console.log("slides length", p.slides.items.length);
const s = p.slides.items[0];
console.log("slide keys", Object.keys(s));
console.log("shapes", Object.keys(s.shapes), s.shapes.items?.length);
for (const sh of s.shapes.items || []) {
  console.log("shape", Object.keys(sh), sh.id, sh.name, typeof sh.text, String(sh.text).slice(0,80));
}
