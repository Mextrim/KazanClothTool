using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace grzyClothTool.Helpers;

public sealed record LanguageOption(string Code, string Name)
{
    public override string ToString() => Name;
}

public static class LocalizationHelper
{
    public const string Russian = "ru";
    public const string Ukrainian = "uk";
    public const string English = "en";

    private static readonly DependencyProperty OriginalStringsProperty =
        DependencyProperty.RegisterAttached(
            "OriginalStrings",
            typeof(Dictionary<string, string>),
            typeof(LocalizationHelper),
            new PropertyMetadata(null));

    private readonly record struct Translation(string Ukrainian, string English);

    private static readonly Dictionary<string, Translation> Catalog = new(StringComparer.Ordinal)
    {
        // Shell and navigation
        ["Главная"] = new("Головна", "Home"),
        ["Редактор"] = new("Редактор", "Editor"),
        ["Настройки"] = new("Налаштування", "Settings"),
        ["Открыть"] = new("Відкрити", "Open"),
        ["Импорт"] = new("Імпорт", "Import"),
        ["Сохранить"] = new("Зберегти", "Save"),
        ["Экспортировать проект"] = new("Експортувати проєкт", "Export project"),
        ["Открыть набор одежды (.meta)"] = new("Відкрити набір одягу (.meta)", "Open clothing set (.meta)"),
        ["Импортировать проект"] = new("Імпортувати проєкт", "Import project"),
        ["Сохранить проект (Ctrl+S)"] = new("Зберегти проєкт (Ctrl+S)", "Save project (Ctrl+S)"),
        ["Свернуть"] = new("Згорнути", "Minimize"),
        ["Развернуть"] = new("Розгорнути", "Maximize"),
        ["Закрыть"] = new("Закрити", "Close"),
        ["Готово"] = new("Готово", "Ready"),
        ["Версия"] = new("Версія", "Version"),
        ["Подготовка редактора одежды..."] = new("Підготовка редактора одягу...", "Preparing the clothing editor..."),
        ["Редактор готов"] = new("Редактор готовий", "Editor ready"),
        ["3D-редактор одежды"] = new("3D-редактор одягу", "3D clothing editor"),
        ["3D-РЕДАКТОР ОДЕЖДЫ"] = new("3D-РЕДАКТОР ОДЯГИ", "3D CLOTHING EDITOR"),
        ["3D ПРЕДПРОСМОТР"] = new("3D-ПЕРЕГЛЯД", "3D PREVIEW"),
        ["РЕДАКТОР ОДЕЖДЫ"] = new("РЕДАКТОР ОДЯГИ", "CLOTHING EDITOR"),

        // Home
        ["Kazan Cloth Tool"] = new("Kazan Cloth Tool", "Kazan Cloth Tool"),
        ["Создавайте, просматривайте и редактируйте элементы одежды, текстуры и физику в едином проекте. KazanClothTool показывает изменения в 3D и сразу проверяет файлы перед сборкой."] = new("Створюйте, переглядайте та редагуйте елементи одягу, текстури й фізику в одному проєкті. Kazan Cloth Tool показує зміни в 3D і перевіряє файли перед збіркою.", "Create, preview and edit clothing, textures and physics in one project. Kazan Cloth Tool shows changes in 3D and validates files before building."),
        ["Новый проект"] = new("Новий проєкт", "New project"),
        ["Открыть набор"] = new("Відкрити набір", "Open set"),
        ["Недавние проекты"] = new("Нещодавні проєкти", "Recent projects"),
        ["Открыть файл"] = new("Відкрити файл", "Open file"),
        ["Открыть файл проекта вручную"] = new("Відкрити файл проєкту вручну", "Open project file manually"),
        ["Убрать из списка"] = new("Прибрати зі списку", "Remove from list"),
        ["Здесь появятся недавние проекты"] = new("Тут з’являться нещодавні проєкти", "Recent projects will appear here"),
        ["Создайте первый проект или откройте готовый набор одежды"] = new("Створіть перший проєкт або відкрийте готовий набір одягу", "Create your first project or open a ready clothing set"),
        ["ПОДСКАЗКА"] = new("ПІДКАЗКА", "TIP"),
        ["Рабочий процесс"] = new("Робочий процес", "Workflow"),
        ["Добавьте YDD и текстуры"] = new("Додайте YDD і текстури", "Add YDD files and textures"),
        ["Настройте свойства и теги"] = new("Налаштуйте властивості та теги", "Configure properties and tags"),
        ["Проверьте в 3D и соберите"] = new("Перевірте у 3D і зберіть", "Check in 3D and build"),
        ["Проекты сохраняются локально и автоматически"] = new("Проєкти зберігаються локально й автоматично", "Projects are saved locally and automatically"),
        ["Локальная рабочая среда"] = new("Локальне робоче середовище", "Local workspace"),
        ["Выберите одежду слева и сразу увидите все доступные свойства и текстуры."] = new("Виберіть одяг ліворуч і одразу побачите всі доступні властивості та текстури.", "Select clothing on the left to see all available properties and textures."),
        ["3D-режим обновляет модель при выборе другого элемента одежды."] = new("3D-режим оновлює модель під час вибору іншого елемента одягу.", "3D mode updates the model when another clothing item is selected."),
        ["Shift+Delete удаляет выбранную одежду без дополнительного диалога."] = new("Shift+Delete видаляє вибраний одяг без додаткового діалогу.", "Shift+Delete removes selected clothing without an extra dialog."),
        ["Ctrl+Delete заменяет одежду на зарезервированный слот, сохраняя нумерацию."] = new("Ctrl+Delete замінює одяг на зарезервований слот, зберігаючи нумерацію.", "Ctrl+Delete replaces clothing with a reserved slot while preserving numbering."),
        ["Экспортируйте текстуры в PNG или DDS из контекстного меню списка."] = new("Експортуйте текстури в PNG або DDS із контекстного меню списку.", "Export textures to PNG or DDS from the list context menu."),
        ["Проекты автоматически сохраняются каждую минуту после изменения."] = new("Проєкти автоматично зберігаються щохвилини після зміни.", "Projects are automatically saved every minute after a change."),
        ["© 2026 MeX · ПО переделал MeX под чутким руководством Evelentdev"] = new("© 2026 MeX · ПО переробив MeX під легким керівництвом Evelentdev", "© 2026 MeX · Software reworked by MeX under the guidance of Evelentdev"),

        // Editor
        ["ОДЕЖДА"] = new("ОДЯГ", "CLOTHING"),
        ["Поиск одежды..."] = new("Пошук одягу...", "Search clothing..."),
        ["Проект не открыт"] = new("Проєкт не відкрито", "No project opened"),
        ["Создайте проект или откройте набор одежды, чтобы начать редактирование"] = new("Створіть проєкт або відкрийте набір одягу, щоб почати редагування", "Create a project or open a clothing set to start editing"),
        ["Перейти на главную"] = new("Перейти на головну", "Go to Home"),
        ["ЖЕНСКАЯ"] = new("ЖІНЧА", "WOMEN"),
        ["МУЖСКАЯ"] = new("ЧОЛОВІЧА", "MEN"),
        ["Выбрать папку"] = new("Вибрати папку", "Choose folder"),
        ["Выбрать папку с женской одеждой"] = new("Вибрати папку з жіночим одягом", "Choose folder with women's clothing"),
        ["Выбрать папку с мужской одеждой"] = new("Вибрати папку з чоловічим одягом", "Choose folder with men's clothing"),
        ["Добавить женскую одежду"] = new("Додати жіночий одяг", "Add women's clothing"),
        ["Добавить мужскую одежду"] = new("Додати чоловічий одяг", "Add men's clothing"),
        ["Удалить выбранную одежду"] = new("Видалити вибраний одяг", "Delete selected clothing"),
        ["3D-ПРОСМОТР"] = new("3D-ПЕРЕГЛЯД", "3D PREVIEW"),
        ["СОБРАТЬ"] = new("ЗІБРАТИ", "BUILD"),
        ["аддонов"] = new("аддонів", "addons"),
        ["предметов"] = new("предметів", "items"),
        ["Параметры проекта"] = new("Параметри проєкту", "Project settings"),

        // Settings
        ["Назад"] = new("Назад", "Back"),
        ["Вернуться в редактор"] = new("Повернутися до редактора", "Back to editor"),
        ["Пути, качество и оформление редактора"] = new("Шляхи, якість і оформлення редактора", "Paths, quality and editor appearance"),
        ["Текущая тема"] = new("Поточна тема", "Current theme"),
        ["Проверка качества"] = new("Перевірка якості", "Quality checks"),
        ["Ограничения помогают находить проблемные модели и текстуры до сборки ресурса."] = new("Обмеження допомагають знаходити проблемні моделі та текстури до збірки ресурсу.", "Limits help find problematic models and textures before building."),
        ["Высокий LOD"] = new("Високий LOD", "High LOD"),
        ["Средний LOD"] = new("Середній LOD", "Medium LOD"),
        ["Низкий LOD"] = new("Низький LOD", "Low LOD"),
        ["Лимит полигонов для верхнего уровня детализации."] = new("Ліміт полігонів для верхнього рівня деталізації.", "Polygon limit for the highest detail level."),
        ["Лимит полигонов для среднего уровня детализации."] = new("Ліміт полігонів для середнього рівня деталізації.", "Polygon limit for the medium detail level."),
        ["Лимит полигонов для нижнего уровня детализации."] = new("Ліміт полігонів для нижнього рівня деталізації.", "Polygon limit for the lowest detail level."),
        ["Максимальное разрешение текстур"] = new("Максимальна роздільність текстур", "Maximum texture resolution"),
        ["Если размер изображения превышает лимит, редактор покажет предупреждение."] = new("Якщо розмір зображення перевищує ліміт, редактор покаже попередження.", "If an image exceeds the limit, the editor will show a warning."),
        ["Рабочие папки"] = new("Робочі папки", "Working folders"),
        ["Папка проектов"] = new("Папка проєктів", "Projects folder"),
        ["Изменить"] = new("Змінити", "Change"),
        ["Все автосохранения и файлы проектов будут храниться здесь."] = new("Усі автозбереження та файли проєктів зберігатимуться тут.", "All autosaves and project files will be stored here."),
        ["Папка GTA V"] = new("Папка GTA V", "GTA V folder"),
        ["Нужна для 3D-просмотра одежды и проверки моделей."] = new("Потрібна для 3D-перегляду одягу та перевірки моделей.", "Required for 3D clothing preview and model validation."),
        ["Поведение редактора"] = new("Поведінка редактора", "Editor behavior"),
        ["Показывать путь выбранного элемента одежды"] = new("Показувати шлях вибраного елемента одягу", "Show selected clothing path"),
        ["Автоматически удалять связанные файлы"] = new("Автоматично видаляти пов’язані файли", "Automatically delete related files"),
        ["Помечать новые элементы одежды"] = new("Позначати нові елементи одягу", "Mark new clothing items"),
        ["Оформление"] = new("Оформлення", "Appearance"),
        ["Выберите настроение редактора. Тема сохраняется автоматически."] = new("Оберіть настрій редактора. Тема зберігається автоматично.", "Choose the editor mood. The theme is saved automatically."),
        ["Язык"] = new("Мова", "Language"),
        ["Русский"] = new("Русский", "Russian"),
        ["Українська"] = new("Українська", "Ukrainian"),
        ["English"] = new("English", "English"),

        // First run and preview
        ["Добро пожаловать в KazanClothTool!"] = new("Ласкаво просимо до Kazan Cloth Tool!", "Welcome to Kazan Cloth Tool!"),
        ["Добро пожаловать в KazanClothTool"] = new("Ласкаво просимо до Kazan Cloth Tool", "Welcome to Kazan Cloth Tool"),
        ["Первоначальная настройка редактора"] = new("Початкове налаштування редактора", "Initial editor setup"),
        ["Главная папка проектов"] = new("Головна папка проєктів", "Main projects folder"),
        ["Обзор..."] = new("Огляд...", "Browse..."),
        ["Выбрать папку проектов"] = new("Вибрати папку проєктів", "Choose projects folder"),
        ["Важная информация"] = new("Важлива інформація", "Important information"),
        ["Продолжить"] = new("Продовжити", "Continue"),
        ["Проекты и автосохранения будут храниться в выбранной папке."] = new("Проєкти та автозбереження зберігатимуться в обраній папці.", "Projects and autosaves will be stored in the selected folder."),
        ["Мышь — вращение · колесо — масштаб"] = new("Миша — обертання · колесо — масштаб", "Mouse — rotate · wheel — zoom"),
        ["3D-модель появится здесь"] = new("3D-модель з’явиться тут", "The 3D model will appear here"),
        ["Выберите одежду слева или перетащите YDD в список"] = new("Виберіть одяг ліворуч або перетягніть YDD до списку", "Select clothing on the left or drag a YDD into the list"),
        ["Элемент одежды не выбран"] = new("Елемент одягу не вибрано", "No clothing item selected"),
        ["Выберите элемент одежды из списка, чтобы просмотреть и изменить его свойства"] = new("Виберіть елемент одягу зі списку, щоб переглянути та змінити його властивості", "Select a clothing item from the list to preview and edit its properties"),
        ["Параметры одежды"] = new("Параметри одягу", "Clothing properties"),
        ["Текстуры"] = new("Текстури", "Textures"),
        ["Встроенные текстуры"] = new("Вбудовані текстури", "Embedded textures"),
        ["Удалить тег"] = new("Видалити тег", "Delete tag"),
        ["Убрать проект из списка"] = new("Прибрати проєкт зі списку", "Remove project from list"),
        ["ДОБАВИТЬ ТЕКСТУРЫ"] = new("ДОДАТИ ТЕКСТУРИ", "ADD TEXTURES"),
        ["Удалить выбранные"] = new("Видалити вибрані", "Delete selected"),
        ["Просмотр"] = new("Перегляд", "Preview"),
        ["Закрыть 3D-предпросмотр"] = new("Закрити 3D-перегляд", "Close 3D preview"),
        ["Предупреждение"] = new("Попередження", "Warning"),
        ["Ошибка"] = new("Помилка", "Error"),
        ["Информация"] = new("Інформація", "Information"),
        ["Успешно"] = new("Успішно", "Success"),
        ["Отмена"] = new("Скасувати", "Cancel"),
        ["ОК"] = new("OK", "OK"),
        ["Да"] = new("Так", "Yes"),
        ["Нет"] = new("Ні", "No"),
        ["Удалить"] = new("Видалити", "Delete"),
        ["Заменить"] = new("Замінити", "Replace"),
        ["Мужской"] = new("Чоловічий", "Male"),
        ["Женский"] = new("Жіночий", "Female"),

        // Theme names and descriptions
        ["Bootstrap 5"] = new("Bootstrap 5", "Bootstrap 5"),
        ["Классический Bootstrap: синий #0d6efd, плоские карточки"] = new("Класичний Bootstrap: синій #0d6efd, пласкі картки", "Classic Bootstrap: blue #0d6efd, flat cards"),
        ["Чистая"] = new("Чиста", "Purity"),
        ["Графит"] = new("Графіт", "Graphite"),
        ["Светлая"] = new("Світла", "Light"),
        ["Океан"] = new("Океан", "Ocean"),
        ["Закат"] = new("Захід сонця", "Sunset"),
        ["Лес"] = new("Ліс", "Forest"),
        ["Неон"] = new("Неон", "Neon"),
        ["Аврора"] = new("Аврора", "Aurora"),
        ["Рубин"] = new("Рубін", "Ruby"),
        ["Кобальт"] = new("Кобальт", "Cobalt"),
        ["Песок"] = new("Пісок", "Sand"),
        ["Роза"] = new("Троянда", "Rose"),
        ["Моно"] = new("Моно", "Mono"),
        ["Графит и бирюзовый акцент"] = new("Графіт і бірюзовий акцент", "Graphite with a turquoise accent"),
        ["Мягкий Purity UI дашборд с фиолетовым акцентом"] = new("М'який Purity UI дашборд із фіолетовим акцентом", "Soft Purity UI dashboard with a violet accent"),
        ["Тёмный DesignCode UI: индиго-поверхности и ледяной акцент"] = new("Темний DesignCode UI: індіго-поверхні та крижаний акцент", "Dark DesignCode UI: indigo surfaces with an ice-blue accent"),
        ["Мягкий Soft UI дашборд: индиго #4318ff и голубые акценты"] = new("М'який Soft UI дашборд: індиго #4318ff і блакитні акценти", "Soft UI dashboard: indigo #4318ff with sky-blue accents"),
        ["Каталог иконок Material"] = new("Каталог іконок Material", "Material icon catalog"),
        ["Более 2000 иконок Material Design Icons с поиском и копированием разметки. Кликните по иконке, чтобы скопировать её в буфер обмена."] = new("Понад 2000 іконок Material Design Icons із пошуком і копіюванням розмітки. Клацніть по іконці, щоб скопіювати її до буфера обміну.", "More than 2000 Material Design Icons with search and markup copying. Click an icon to copy it to the clipboard."),
        ["Открыть каталог"] = new("Відкрити каталог", "Open catalog"),
        ["Открыть каталог иконок Material"] = new("Відкрити каталог іконок Material", "Open the Material icon catalog"),
        ["Размер"] = new("Розмір", "Size"),
        ["Поиск иконки"] = new("Пошук іконки", "Search icon"),
        ["Фильтр по названию иконки"] = new("Фільтр за назвою іконки", "Filter by icon name"),
        ["Кликните по иконке, чтобы скопировать разметку"] = new("Клацніть по іконці, щоб скопіювати розмітку", "Click an icon to copy its markup"),
        ["Ничего не найдено"] = new("Нічого не знайдено", "Nothing found"),
        ["Ничего не найдено: {0}"] = new("Нічого не знайдено: {0}", "Nothing found: {0}"),
        ["Иконок: {0}"] = new("Іконок: {0}", "Icons: {0}"),
        ["Показано: {0} из {1}"] = new("Показано: {0} з {1}", "Showing: {0} of {1}"),
        ["Найдено: {0} (показаны первые {1})"] = new("Знайдено: {0} (показано перші {1})", "Found: {0} (showing first {1})"),
        ["Скопировано: {0}"] = new("Скопійовано: {0}", "Copied: {0}"),
        ["Не удалось скопировать разметку: {0}"] = new("Не вдалося скопіювати розмітку: {0}", "Could not copy the markup: {0}"),
        ["Официальный MUI: синий #0072e5, фиолетовый и Material 3 формы"] = new("Офіційний MUI: синій #0072e5, фіолетовий та форми Material 3", "Official MUI: blue #0072e5, violet and Material 3 shapes"),
        ["Untitled UI: фиолетовый #6941c6 и глубокий текст"] = new("Untitled UI: фіолетовий #6941c6 і глибокий текст", "Untitled UI: violet #6941c6 with deep text"),
        ["Ant Design: синий #1677ff и геометрия 4/8/12"] = new("Ant Design: синій #1677ff і геометрія 4/8/12", "Ant Design: blue #1677ff with 4/8/12 geometry"),
        ["IBM Carbon: синий #0f62fe и строгая сетка"] = new("IBM Carbon: синій #0f62fe і сувора сітка", "IBM Carbon: blue #0f62fe with strict geometry"),
        ["Microsoft Fluent 2: тёмный #1f1f1f и #479ef5"] = new("Microsoft Fluent 2: темний #1f1f1f і #479ef5", "Microsoft Fluent 2: dark #1f1f1f with #479ef5"),
        ["Radix: цинковые нейтрали и синий #0090ff"] = new("Radix: цинкові нейтралі та синій #0090ff", "Radix: zinc neutrals with blue #0090ff"),
        ["PrimeVue Aura: изумрудный #10b981"] = new("PrimeVue Aura: смарагдовий #10b981", "PrimeVue Aura: emerald #10b981"),
        ["Mantine: синий #228be6 и фиолетовый"] = new("Mantine: синій #228be6 і фіолетовий", "Mantine: blue #228be6 with violet"),
        ["Bulma: бирюзовый #00d1b2"] = new("Bulma: бірюзовий #00d1b2", "Bulma: turquoise #00d1b2"),
        ["Spectre.css: минимализм и фиолетовый #5755d9"] = new("Spectre.css: мінімалізм і фіолетовий #5755d9", "Spectre.css: minimal purple #5755d9"),
        ["shadcn/ui: цинк и чёрные кнопки"] = new("shadcn/ui: цинк і чорні кнопки", "shadcn/ui: zinc neutrals with black buttons"),
        ["GitHub Primer: синий #0969da и холст #f6f8fa"] = new("GitHub Primer: синій #0969da і полотно #f6f8fa", "GitHub Primer: blue #0969da on #f6f8fa"),
        ["Чистый и контрастный интерфейс"] = new("Чистий і контрастний інтерфейс", "Clean and high-contrast interface"),
        ["Классический синий"] = new("Класичний синій", "Classic blue"),
        ["Лавандовый и бирюзовый"] = new("Лавандовий і бірюзовий", "Lavender and turquoise"),
        ["Глубокий синий и морская бирюза"] = new("Глибокий синій і морська бірюза", "Deep blue and sea turquoise"),
        ["Тёплый Sunset-градиент"] = new("Теплий Sunset-градієнт", "Warm sunset gradient"),
        ["Холодный зелёный и лаймовый акцент"] = new("Холодний зелений і лаймовий акцент", "Cool green and lime accent"),
        ["Электрический magenta и cyan"] = new("Електричний magenta і cyan", "Electric magenta and cyan"),
        ["Северное сияние с мятным свечением"] = new("Північне сяйво з м'ятним сяянням", "Northern lights with a mint glow"),
        ["Глубокий красный и розовый акцент"] = new("Глибокий червоний і рожевий акцент", "Deep red and pink accent"),
        ["Синий графит с ярким акцентом"] = new("Синій графіт із яскравим акцентом", "Blue graphite with a bright accent"),
        ["Тёплая светлая палитра с терракотовым акцентом"] = new("Тепла світла палітра з теракотовим акцентом", "Warm light palette with a terracotta accent"),
        ["Мягкий розовый интерфейс"] = new("М'який рожевий інтерфейс", "Soft pink interface"),
        ["Минималистичная графитовая палитра"] = new("Мінімалістична графіто́ва палітра", "Minimalist graphite palette"),

        ["изменён {0:dd.MM.yyyy HH:mm}"] = new("змінено {0:dd.MM.yyyy HH:mm}", "modified {0:dd.MM.yyyy HH:mm}"),
        ["Выбрано элементов одежды: {0}"] = new("Вибрано елементів одягу: {0}", "Selected clothing items: {0}"),
        ["Статус: {0}"] = new("Статус: {0}", "Status: {0}"),
        ["Последний платёж: {0}"] = new("Останній платіж: {0}", "Last payment: {0}"),
        ["Следующий платёж: {0}"] = new("Наступний платіж: {0}", "Next payment: {0}"),
        ["Выбрано текстур: {0}"] = new("Вибрано текстур: {0}", "Selected textures: {0}"),
        [" аддонов"] = new(" аддонів", " addons"),
        [" вещей"] = new(" речей", " items"),

        // Common dialogs and actions
        ["Выберите папку GTA V"] = new("Виберіть папку GTA V", "Choose GTA V folder"),
        ["В выбранной папке не найден файл GTA5.exe."] = new("У вибраній папці не знайдено файл GTA5.exe.", "GTA5.exe was not found in the selected folder."),
        ["Некорректная папка GTA V"] = new("Некоректна папка GTA V", "Invalid GTA V folder"),
        ["Выберите главную папку проектов"] = new("Виберіть головну папку проєктів", "Choose main projects folder"),
        ["Нельзя использовать корневой диск (например, C:\\) как папку проектов.\\n\\nВыберите или создайте вложенную папку."] = new("Не можна використовувати кореневий диск (наприклад, C:\\) як папку проєктів.\\n\\nВиберіть або створіть вкладену папку.", "The root drive (for example, C:\\) cannot be used as the projects folder.\\n\\nChoose or create a nested folder."),
        ["Нет доступа к папке. Выберите папку, в которую у вас есть права записи."] = new("Немає доступу до папки. Виберіть папку, до якої у вас є права запису.", "Access denied. Choose a folder you have permission to write to."),
        ["Сначала укажите папку проектов в настройках."] = new("Спершу вкажіть папку проєктів у налаштуваннях.", "First choose the projects folder in Settings."),
        ["Название проекта содержит недопустимые символы."] = new("Назва проєкту містить недопустимі символи.", "The project name contains invalid characters."),
        ["Неверное название"] = new("Некоректна назва", "Invalid name"),
        ["Путь проекта находится за пределами папки проектов."] = new("Шлях проєкту знаходиться за межами папки проєктів.", "The project path is outside the projects folder."),
        ["Проект с таким названием уже существует. Выберите другое название."] = new("Проєкт із такою назвою вже існує. Виберіть іншу назву.", "A project with this name already exists. Choose another name."),
        ["Проект уже существует"] = new("Проєкт уже існує", "Project already exists"),
        ["Открыть проект KazanClothTool"] = new("Відкрити проєкт Kazan Cloth Tool", "Open Kazan Cloth Tool project"),
        ["Файл проекта больше не существует."] = new("Файл проєкту більше не існує.", "The project file no longer exists."),
        ["Проект не найден"] = new("Проєкт не знайдено", "Project not found"),
        ["Выберите мужские YDD"] = new("Виберіть чоловічі YDD", "Choose male YDD files"),
        ["Выберите женские YDD"] = new("Виберіть жіночі YDD", "Choose female YDD files"),
        ["Выберите папку с мужскими YDD"] = new("Виберіть папку з чоловічими YDD", "Choose a folder with male YDD files"),
        ["Выберите папку с женскими YDD"] = new("Виберіть папку з жіночими YDD", "Choose a folder with female YDD files"),
        ["Добавление одежды..."] = new("Додавання одягу...", "Adding clothing..."),
        ["Одежда добавлена"] = new("Одяг додано", "Clothing added"),
        ["Не удалось добавить одежду"] = new("Не вдалося додати одяг", "Failed to add clothing"),
        ["Сначала откройте или создайте проект, а затем добавьте одежду."] = new("Спершу відкрийте або створіть проєкт, а потім додайте одяг.", "First open or create a project, then add clothing."),
        ["Сначала откройте или создайте проект, а затем выберите папку с одеждой."] = new("Спершу відкрийте або створіть проєкт, а потім виберіть папку з одягом.", "First open or create a project, then choose a clothing folder."),
        ["Автоудаление изменяет файлы на диске. Удалённые файлы нельзя восстановить.\\n\\nВключить эту функцию?"] = new("Автовидалення змінює файли на диску. Видалені файли неможливо відновити.\\n\\nУвімкнути цю функцію?", "Auto-delete changes files on disk. Deleted files cannot be recovered.\\n\\nEnable this feature?"),
        ["Путь GTA V не настроен. 3D-просмотр будет доступен после выбора пути в настройках."] = new("Шлях GTA V не налаштовано. 3D-перегляд буде доступний після вибору шляху в налаштуваннях.", "The GTA V path is not configured. 3D preview will be available after selecting it in Settings.")
    };

