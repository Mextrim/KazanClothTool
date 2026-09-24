using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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

    private static readonly DependencyProperty OriginalStringProperty =
        DependencyProperty.RegisterAttached(
            "OriginalString",
            typeof(string),
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
        ["Новый проект"] = new("Новий проєкт", "New project"),
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
        ["Лимит полигонов для низкого уровня детализации."] = new("Ліміт полігонів для низького рівня деталізації.", "Polygon limit for the lowest detail level."),
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

    private static readonly Dictionary<string, string> UkrainianToRussian =
        BuildReverseCatalog(false);

    private static readonly Dictionary<string, string> EnglishToRussian =
        BuildReverseCatalog(true);

    private static Dictionary<string, string> BuildReverseCatalog(bool english)
    {
        var reverse = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach ((string source, Translation translation) in Catalog)
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

    public static string Translate(string source)
    {
        string canonicalSource = source;
        if (!Catalog.ContainsKey(canonicalSource))
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

        if (CurrentLanguage == Russian || !Catalog.TryGetValue(canonicalSource, out Translation translation))
        {
            return canonicalSource;
        }

        return CurrentLanguage == Ukrainian ? translation.Ukrainian : translation.English;
    }

    public static string Translate(string source, params object[] args)
    {
        return string.Format(System.Globalization.CultureInfo.CurrentCulture, Translate(source), args);
    }

    public static void ApplyToOpenWindows()
    {
        if (Application.Current == null)
        {
            return;
        }

        foreach (Window window in Application.Current.Windows)
        {
            ApplyTo(window);
        }
    }

    public static void ApplyTo(DependencyObject root)
    {
        if (root == null)
        {
            return;
        }

        ApplyElement(root);
        foreach (DependencyObject child in EnumerateChildren(root))
        {
            ApplyTo(child);
        }
    }

    private static IEnumerable<DependencyObject> EnumerateChildren(DependencyObject parent)
    {
        if (parent is not Visual visual)
        {
            yield break;
        }

        int count = VisualTreeHelper.GetChildrenCount(visual);
        for (int index = 0; index < count; index++)
        {
            if (VisualTreeHelper.GetChild(visual, index) is DependencyObject child)
            {
                yield return child;
            }
        }
    }

    private static void ApplyElement(DependencyObject element)
    {
        switch (element)
        {
            case Window window:
                window.Title = TranslateAndStore(window, window.Title);
                break;
            case TextBlock textBlock when textBlock.Inlines.Count == 0 && BindingOperations.GetBindingExpression(textBlock, TextBlock.TextProperty) == null:
                textBlock.Text = TranslateAndStore(textBlock, textBlock.Text);
                break;
            case TextBlock textBlock when BindingOperations.GetBindingExpression(textBlock, TextBlock.TextProperty) == null:
                foreach (Run run in textBlock.Inlines.OfType<Run>().ToArray())
                {
                    run.Text = TranslateAndStore(run, run.Text);
                }
                break;
            case ContentControl contentControl when contentControl.Content is string content:
                contentControl.Content = TranslateAndStore(contentControl, content);
                break;
            case GroupBox groupBox when groupBox.Header is string groupHeader:
                groupBox.Header = TranslateAndStore(groupBox, groupHeader);
                break;
            case TabItem tabItem when tabItem.Header is string tabHeader:
                tabItem.Header = TranslateAndStore(tabItem, tabHeader);
                break;
            case MenuItem menuItem when menuItem.Header is string menuHeader:
                menuItem.Header = TranslateAndStore(menuItem, menuHeader);
                break;
        }

        if (element is FrameworkElement frameworkElement && frameworkElement.ToolTip is string toolTip)
        {
            frameworkElement.ToolTip = TranslateAndStore(frameworkElement, toolTip);
        }
    }

    private static string TranslateAndStore(DependencyObject owner, string current)
    {
        string source = GetOriginalString(owner) ?? current;
        SetOriginalString(owner, source);
        return Translate(source);
    }

    private static string? GetOriginalString(DependencyObject element)
    {
        return (string?)element.GetValue(OriginalStringProperty);
    }

    private static void SetOriginalString(DependencyObject element, string value)
    {
        element.SetValue(OriginalStringProperty, value);
    }
}
