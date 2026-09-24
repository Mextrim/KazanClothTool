#pragma once

#include <QObject>
#include <QString>
#include <QStringList>

/**
 * Small JSON-backed application settings store.
 *
 * On Windows the file is deliberately kept at
 * %LOCALAPPDATA%\KazanClothTool\settings.json.  The fallback is based on
 * QStandardPaths so the application remains usable on other platforms while
 * the Qt/WPF port is being developed.
 */
class SettingsStore final : public QObject
{
    Q_OBJECT

public:
    explicit SettingsStore(QObject* parent = nullptr);

    bool load();
    bool save() const;

    QString filePath() const;
    QString directoryPath() const;

    QString theme() const;
    void setTheme(const QString& theme);

    QString language() const;
    void setLanguage(const QString& language);

    bool isFirstRun() const;
    void setIsFirstRun(bool isFirstRun);

    QString projectsFolder() const;
    void setProjectsFolder(const QString& folder);

    QStringList recentProjects() const;
    void setRecentProjects(const QStringList& projects);
    void addRecentProject(const QString& projectPath);
    void removeRecentProject(const QString& projectPath);

signals:
    void settingsChanged();
    void themeChanged(const QString& theme);
    void languageChanged(const QString& language);
    void projectsFolderChanged(const QString& folder);
    void recentProjectsChanged();

private:
    static QString defaultProjectsFolder();
    static QString settingsDirectoryPath();

    QString m_theme;
    QString m_language;
    bool m_isFirstRun = true;
    QString m_projectsFolder;
    QStringList m_recentProjects;
};