    private static readonly Dictionary<string, Translation> SupplementalCatalog = new(StringComparer.Ordinal)
    {
        // Editor labels and messages
        ["Параметры отключены"] = new("Параметри вимкнено", "Parameters disabled"),
        ["ПРЕДУПРЕЖДЕНИЕ"] = new("ПОПЕРЕДЖЕННЯ", "WARNING"),
        ["Выбранные элементы одежды не полностью совпадают. Чтобы изменить общие параметры, выберите одинаковые элементы или нажмите кнопку ниже для принудительного применения."] = new("Вибрані елементи одягу не повністю збігаються. Щоб змінити спільні параметри, виберіть однакові елементи або натисніть кнопку нижче для примусового застосування.", "The selected clothing items do not fully match. To change common properties, select identical items or use the button below to apply them anyway."),
        ["Принудительно применить"] = new("Примусово застосувати", "Apply anyway"),
        ["Применение отключено"] = new("Застосування вимкнено", "Apply disabled"),
        ["ПРИМЕНЕНИЕ ВКЛЮЧЕНО"] = new("ЗАСТОСУВАННЯ УВІМКНЕНО", "APPLY ENABLED"),
        ["Нельзя включить применение для элементов одежды разных типов. Выберите только один тип одежды"] = new("Не можна ввімкнути застосування для елементів одягу різних типів. Виберіть лише один тип одягу", "Cannot enable apply for different clothing types. Select only one clothing type."),
        ["Параметры одежды"] = new("Параметри одягу", "Clothing properties"),
        ["Звук"] = new("Звук", "Sound"),
        ["Флаг отрисовки"] = new("Прапорець відображення", "Render flag"),
        ["Встроенная текстура"] = new("Вбудована текстура", "Embedded texture"),
        ["Флаги"] = new("Прапорці", "Flags"),
        ["Скрывает волосы"] = new("Приховує волосся", "Hides hair"),
        ["Включить масштаб волос"] = new("Увімкнути масштаб волосся", "Enable hair scale"),
        ["Масштаб волос"] = new("Масштаб волосся", "Hair scale"),
        ["Включить каблуки"] = new("Увімкнути каблуки", "Enable high heels"),
        ["Высота каблуков"] = new("Висота каблуків", "High heel height"),
        ["Параметры предпросмотра"] = new("Параметри перегляду", "Preview settings"),
        ["Запустите 3D-предпросмотр, чтобы включить параметры"] = new("Запустіть 3D-перегляд, щоб увімкнути параметри", "Start the 3D preview to enable these settings"),
        ["Сохранять предпросмотр"] = new("Зберігати перегляд", "Keep preview"),
        ["Организация"] = new("Організація", "Organization"),
        ["Группа"] = new("Група", "Group"),
        ["(Несколько групп)"] = new("(Кілька груп)", "(Several groups)"),
        ["Теги"] = new("Теги", "Tags"),
        ["Отображаемое имя"] = new("Відображуване ім’я", "Display name"),
        ["Тип одежды"] = new("Тип одягу", "Clothing type"),
        ["Пол"] = new("Стать", "Gender"),
        ["Файл от первого лица"] = new("Файл від першої особи", "First-person file"),
        ["Файл физики одежды (.yld)"] = new("Файл фізики одягу (.yld)", "Clothing physics file (.yld)"),
        ["Встроенные"] = new("Вбудовані", "Embedded"),
        ["У одной или нескольких текстур есть предупреждение"] = new("В однієї або кількох текстур є попередження", "One or more textures have a warning"),
        ["У одной или нескольких встроенных текстур есть предупреждение"] = new("В однієї або кількох вбудованих текстур є попередження", "One or more embedded textures have a warning"),
        ["Эта текстура будет оптимизирована при сборке ресурса"] = new("Ця текстура буде оптимізована під час складання ресурсу", "This texture will be optimized during resource build"),
        ["Текстура не найдена. Вероятно, файл .ytd пуст"] = new("Текстуру не знайдено. Ймовірно, файл .ytd порожній", "Texture not found. The .ytd file may be empty"),
        ["Это заменённая текстура"] = new("Це замінена текстура", "This is a replaced texture"),
        ["Оптимизировать текстуру"] = new("Оптимізувати текстуру", "Optimize texture"),
        ["Отменить оптимизацию"] = new("Скасувати оптимізацію", "Cancel optimization"),
        ["Удалить"] = new("Видалити", "Delete"),
        ["Заменить"] = new("Замінити", "Replace"),
        ["Открыть расположение файла"] = new("Відкрити розташування файлу", "Open file location"),
        ["Экспортировать как YTD"] = new("Експортувати як YTD", "Export as YTD"),
        ["Экспортировать текстуру в DDS"] = new("Експортувати текстуру в DDS", "Export texture to DDS"),
        ["Экспортировать текстуру в PNG"] = new("Експортувати текстуру в PNG", "Export texture to PNG"),
        ["ДОБАВИТЬ ТЕКСТУРЫ"] = new("ДОДАТИ ТЕКСТУРИ", "ADD TEXTURES"),
        ["Просмотр"] = new("Перегляд", "Preview"),
        ["Н/Д"] = new("Н/Д", "N/A"),

        // Project and action labels
        [" аддонов · "] = new(" аддонів · ", " addons · "),
        [" предметов"] = new(" предметів", " items"),
        ["Открыть интерактивный 3D-просмотр"] = new("Відкрити інтерактивний 3D-перегляд", "Open interactive 3D preview"),
        ["Добавить женские YDD или выбрать папку"] = new("Додати жіночі YDD або вибрати папку", "Add female YDD files or choose a folder"),
        ["Добавить мужские YDD или выбрать папку"] = new("Додати чоловічі YDD або вибрати папку", "Add male YDD files or choose a folder"),
        ["Выбрать папку с женской одеждой"] = new("Вибрати папку з жіночим одягом", "Choose a folder with women's clothing"),
        ["Выбрать папку с мужской одеждой"] = new("Вибрати папку з чоловічим одягом", "Choose a folder with men's clothing"),
        ["Категория одежды"] = new("Категорія одягу", "Clothing category"),
        ["Удалить выбранное"] = new("Видалити вибране", "Delete selected"),
        ["Удаление доступно после выбора элемента"] = new("Видалення доступне після вибору елемента", "Deletion is available after selecting an item"),
        ["ЖЕНСКАЯ ОДЕЖДА"] = new("ЖІНОЧИЙ ОДЯГ", "WOMEN'S CLOTHING"),
        ["МУЖСКАЯ ОДЕЖДА"] = new("ЧОЛОВІЧИЙ ОДЯГ", "MEN'S CLOTHING"),
        ["0 элементов"] = new("0 елементів", "0 items"),
        ["Одежда не добавлена"] = new("Одяг не додано", "No clothing added"),
        ["Элементы одежды"] = new("Елементи одягу", "Clothing items"),
        ["описание этого параметра"] = new("опис цього параметра", "description of this setting"),
        ["Выберите язык интерфейса"] = new("Виберіть мову інтерфейсу", "Choose the interface language"),
        ["Нет доступа к выбранному пути. Проверьте права доступа."] = new("Немає доступу до вибраного шляху. Перевірте права доступу.", "Access to the selected path is denied. Check your permissions."),
        ["Файл или папка не найдены. Проверьте путь."] = new("Файл або папку не знайдено. Перевірте шлях.", "The file or folder was not found. Check the path."),
        ["Не удалось выполнить операцию с файлом. Проверьте, что файл не используется другим приложением."] = new("Не вдалося виконати операцію з файлом. Перевірте, чи файл не використовується іншим застосунком.", "The file operation could not be completed. Check whether another application is using the file."),
        ["Проверьте введённые данные и повторите действие."] = new("Перевірте введені дані та повторіть дію.", "Check the entered data and try again."),
        ["Данные изменились во время операции. Повторите действие."] = new("Дані змінилися під час операції. Повторіть дію.", "The data changed during the operation. Please try again."),
        ["Непредвиденная ошибка. Подробности записаны в журнал."] = new("Несподівана помилка. Подробиці записано в журналі.", "An unexpected error occurred. Details were written to the log."),
        ["Выберите папку проекта"] = new("Виберіть папку проєкту", "Choose project folder"),
        ["Удалить предмет?"] = new("Видалити предмет?", "Delete item?"),
        ["Удалить выбранный предмет из текущего проекта?"] = new("Видалити вибраний предмет із поточного проєкту?", "Delete the selected item from the current project?"),
        ["Женская одежда"] = new("Жіночий одяг", "Women's clothing"),
        ["Мужская одежда"] = new("Чоловічий одяг", "Men's clothing"),
        ["не выбрана"] = new("не вибрано", "not selected"),
        ["Папка проекта:"] = new("Папка проєкту:", "Project folder:"),

        // Common dialog captions and actions
        ["Удаление одежды"] = new("Видалення одягу", "Delete clothing"),
        ["Удаление текстуры"] = new("Видалення текстури", "Delete texture"),
        ["Настройка"] = new("Налаштування", "Settings"),
        ["Ошибка экспорта"] = new("Помилка експорту", "Export error"),
        ["Экспорт всех PNG"] = new("Експорт усіх PNG", "Export all PNG"),
        ["Оптимизация"] = new("Оптимізація", "Optimization"),
        ["Дубликаты"] = new("Дублікати", "Duplicates"),
        ["Инспектор дубликатов"] = new("Інспектор дублікатів", "Duplicate inspector"),
        ["Группа одежды"] = new("Група одягу", "Clothing group"),
        ["Выбрать"] = new("Вибрати", "Select"),
        ["Выбрать папку"] = new("Вибрати папку", "Choose folder"),
        ["Отмена"] = new("Скасувати", "Cancel"),
        ["Закрыть"] = new("Закрити", "Close"),
        ["Сохранить"] = new("Зберегти", "Save"),

        // Shell, first-run setup and project setup
        ["Редактор одежды"] = new("Редактор одягу", "Clothing editor"),
        ["Открыть набор одежды"] = new("Відкрити набір одягу", "Open clothing set"),
        ["Сохранить проект"] = new("Зберегти проєкт", "Save project"),
        ["3D-редактор одежды и текстур"] = new("3D-редактор одягу та текстур", "3D clothing and texture editor"),
        ["Подготовка редактора..."] = new("Підготовка редактора...", "Preparing the editor..."),
        ["Выберите родительскую папку, в которой будут храниться все автономные проекты KazanClothTool."] = new("Виберіть батьківську папку, у якій зберігатимуться всі автономні проєкти Kazan Cloth Tool.", "Choose the parent folder where all self-contained Kazan Cloth Tool projects will be stored."),
        ["Не выбирайте папку конкретного проекта — в этой папке можно будет создать несколько проектов."] = new("Не вибирайте папку конкретного проєкту — у цій папці можна буде створити кілька проєктів.", "Do not choose a specific project folder — you can create several projects inside it."),
        ["• Настройка сохраняется в профиле пользователя и не исчезает при обновлении приложения."] = new("• Налаштування зберігається в профілі користувача і не зникає під час оновлення застосунку.", "• The setting is stored in your user profile and is preserved when the app is updated."),
        ["• Главную папку можно изменить позже в разделе «Настройки»."] = new("• Головну папку можна змінити пізніше в розділі «Налаштування».", "• You can change the main folder later in Settings."),
        ["Настройка проекта"] = new("Налаштування проєкту", "Project setup"),
        ["Название проекта"] = new("Назва проєкту", "Project name"),
        ["Тип проекта"] = new("Тип проєкту", "Project type"),
        ["Автономный"] = new("Автономний", "Self-contained"),
        ["Внешний"] = new("Зовнішній", "External"),
        ["Файлы копируются в папку проекта. Проект можно легко переносить и передавать другим пользователям."] = new("Файли копіюються в папку проєкту. Проєкт можна легко переносити й передавати іншим користувачам.", "Files are copied into the project folder. The project can be easily moved and shared."),
        ["Файлы остаются в исходных расположениях. Занимает меньше места на диске, но проект нельзя переносить."] = new("Файли залишаються в початкових розташуваннях. Займає менше місця на диску, але проєкт не можна переносити.", "Files remain in their original locations. This uses less disk space, but the project cannot be moved."),
        ["Перемещение или удаление исходных файлов приведёт к повреждению проекта."] = new("Переміщення або видалення вихідних файлів призведе до пошкодження проєкту.", "Moving or deleting the original files will damage the project."),
        ["Создание нового проекта"] = new("Створення нового проєкту", "Create a new project"),
        ["Открытие существующего аддона"] = new("Відкриття наявного аддона", "Open an existing addon"),
        ["Создать"] = new("Створити", "Create"),
        ["Открыть"] = new("Відкрити", "Open"),
        ["Требуется настройка"] = new("Потрібне налаштування", "Setup required"),
        ["Выберите ГЛАВНУЮ папку, в которой будут храниться ВСЕ проекты (не папку конкретного проекта)"] = new("Виберіть ГОЛОВНУ папку, у якій зберігатимуться ВСІ проєкти (не папку конкретного проєкту)", "Choose the MAIN folder where ALL projects will be stored (not a specific project folder)"),
        ["Перед продолжением выберите главную папку."] = new("Перед продовженням виберіть головну папку.", "Choose the main folder before continuing."),
        ["Корневой диск (например, C:\\) нельзя использовать как главную папку. Выберите или создайте вложенную папку."] = new("Кореневий диск (наприклад, C:\\) не можна використовувати як головну папку. Виберіть або створіть вкладену папку.", "The root drive (for example, C:\\) cannot be used as the main folder. Choose or create a nested folder."),
        ["Доступ запрещён. Выберите папку, в которую у вас есть права записи."] = new("Доступ заборонено. Виберіть папку, до якої у вас є права запису.", "Access denied. Choose a folder you have permission to write to."),

        // Build window
        ["KazanClothTool — сборка ресурса"] = new("Kazan Cloth Tool — складання ресурсу", "Kazan Cloth Tool — resource build"),
        ["Выбор типа ресурса"] = new("Вибір типу ресурсу", "Resource type"),
        ["Стандартный ресурс"] = new("Стандартний ресурс", "Standard resource"),
        ["Одиночная игра"] = new("Одиночна гра", "Single-player"),
        ["Папка вывода"] = new("Папка виводу", "Output folder"),
        ["Собирать аддоны как отдельные ресурсы"] = new("Збирати аддони як окремі ресурси", "Build addons as separate resources"),
        ["Собрать ресурс"] = new("Зібрати ресурс", "Build resource"),
        ["Ошибка сборки"] = new("Помилка складання", "Build error"),
        ["Сборка завершена"] = new("Складання завершено", "Build completed"),
        ["Сборка"] = new("Складання", "Build"),

        // Optimization, selection and account dialogs
        ["Оптимизация текстур"] = new("Оптимізація текстур", "Texture optimization"),
        ["Текущая текстура"] = new("Поточна текстура", "Current texture"),
        ["Название: "] = new("Назва: ", "Name: "),
        ["Сжатие: "] = new("Стиснення: ", "Compression: "),
        ["Ширина: "] = new("Ширина: ", "Width: "),
        ["Высота: "] = new("Висота: ", "Height: "),
        ["Количество мип-уровней: "] = new("Кількість mip-рівнів: ", "Mip level count: "),
        ["Уменьшить размер текстуры"] = new("Зменшити розмір текстури", "Downscale texture"),
        ["Выбрать размер текстуры"] = new("Вибрати розмір текстури", "Choose texture size"),
        ["Изменение размера текстуры здесь ухудшит качество. Чтобы сохранить качество, выполняйте эту операцию в 3D-программах с запеканием текстур."] = new("Зменшення розміру текстури тут погіршить якість. Щоб зберегти якість, виконуйте цю операцію у 3D-програмах із запіканням текстур.", "Reducing the texture size here reduces quality. To preserve quality, perform this operation in a 3D application with texture baking."),
        ["Выходная текстура"] = new("Вихідна текстура", "Output texture"),
        ["Изменить сжатие текстуры"] = new("Змінити стиснення текстури", "Change texture compression"),
        ["Выбрать сжатие"] = new("Вибрати стиснення", "Choose compression"),
        ["Оптимизировать"] = new("Оптимізувати", "Optimize"),
        ["Параметры оптимизации не выбраны"] = new("Параметри оптимізації не вибрано", "No optimization settings selected"),
        ["Текстуры будут оптимизированы при сборке ресурса"] = new("Текстури буде оптимізовано під час складання ресурсу", "Textures will be optimized during the resource build"),
        ["DXT1 (без альфа)"] = new("DXT1 (без альфа)", "DXT1 (no alpha)"),
        ["DXT3 (резкая альфа)"] = new("DXT3 (різка альфа)", "DXT3 (sharp alpha)"),
        ["DXT5 (градиентная альфа)"] = new("DXT5 (градієнтна альфа)", "DXT5 (gradient alpha)"),
        ["Выбор свойств одежды"] = new("Вибір властивостей одягу", "Clothing properties selection"),
        ["Не удалось распознать свойства одежды"] = new("Не вдалося розпізнати властивості одягу", "Could not identify clothing properties"),
        ["Тип актива"] = new("Тип актива", "Asset type"),
        ["Аккаунт"] = new("Обліковий запис", "Account"),
        ["Информация об аккаунте"] = new("Інформація про обліковий запис", "Account information"),
        ["ВОЙТИ"] = new("УВІЙТИ", "SIGN IN"),
        ["ВЫЙТИ"] = new("ВИЙТИ", "SIGN OUT"),
        ["НУЖНА НАСТРОЙКА"] = new("ПОТРІБНЕ НАЛАШТУВАННЯ", "SETUP REQUIRED"),
        ["Журнал"] = new("Журнал", "Log"),
        ["Время"] = new("Час", "Time"),
        ["Сообщение"] = new("Повідомлення", "Message"),
        ["Диалог сообщения"] = new("Діалог повідомлення", "Message dialog"),
        ["Заголовок сообщения"] = new("Заголовок повідомлення", "Message title"),
        ["Текст сообщения"] = new("Текст повідомлення", "Message text"),
        ["Выбрать все"] = new("Вибрати все", "Select all"),
        ["Снять выбор"] = new("Зняти вибір", "Clear selection"),
        ["Отменить все"] = new("Скасувати все", "Cancel all"),
        ["Подтвердить"] = new("Підтвердити", "Confirm"),
        ["Дубликаты не найдены"] = new("Дублікатів не знайдено", "No duplicates found"),
        ["Найдено групп дубликатов: "] = new("Знайдено груп дублікатів: ", "Duplicate groups found: "),
        ["ДУП"] = new("ДУП", "DUP"),
        ["Удалить все, кроме первого"] = new("Видалити всі, крім першого", "Delete all except the first"),
        ["Обновить"] = new("Оновити", "Refresh"),
        ["Показать в инспекторе дубликатов"] = new("Показати в інспекторі дублікатів", "Show in duplicate inspector"),
        ["Дублировать для противоположного пола"] = new("Дублювати для протилежної статі", "Duplicate for the opposite gender"),
        ["Переместить в другой аддон"] = new("Перемістити до іншого аддона", "Move to another addon"),
        ["Экспортировать как YDD"] = new("Експортувати як YDD", "Export as YDD"),
        ["Экспортировать как YTD с текстурами"] = new("Експортувати як YTD з текстурами", "Export as YTD with textures"),
        ["Экспортировать только текстуры в DDS"] = new("Експортувати лише текстури в DDS", "Export textures only to DDS"),
        ["Экспортировать только текстуры в PNG"] = new("Експортувати лише текстури в PNG", "Export textures only to PNG"),
        ["Переместить"] = new("Перемістити", "Move"),
        ["Зарезервировано"] = new("Зарезервовано", "Reserved"),
        ["РЗР"] = new("РЗР", "RSV"),
        ["Новое"] = new("Нове", "New"),
        ["НОВ"] = new("НОВ", "NEW"),
        ["Выбрано"] = new("Вибрано", "Selected"),
        ["Зашифровано — предпросмотр недоступен"] = new("Зашифровано — перегляд недоступний", "Encrypted — preview unavailable"),
        ["Добавить"] = new("Додати", "Add"),
        ["Очистить группу"] = new("Очистити групу", "Clear group"),
        ["Введите значение"] = new("Введіть значення", "Enter a value"),
        ["Числовое поле"] = new("Числове поле", "Numeric field"),
        ["Без названия"] = new("Без назви", "No name"),
        ["Открыть папку"] = new("Відкрити теку", "Open folder"),
        ["Язык интерфейса"] = new("Мова інтерфейсу", "Interface language"),
        ["Diffuse"] = new("Diffuse (дифузія)", "Diffuse map"),
        ["Normal"] = new("Normal (нормалі)", "Normal map"),
        ["Specular"] = new("Specular (спекулярна)", "Specular map"),
        ["Разрешение diffuse-текстуры"] = new("Роздільність diffuse-текстури", "Diffuse texture resolution"),
        ["Разрешение normal-текстуры"] = new("Роздільність normal-текстури", "Normal texture resolution"),
        ["Разрешение specular-текстуры"] = new("Роздільність specular-текстури", "Specular texture resolution"),
        ["Поиск одежды"] = new("Пошук одягу", "Search clothing"),

        // Project list, editor actions and dialogs
        ["Зарезервированный элемент одежды"] = new("Зарезервований елемент одягу", "Reserved clothing item"),
        ["Это зарезервированный слот одежды, его нельзя изменить."] = new("Це зарезервований слот одягу, його не можна змінити.", "This is a reserved clothing slot and cannot be changed."),
        ["ЗАМЕНИТЬ ЗАРЕЗЕРВИРОВАННЫЙ ЭЛЕМЕНТ"] = new("ЗАМІНИТИ ЗАРЕЗЕРВОВАНИЙ ЕЛЕМЕНТ", "REPLACE RESERVED ITEM"),
        ["Выбрано элементов одежды: {0}"] = new("Вибрано елементів одягу: {0}", "Selected clothing items: {0}"),
        ["Удалить выбранный элемент одежды «{0}»?"] = new("Видалити вибраний елемент одягу «{0}»?", "Delete selected clothing item '{0}'?"),
        ["Удалить выбранные элементы одежды ({0})?"] = new("Видалити вибрані елементи одягу ({0})?", "Delete selected clothing items ({0})?"),
        ["Нумерация последующих элементов изменится."] = new("Нумерація наступних елементів зміниться.", "The numbering of subsequent items will change."),
        ["Заменить их зарезервированными слотами вместо удаления?"] = new("Замінити їх зарезервованими слотами замість видалення?", "Replace them with reserved slots instead of deleting?"),
        ["Нет проекта"] = new("Немає проєкту", "No project"),
        ["Ошибка проверки перетаскивания: {0}"] = new("Помилка перевірки перетягування: {0}", "Drag-and-drop validation error: {0}"),
        ["Ошибка перетаскивания"] = new("Помилка перетягування", "Drag-and-drop error"),
        ["Не удалось извлечь виртуальные файлы"] = new("Не вдалося витягти віртуальні файли", "Could not extract virtual files"),
        ["Не удалось получить доступ к следующим файлам:"] = new("Не вдалося отримати доступ до таких файлів:", "Could not access the following files:"),
        ["Это могут быть виртуальные пути. Сначала извлеките файлы в папку и перетащите их оттуда."] = new("Це можуть бути віртуальні шляхи. Спочатку витягніть файли в папку та перетягніть їх звідти.", "These may be virtual paths. Extract the files to a folder first, then drag them from there."),
        ["Файлы недоступны"] = new("Файли недоступні", "Files unavailable"),
        ["Добавление перетащенной одежды..."] = new("Додавання перетягнутого одягу...", "Adding dragged clothing..."),
        ["Не удалось определить пол для {0} файлов. Выберите пол одежды:"] = new("Не вдалося визначити стать для {0} файлів. Виберіть стать одягу:", "Could not determine gender for {0} files. Choose clothing gender:"),
        ["Выбор пола"] = new("Вибір статі", "Choose gender"),
        ["Ошибка при добавлении перетащенных файлов: {0}"] = new("Помилка під час додавання перетягнутих файлів: {0}", "Error adding dragged files: {0}"),
        ["Не удалось обработать перетащенные файлы: {0}"] = new("Не вдалося обробити перетягнуті файли: {0}", "Could not process dragged files: {0}"),
        ["Поиск одежды..."] = new("Пошук одягу...", "Search clothing..."),
        ["В выбранных папках нет файлов YDD."] = new("У вибраних папках немає файлів YDD.", "There are no YDD files in the selected folders."),
        ["Добавлено файлов: {0}"] = new("Додано файлів: {0}", "Files added: {0}"),
        ["Не выбрано ни одного элемента одежды."] = new("Не вибрано жодного елемента одягу.", "No clothing items selected."),
        ["Удалить одежду"] = new("Видалити одяг", "Delete clothing"),
        ["Ошибка открытия"] = new("Помилка відкриття", "Open error"),
        ["Одежда не найдена"] = new("Одяг не знайдено", "Clothing not found"),
        ["Загрузка одежды..."] = new("Завантаження одягу...", "Loading clothing..."),
        ["Одежда загружена"] = new("Одяг завантажено", "Clothing loaded"),
        ["Загрузка не завершена"] = new("Завантаження не завершено", "Loading incomplete"),
        ["Не выбрано подходящих .meta файлов одежды."] = new("Не вибрано відповідних .meta файлів одягу.", "No suitable clothing .meta files selected."),
        ["Для выбранных .meta файлов не найдены элементы одежды (.ydd)."] = new("Для вибраних .meta файлів не знайдено елементів одягу (.ydd).", "No clothing items (.ydd) were found for the selected .meta files."),
        ["Проверьте, что файлы .ydd находятся в той же папке или во вложенных папках рядом с .meta."] = new("Перевірте, чи файли .ydd містяться в тій самій теці або у вкладених теках поруч із .meta.", "Make sure the .ydd files are in the same folder or nested folders next to the .meta file."),

        // First-run and settings messages
        ["Чтобы продолжить работу с приложением, необходимо выбрать главную папку.\\n\\nЗакрыть приложение?"] = new("Щоб продовжити роботу в застосунку, потрібно вибрати головну папку.\\n\\nЗакрити застосунок?", "To continue using the app, you must choose the main folder.\\n\\nClose the app?"),
        ["Ошибка: {0}"] = new("Помилка: {0}", "Error: {0}"),
        ["Некорректная папка"] = new("Некоректна папка", "Invalid folder"),
        ["Не удалось настроить папку проектов: {0}"] = new("Не вдалося налаштувати папку проєктів: {0}", "Could not configure the projects folder: {0}"),
        ["Папка проектов обновлена: {0}"] = new("Папку проєктів оновлено: {0}", "Projects folder updated: {0}"),

        // Build validation and duplicate inspector
        ["Проект не загружен. Сначала создайте или откройте проект."] = new("Проєкт не завантажено. Спочатку створіть або відкрийте проєкт.", "The project is not loaded. Create or open a project first."),
        ["Не удалось определить путь сборки. Проверьте настройки проекта."] = new("Не вдалося визначити шлях складання. Перевірте налаштування проєкту.", "Could not determine the build path. Check the project settings."),
        ["Элементы одежды не найдены. Добавьте одежду, чтобы собрать ресурс."] = new("Елементи одягу не знайдено. Додайте одяг, щоб зібрати ресурс.", "No clothing items found. Add clothing before building the resource."),
        ["Неподдерживаемый тип ресурса: {0}"] = new("Непідтримуваний тип ресурсу: {0}", "Unsupported resource type: {0}"),
        ["Заполните все поля и убедитесь, что проект загружен."] = new("Заповніть усі поля та переконайтеся, що проєкт завантажено.", "Fill in all fields and make sure the project is loaded."),
        ["Сборка завершена, затраченное время: {0}"] = new("Складання завершено, витрачений час: {0}", "Build completed in: {0}"),
        ["Не удалось собрать ресурс: {0}"] = new("Не вдалося зібрати ресурс: {0}", "Could not build the resource: {0}"),
        ["Название проекта не может быть пустым"] = new("Назва проєкту не може бути порожньою", "The project name cannot be empty"),
        ["Название проекта должно содержать не менее 3 символов"] = new("Назва проєкту має містити щонайменше 3 символи", "The project name must contain at least 3 characters"),
        ["Название проекта не может быть длиннее 50 символов"] = new("Назва проєкту не може бути довшою за 50 символів", "The project name cannot be longer than 50 characters"),
        ["Название проекта может содержать только строчные буквы, цифры и подчёркивания"] = new("Назва проєкту може містити лише малі літери, цифри та підкреслення", "The project name may contain only lowercase letters, digits, and underscores"),
        ["Элемент одежды: {0}"] = new("Елемент одягу: {0}", "Clothing item: {0}"),
        ["{0} одинаковых элементов одежды"] = new("{0} однакових елементів одягу", "{0} identical clothing items"),
        ["Неизвестно"] = new("Невідомо", "Unknown"),
        ["Аддон {0}"] = new("Аддон {0}", "Addon {0}"),
        ["Дубликат (всего: {0})"] = new("Дублікат (усього: {0})", "Duplicate (total: {0})"),
        ["Дублирующийся элемент:"] = new("Дублюючий елемент:", "Duplicating item:"),
        ["(этот элемент)"] = new("(цей елемент)", "(this item)"),
        ["мужской"] = new("чоловічий", "male"),
        ["женский"] = new("жіночий", "female"),
        ["неизвестно"] = new("невідомо", "unknown"),
        ["Аддон {0}: {1}"] = new("Аддон {0}: {1}", "Addon {0}: {1}"),
        ["Аддон {0}: {1} → {2}"] = new("Аддон {0}: {1} → {2}", "Addon {0}: {1} → {2}"),
        ["Удалить «{0}»?"] = new("Видалити «{0}»?", "Delete '{0}'?"),
        ["Подтверждение удаления"] = new("Підтвердження видалення", "Delete confirmation"),
        ["Все дубликаты устранены!"] = new("Усі дублікати усунено!", "All duplicates removed!"),
        ["Будет удалено {0} дубликатов из этой группы, кроме первого элемента.\\n\\nПродолжить?"] = new("Буде видалено {0} дублікатів із цієї групи, крім першого елемента.\\n\\nПродовжити?", "{0} duplicates will be deleted from this group except the first item.\\n\\nContinue?"),
        ["Подтверждение массового удаления"] = new("Підтвердження масового видалення", "Bulk delete confirmation"),
        ["Удалено дубликатов: {0}.\\n\\nВсе дубликаты устранены!"] = new("Видалено дублікатів: {0}.\\n\\nУсі дублікати усунено!", "Duplicates deleted: {0}.\\n\\nAll duplicates removed!"),
        ["Удалено дубликатов: {0}!"] = new("Видалено дублікатів: {0}!", "Duplicates deleted: {0}!"),
        ["Обнаружены дубликаты одежды"] = new("Виявлено дублікати одягу", "Clothing duplicates found"),
        ["Следующие элементы одежды похожи на уже существующие. Выберите, какие из них добавить:"] = new("Наведені елементи одягу схожі на наявні. Виберіть, які з них додати:", "The following clothing items are similar to existing ones. Choose which to add:"),
        ["Будет добавлено дубликатов: {0} из {1}"] = new("Буде додано дублікатів: {0} із {1}", "Duplicates to add: {0} of {1}"),
        ["Совпадающих элементов не найдено"] = new("Співпадаючих елементів не знайдено", "No matching items found"),
        ["Дубликат: {0}"] = new("Дублікат: {0}", "Duplicate: {0}"),
        ["(ещё {0})"] = new("(ще {0})", "({0} more)"),
        ["Пропс"] = new("Аксесуар", "Accessory"),
        ["Компонент"] = new("Компонент", "Component"),

        // Project open, import, export and log messages
        ["Ошибка инициализации 3D-просмотра"] = new("Помилка ініціалізації 3D-перегляду", "3D preview initialization error"),
        ["Cleared data, moved to home screen"] = new("Дані очищено, перейдено на головну", "Cleared data, moved to Home"),
        ["Папка проектов настроена: {0}"] = new("Папку проєктів налаштовано: {0}", "Projects folder configured: {0}"),
        ["Не удалось открыть раздел «{0}»: {1}"] = new("Не вдалося відкрити розділ «{0}»: {1}", "Could not open section '{0}': {1}"),
        ["Ошибка навигации"] = new("Помилка навігації", "Navigation error"),
        ["Нет несохранённых изменений"] = new("Немає незбережених змін", "No unsaved changes"),
        ["Не удалось сохранить проект: {0}"] = new("Не вдалося зберегти проєкт: {0}", "Could not save the project: {0}"),
        ["Ошибка сохранения"] = new("Помилка збереження", "Save error"),
        ["Не удалось открыть набор одежды: {0}"] = new("Не вдалося відкрити набір одягу: {0}", "Could not open the clothing set: {0}"),
        ["Выберите файлы .meta одежды"] = new("Виберіть файли .meta одягу", "Select clothing .meta files"),
        ["Файлы .meta одежды (*.meta)|*.meta"] = new("Файли .meta одягу (*.meta)|*.meta", "Clothing .meta files (*.meta)|*.meta"),
        ["Файл пропущен: {0}, вероятно, это не .meta одежды"] = new("Файл пропущено: {0}, імовірно, це не .meta одягу", "File skipped: {0}; it is probably not a clothing .meta file"),
        ["Не удалось загрузить ни один набор одежды."] = new("Не вдалося завантажити жоден набір одягу.", "Could not load any clothing sets."),
        ["Ошибка загрузки набора одежды: {0}"] = new("Помилка завантаження набору одягу: {0}", "Clothing set loading error: {0}"),
        ["Не удалось загрузить набор одежды: {0}"] = new("Не вдалося завантажити набір одягу: {0}", "Could not load the clothing set: {0}"),
        ["Выберите .meta файлы для добавления"] = new("Виберіть .meta файли для додавання", "Select .meta files to add"),
        ["Meta files (*.meta)|*.meta"] = new("Файли .meta (*.meta)|*.meta", "Meta files (*.meta)|*.meta"),
        ["Файл пропущен: {0} не является .meta одежды"] = new("Файл пропущено: {0} не є .meta одягу", "File skipped: {0} is not a clothing .meta file"),
        ["Не выбрано подходящих .meta файлов."] = new("Не вибрано відповідних .meta файлів.", "No suitable .meta files selected."),
        ["Не удалось импортировать проект: {0}"] = new("Не вдалося імпортувати проєкт: {0}", "Could not import the project: {0}"),
        ["Ошибка импорта"] = new("Помилка імпорту", "Import error"),
        ["Импорт проекта KazanClothTool"] = new("Імпорт проєкту Kazan Cloth Tool", "Import Kazan Cloth Tool project"),
        ["Проект KazanClothTool (*.kctproject;*.gctproject)|*.kctproject;*.gctproject"] = new("Проєкт Kazan Cloth Tool (*.kctproject;*.gctproject)|*.kctproject;*.gctproject", "Kazan Cloth Tool project (*.kctproject;*.gctproject)|*.kctproject;*.gctproject"),
        ["В архиве проекта не найдены .meta файлы одежды."] = new("В архіві проєкту не знайдено .meta файлів одягу.", "No clothing .meta files were found in the project archive."),
        ["Не удалось загрузить одежду из архива."] = new("Не вдалося завантажити одяг з архіву.", "Could not load clothing from the archive."),
        ["Ошибка импорта: {0}"] = new("Помилка імпорту: {0}", "Import error: {0}"),
        ["Импорт проекта..."] = new("Імпорт проєкту...", "Importing project..."),
        ["Проект импортирован"] = new("Проєкт імпортовано", "Project imported"),
        ["Импорт не завершён"] = new("Імпорт не завершено", "Import incomplete"),
        ["Не удалось удалить временные файлы импорта: {0}"] = new("Не вдалося видалити тимчасові файли імпорту: {0}", "Could not remove temporary import files: {0}"),
        ["Откройте или создайте проект перед экспортом."] = new("Відкрийте або створіть проєкт перед експортом.", "Open or create a project before exporting."),
        ["Экспорт проекта KazanClothTool"] = new("Експорт проєкту Kazan Cloth Tool", "Export Kazan Cloth Tool project"),
        ["Проект KazanClothTool (*.kctproject)|*.kctproject"] = new("Проєкт Kazan Cloth Tool (*.kctproject)|*.kctproject", "Kazan Cloth Tool project (*.kctproject)|*.kctproject"),
        ["Экспорт проекта..."] = new("Експорт проєкту...", "Exporting project..."),
        ["Ошибка экспорта: {0}"] = new("Помилка експорту: {0}", "Export error: {0}"),
        ["Не удалось экспортировать проект: {0}"] = new("Не вдалося експортувати проєкт: {0}", "Could not export the project: {0}"),
        ["Проект экспортирован"] = new("Проєкт експортовано", "Project exported"),
        ["Экспорт не завершён"] = new("Експорт не завершено", "Export incomplete"),
        ["Не удалось удалить временные файлы экспорта: {0}"] = new("Не вдалося видалити тимчасові файли експорту: {0}", "Could not remove temporary export files: {0}"),
        ["Начато сохранение проекта..."] = new("Розпочато збереження проєкту...", "Saving project started..."),
        ["Проект сохранён за {0} мс"] = new("Проєкт збережено за {0} мс", "Project saved in {0} ms"),
        ["Критическая ошибка сохранения: {0}"] = new("Критична помилка збереження: {0}", "Critical save error: {0}"),
        ["Есть несохранённые изменения. Продолжить без сохранения?"] = new("Є незбережені зміни. Продовжити без збереження?", "There are unsaved changes. Continue without saving?"),
        ["Несохранённые изменения"] = new("Незбережені зміни", "Unsaved changes"),
        ["Сканирование проекта на дубликаты одежды..."] = new("Сканування проєкту на дублікати одягу...", "Scanning the project for duplicate clothing..."),
        ["Проект загружен: {0}"] = new("Проєкт завантажено: {0}", "Project loaded: {0}"),
        ["Папка проектов или название проекта не настроены."] = new("Папку проєктів або назву проєкту не налаштовано.", "The projects folder or project name is not configured."),
        ["Исходный файл не найден."] = new("Вихідний файл не знайдено.", "Source file not found."),
        ["Начато экспортирование текстур"] = new("Розпочато експорт текстур", "Texture export started"),
        ["Начато экспортирование одежды"] = new("Розпочато експорт одягу", "Clothing export started"),
        ["Без группы"] = new("Без групи", "No group"),
        ["НЕТ"] = new("НІ", "NO"),
        [" ({0} выбрано)"] = new(" ({0} вибрано)", " ({0} selected)"),
        ["Автосохранение через {0} с"] = new("Автозбереження через {0} с", "Autosave in {0} s"),
        ["Сохранение"] = new("Збереження", "Saving"),
        ["Переименовать"] = new("Перейменувати", "Rename"),
        ["Текстура «{0}» перемещена с позиции {1} на позицию {2}"] = new("Текстуру «{0}» переміщено з позиції {1} на позицію {2}", "Texture '{0}' moved from position {1} to position {2}"),
        ["Выбрать текстуры"] = new("Вибрати текстури", "Select textures"),
        ["Drawable YDD (*.ydd)|*.ydd"] = new("YDD-файли одягу (*.ydd)|*.ydd", "Drawable YDD files (*.ydd)|*.ydd"),
        ["Добавлено файлов: {0}"] = new("Додано файлів: {0}", "Files added: {0}"),
        ["Удалить выбранный элемент одежды «{0}»?"] = new("Видалити вибраний елемент одягу «{0}»?", "Delete selected clothing item '{0}'?"),
        ["Удалить выбранные элементы одежды ({0})?"] = new("Видалити вибрані елементи одягу ({0})?", "Delete selected clothing items ({0})?"),
        ["Нумерация последующих элементов изменится."] = new("Нумерація наступних елементів зміниться.", "The numbering of subsequent items will change."),
        ["Заменить их зарезервированными слотами вместо удаления?"] = new("Замінити їх зарезервованими слотами замість видалення?", "Replace them with reserved slots instead of deleting?"),
        ["Проект KazanClothTool (*.json)|*.json|Все файлы (*.*)|*.*"] = new("Проєкт Kazan Cloth Tool (*.json)|*.json|Усі файли (*.*)|*.*", "Kazan Cloth Tool project (*.json)|*.json|All files (*.*)|*.*"),
        ["Создан новый проект: {0}"] = new("Створено новий проєкт: {0}", "New project created: {0}"),
        ["Не удалось создать проект: {0}"] = new("Не вдалося створити проєкт: {0}", "Could not create the project: {0}"),
        ["Не удалось открыть проект: {0}"] = new("Не вдалося відкрити проєкт: {0}", "Could not open the project: {0}"),
        ["Проект с названием \"{0}\" уже существует. Если продолжить, он будет перезаписан."] = new("Проєкт із назвою \"{0}\" уже існує. Якщо продовжити, його буде перезаписано.", "A project named '{0}' already exists. If you continue, it will be overwritten."),
        ["Обнаружено элементов одежды: {0}"] = new("Виявлено елементів одягу: {0}", "Clothing items found: {0}"),
        ["Обнаружено элементов одежды: {0}. Файлов .meta: {1}"] = new("Виявлено елементів одягу: {0}. Файлів .meta: {1}", "Clothing items found: {0}. .meta files: {1}"),
        ["Проект с названием \"{0}\" уже существует. Выберите другое название — существующие данные не будут удалены."] = new("Проєкт із назвою \"{0}\" уже існує. Виберіть іншу назву — наявні дані не буде видалено.", "A project named '{0}' already exists. Choose another name; existing data will not be deleted."),
        ["Не удалось загрузить данные аккаунта"] = new("Не вдалося завантажити дані облікового запису", "Could not load account data"),
        ["Выберите папку"] = new("Виберіть папку", "Choose a folder"),
        ["Выберите файл"] = new("Виберіть файл", "Choose a file"),
        ["Открыть список"] = new("Відкрити список", "Open list"),
        ["Выберите вариант из списка"] = new("Виберіть варіант зі списку", "Choose an option from the list"),
        ["Удалить выбранную текстуру из проекта?"] = new("Видалити вибрану текстуру з проєкту?", "Remove the selected texture from the project?"),
        ["Удалить выбранные текстуры ({0}) из проекта?"] = new("Видалити вибрані текстури ({0}) з проєкту?", "Remove the selected textures ({0}) from the project?"),
        ["Удалить выбранный элемент одежды?"] = new("Видалити вибраний елемент одягу?", "Remove the selected clothing item?"),
        ["Файл физики одежды"] = new("Файл фізики одягу", "Clothing physics file"),
        ["Свернуть в окно"] = new("Згорнути у вікно", "Restore down"),
        ["Вернуться на главную"] = new("Повернутися на головну", "Back to Home"),
        ["НЕ АКТИВЕН"] = new("НЕ АКТИВНИЙ", "INACTIVE"),
        ["АКТИВЕН"] = new("АКТИВНИЙ", "ACTIVE"),
        ["Открыть журнал"] = new("Відкрити журнал", "Open log"),
        ["Увеличить"] = new("Збільшити", "Increase"),
        ["Уменьшить"] = new("Зменшити", "Decrease"),
        ["Зашифрованная одежда не может быть показана в 3D-просмотре"] = new("Зашифрований одяг не може бути показаний у 3D-перегляді", "Encrypted clothing cannot be shown in 3D preview"),

        // Localized technical and status messages
        ["Пропущена повреждённая текстура: {0}"] = new("Пропущено пошкоджену текстуру: {0}", "Skipped damaged texture: {0}"),
        ["Пропущена повреждённая встроенная текстура: {0} в элементе одежды {1}"] = new("Пропущено пошкоджену вбудовану текстуру: {0} в елементі одягу {1}", "Skipped damaged embedded texture: {0} in clothing item {1}"),
        ["Исходная текстура «{0}» не найдена в TextureDictionary для элемента одежды {1}. Пропуск."] = new("Вихідну текстуру «{0}» не знайдено в TextureDictionary для елемента одягу {1}. Пропуск.", "Source texture '{0}' was not found in TextureDictionary for clothing item {1}. Skipping."),
        ["Удалён существующий каталог вывода сборки: {0}"] = new("Видалено наявний каталог виводу складання: {0}", "Removed existing build output directory: {0}"),
        ["Не удалось удалить каталог вывода сборки: {0}"] = new("Не вдалося видалити каталог виводу складання: {0}", "Could not remove build output directory: {0}"),
        ["Удалён временный каталог сборки: {0}"] = new("Видалено тимчасовий каталог складання: {0}", "Removed temporary build directory: {0}"),
        ["Не удалось очистить временный каталог: {0}"] = new("Не вдалося очистити тимчасовий каталог: {0}", "Could not clean temporary directory: {0}"),
        ["Не удалось извлечь файл {0} ({1}): {2}"] = new("Не вдалося витягти файл {0} ({1}): {2}", "Could not extract file {0} ({1}): {2}"),
        ["Ошибка извлечения файла {0}: {1}"] = new("Помилка витягнення файлу {0}: {1}", "File extraction error {0}: {1}"),
        ["Ошибка в ExtractVirtualFilesAsync: {0}"] = new("Помилка в ExtractVirtualFilesAsync: {0}", "Error in ExtractVirtualFilesAsync: {0}"),
        ["Ошибка проверки FileGroupDescriptor: {0}"] = new("Помилка перевірки FileGroupDescriptor: {0}", "FileGroupDescriptor validation error: {0}"),
        ["Путь GTA V не настроен. Выберите его в настройках, чтобы включить 3D-просмотр."] = new("Шлях GTA V не налаштовано. Виберіть його в налаштуваннях, щоб увімкнути 3D-перегляд.", "The GTA V path is not configured. Select it in Settings to enable 3D preview."),
        ["Не удалось инициализировать 3D-просмотр: {0}"] = new("Не вдалося ініціалізувати 3D-перегляд: {0}", "Could not initialize 3D preview: {0}"),
        ["Не удалось инициализировать 3D-просмотр"] = new("Не вдалося ініціалізувати 3D-перегляд", "Could not initialize 3D preview"),
        ["Не удалось инициализировать 3D-просмотр в фоне: {0}"] = new("Не вдалося ініціалізувати 3D-перегляд у фоні: {0}", "Could not initialize 3D preview in the background: {0}"),
        ["3D-предпросмотр недоступен: проверьте установку GTA V и журнал"] = new("3D-перегляд недоступний: перевірте встановлення GTA V і журнал", "3D preview unavailable: check the GTA V installation and log"),
        ["3D-предпросмотр недоступен: проверьте DirectX и видеодрайвер"] = new("3D-перегляд недоступний: перевірте DirectX і відеодрайвер", "3D preview unavailable: check DirectX and the graphics driver"),
        ["Ошибка закрытия 3D-предпросмотра: {0}"] = new("Помилка закриття 3D-перегляду: {0}", "Error closing 3D preview: {0}"),
        ["3D-предпросмотр закрыт. Откройте его снова, когда он понадобится."] = new("3D-перегляд закрито. Відкрийте його знову, коли він знадобиться.", "The 3D preview was closed. Open it again when needed."),
        ["3D-просмотр отключён из-за ошибок. Подробности в журнале"] = new("3D-перегляд вимкнено через помилки. Подробиці у журналі", "3D preview disabled due to errors. See the log for details"),
        ["Отсутствует модель LOD."] = new("Відсутня модель LOD.", "LOD model is missing."),
        ["Количество полигонов ({0}) превышает предел ({1})."] = new("Кількість полігонів ({0}) перевищує ліміт ({1}).", "Polygon count ({0}) exceeds the limit ({1})."),
        ["Отсутствует текстура: {0}."] = new("Відсутня текстура: {0}.", "Texture is missing: {0}."),
        ["У элемента одежды нет текстур."] = new("В елемента одягу немає текстур.", "The clothing item has no textures."),
        ["Разрешение текстуры: {0}x{1}. Оно превышает установленный предел ({2}). Оптимизируйте текстуру, чтобы уменьшить размер."] = new("Роздільність текстури: {0}x{1}. Вона перевищує встановлений ліміт ({2}). Оптимізуйте текстуру, щоб зменшити розмір.", "Texture resolution: {0}x{1}. It exceeds the configured limit ({2}). Optimize the texture to reduce its size."),
        ["Размер текстуры недействителен."] = new("Розмір текстури недійсний.", "Texture size is invalid."),
        ["Ширина или высота текстуры не является степенью двойки."] = new("Ширина або висота текстури не є степеню двійки.", "Texture width or height is not a power of two."),
        ["Текстура содержит {0} mip-уровней, а требуется {1}."] = new("Текстура містить {0} mip-рівнів, а потрібно {1}.", "Texture contains {0} mip levels, but {1} are required."),
        ["У одного элемента одежды не может быть больше {0} текстур!"] = new("В одного елемента одягу не може бути більше {0} текстур!", "A clothing item cannot have more than {0} textures!"),
        ["Достигнут лимит в {0} текстур. Последняя добавленная текстура: {1}."] = new("Досягнуто ліміт у {0} текстур. Остання додана текстура: {1}.", "Texture limit of {0} reached. Last added texture: {1}."),
        ["Не удалось добавить текстуры"] = new("Не вдалося додати текстури", "Could not add textures"),
        ["Не удалось заменить текстуру: {0}"] = new("Не вдалося замінити текстуру: {0}", "Could not replace the texture: {0}"),
        ["Оптимизация текстуры «{0}» отменена"] = new("Оптимізацію текстури «{0}» скасовано", "Texture optimization for '{0}' canceled"),
        ["Встроенная текстура ({0}) заменена, оптимизация сброшена"] = new("Вбудовану текстуру ({0}) замінено, оптимізацію скинуто", "Embedded texture ({0}) replaced; optimization reset"),
        ["Не удалось заменить встроенную текстуру: {0}"] = new("Не вдалося замінити вбудовану текстуру: {0}", "Could not replace the embedded texture: {0}"),
        ["Не удалось показать предпросмотр текстуры: {0}"] = new("Не вдалося показати перегляд текстури: {0}", "Could not show texture preview: {0}"),
        ["Не удалось показать предпросмотр встроенной текстуры: {0}"] = new("Не вдалося показати перегляд вбудованої текстури: {0}", "Could not show embedded texture preview: {0}"),
        ["Не удалось сохранить встроенную текстуру «{0}»: {1}"] = new("Не вдалося зберегти вбудовану текстуру «{0}»: {1}", "Could not save embedded texture '{0}': {1}"),
        ["Не удалось восстановить встроенную текстуру «{0}»: {1}"] = new("Не вдалося відновити вбудовану текстуру «{0}»: {1}", "Could not restore embedded texture '{0}': {1}"),
        ["Путь встроенной текстуры находится за пределами проекта."] = new("Шлях вбудованої текстури знаходиться за межами проєкту.", "The embedded texture path is outside the project."),
        ["Не удалось создать миниатюру встроенной текстуры для {0}"] = new("Не вдалося створити мініатюру вбудованої текстури для {0}", "Could not create an embedded texture thumbnail for {0}"),
        ["Не удалось загрузить сведения о текстуре «{0}»: {1}"] = new("Не вдалося завантажити відомості про текстуру «{0}»: {1}", "Could not load texture details for '{0}': {1}"),
        ["Не удалось найти текстуру «{0}»: {1}"] = new("Не вдалося знайти текстуру «{0}»: {1}", "Could not find texture '{0}': {1}"),
        ["Не удалось создать миниатюру текстуры «{0}»"] = new("Не вдалося створити мініатюру текстури «{0}»", "Could not create thumbnail for texture '{0}'"),
        ["Не удалось загрузить текстуру «{0}»: {1}"] = new("Не вдалося завантажити текстуру «{0}»: {1}", "Could not load texture '{0}': {1}"),
        ["Не удалось загрузить текстуру {0}: {1}"] = new("Не вдалося завантажити текстуру {0}: {1}", "Could not load texture {0}: {1}"),
        ["Не удалось найти файл одежды «{0}»: {1}"] = new("Не вдалося знайти файл одягу «{0}»: {1}", "Could not find clothing file '{0}': {1}"),
        ["Не удалось загрузить сведения об элементе одежды {0}: {1}"] = new("Не вдалося завантажити відомості про елемент одягу {0}: {1}", "Could not load clothing item details {0}: {1}"),
        ["Не удалось загрузить сведения об элементе одежды «{0}»: {1}"] = new("Не вдалося завантажити відомості про елемент одягу «{0}»: {1}", "Could not load clothing item details '{0}': {1}"),
        ["Не удалось загрузить файл одежды «{0}»: {1}"] = new("Не вдалося завантажити файл одягу «{0}»: {1}", "Could not load clothing file '{0}': {1}"),
        ["Не удалось сохранить текстуру: {0}. Ошибка: файл уже существует."] = new("Не вдалося зберегти текстуру: {0}. Помилка: файл уже існує.", "Could not save texture: {0}. Error: the file already exists."),
        ["Не удалось сохранить текстуру: {0}. Ошибка: {1}."] = new("Не вдалося зберегти текстуру: {0}. Помилка: {1}.", "Could not save texture: {0}. Error: {1}."),
        ["Не удалось сохранить элемент одежды: {0}. Ошибка: файл уже существует."] = new("Не вдалося зберегти елемент одягу: {0}. Помилка: файл уже існує.", "Could not save clothing item: {0}. Error: the file already exists."),
        ["Не удалось сохранить элемент одежды: {0}. Ошибка: {1}."] = new("Не вдалося зберегти елемент одягу: {0}. Помилка: {1}.", "Could not save clothing item: {0}. Error: {1}."),
        ["Некоторые текстуры имеют предупреждения. Проверьте сведения о текстурах."] = new("Деякі текстури мають попередження. Перевірте відомості про текстури.", "Some textures have warnings. Check the texture details."),
        ["высокий"] = new("високий", "high"),
        ["средний"] = new("середній", "medium"),
        ["низкий"] = new("низький", "low"),
        ["отражённая"] = new("відображена", "reflective"),
        ["нормали"] = new("нормалі", "normals")
    };

