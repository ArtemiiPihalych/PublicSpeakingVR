import { FileBlob, PresentationFile } from "@oai/artifact-tool";

const source = "C:/Users/Artem/Downloads/diplom/outputs/presentation_v4_work/template.pptx";
const presentation = await PresentationFile.importPptx(await FileBlob.load(source));
console.log("slides", presentation.slides.items.length);
const out = await PresentationFile.exportPptx(presentation);
await out.save("C:/Users/Artem/Downloads/diplom/outputs/presentation_v4_work/import_roundtrip.pptx");
