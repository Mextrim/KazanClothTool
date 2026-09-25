# Kazan Cloth Tool

Kazan Cloth Tool — редактор одежды для GTA V с просмотром, редактированием свойств, проверкой файлов и сборкой ресурсов.

## Скачать

Готовая portable-сборка для Windows x64 доступна в GitHub Releases:

[Скачать KazanClothTool v1.2.4](https://github.com/Mextrim/KazanClothTool/releases/download/v1.2.4/KazanClothTool-v1.2.4-win-x64.zip)

## Исходный код

В репозитории доступны исходники WPF-приложения, CodeWalker и C++/Qt MVP:

- `source/grzyClothTool` — основное WPF-приложение;
- `source/CodeWalker` — интегрированный 3D/ресурсный компонент;
- `source/KazanClothToolCpp` — параллельный C++/Qt MVP.

## Запуск

Готовая сборка находится в папке `KazanClothTool`:

```text
KazanClothTool\KazanClothTool.exe
```

## Настройки

Выбор темы, языка (русский, украинский или английский) и путь к папке проектов сохраняются в `%LOCALAPPDATA%\KazanClothTool\settings.json`.

Доступны темы: «Графит», «Светлая», «Bootstrap», «Material Design», «Океан», «Закат», «Лес», «Неон», «Аврора», «Рубин», «Кобальт», «Песок», «Роза» и «Моно».

Пользовательские проекты и автосохранения не удаляются при очистке папки сборки.

## Авторы и оформление

© 2026 MeX. Программное обеспечение переделал MeX под чутким руководством Evelentdev.

В интерфейсе используются иконки Font Awesome через FontAwesome.Sharp.
