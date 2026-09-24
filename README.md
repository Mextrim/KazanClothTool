# KazanClothTool

KazanClothTool — редактор одежды для GTA V с просмотром, редактированием свойств, проверкой файлов и сборкой ресурсов.

## Скачать

Готовая portable-сборка для Windows x64 доступна в GitHub Releases:

[Скачать KazanClothTool v1.2.1](https://github.com/Mextrim/KazanClothTool/releases/download/v1.2.1/KazanClothTool-v1.2.1-win-x64.zip)

Исходный код v1.2.1 можно скачать автоматическим архивом GitHub: [архив исходников](https://github.com/Mextrim/KazanClothTool/archive/refs/tags/v1.2.1.zip).

## Запуск

Готовая сборка находится в папке `KazanClothTool`:

```text
KazanClothTool\KazanClothTool.exe
```

## Разработка

Исходный проект находится в `source`.

```powershell
dotnet restore .\source\grzyClothTool.sln
dotnet build .\source\grzyClothTool.sln -c Release
dotnet publish .\source\grzyClothTool\grzyClothTool.csproj -c Release -r win-x64 --self-contained true
```

## Настройки

Выбор темы, языка (русский, украинский или английский) и путь к папке проектов сохраняются в `%LOCALAPPDATA%\KazanClothTool\settings.json`.

Доступны темы: «Графит», «Светлая», «Bootstrap», «Material Design», «Океан», «Закат», «Лес», «Неон», «Аврора», «Рубин», «Кобальт», «Песок», «Роза» и «Моно».

Пользовательские проекты и автосохранения не удаляются при очистке папки сборки.

## C++-версия

Параллельная миграция на C++20/Qt 6 находится в `source/KazanClothToolCpp`. Текущая WPF-сборка в `KazanClothTool` остаётся рабочей и обновлённой; C++-каркас пока не собирался в окружении без Qt 6 SDK.

## Авторы и оформление

© 2026 MeX. Программное обеспечение переделал MeX под чутким руководством Evelentdev.

В интерфейсе используются иконки Font Awesome через FontAwesome.Sharp.
