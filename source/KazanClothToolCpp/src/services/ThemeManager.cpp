#include "ThemeManager.h"

#include "SettingsStore.h"

#include <QApplication>
#include <QColor>
#include <QFile>

namespace
{
constexpr auto ThemeNames[] = {
    "Dark",
    "Light",
    "Ocean",
    "Sunset",
    "Forest",
    "Neon",
    "Aurora",
    "Ruby",
    "Cobalt",
    "Sand",
    "Rose",
    "Mono",
};
}

struct ThemeManager::Palette
{
    QString background;
    QString surface;
    QString surfaceAlt;
    QString text;
    QString muted;
    QString border;
    QString accent;
    QString accentHover;
    QString accentText;
    QString danger;
    QString success;
};

ThemeManager::ThemeManager(SettingsStore& settings, QObject* parent)
    : QObject(parent)
    , m_settings(settings)
{
    connect(&m_settings, &SettingsStore::themeChanged,
            this, &ThemeManager::themeChanged);
}

QStringList ThemeManager::availableThemes() const
{
    QStringList themes;
    for (const auto& name : ThemeNames) {
        themes.append(QString::fromLatin1(name));
    }
    return themes;
}

QString ThemeManager::canonicalThemeName(const QString& theme)
{
    for (const auto& name : ThemeNames) {
        const QString candidate = QString::fromLatin1(name);
        if (theme.compare(candidate, Qt::CaseInsensitive) == 0) {
            return candidate;
        }
    }
    return QStringLiteral("Dark");
}

ThemeManager::Palette ThemeManager::paletteFor(const QString& theme)
{
    const QString name = canonicalThemeName(theme);
    Palette palette;

    if (name == QLatin1String("Light")) {
        palette = {
            QStringLiteral("#f4f7fb"),
            QStringLiteral("#ffffff"),
            QStringLiteral("#edf2f8"),
            QStringLiteral("#172033"),
            QStringLiteral("#64748b"),
            QStringLiteral("#d7e0ec"),
            QStringLiteral("#635bdf"),
            QStringLiteral("#5148cc"),
            QStringLiteral("#ffffff"),
            QStringLiteral("#d9485f"),
            QStringLiteral("#168a61"),
        };
    } else if (name == QLatin1String("Ocean")) {
        palette = {
            QStringLiteral("#071c2c"),
            QStringLiteral("#0d2b40"),
            QStringLiteral("#123b55"),
            QStringLiteral("#e5f7ff"),
            QStringLiteral("#8db5c9"),
            QStringLiteral("#20516d"),
            QStringLiteral("#16b8d4"),
            QStringLiteral("#35d5e8"),
            QStringLiteral("#062330"),
            QStringLiteral("#f06c8d"),
            QStringLiteral("#43d6a2"),
        };
    } else if (name == QLatin1String("Sunset")) {
        palette = {
            QStringLiteral("#241522"),
            QStringLiteral("#38202f"),
            QStringLiteral("#4a2a3c"),
            QStringLiteral("#fff4ed"),
            QStringLiteral("#d6a9b4"),
            QStringLiteral("#704056"),
            QStringLiteral("#ff8a5b"),
            QStringLiteral("#ffad75"),
            QStringLiteral("#3b1720"),
            QStringLiteral("#ff5d73"),
            QStringLiteral("#f7c66b"),
        };
    } else if (name == QLatin1String("Forest")) {
        palette = {
            QStringLiteral("#0d1b16"),
            QStringLiteral("#14271f"),
            QStringLiteral("#1c3529"),
            QStringLiteral("#edf9ef"),
            QStringLiteral("#9ab9a4"),
            QStringLiteral("#315341"),
            QStringLiteral("#55c878"),
            QStringLiteral("#79df98"),
            QStringLiteral("#092016"),
            QStringLiteral("#ef6a73"),
            QStringLiteral("#f0c66b"),
        };
    } else if (name == QLatin1String("Neon")) {
        palette = {
            QStringLiteral("#090817"),
            QStringLiteral("#121126"),
            QStringLiteral("#1b1a35"),
            QStringLiteral("#f8f7ff"),
            QStringLiteral("#aaa7c8"),
            QStringLiteral("#3d3b63"),
            QStringLiteral("#ff4ecd"),
            QStringLiteral("#ff72dc"),
            QStringLiteral("#190b1c"),
            QStringLiteral("#ff5c93"),
            QStringLiteral("#5df2e8"),
        };
    } else if (name == QLatin1String("Aurora")) {
        palette = {
            QStringLiteral("#07161a"),
            QStringLiteral("#0d2a30"),
            QStringLiteral("#164149"),
            QStringLiteral("#f5fefc"),
            QStringLiteral("#93d4d1"),
            QStringLiteral("#2e737e"),
            QStringLiteral("#5eead4"),
            QStringLiteral("#99f6e4"),
            QStringLiteral("#062421"),
            QStringLiteral("#fda4af"),
            QStringLiteral("#86efac"),
        };
    } else if (name == QLatin1String("Ruby")) {
        palette = {
            QStringLiteral("#180a11"),
            QStringLiteral("#2d1420"),
            QStringLiteral("#472131"),
            QStringLiteral("#fff4f6"),
            QStringLiteral("#d99bad"),
            QStringLiteral("#7d3e56"),
            QStringLiteral("#fb7185"),
            QStringLiteral("#fda4af"),
            QStringLiteral("#2b0712"),
            QStringLiteral("#fda4af"),
            QStringLiteral("#86efac"),
        };
    } else if (name == QLatin1String("Cobalt")) {
        palette = {
            QStringLiteral("#07111f"),
            QStringLiteral("#0f2038"),
            QStringLiteral("#1a3357"),
            QStringLiteral("#f7faff"),
            QStringLiteral("#9ab9e2"),
            QStringLiteral("#385f95"),
            QStringLiteral("#60a5fa"),
            QStringLiteral("#93c5fd"),
            QStringLiteral("#071426"),
            QStringLiteral("#fda4af"),
            QStringLiteral("#86efac"),
        };
    } else if (name == QLatin1String("Sand")) {
        palette = {
            QStringLiteral("#fff8ed"),
            QStringLiteral("#fffbf5"),
            QStringLiteral("#f8ebd9"),
            QStringLiteral("#302622"),
            QStringLiteral("#86684d"),
            QStringLiteral("#e1c09c"),
            QStringLiteral("#ea580c"),
            QStringLiteral("#fb923c"),
            QStringLiteral("#ffffff"),
            QStringLiteral("#dc2626"),
            QStringLiteral("#15803d"),
        };
    } else if (name == QLatin1String("Rose")) {
        palette = {
            QStringLiteral("#fff5f8"),
            QStringLiteral("#fffbfd"),
            QStringLiteral("#fce7f0"),
            QStringLiteral("#421624"),
            QStringLiteral("#a43d5c"),
            QStringLiteral("#efa9c5"),
            QStringLiteral("#db2777"),
            QStringLiteral("#ec4899"),
            QStringLiteral("#ffffff"),
            QStringLiteral("#dc2626"),
            QStringLiteral("#15803d"),
        };
    } else if (name == QLatin1String("Mono")) {
        palette = {
            QStringLiteral("#f8fafc"),
            QStringLiteral("#ffffff"),
            QStringLiteral("#e2e8f0"),
            QStringLiteral("#0f172a"),
            QStringLiteral("#64748b"),
            QStringLiteral("#94a3b8"),
            QStringLiteral("#64748b"),
            QStringLiteral("#94a3b8"),
            QStringLiteral("#ffffff"),
            QStringLiteral("#b91c1c"),
            QStringLiteral("#166534"),
        };
    } else {
        palette = {
            QStringLiteral("#111827"),
            QStringLiteral("#1f2937"),
            QStringLiteral("#273449"),
            QStringLiteral("#f3f4f6"),
            QStringLiteral("#9ca3af"),
            QStringLiteral("#374151"),
            QStringLiteral("#8b5cf6"),
            QStringLiteral("#a78bfa"),
            QStringLiteral("#ffffff"),
            QStringLiteral("#f87171"),
            QStringLiteral("#34d399"),
        };
    }

    return palette;
}

