# Kazan Cloth Tool C++ / Qt 6

Параллельный каркас MVP Kazan Cloth Tool на C++20 и Qt 6 Widgets. Этот каталог не изменяет и не заменяет существующую WPF-реализацию в `../grzyClothTool`.

## Что уже есть

- `MainWindow` с верхней панелью и навигацией `Home` / `Editor` / `Settings`;
- `HomePage` с заголовком **Kazan Cloth Tool**, созданием, открытием и заглушкой импорта;
- `EditorPage` с переключателем `ЖЕНСКАЯ ОДЕЖДА` / `МУЖСКАЯ ОДЕЖДА`, меню выбора папки, удалением выбранного элемента и пустым состоянием;
- `SettingsPage` с темами `Dark`, `Light`, `Ocean`, `Sunset`, `Forest`, `Neon`, `Aurora`, `Ruby`, `Cobalt`, `Sand`, `Rose`, `Mono`, выбором языка (ru/uk/en; сохраняется в JSON) и папкой проектов;
- `SettingsStore`: JSON-настройки в `%LOCALAPPDATA%\KazanClothTool\settings.json`;
- `ThemeManager`: загрузка общего QSS из `resources/app.qss` и замена цветовых токенов;
- `ProjectStore`: недавние проекты, базовое создание структуры проекта и `project.json`;
- `resources/app.qrc` и `resources/styles/app.qss`;
- CMake с C++20, Qt 6 Widgets, `AUTOMOC`, `AUTOUIC` и `AUTORCC`.

## Требования

- CMake 3.21 или новее;
- Qt 6.2 или новее с компонентом **Qt Widgets**;
- компилятор с поддержкой C++20 (MSVC 2019/2022, GCC или Clang);
- Windows рекомендуется для соответствия пути `%LOCALAPPDATA%`.

## Сборка в Windows (PowerShell)

Из каталога `source/KazanClothToolCpp`:

```powershell
cmake -S . -B build -G "Visual Studio 17 2022" -A x64
cmake --build build --config Release
```

Если Qt установлен не в стандартном месте, добавьте его bin-каталог в `PATH` или передайте путь к Qt:

```powershell
cmake -S . -B build -G Ninja `
  -DCMAKE_PREFIX_PATH="C:/Qt/6.8.3/msvc2022_64"
cmake --build build
```

Пример запуска из каталога сборки:

```powershell
.\build\KazanClothToolCpp.exe
```

Для Linux/macOS аналогично используются Ninja или стандартный генератор CMake, Qt 6 Widgets и платформенный Qt application.

## Структура

```text
KazanClothToolCpp/
├── CMakeLists.txt
├── README.md
├── resources/
│   ├── app.qrc
│   └── styles/app.qss
└── src/
    ├── MainWindow.{h,cpp}
    ├── main.cpp
    ├── pages/
    │   ├── HomePage.{h,cpp}
    │   ├── EditorPage.{h,cpp}
    │   └── SettingsPage.{h,cpp}
    ├── services/
    │   ├── ProjectStore.{h,cpp}
    │   ├── SettingsStore.{h,cpp}
    │   └── ThemeManager.{h,cpp}
    └── widgets/
        └── SplitActionButton.{h,cpp}
```

## Честные ограничения текущего MVP

Это каркас интерфейса и инфраструктуры проектов, а не законченный редактор одежды. Пока **не перенесены**:

- интеграция с CodeWalker и его runtime/моделями;
- 3D-просмотр, рендеринг, камера и интерактивание с 3D-моделями;
- чтение, запись, импорт и экспорт GTA-форматов, включая YDD/YTD и связанные ресурсы;
- полноценная модель одежды, текстур, тегов, групп, drawable и проверок полигонов;
- экспорт ресурсов FiveM, Rage MP, Alt:V и Singleplayer;
- автосохранение, миграция и совместимость со всеми проектами WPF-версии;
- полноценная интеграция импорта, предварительного просмотра и редактирования текстур.

Кнопка импорта намеренно показывает это ограничение, а не делает вид, что GTA-ресурсы уже поддерживаются. Созданный MVP-проект содержит только базовые каталоги `source`, `output`, `cache` и файл `project.json`.
