#pragma once

#include <QObject>
#include <QString>
#include <QStringList>

class SettingsStore;

/** Creates the deliberately small project layout used by the Qt MVP. */
class ProjectStore final : public QObject
{
    Q_OBJECT

public:
    explicit ProjectStore(SettingsStore& settings, QObject* parent = nullptr);

    QStringList recentProjects() const;

    bool createProject(const QString& projectName,
                       const QString& rootDirectory,
                       QString* createdPath = nullptr,
                       QString* errorMessage = nullptr);

    bool openProject(const QString& projectPath,
                     QString* errorMessage = nullptr);

    bool isProjectDirectory(const QString& projectPath) const;
    void rememberProject(const QString& projectPath);
    void forgetProject(const QString& projectPath);

signals:
    void recentProjectsChanged();

private:
    static QString safeDirectoryName(const QString& projectName);

    SettingsStore& m_settings;
};
