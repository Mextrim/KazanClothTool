#include "SettingsStore.h"

#include <QDir>
#include <QFile>
#include <QJsonArray>
#include <QJsonDocument>
#include <QJsonObject>
#include <QSaveFile>
#include <QStandardPaths>

SettingsStore::SettingsStore(QObject* parent)
    : QObject(parent)
    , m_theme(QStringLiteral("Dark"))
    , m_language(QStringLiteral("ru"))
    , m_isFirstRun(true)
    , m_projectsFolder(defaultProjectsFolder())
{
}

QString SettingsStore::settingsDirectoryPath()
{
    // QStandardPaths can include an organization name for some Qt versions.
    // Using LOCALAPPDATA directly keeps the promised Windows location exact.
    QString baseDirectory = qEnvironmentVariable("LOCALAPPDATA");
    if (baseDirectory.isEmpty()) {
        baseDirectory = QStandardPaths::writableLocation(QStandardPaths::GenericDataLocation);
    }
    if (baseDirectory.isEmpty()) {
        baseDirectory = QDir::homePath() + QStringLiteral("/.local/share");
    }

    return QDir(baseDirectory).filePath(QStringLiteral("KazanClothTool"));
}

QString SettingsStore::filePath() const
{
    return QDir(settingsDirectoryPath()).filePath(QStringLiteral("settings.json"));
}

QString SettingsStore::directoryPath() const
{
    return settingsDirectoryPath();
}

QString SettingsStore::defaultProjectsFolder()
{
    QString documents = QStandardPaths::writableLocation(QStandardPaths::DocumentsLocation);
    if (documents.isEmpty()) {
        documents = QDir::homePath();
    }
    return QDir(documents).filePath(QStringLiteral("KazanClothProjects"));
}

bool SettingsStore::load()
{
    m_theme = QStringLiteral("Dark");
    m_language = QStringLiteral("ru");
    m_isFirstRun = true;
    m_projectsFolder = defaultProjectsFolder();
    m_recentProjects.clear();

    QFile file(filePath());
    if (!file.exists()) {
        return true;
    }
    if (!file.open(QIODevice::ReadOnly | QIODevice::Text)) {
        return false;
    }

    QJsonParseError parseError{};
    const QJsonDocument document = QJsonDocument::fromJson(file.readAll(), &parseError);
    if (parseError.error != QJsonParseError::NoError || !document.isObject()) {
        return false;
    }

    const QJsonObject root = document.object();
    if (root.contains(QStringLiteral("IsFirstRun"))) {
        m_isFirstRun = root.value(QStringLiteral("IsFirstRun")).toBool(true);
    } else if (root.contains(QStringLiteral("isFirstRun"))) {
        m_isFirstRun = root.value(QStringLiteral("isFirstRun")).toBool(true);
    }

    const QString savedTheme = !root.value(QStringLiteral("Theme")).toString().isEmpty()
        ? root.value(QStringLiteral("Theme")).toString()
        : root.value(QStringLiteral("theme")).toString();
    if (!savedTheme.isEmpty()) {
        m_theme = savedTheme;
    }

    const QString savedLanguage = !root.value(QStringLiteral("Language")).toString().isEmpty()
        ? root.value(QStringLiteral("Language")).toString()
        : root.value(QStringLiteral("language")).toString();
    if (!savedLanguage.isEmpty()) {
        m_language = savedLanguage;
    }

    const QString savedProjectsFolder = !root.value(QStringLiteral("MainProjectsFolder")).toString().isEmpty()
        ? root.value(QStringLiteral("MainProjectsFolder")).toString()
        : root.value(QStringLiteral("projectsFolder")).toString();
    if (!savedProjectsFolder.isEmpty()) {
        m_projectsFolder = savedProjectsFolder;
    }

    const QJsonArray recent = !root.value(QStringLiteral("recentProjects")).isArray()
        ? root.value(QStringLiteral("RecentlyOpenedProjects")).toArray()
        : root.value(QStringLiteral("recentProjects")).toArray();
    for (const QJsonValue& value : recent) {
        QString projectPath;
        if (value.isString()) {
            projectPath = value.toString();
        } else if (value.isObject()) {
            projectPath = value.toObject().value(QStringLiteral("FilePath")).toString();
        }
        if (!projectPath.isEmpty() && !m_recentProjects.contains(projectPath)) {
            m_recentProjects.append(projectPath);
        }
    }

    emit settingsChanged();
    emit themeChanged(m_theme);
    emit languageChanged(m_language);
    emit projectsFolderChanged(m_projectsFolder);
    emit recentProjectsChanged();
    return true;
}

