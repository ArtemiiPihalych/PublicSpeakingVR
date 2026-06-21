import fs from "node:fs/promises";
import { FileBlob, PresentationFile } from "@oai/artifact-tool";

const TEMPLATE = "C:/Users/Artem/Downloads/diplom/outputs/presentation_v4_work/template.pptx";
const OUT = "C:/Users/Artem/Downloads/diplom/Презентация_защита_ДП_Федин_АА.pptx";
const PREVIEW_DIR = "C:/Users/Artem/Downloads/diplom/outputs/presentation_v4_work/preview";

const presentation = await PresentationFile.importPptx(await FileBlob.load(TEMPLATE));

function textShapes(slide) {
  return slide.shapes.items.filter((shape) => shape.text !== undefined);
}

function setSlideText(slideIndex, title, body) {
  const slide = presentation.slides.items[slideIndex - 1];
  const shapes = textShapes(slide);
  if (shapes.length >= 2) {
    shapes[0].text = title;
    shapes[0].text.style = { fontSize: 34, bold: true, color: "#000000", fontFace: "Times New Roman" };
    shapes[1].text = body;
    shapes[1].text.style = { fontSize: 18, color: "#000000", fontFace: "Times New Roman" };
  } else if (shapes.length === 1) {
    shapes[0].text = `${title}\n${body}`;
    shapes[0].text.style = { fontSize: 18, color: "#000000", fontFace: "Times New Roman" };
  }
}

function addBox(slide, left, top, width, height, text, fill = "#EAF3F8", line = "#9CB7C7", fontSize = 18) {
  const box = slide.shapes.add({
    geometry: "roundRect",
    position: { left, top, width, height },
    fill,
    line: { style: "solid", fill: line, width: 1 },
  });
  box.text = text;
  box.text.style = { fontSize, color: "#1F2933", fontFace: "Aptos" };
  return box;
}

function addLabel(slide, left, top, width, height, text, fontSize = 18, bold = false, color = "#1F2933") {
  const label = slide.shapes.add({
    geometry: "textbox",
    position: { left, top, width, height },
    fill: "none",
    line: { style: "solid", fill: "none", width: 0 },
  });
  label.text = text;
  label.text.style = { fontSize, bold, color, fontFace: "Aptos" };
  return label;
}

function addSmallTable(slide, left, top, colWidths, rowHeight, rows, fontSize = 14) {
  let y = top;
  for (let r = 0; r < rows.length; r++) {
    let x = left;
    for (let c = 0; c < rows[r].length; c++) {
      const fill = r === 0 ? "#1F4E5F" : c === rows[r].length - 1 ? "#EAF6EA" : "#FFFFFF";
      const color = r === 0 ? "#FFFFFF" : "#1F2933";
      const box = slide.shapes.add({
        geometry: "rect",
        position: { left: x, top: y, width: colWidths[c], height: rowHeight },
        fill,
        line: { style: "solid", fill: "#B8C7D1", width: 1 },
      });
      box.text = rows[r][c];
      box.text.style = { fontSize, bold: r === 0, color, fontFace: "Aptos" };
      x += colWidths[c];
    }
    y += rowHeight;
  }
}

function addBarChart(slide, left, top, width, height, labels, values) {
  const max = 80;
  addLabel(slide, left, top - 34, width, 26, "Средний FPS по этапам", 18, true);
  const chartBottom = top + height;
  slide.shapes.add({
    geometry: "line",
    position: { left, top: chartBottom, width, height: 0 },
    line: { style: "solid", fill: "#607D8B", width: 2 },
  });
  const gap = 28;
  const barW = (width - gap * (values.length - 1)) / values.length;
  values.forEach((value, i) => {
    const h = (value / max) * height;
    const x = left + i * (barW + gap);
    const y = chartBottom - h;
    const bar = slide.shapes.add({
      geometry: "roundRect",
      position: { left: x, top: y, width: barW, height: h },
      fill: ["#2F80ED", "#27AE60", "#F2994A", "#9B51E0"][i],
      line: { style: "solid", fill: "none", width: 0 },
    });
    bar.text = String(value);
    bar.text.style = { fontSize: 18, bold: true, color: "#FFFFFF", fontFace: "Aptos" };
    addLabel(slide, x - 8, chartBottom + 10, barW + 16, 44, labels[i], 12, false);
  });
}

