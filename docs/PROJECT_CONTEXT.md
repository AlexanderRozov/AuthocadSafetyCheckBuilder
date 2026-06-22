# Контекст проекта ПТ (для AI / новых разработчиков)

Краткий обзор кодовой базы плагина AutoCAD. Пользовательское руководство: [MANUAL.md](MANUAL.md).

---

## Что это

**Плагин .NET для AutoCAD 2022 / 2023** — расстановка объектов пожарной сигнализации на чертеже, ведение таблиц на листе, связи между объектами, автоматическая расстановка извещателей, импорт шаблонов символов, горячие клавиши.

- Сборка: `Demo.dll` (имя сборки историческое; код в namespace `Pt.*`)
- Путь: `src/DemoAutoCad/WindowsFormsApp1/`
- UI: **WPF** (`PtMainWindow`), показывается **немодально** через `Application.ShowModelessWindow`
- Данные чертежа: **JSON в NOD** DWG (`PtPersistenceService`, ключ `DEMO_PT_PLUGIN`, snapshot **v3**)
- Настройки горячих клавиш: `%LocalAppData%\.ptautocad_module\hotkeys.json`

---

## Стек и ограничения

| Параметр | Значение |
|----------|----------|
| .NET | `net48`, x64 |
| AutoCAD API | accoremgd / acdbmgd / acmgd (2022 или 2023, по конфигурации сборки) |
| UI | WPF + частично WinForms (legacy `ContextForm` отключён в csproj) |
| Сериализация | `System.Web.Script.Serialization` (JavaScriptSerializer) |
| Nullable | выключен |

**Важно при разработке:** пока AutoCAD загрузил `Demo.dll`, файл в `bin\<Configuration>\` заблокирован → `NETUNLOAD` перед пересборкой.

---

## Структура репозитория

```
c:\olga\
├── docs\
│   ├── MANUAL.md              # руководство пользователя
│   └── PROJECT_CONTEXT.md     # этот файл
└── src\DemoAutoCad\WindowsFormsApp1\
    ├── AutoCadPlugin.cs       # IExtensionApplication (init/terminate)
    ├── PtCommands.cs          # команды AutoCAD
    ├── ContextMenuManager.cs  # ПКМ «Поставить объект ПТ»
    ├── Abstractions\          # интерфейсы (SOLID)
    ├── Models\                # DTO / доменные модели
    ├── Services\              # бизнес-логика, рисование, persistence
    │   ├── Drawing\           # ShapeDrawer, DeviceComposer, LabelTextFitter…
    │   ├── Hotkeys\           # конфиг, virtual keys, focus helper
    │   └── Infrastructure\    # impl репозиториев, HotkeyInputController
    └── ui\                    # WPF (partial PtMainWindow)
```

---

## Точки входа

| Файл | Роль |
|------|------|
| `AutoCadPlugin.cs` | Загрузка: контекстное меню, `PtDocumentRegistry`, hotkeys `Attach()` |
| `PtCommands.cs` | `PLACEPT`, `PTPANEL`, `PTPICKADD`, `PTPICKNEWTABLE`, `PTPICKDETECTOR`, `PTPICKPRECREATE`, `PTPICKLEGENDTABLE` |
| `ui/PtMainWindow.xaml` + partial `.cs` | Главная панель (6 вкладок) |

### Команды пользователя

- `PLACEPT` / `PTPANEL` — открыть панель
- Остальные `PTPICK*` — **внутренние**, вызываются через `PtInteractionScheduler` после подготовки сессии в UI

---

## Архитектура

### Паттерн: document-scoped state

```
Document (AutoCAD)
  └─ PtDocumentRegistry → PtDocumentState (in-memory)
       └─ PtServices (per document)
            ├─ Objects, Tables, Blocks, DetectorZones, PrecreatedTemplates
            └─ доступ через static-фасады: PtObjectRepository, PtTableRepository…
```

`PtServiceRegistry.Current` всегда привязан к активному документу.

### Паттерн: UI → сессия → команда → pick

WPF **не вызывает Editor напрямую** (кроме скрытия окна). Типичный поток:

```
PtMainWindow.TryAddObject()
  → PtInteractionSession.BeginAddDevice(request)
  → PtInteractionScheduler.RunCommandWhenIdle("PTPICKADD")
  → [Idle] doc.SendStringToExecute("PTPICKADD ")
  → PtCommands.ExecutePickAdd()
      → HidePluginWindow()
      → Editor.GetPoint()
      → PtLayoutManager.AddDevice()
      → NotifyInteractionFinished() → AfterDrawingInteraction() на UI thread
