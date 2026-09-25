#pragma once

#include <QMainWindow>
#include <QString>

class EditorPage;
class HomePage;
class SettingsPage;
class SettingsStore;
class ProjectStore;
class ThemeManager;
class QButtonGroup;
class QStackedWidget;

/** Application shell: top bar, three navigation destinations, and page stack. */
class MainWindow final : public QMainWindow
{
    Q_OBJECT

public:
    MainWindow(SettingsStore& settingsStore,
               ProjectStore& projectStore,
               ThemeManager& themeManager,
               QWidget* parent = nullptr);

private slots:
    void navigateToHome();
    void navigateToEditor();
    void navigateToSettings();
    void handleProjectOpened(const QString& projectPath);

private:
    void showPage(int pageIndex);
    void updateNavigation(int pageIndex);

    SettingsStore& m_settingsStore;
    ProjectStore& m_projectStore;
    ThemeManager& m_themeManager;
    QStackedWidget* m_pageStack = nullptr;
    QButtonGroup* m_navigationGroup = nullptr;
    HomePage* m_homePage = nullptr;
    EditorPage* m_editorPage = nullptr;
    SettingsPage* m_settingsPage = nullptr;
};