function addFlow(slide, items, left = 130, top = 325) {
  const boxW = 142;
  const boxH = 66;
  const gap = 28;
  items.forEach((item, i) => {
    addBox(slide, left + i * (boxW + gap), top, boxW, boxH, item, "#F4F8FA", "#9CB7C7", 15);
    if (i < items.length - 1) {
      addLabel(slide, left + i * (boxW + gap) + boxW + 4, top + 17, gap, 26, "→", 22, true, "#607D8B");
    }
  });
}

const titleText = `Министерство образования и науки Челябинской области
государственное бюджетное профессиональное образовательное учреждение
«Южно-Уральский государственный колледж»
Кафедра «Информатики и вычислительной техники»

ДИПЛОМНЫЙ ПРОЕКТ
Разработка обучающего VR-приложения

специальность СПО 09.02.07 «Информационные системы и программирование»

Дипломный проект выполнил:
студент группы ИСп319ДК, Федин А.А.
очного отделения
Руководитель: преподаватель Исаев А.Н.

Челябинск 2026`;

presentation.slides.items[0].shapes.items[0].text = titleText;
presentation.slides.items[0].shapes.items[0].text.style = { fontSize: 18, color: "#000000", fontFace: "Times New Roman" };

setSlideText(
  2,
  "ВВЕДЕНИЕ",
  `Актуальность
• Репетиция перед монитором не воспроизводит давление аудитории
• VR-среда позволяет безопасно повторять выступление
• Пользователь тренирует регламент, слайды и поведение перед залом

Цель
Разработать обучающее VR-приложение для тренировки публичного выступления в виртуальной аудитории

Задачи
• Проанализировать предметную область и аналоги
• Выбрать и обосновать стек разработки
• Спроектировать и реализовать VR-тренажер
• Провести тестирование и подготовить документацию`
);

setSlideText(
  3,
  "1 ТЕОРЕТИЧЕСКАЯ ЧАСТЬ",
  `1.1 Анализ предметной области и существующих аналогов

Рассмотрены решения:
• VirtualSpeech — VR-сценарии soft skills и аналитика
• Ovation — репетиции презентаций и оценка речи
• Public Speaking VR — тренировка выступления перед виртуальным залом
• PowerPoint rehearsal — контроль слайдов и времени без эффекта аудитории
• Очная репетиция — реалистична, но требует людей и помещения

Вывод: свободная ниша — учебный VR-прототип, который можно открыть в Unity, объяснить по исходному коду, заменить слайды и использовать на защите.`
);
addBox(presentation.slides.items[2], 130, 425, 190, 70, "VirtualSpeech\nаналитика", "#EAF3F8");
addBox(presentation.slides.items[2], 345, 425, 160, 70, "Ovation\nоценка речи", "#EAF3F8");
addBox(presentation.slides.items[2], 530, 425, 190, 70, "Public Speaking VR\nвиртуальный зал", "#EAF3F8");
addBox(presentation.slides.items[2], 745, 425, 170, 70, "PowerPoint\nтайминг", "#F7F2E8");
addBox(presentation.slides.items[2], 940, 425, 170, 70, "Очная\nрепетиция", "#F7F2E8");

setSlideText(
  4,
  "1.2 Основные теоретические аспекты",
  `Ключевые аспекты VR-тренажера:

• Иммерсия — ощущение присутствия в аудитории
• Интерактивность — пользователь работает с меню, кликером, слайдами и таймером
• Обратная связь — звуки реакции зала зависят от результата выступления
• Ограничение времени — тренировка соблюдения регламента
• Повторяемость — возможность проходить сценарий несколько раз
• Доступность тестирования — PC fallback без VR-гарнитуры`
);
addFlow(presentation.slides.items[3], ["Меню", "Таймер", "Слайды", "Реакция", "Статистика"], 150, 420);