QString ThemeManager::styleSheetFor(const QString& theme) const
{
    QString styleSheet = readSharedStyleSheet();
    const Palette palette = paletteFor(theme);

    styleSheet.replace(QStringLiteral("{{BACKGROUND}}"), palette.background);
    styleSheet.replace(QStringLiteral("{{SURFACE}}"), palette.surface);
    styleSheet.replace(QStringLiteral("{{SURFACE_ALT}}"), palette.surfaceAlt);
    styleSheet.replace(QStringLiteral("{{TEXT}}"), palette.text);
    styleSheet.replace(QStringLiteral("{{MUTED}}"), palette.muted);
    styleSheet.replace(QStringLiteral("{{BORDER}}"), palette.border);
    styleSheet.replace(QStringLiteral("{{ACCENT}}"), palette.accent);
    styleSheet.replace(QStringLiteral("{{ACCENT_HOVER}}"), palette.accentHover);
    styleSheet.replace(QStringLiteral("{{ACCENT_TEXT}}"), palette.accentText);
    styleSheet.replace(QStringLiteral("{{DANGER}}"), palette.danger);
    QColor dangerColor(palette.danger);
    styleSheet.replace(QStringLiteral("{{DANGER_HOVER}}"),
                       dangerColor.lighter(115).name());
    styleSheet.replace(QStringLiteral("{{SUCCESS}}"), palette.success);
    return styleSheet;
}

QString ThemeManager::readSharedStyleSheet() const
{
    QFile file(QStringLiteral(":/styles/app.qss"));
    if (!file.open(QIODevice::ReadOnly | QIODevice::Text)) {
        return {};
    }
    return QString::fromUtf8(file.readAll());
}

QString ThemeManager::currentTheme() const
{
    return canonicalThemeName(m_settings.theme());
}

bool ThemeManager::setTheme(const QString& theme)
{
    const QString canonical = canonicalThemeName(theme);
    if (canonical.compare(theme, Qt::CaseInsensitive) != 0) {
        return false;
    }

    m_settings.setTheme(canonical);
    return m_settings.save();
}

void ThemeManager::apply(QApplication& application)
{
    m_application = &application;

    const auto updateStyleSheet = [this] {
        if (m_application != nullptr) {
            m_application->setStyleSheet(styleSheetFor(m_settings.theme()));
        }
    };

    connect(this, &ThemeManager::themeChanged, this, [updateStyleSheet] {
        updateStyleSheet();
    }, Qt::QueuedConnection);
    updateStyleSheet();
}
