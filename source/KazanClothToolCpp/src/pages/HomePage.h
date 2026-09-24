#pragma once

#include <QString>
#include <QWidget>

class ProjectStore;
class SettingsStore;
class QLabel;
class QListWidget;
class QListWidgetItem;
class QPushButton;

/** Landing page for creating or opening an MVP project. */
class HomePage final : public QWidget
{
    Q_OBJECT

public:
    HomePage(ProjectStore& projectStore,
             SettingsStore& settingsStore,
             QWidget* parent = nullptr);

    void refreshRecentProjects();

signals:
    void editorRequested();
    void projectOpened(const QString& projectPath);

private slots:
    void createProject();
    void openProject();
    void importProject();
    void openRecentProject();
    void openRecentProjectItem(QListWidgetItem* item);
    void removeRecentProject();

private:
    void showStatus(const QString& text, bool isError = false);
    bool openProjectPath(const QString& path);

    ProjectStore& m_projectStore;
    SettingsStore& m_settingsStore;
    QListWidget* m_recentProjectsList = nullptr;
    QLabel* m_emptyRecentLabel = nullptr;
    QLabel* m_statusLabel = nullptr;
    QPushButton* m_newProjectButton = nullptr;
};