setSlideText(
  5,
  "1.3 Обзор и обоснование выбора технологий и инструментов",
  `Были рассмотрены игровые движки и инструменты разработки:

• Unity — OpenXR, XR Management, XR Interaction Toolkit, C#, удобная компонентная модель
• Unreal Engine — мощная графика, но выше порог входа и избыточность для прототипа
• Godot — легкий движок, но меньше готовых VR/XR-решений

Итоговый стек проекта:
Unity 2022.3 + C# + OpenXR + XR Interaction Toolkit + TextMeshPro + Mixamo.`
);
addSmallTable(presentation.slides.items[4], 135, 410, [170, 250, 250, 190], 48, [
  ["Критерий", "Unity", "Unreal Engine", "Godot"],
  ["VR/XR", "OpenXR, XR Toolkit", "Есть, но сложнее", "Меньше готовых решений"],
  ["Освоение", "C# и компоненты", "C++ и сложная архитектура", "Проще, но меньше материалов"],
  ["Вывод", "Выбран", "Избыточен", "Риск по XR"],
], 12);

setSlideText(
  6,
  "2 ПРАКТИЧЕСКАЯ ЧАСТЬ",
  `2.1 Проектирование

Пользовательский сценарий:
• запуск приложения и выбор длительности тренировки;
• загрузка виртуальной аудитории;
• работа с таймером и презентацией;
• использование VR-кликера;
• получение реакции аудитории;
• просмотр статистики и возврат в меню.

Основные объекты сцены: MainMenu, XR Origin, RoomModel, Quad, TimerText, Clicker, AudienceReactionController.`
);
addFlow(presentation.slides.items[5], ["Выбор\nвремени", "Сцена\nаудитории", "Доклад\nи слайды", "Реакция\nзала", "Статистика"], 145, 420);

setSlideText(
  7,
  "2.2 Реализация проекта",
  `Разработанные модули:
• MainMenuController — World Space меню и выбор времени
• PresentationSessionManager — сценарий тренировки, статистика и переходы
• SpeakerTimer — таймер доклада
• SlideChanger — смена слайдов
• ClickerControls — поведение VR-кликера
• AudienceReactionController — звуки и реакция аудитории

Фрагмент логики завершения:
if (lastSlideReached && timer.HasTimeLeft)
{
    timer.Pause();
    audience.PlayApplause();
    ShowStatsAfterDelay();
}`
);

setSlideText(
  8,
  "2.3 Тестирование проекта",
  `Выбран метод интеграционного тестирования: проверялась совместная работа меню, таймера, слайдов, кликера, звука, VR-управления и статистики.

Критерии:
• стабильность запуска и переходов между сценами;
• производительность и отсутствие заметных просадок FPS;
• корректность механик;
• удобство интерфейса и VR-взаимодействия.`
);
addBarChart(presentation.slides.items[7], 650, 345, 390, 170, ["Меню", "Аудитория", "Слайды", "Реакция"], [72, 58, 54, 50]);
addSmallTable(presentation.slides.items[7], 115, 430, [170, 235, 135], 38, [
  ["Проверка", "Результат", "Итог"],
  ["Меню", "читаемо, запуск работает", "выполнено"],
  ["Кликер", "не проваливается, доступен", "выполнено"],
  ["Звук", "аплодисменты/негативная реакция", "выполнено"],
], 11);