    private static readonly Dictionary<Type, DependencyProperty[]> StringDependencyProperties = new();

    private static readonly Dictionary<string, Regex> TemplateRegexCache = new(StringComparer.Ordinal);

    private static readonly Dictionary<string, string> UkrainianToRussian =
        BuildReverseCatalog(false);

    private static readonly Dictionary<string, string> EnglishToRussian =
        BuildReverseCatalog(true);

    private static Dictionary<string, string> BuildReverseCatalog(bool english)
    {
        var reverse = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string source, Translation translation) in Catalog.Concat(SupplementalCatalog))
        {
            string translated = english ? translation.English : translation.Ukrainian;
            reverse.TryAdd(translated, source);
        }
        return reverse;
    }

    public static IReadOnlyList<LanguageOption> AvailableLanguages { get; } =
    [
        new(Russian, "Русский"),
        new(Ukrainian, "Українська"),
        new(English, "English")
    ];

    public static string CurrentLanguage { get; private set; } = Russian;

    public static event EventHandler? LanguageChanged;

    public static LanguageOption GetLanguage(string? code)
    {
        string normalized = Normalize(code);
        return AvailableLanguages.First(language => language.Code == normalized);
    }

    public static string Normalize(string? code)
    {
        return AvailableLanguages.Any(language => string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase))
            ? AvailableLanguages.First(language => string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase)).Code
            : Russian;
    }

    public static void SetLanguage(string? code, bool save = true)
    {
        string normalized = Normalize(code);
        if (save)
        {
            PersistentSettingsHelper.Instance.Language = normalized;
        }

        CurrentLanguage = normalized;
        LanguageChanged?.Invoke(null, EventArgs.Empty);
        ApplyToOpenWindows();
        Application.Current?.Dispatcher.BeginInvoke(
            new Action(ApplyToOpenWindows),
            DispatcherPriority.Background);
    }

    private static bool TryGetTranslation(string source, out Translation translation)
    {
        return Catalog.TryGetValue(source, out translation)
            || SupplementalCatalog.TryGetValue(source, out translation);
    }

    public static string Translate(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return string.Empty;
        }

        string canonicalSource = source;
        if (!TryGetTranslation(canonicalSource, out _))
        {
            Dictionary<string, string>? reverse = CurrentLanguage == Ukrainian
                ? UkrainianToRussian
                : CurrentLanguage == English ? EnglishToRussian : null;
            if (reverse != null && reverse.TryGetValue(source, out string? russianSource))
            {
                canonicalSource = russianSource;
            }
            else if (CurrentLanguage == Russian)
            {
                if (!EnglishToRussian.TryGetValue(source, out string? englishSource))
                {
                    UkrainianToRussian.TryGetValue(source, out englishSource);
                }
                if (englishSource != null)
                {
                    canonicalSource = englishSource;
                }
            }
        }

        if (CurrentLanguage != Russian && TryTranslateTemplate(canonicalSource, out string? templateResult))
        {
            return templateResult;
        }

        if (CurrentLanguage == Russian || !TryGetTranslation(canonicalSource, out Translation translation))
        {
            return canonicalSource;
        }

        return CurrentLanguage == Ukrainian ? translation.Ukrainian : translation.English;
    }

    private static bool TryTranslateTemplate(string source, out string? result)
    {
        foreach ((string template, Translation translation) in Catalog.Concat(SupplementalCatalog))
        {
            if (!template.Contains('{') || !template.Contains('}'))
            {
                continue;
            }

            Regex regex;
            if (!TemplateRegexCache.TryGetValue(template, out regex!))
            {
                regex = new Regex(BuildTemplatePattern(template), RegexOptions.CultureInvariant | RegexOptions.Compiled);
                TemplateRegexCache[template] = regex;
            }

            Match match = regex.Match(source);
            if (!match.Success)
            {
                continue;
            }

            object[] values = match.Groups.Cast<Group>()
                .Skip(1)
                .Select(group => (object)group.Value)
                .ToArray();

            try
            {
                string translatedTemplate = CurrentLanguage == Ukrainian
                    ? translation.Ukrainian
                    : translation.English;
                result = string.Format(System.Globalization.CultureInfo.CurrentCulture, translatedTemplate, values);
                return true;
            }
            catch (FormatException)
            {
                // A malformed or mismatched template should not break the UI.
            }
        }

        result = null;
        return false;
    }

    private static string BuildTemplatePattern(string template)
    {
        var pattern = new StringBuilder("^");
        for (int index = 0; index < template.Length; index++)
        {
            if (template[index] == '{')
            {
                int closingBracket = template.IndexOf('}', index);
                if (closingBracket > index)
                {
                    string placeholder = template.Substring(index + 1, closingBracket - index - 1);
                    int separator = placeholder.IndexOf(':');
                    string indexPart = separator >= 0 ? placeholder[..separator] : placeholder;
                    if (int.TryParse(indexPart, out _))
                    {
                        pattern.Append("(.*?)");
                        index = closingBracket;
                        continue;
                    }
                }
            }

            pattern.Append(Regex.Escape(template[index].ToString()));
        }

        return pattern.Append('$').ToString();
    }

    public static string Translate(string source, params object[] args)
    {
        return string.Format(System.Globalization.CultureInfo.CurrentCulture, Translate(source), args);
    }

    public static string Format(string source, params object[] args)
    {
        return Translate(source, args);
    }

    public static string T(string russian, string ukrainian, string english)
    {
        return CurrentLanguage switch
        {
            Ukrainian => ukrainian,
            English => english,
            _ => russian
        };
    }

    public static void ApplyToOpenWindows()
    {
        if (Application.Current == null)
        {
            return;
        }

        if (!Application.Current.Dispatcher.CheckAccess())
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(ApplyToOpenWindows));
            return;
        }

        foreach (Window window in Application.Current.Windows.OfType<Window>().ToArray())
        {
            ApplyTo(window);
        }
    }

    public static void ApplyTo(DependencyObject root)
    {
        ApplyTo(root, new HashSet<DependencyObject>());
    }

    private static void ApplyTo(DependencyObject root, HashSet<DependencyObject> visited)
    {
        if (root == null || !visited.Add(root))
        {
            return;
        }

        try
        {
            ApplyElement(root);
        }
        catch (Exception ex)
        {
            // Localization must never make a screen unusable because a third-party
            // control exposes a read-only or otherwise unusual dependency property.
            System.Diagnostics.Debug.WriteLine($"Localization skipped for {root.GetType().Name}: {ex}");
        }

        if (root is FrameworkElement owner)
        {
            ContextMenu? contextMenu = ContextMenuService.GetContextMenu(owner);
            if (contextMenu != null)
            {
                ApplyTo(contextMenu, visited);
            }

            if (owner.ToolTip is DependencyObject toolTip && owner.ToolTip is not string)
            {
                ApplyTo(toolTip, visited);
            }
        }

        DependencyObject[] children;
        try
        {
            children = EnumerateChildren(root).ToArray();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unable to enumerate localized children for {root.GetType().Name}: {ex}");
            return;
        }

        foreach (DependencyObject child in children)
        {
            ApplyTo(child, visited);
        }
    }

    private static IEnumerable<DependencyObject> EnumerateChildren(DependencyObject parent)
    {
        // Take a snapshot before translating anything. Some WPF controls rebuild
        // their visual/logical children when a property is localized; iterating
        // the live tree lazily can then throw "Collection was modified".
        var children = new List<DependencyObject>();

        if (parent is Popup popup && popup.Child != null)
        {
            children.Add(popup.Child);
        }

        if (parent is Visual visual)
        {
            int count = VisualTreeHelper.GetChildrenCount(visual);
            for (int index = 0; index < count; index++)
            {
                if (VisualTreeHelper.GetChild(visual, index) is DependencyObject child)
                {
                    children.Add(child);
                }
            }
        }

        if (parent is FrameworkElement or FrameworkContentElement)
        {
            foreach (object logicalChild in LogicalTreeHelper.GetChildren(parent))
            {
                if (logicalChild is DependencyObject dependencyObject)
                {
                    children.Add(dependencyObject);
                }
            }
        }

        return children;
    }

    private static void ApplyElement(DependencyObject element)
    {
        switch (element)
        {
            case Window window:
                window.Title = TranslateAndStore(window, window.Title, "Title");
                break;
            case TextBlock textBlock when textBlock.Inlines.Count == 0 && BindingOperations.GetBindingExpression(textBlock, TextBlock.TextProperty) == null:
                textBlock.Text = TranslateAndStore(textBlock, textBlock.Text, "Text");
                break;
            case TextBlock textBlock when BindingOperations.GetBindingExpression(textBlock, TextBlock.TextProperty) == null:
                foreach (Run run in textBlock.Inlines.OfType<Run>().ToArray())
                {
                    run.Text = TranslateAndStore(run, run.Text, "Text");
                }
                break;
            case ContentPresenter contentPresenter when contentPresenter.Content is string presenterContent
                     && BindingOperations.GetBindingExpression(contentPresenter, ContentPresenter.ContentProperty) == null:
                contentPresenter.Content = TranslateAndStore(contentPresenter, presenterContent, "Content");
                break;
            case Expander expander when expander.Header is string expanderHeader
                     && BindingOperations.GetBindingExpression(expander, Expander.HeaderProperty) == null:
                expander.Header = TranslateAndStore(expander, expanderHeader, "Header");
                break;
            case ContentControl contentControl when contentControl.Content is string content
                     && BindingOperations.GetBindingExpression(contentControl, ContentControl.ContentProperty) == null:
                contentControl.Content = TranslateAndStore(contentControl, content, "Content");
                break;
        }

        if (element is GroupBox groupBox && groupBox.Header is string groupHeader
            && BindingOperations.GetBindingExpression(groupBox, GroupBox.HeaderProperty) == null)
        {
            groupBox.Header = TranslateAndStore(groupBox, groupHeader, "Header");
        }
        else if (element is TabItem tabItem && tabItem.Header is string tabHeader
                 && BindingOperations.GetBindingExpression(tabItem, TabItem.HeaderProperty) == null)
        {
            tabItem.Header = TranslateAndStore(tabItem, tabHeader, "Header");
        }
        else if (element is MenuItem menuItem && menuItem.Header is string menuHeader
                 && BindingOperations.GetBindingExpression(menuItem, MenuItem.HeaderProperty) == null)
        {
            menuItem.Header = TranslateAndStore(menuItem, menuHeader, "Header");
        }

        if (element is FrameworkElement frameworkElement)
        {
            if (frameworkElement.ToolTip is string toolTip
                && BindingOperations.GetBindingExpression(frameworkElement, FrameworkElement.ToolTipProperty) == null)
            {
                frameworkElement.ToolTip = TranslateAndStore(frameworkElement, toolTip, "ToolTip");
            }

            string automationName = AutomationProperties.GetName(frameworkElement);
            if (!string.IsNullOrWhiteSpace(automationName))
            {
                string translatedAutomationName = TranslateAndStore(frameworkElement, automationName, "AutomationName");
                if (!string.Equals(automationName, translatedAutomationName, StringComparison.Ordinal))
                {
                    AutomationProperties.SetName(frameworkElement, translatedAutomationName);
                }
            }
        }

        ApplyStringDependencyProperties(element);
    }

    private static void ApplyStringDependencyProperties(DependencyObject element)
    {
        foreach (DependencyProperty property in GetStringDependencyProperties(element.GetType()))
        {
            // ComboBox.Text is part of selection coercion: writing a translated
            // display value can silently select another item (for example, reset
            // the language selector to English). The selected item is data-bound.
            if (property == TextBlock.TextProperty ||
                property == TextBox.TextProperty ||
                property == ComboBox.TextProperty ||
                property.ReadOnly)
            {
                continue;
            }

            if (BindingOperations.GetBindingExpression(element, property) != null)
            {
                continue;
            }

            if (element.GetValue(property) is not string current || string.IsNullOrEmpty(current))
            {
                continue;
            }

            string translated = TranslateAndStore(element, current, $"DP:{property.Name}");
            if (!string.Equals(current, translated, StringComparison.Ordinal))
            {
                try
                {
                    element.SetValue(property, translated);
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentException)
                {
                    System.Diagnostics.Debug.WriteLine($"Localization skipped property {property.Name} on {element.GetType().Name}: {ex.Message}");
                }
            }
        }
    }

    private static DependencyProperty[] GetStringDependencyProperties(Type type)
    {
        if (StringDependencyProperties.TryGetValue(type, out DependencyProperty[]? cached))
        {
            return cached;
        }

        var properties = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.FieldType == typeof(DependencyProperty))
            .Select(field => (DependencyProperty?)field.GetValue(null))
            .Where(property => property?.PropertyType == typeof(string))
            .Cast<DependencyProperty>()
            .ToArray();

        StringDependencyProperties[type] = properties;
        return properties;
    }

    private static string TranslateAndStore(DependencyObject owner, string current, string propertyKey)
    {
        string source = GetOriginalString(owner, propertyKey) ?? current;
        SetOriginalString(owner, propertyKey, source);
        return Translate(source);
    }

    private static string? GetOriginalString(DependencyObject element, string propertyKey)
    {
        var values = (Dictionary<string, string>?)element.GetValue(OriginalStringsProperty);
        return values != null && values.TryGetValue(propertyKey, out string? value) ? value : null;
    }

    private static void SetOriginalString(DependencyObject element, string propertyKey, string value)
    {
        var values = (Dictionary<string, string>?)element.GetValue(OriginalStringsProperty);
        if (values == null)
        {
            values = new Dictionary<string, string>(StringComparer.Ordinal);
            element.SetValue(OriginalStringsProperty, values);
        }

        values[propertyKey] = value;
    }
}
