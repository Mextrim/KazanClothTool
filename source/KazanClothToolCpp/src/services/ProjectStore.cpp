#include "ProjectStore.h"

#include "SettingsStore.h"

#include <QDateTime>
#include <QDir>
#include <QFileInfo>
#include <QJsonDocument>
#include <QJsonObject>
#include <QSaveFile>

ProjectStore::ProjectStore(SettingsStore& settings, QObject* parent)
    : QObject(parent)
    , m_settings(settings)
{
    connect(&m_settings, &SettingsStore::recentProjectsChanged,
            this, &ProjectStore::recentProjectsChanged);
}

QStringList ProjectStore::recentProjects() const
{
    return m_settings.recentProjects();
}

QString ProjectStore::safeDirectoryName(const QString& projectName)
{
    QString result;
    result.reserve(projectName.size());

    for (const QChar character : projectName.trimmed()) {
        if (character.isLetterOrNumber() || character == QLatin1Char('_')
            || character == QLatin1Char('-')) {
            result.append(character);
        } else if (!result.endsWith(QLatin1Char('_'))) {
            result.append(QLatin1Char('_'));
        }
    }

    if (result.isEmpty()) {
        return QStringLiteral("project");
    }
    return result.left(80);
}

bool ProjectStore::isProjectDirectory(const QString& projectPath) const
{
    if (projectPath.trimmed().isEmpty()) {
        return false;
    }

    const QFileInfo info(projectPath);
    return info.exists() && info.isDir() && info.isReadable();
}

bool ProjectStore::createProject(const QString& projectName,
                                 const QString& rootDirectory,
                                 QString* createdPath,
                                 QString* errorMessage)
{
    const auto fail = [errorMessage](const QString& message) {
        if (errorMessage != nullptr) {
            *errorMessage = message;
        }
        return false;
    };

    const QString name = projectName.trimmed();
    if (name.isEmpty()) {
        return fail(QStringLiteral("Введите название проекта."));
    }

    QString root = rootDirectory.trimmed();
    if (root.isEmpty()) {
        root = m_settings.projectsFolder();
    }
    if (root.isEmpty()) {
        return fail(QStringLiteral("Папка проектов не настроена."));
    }

    if (!QDir().mkpath(root)) {
        return fail(QStringLiteral("Не удалось создать папку проектов: %1").arg(root));
    }

    const QDir rootDirectoryObject(root);
    const QString baseName = safeDirectoryName(name);
    QString candidate = rootDirectoryObject.absoluteFilePath(baseName);
    int suffix = 2;
    while (QFileInfo::exists(candidate)) {
        candidate = rootDirectoryObject.absoluteFilePath(
            QStringLiteral("%1_%2").arg(baseName).arg(suffix));
        ++suffix;
    }

    if (!QDir().mkpath(candidate)) {
        return fail(QStringLiteral("Не удалось создать папку нового проекта."));
    }

    const QDir projectDirectory(candidate);
    if (!projectDirectory.mkpath(QStringLiteral("source"))
        || !projectDirectory.mkpath(QStringLiteral("output"))
        || !projectDirectory.mkpath(QStringLiteral("cache"))) {
        QDir(candidate).removeRecursively();
        return fail(QStringLiteral("Не удалось создать структуру проекта."));
    }

    QJsonObject metadata;
    metadata.insert(QStringLiteral("formatVersion"), 1);
    metadata.insert(QStringLiteral("name"), name);
    metadata.insert(QStringLiteral("createdAt"),
                    QDateTime::currentDateTimeUtc().toString(Qt::ISODate));

    QSaveFile projectFile(projectDirectory.filePath(QStringLiteral("project.json")));
    if (!projectFile.open(QIODevice::WriteOnly | QIODevice::Text)
        || projectFile.write(QJsonDocument(metadata).toJson(QJsonDocument::Indented)) < 0
        || !projectFile.commit()) {
        projectFile.cancelWriting();
        QDir(candidate).removeRecursively();
        return fail(QStringLiteral("Не удалось записать project.json."));
    }

    QFileInfo info(candidate);
    const QString absolutePath = info.absoluteFilePath();
    if (createdPath != nullptr) {
        *createdPath = absolutePath;
    }
    rememberProject(absolutePath);
    return true;
}

bool ProjectStore::openProject(const QString& projectPath, QString* errorMessage)
{
    if (!isProjectDirectory(projectPath)) {
        if (errorMessage != nullptr) {
            *errorMessage = QStringLiteral("Папка проекта не найдена или недоступна.");
        }
        return false;
    }

    const QString absolutePath = QFileInfo(projectPath).absoluteFilePath();
    rememberProject(absolutePath);
    return true;
}

void ProjectStore::rememberProject(const QString& projectPath)
{
    if (projectPath.trimmed().isEmpty()) {
        return;
    }

    const QString absolutePath = QFileInfo(projectPath).absoluteFilePath();
    m_settings.addRecentProject(absolutePath);
    m_settings.save();
}

void ProjectStore::forgetProject(const QString& projectPath)
{
    m_settings.removeRecentProject(projectPath);
    m_settings.save();
}
