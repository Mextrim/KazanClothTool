#include "MainWindow.h"
#include "services/ProjectStore.h"
#include "services/SettingsStore.h"
#include "services/ThemeManager.h"

#include <QApplication>
#include <QCoreApplication>
#include <QGuiApplication>

int main(int argc, char* argv[])
{
    QApplication application(argc, argv);
    QCoreApplication::setOrganizationName(QStringLiteral("Kazan"));
    QCoreApplication::setApplicationName(QStringLiteral("KazanClothTool"));
    QGuiApplication::setApplicationDisplayName(QStringLiteral("Kazan Cloth Tool"));
    QApplication::setStyle(QStringLiteral("Fusion"));

    SettingsStore settingsStore;
    settingsStore.load();
    // Materialize the documented settings.json on first launch as well.
    settingsStore.save();

    ThemeManager themeManager(settingsStore);
    themeManager.apply(application);

    ProjectStore projectStore(settingsStore);
    MainWindow window(settingsStore, projectStore, themeManager);
    window.show();

    return application.exec();
}