```

Аналогично: новая таблица, извещатели, шаблон из выделения, импорт легенды.

### Паттерн: SOLID / фасады

- **Интерфейсы:** `Abstractions/` (`ILayoutManager`, `IHotkeyInputController`, `IPtObjectRepository`…)
- **Реализации:** `Services/Infrastructure/`
- **Фасад рисования:** `DrawingService` → `Services/Drawing/*`
- **Фасад раскладки:** `PtLayoutManager` (static, основная логика таблиц и объектов)

### Partial UI

`PtMainWindow` разбит по вкладкам:

| Файл | Вкладка / зона |
|------|----------------|
| `PtMainWindow.xaml.cs` | Размещение, Связи, Таблицы, общее |
| `PtMainWindow.Templates.cs` | Шаблоны объектов |
| `PtMainWindow.Hotkeys.cs` | Горячие клавиши |
| `PtMainWindow.Detectors.cs` | Расставить извещатели |

---

## Доменные сущности

### PtObject

Объект на чертеже + строка в таблице. Хранит `ObjectId` сущностей: фигура (`EntityId`), подпись (`LabelTextId`), id снизу (`IdTextId`), группа (`GroupId`).

### PtTableSession

Логическая таблица: ссылка на `Table` AutoCAD, шина (`BusLineId`), счётчик колонок, origin.

### PlaceDeviceRequest

Запрос на добавление: `TableId`, `DeviceType`, `BlockTemplate`, `CustomLabel`, `FontSize`, `ParentObjectId`, `BlockGroupId`, `InsertionPoint`.

### BlockTemplate / PrecreatedTemplate

- **Встроенные формы:** Rectangle, Square, Circle, Triangle, Diamond, AutoCadBlock (`PT_BTH`…)
- **PrecreatedObject:** пользовательский блок + preview Base64 + `ShapeHalfHeight`

### PtDetectorZone

Контур полилинии, hatch, список `DetectorObjectIds`, привязка к таблице.

### HotkeySelection

`ListKind` (device/shape) + `ItemId` → применяется в UI и запускает `TryAddObject()`.

---

## Рисование на чертеже

Цепочка добавления объекта (`PtLayoutManager.AddDevice`):

1. Расширить `Table` новой колонкой (FD, JS05, код, номер…)
2. `DrawingService.DrawDevice` → `DeviceComposer.DrawDevice`
3. `ShapeDrawer.DrawDeviceShape` — фигура на `PT_DEVICES`
4. `LabelTextFitter.DrawInsideLabel` — подпись **внутри** фигуры с подгонкой размера
5. `AnnotationDrawer.DrawMText` — `*-КОД-НОМЕР` под объектом
6. `DeviceComposer.CreateObjectGroup` — группа AutoCAD
7. `PtObjectRepository.Add` + опционально `CreateObjectLink` для родителя
8. `PtPersistenceService.Save`

### Слои (`PtLayoutConstants`)

`PT_TABLE`, `PT_DEVICES`, `PT_TEXT`, `PT_BUS`, `PT_LINKS`, `PT_DETECTOR_ZONE`

### Размеры по умолчанию

- Прямоугольник: 28×10
- Шрифт: 3.5
- Радиус извещателя: 6.4 м

---

## Горячие клавиши

| Компонент | Файл |
|-----------|------|
| Перехват в AutoCAD | `HotkeyInputController` — `Application.PreTranslateMessage` |
| Fallback в WPF | `PtMainWindow_OnPreviewKeyDown` когда `IsArmed` |
| Конфиг | `HotkeyConfigFileRepository` |
| Индикатор | `HotkeyArmedIndicatorWindow` (оранжевая полоска, не крадёт фокус) |

**Двухшаговый режим:** `ModeTriggerKey` (F11) → binding key → `SelectionApplied` → `ApplyHotkeySelection` + `BeginPlacementAfterHotkeySelection`.

**Поведение в armed-режиме:** `tabMain.IsEnabled = false`, фокус возвращается в чертёж (`AutoCadFocusHelper`), панель не вызывает `Show()` при arm.

Не перехватывает клавиши при активной команде AutoCAD (`CommandInProgress`).

---

## Расстановка извещателей

`DetectorPlacementService` — жадный алгоритм покрытия сеткой внутри полигона:

- Фазы смещения сетки (3×3)
- Лимит `MaxGridCellsPerAxis = 120`
- Параллельные вычисления (`Task.Run`)
- Тип объекта: BTH, форма Circle, `DetectorRadius` из UI
- `DetectorAreaPicker` — интерактивная полилиния

---

## Импорт шаблонов

| Источник | Сервис |
|----------|--------|
| Выделение на чертеже | `PrecreatedBlockService.CreateFromSelection` |
| DWG файл | `PrecreatedBlockService` + import block |
| CSV/Excel | `LegendImportService.ImportFromCsvFile` |
| Таблица AutoCAD | `LegendImportService.ImportFromAutoCadTable` |

Preview: `BlockPreviewRenderer` → PNG Base64 в `PrecreatedTemplate.PreviewImageBase64`.

---

## Persistence

- **Где:** NOD чертежа, JSON snapshot v3
- **Что:** tables, objects, links, blocks, detector zones, precreated templates, counters, handles сущностей
- **Когда:** после мутаций через `PtLayoutManager` / явный `PtDocumentRegistry.Save`
- **Загрузка:** `PtDocumentRegistry.EnsureLoaded` при активации документа

Handles (`HandleHelper`) связывают логические `PtObject` с `ObjectId` на чертеже после reopen.

---

## Справочники (зашиты в код)

- **Типы устройств:** `DeviceCatalogImpl` (~25 типов: BTH, BTM, BIAL, шкафы…)
- **Формы:** `BlockCatalogImpl` (rect, circle, PT_BTH block…)
- **Расширение форм:** `PrecreatedTemplateRepository` (per-document, в snapshot)

---

## Соглашения кода

- Namespace: **`Pt.*`** (модели, сервисы, UI); **`AutoCadPlugin.*`** (entry, commands); csproj `RootNamespace=Demo` — legacy, новый код в `Pt`
- Репозитории: static-фасад (`PtObjectRepository`) → `PtServiceRegistry.Current.*`
- UI-поток: `Dispatcher.Invoke` при callback из AutoCAD
- Окно плагина при `Closing` → `e.Cancel = true; Hide()` (не закрывается, а скрывается)
- Минимальный diff, без лишней абстракции — предпочтение пользователя

---

## Известные нюансы / грабли

1. **Таблица обязательна** перед добавлением объектов (и для hotkeys placement).
2. **Окно скрывается** на время `GetPoint` — иначе AutoCAD не принимает клик.
3. **DLL lock** — нужен `NETUNLOAD` для rebuild.
4. **Подпись внутри фигуры** — `LabelTextFitter`; при редактировании в DataGrid вызывается `SyncObjectToDrawing` → `FitInsideLabel`.
5. **Циклы в связях** запрещены (`WouldCreateParentCycle`).
6. **Шаблон нельзя удалить**, если используется размещёнными объектами.
7. **Hotkeys** не работают во время активной команды AutoCAD.
8. Namespace `Demo` vs `Pt` — не унифицировано полностью.

---

## Сборка

Конфигурации: `Debug-2022`, `Release-2022`, `Debug-2023`, `Release-2023`.

```powershell
dotnet build "c:\olga\src\DemoAutoCad\WindowsFormsApp1\Demo.csproj" -c Debug-2022
dotnet build "c:\olga\src\DemoAutoCad\WindowsFormsApp1\Demo.csproj" -c Debug-2023
```

Выход: `bin\<Configuration>\Demo.dll` (например `bin\Debug-2022\Demo.dll` или `bin\Debug-2023\Demo.dll`).  
Загрузка в AutoCAD: `NETLOAD` → указать DLL из папки, соответствующей версии AutoCAD.

---

## Карта ключевых файлов (быстрый поиск)

| Задача | Файл |
|--------|------|
| Добавить тип устройства | `Services/Infrastructure/DeviceCatalogImpl.cs` |
| Добавить форму | `Services/Infrastructure/BlockCatalogImpl.cs` |
| Логика таблицы/объекта | `Services/PtLayoutManager.cs` |
| Рисование фигур/текста | `Services/Drawing/ShapeDrawer.cs`, `DeviceComposer.cs`, `LabelTextFitter.cs` |
| Подгонка надписи | `Services/Drawing/LabelTextFitter.cs` |
| Snapshot save/load | `Services/PtPersistenceService.cs` |
| UI размещение | `ui/PtMainWindow.xaml.cs` → `TryAddObject` |
| Hotkeys | `Services/Infrastructure/HotkeyInputController.cs`, `ui/PtMainWindow.Hotkeys.cs` |
| Извещатели | `Services/DetectorPlacementService.cs`, `ui/PtMainWindow.Detectors.cs` |
| Шаблоны | `Services/PrecreatedBlockService.cs`, `ui/PtMainWindow.Templates.cs` |
| Idle-команды | `Services/PtInteractionScheduler.cs`, `Services/PtInteractionSession.cs` |

---

## Типичные задачи для AI

| Запрос пользователя | Куда смотреть |
|---------------------|---------------|
| Объект не ставится | `TryAddObject`, `ExecutePickAdd`, наличие таблицы |
| Hotkey не работает | `HotkeyInputController`, armed state, `CommandInProgress` |
| Текст вылезает из фигуры | `LabelTextFitter`, `ShapeInnerBounds` |
| Зависание при извещателях | `DetectorPlacementService` лимиты сетки |
| Данные не сохраняются | `PtPersistenceService`, `PtDocumentRegistry.Save` |
| Импорт легенды | `LegendImportService` |
| Связи/дерево | `PtMainWindow.xaml.cs` (tree), `PtLayoutManager.CreateObjectLink` |

---

## История фич (кратко)

Эволюция проекта в чатах:

1. Базовое размещение + таблица на чертеже
2. WPF-панель, связи, редактируемые подписи через UI (не на чертеже)
3. Расстановка извещателей по полигону (BTH, R=6.4)
4. Параллельный алгоритм + лимиты производительности
5. Precreated templates: DWG, CSV, таблица AutoCAD, preview
6. Hotkeys SOLID (F11, json config, armed indicator)
7. Fix: hotkeys → placement без переключения вкладок
8. Fix: `LabelTextFitter` — надпись внутри границ фигуры