bool SettingsStore::save() const
{
    const QString directory = directoryPath();
    if (!QDir().mkpath(directory)) {
        return false;
    }

    QJsonObject root;
    // Use the same PascalCase keys as the WPF application so switching
    // between the two frontends does not discard user preferences.
    root.insert(QStringLiteral("IsFirstRun"), m_isFirstRun);
    root.insert(QStringLiteral("Theme"), m_theme);
    root.insert(QStringLiteral("Language"), m_language);
    root.insert(QStringLiteral("MainProjectsFolder"), m_projectsFolder);

    QJsonArray recent;
    for (const QString& projectPath : m_recentProjects) {
        QJsonObject item;
        item.insert(QStringLiteral("FilePath"), projectPath);
        recent.append(item);
    }
    root.insert(QStringLiteral("RecentlyOpenedProjects"), recent);

    QSaveFile file(filePath());
    if (!file.open(QIODevice::WriteOnly | QIODevice::Text)) {
        return false;
    }
    if (file.write(QJsonDocument(root).toJson(QJsonDocument::Indented)) < 0) {
        file.cancelWriting();
        return false;
    }
    return file.commit();
}

QString SettingsStore::theme() const
{
    return m_theme;
}

void SettingsStore::setTheme(const QString& theme)
{
    const QString normalized = theme.trimmed();
    if (normalized.isEmpty() || normalized == m_theme) {
        return;
    }

    m_theme = normalized;
    emit themeChanged(m_theme);
    emit settingsChanged();
}

QString SettingsStore::language() const
{
    return m_language;
}

void SettingsStore::setLanguage(const QString& language)
{
    const QString normalized = language.trimmed().toLower();
    if (normalized.isEmpty() || normalized == m_language) {
        return;
    }

    m_language = normalized;
    emit languageChanged(m_language);
    emit settingsChanged();
}

bool SettingsStore::isFirstRun() const
{
    return m_isFirstRun;
}

void SettingsStore::setIsFirstRun(bool isFirstRun)
{
    if (m_isFirstRun == isFirstRun) {
        return;
    }

    m_isFirstRun = isFirstRun;
    emit settingsChanged();
}

QString SettingsStore::projectsFolder() const
{
    return m_projectsFolder;
}

void SettingsStore::setProjectsFolder(const QString& folder)
{
    const QString trimmed = folder.trimmed();
    if (trimmed.isEmpty()) {
        return;
    }

    const QString normalized = QDir::cleanPath(trimmed);
    if (normalized == m_projectsFolder) {
        return;
    }

    m_projectsFolder = normalized;
    emit projectsFolderChanged(m_projectsFolder);
    emit settingsChanged();
}

QStringList SettingsStore::recentProjects() const
{
    return m_recentProjects;
}

void SettingsStore::setRecentProjects(const QStringList& projects)
{
    QStringList normalized;
    normalized.reserve(projects.size());
    for (const QString& projectPath : projects) {
        const QString trimmed = projectPath.trimmed();
        if (trimmed.isEmpty()) {
            continue;
        }
        const QString cleanPath = QDir::cleanPath(trimmed);
        if (!normalized.contains(cleanPath)) {
            normalized.append(cleanPath);
        }
    }

    if (normalized == m_recentProjects) {
        return;
    }

    m_recentProjects = normalized;
    emit recentProjectsChanged();
    emit settingsChanged();
}

void SettingsStore::addRecentProject(const QString& projectPath)
{
    const QString trimmed = projectPath.trimmed();
    if (trimmed.isEmpty()) {
        return;
    }
    const QString cleanPath = QDir::cleanPath(trimmed);

    m_recentProjects.removeAll(cleanPath);
    m_recentProjects.prepend(cleanPath);
    while (m_recentProjects.size() > 12) {
        m_recentProjects.removeLast();
    }

    emit recentProjectsChanged();
    emit settingsChanged();
}

void SettingsStore::removeRecentProject(const QString& projectPath)
{
    const QString trimmed = projectPath.trimmed();
    if (trimmed.isEmpty()) {
        return;
    }

    const QString cleanPath = QDir::cleanPath(trimmed);
    if (!m_recentProjects.removeAll(cleanPath)) {
        return;
    }

    emit recentProjectsChanged();
    emit settingsChanged();
}