setSlideText(
  9,
  "2.4 Руководство программиста",
  `Руководство программиста подготовлено по ГОСТ 19.504-79.

Состав проекта:
• Unity 2022.3.45f1;
• сцены MainMenu и SampleScene;
• папка Assets/Scripts с основными C#-скриптами;
• Resources/Audio для звуков реакции;
• test_slides для материалов презентации.

Основные точки изменения:
• время тренировки — MainMenuController и PlayerPrefs;
• логика слайдов — SlideChanger;
• сценарий завершения — PresentationSessionManager;
• управление кликером — ClickerControls.`
);

setSlideText(
  10,
  "2.5 Руководство пользователя",
  `Руководство пользователя подготовлено по ГОСТ Р 59795-2021.

Основные действия пользователя:
1. Запустить приложение.
2. В главном меню выбрать время тренировки.
3. Нажать «Начать тренировку».
4. В сцене аудитории использовать VR-контроллер и кликер.
5. Переключать слайды и следить за таймером.
6. После завершения просмотреть статистику.
7. Вернуться в меню для повторной тренировки.

В VR: луч контроллера наводится на кнопку, нажатие выполняется курком Trigger.`
);

setSlideText(
  11,
  "2.6 Экономическое обоснование проекта",
  `Экономическая часть включает расчет трудовых затрат, эксплуатационных материалов, амортизации оборудования и структуры себестоимости.

Основные статьи затрат:
• оплата труда разработчика;
• эксплуатационные материалы;
• амортизация оборудования;
• накладные расходы.

Полная себестоимость готового программного продукта составила 54 688,9 рублей.`
);
addBox(presentation.slides.items[10], 775, 335, 260, 120, "54 688,9 руб.\nполная себестоимость", "#EAF6EA", "#7EB77F", 24);

setSlideText(
  12,
  "2.7 Техника безопасности и охрана труда",
  `Рассмотрены требования безопасной разработки и эксплуатации VR-приложения.

Для рабочего места разработчика:
• правильное положение монитора и освещение;
• регулярные перерывы при работе за ПК;
• исправность кабелей и оборудования;
• снижение зрительной нагрузки.

Для VR-эксплуатации:
• свободная зона вокруг пользователя;
• контроль самочувствия при использовании гарнитуры;
• надежное крепление гарнитуры и контроллеров;
• прекращение тренировки при дискомфорте.`
);

setSlideText(
  13,
  "ЗАКЛЮЧЕНИЕ",
  `Цель дипломного проекта достигнута: разработано обучающее VR-приложение для тренировки публичного выступления.

Решены задачи:
• проанализированы предметная область и аналоги;
• выбран и обоснован стек Unity + C# + OpenXR;
• спроектированы сцены, интерфейс и пользовательский сценарий;
• реализованы меню, таймер, слайды, кликер, реакции аудитории и статистика;
• проведено тестирование и исправлены выявленные ошибки;
• подготовлены руководства пользователя и программиста.

Проект может использоваться для подготовки студентов к защите и как основа для дальнейшего развития VR-тренажера.`
);

presentation.slides.items[13].shapes.items[0].text = `Спасибо за внимание!

Дипломный проект
«Разработка обучающего VR-приложения»

Выполнил: Федин А.А.
группа ИСп319ДК

Руководитель: Исаев А.Н.
Челябинск 2026`;
presentation.slides.items[13].shapes.items[0].text.style = { fontSize: 28, color: "#000000", fontFace: "Times New Roman" };

await fs.mkdir(PREVIEW_DIR, { recursive: true });
for (const [index, slide] of presentation.slides.items.entries()) {
  const png = await presentation.export({ slide, format: "png", scale: 1 });
  await fs.writeFile(`${PREVIEW_DIR}/slide-${String(index + 1).padStart(2, "0")}.png`, new Uint8Array(await png.arrayBuffer()));
}
const montage = await presentation.export({ format: "png", montage: true, scale: 0.55 });
await fs.writeFile(`${PREVIEW_DIR}/montage.png`, new Uint8Array(await montage.arrayBuffer()));

const pptx = await PresentationFile.exportPptx(presentation);
await pptx.save(OUT);
console.log(OUT);
