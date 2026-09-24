#include "MainWindow.h"

#include "pages/EditorPage.h"
#include "pages/HomePage.h"
#include "pages/SettingsPage.h"
#include "services/ProjectStore.h"
#include "services/SettingsStore.h"
#include "services/ThemeManager.h"

#include <QAbstractButton>
#include <QButtonGroup>
#include <QFrame>
#include <QHBoxLayout>
#include <QLabel>
#include <QPushButton>
#include <QStackedWidget>
#include <QVBoxLayout>

MainWindow::MainWindow(SettingsStore& settingsStore,
                       ProjectStore& projectStore,
                       ThemeManager& themeManager,
                       QWidget* parent)
    : QMainWindow(parent)
    , m_settingsStore(settingsStore)
    , m_projectStore(projectStore)
    , m_themeManager(themeManager)
{
    setWindowTitle(QStringLiteral("Kazan Cloth Tool"));
    setMinimumSize(960, 640);
    resize(1180, 760);

    auto* central = new QWidget(this);
    central->setObjectName(QStringLiteral("centralWidget"));
    auto* centralLayout = new QVBoxLayout(central);
    centralLayout->setContentsMargins(0, 0, 0, 0);
    centralLayout->setSpacing(0);

    auto* topBar = new QFrame(central);
    topBar->setObjectName(QStringLiteral("topBar"));
    auto* topLayout = new QVBoxLayout(topBar);
    topLayout->setContentsMargins(28, 16, 28, 14);
    topLayout->setSpacing(14);

    auto* brandRow = new QHBoxLayout;
    auto* brandMark = new QLabel(QStringLiteral("KC"), topBar);
    brandMark->setObjectName(QStringLiteral("brandMark"));
    brandMark->setAlignment(Qt::AlignCenter);
    brandMark->setFixedSize(42, 42);

    auto* brandText = new QVBoxLayout;
    brandText->setSpacing(1);
    auto* brandTitle = new QLabel(QStringLiteral("Kazan Cloth Tool"), topBar);
    brandTitle->setObjectName(QStringLiteral("brandTitle"));
    auto* brandSubtitle = new QLabel(QStringLiteral("C++ / Qt 6 MVP"), topBar);
    brandSubtitle->setObjectName(QStringLiteral("brandSubtitle"));
    brandText->addWidget(brandTitle);
    brandText->addWidget(brandSubtitle);
    brandRow->addWidget(brandMark);
    brandRow->addLayout(brandText);
    brandRow->addStretch(1);

    auto* buildBadge = new QLabel(QStringLiteral("PARALLEL EDITION  •  0.1"), topBar);
    buildBadge->setObjectName(QStringLiteral("buildBadge"));
    brandRow->addWidget(buildBadge);
    topLayout->addLayout(brandRow);

    auto* navigationBar = new QFrame(topBar);
    navigationBar->setObjectName(QStringLiteral("navigationBar"));
    auto* navigationLayout = new QHBoxLayout(navigationBar);
    navigationLayout->setContentsMargins(0, 0, 0, 0);
    navigationLayout->setSpacing(8);

    m_navigationGroup = new QButtonGroup(this);
    m_navigationGroup->setExclusive(true);

    const auto addNavigationButton = [this, navigationBar, navigationLayout](
                                         const QString& text,
                                         int id) {
        auto* button = new QPushButton(text, navigationBar);
        button->setObjectName(QStringLiteral("navButton"));
        button->setCheckable(true);
        button->setMinimumHeight(36);
        m_navigationGroup->addButton(button, id);
        navigationLayout->addWidget(button);
        return button;
    };

    auto* homeButton = addNavigationButton(QStringLiteral("Home"), 0);
    auto* editorButton = addNavigationButton(QStringLiteral("Editor"), 1);
    auto* settingsButton = addNavigationButton(QStringLiteral("Settings"), 2);
    navigationLayout->addStretch(1);
    topLayout->addWidget(navigationBar);

    connect(homeButton, &QPushButton::clicked, this, &MainWindow::navigateToHome);
    connect(editorButton, &QPushButton::clicked, this, &MainWindow::navigateToEditor);
    connect(settingsButton, &QPushButton::clicked, this, &MainWindow::navigateToSettings);

    m_pageStack = new QStackedWidget(central);
    m_homePage = new HomePage(m_projectStore, m_settingsStore, m_pageStack);
    m_editorPage = new EditorPage(m_settingsStore, m_pageStack);
    m_settingsPage = new SettingsPage(m_settingsStore, m_themeManager, m_pageStack);
    m_pageStack->addWidget(m_homePage);
    m_pageStack->addWidget(m_editorPage);
    m_pageStack->addWidget(m_settingsPage);
    centralLayout->addWidget(m_pageStack, 1);

    connect(m_homePage, &HomePage::projectOpened,
            this, &MainWindow::handleProjectOpened);
    connect(m_homePage, &HomePage::editorRequested,
            this, &MainWindow::navigateToEditor);

    setCentralWidget(central);
    showPage(0);
}

void MainWindow::showPage(int pageIndex)
{
    if (m_pageStack != nullptr) {
        m_pageStack->setCurrentIndex(pageIndex);
    }
    updateNavigation(pageIndex);
}

void MainWindow::updateNavigation(int pageIndex)
{
    if (m_navigationGroup == nullptr) {
        return;
    }
    if (QAbstractButton* button = m_navigationGroup->button(pageIndex)) {
        button->setChecked(true);
    }
}

void MainWindow::navigateToHome()
{
    showPage(0);
}

void MainWindow::navigateToEditor()
{
    showPage(1);
}

void MainWindow::navigateToSettings()
{
    showPage(2);
}

void MainWindow::handleProjectOpened(const QString& projectPath)
{
    if (m_editorPage != nullptr) {
        m_editorPage->setProjectPath(projectPath);
    }
    showPage(1);
}
