#pragma once

#include <QObject>
#include <QString>
#include <QStringList>

class QApplication;
class SettingsStore;

/** Applies the small MVP palette and the shared Qt stylesheet. */
class ThemeManager final : public QObject
{
    Q_OBJECT

public:
    explicit ThemeManager(SettingsStore& settings, QObject* parent = nullptr);

    QStringList availableThemes() const;
    QString currentTheme() const;

    bool setTheme(const QString& theme);

    void apply(QApplication& application);
    QString styleSheetFor(const QString& theme) const;

signals:
    void themeChanged(const QString& theme);

private:
    struct Palette;
    static QString canonicalThemeName(const QString& theme);
    static Palette paletteFor(const QString& theme);
    QString readSharedStyleSheet() const;

    SettingsStore& m_settings;
    QApplication* m_application = nullptr;
};
