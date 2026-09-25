#include "HomePage.h"

#include "../services/ProjectStore.h"
#include "../services/SettingsStore.h"

#include <QAbstractItemView>
#include <QDir>
#include <QFile>
#include <QFileDialog>
#include <QFileInfo>
#include <QFrame>
#include <QGridLayout>
#include <QInputDialog>
#include <QJsonDocument>
#include <QLabel>
#include <QLineEdit>
#include <QListWidget>
#include <QListWidgetItem>
#include <QMessageBox>
#include <QPushButton>
#include <QStyle>
#include <QVBoxLayout>

HomePage::HomePage(ProjectStore& projectStore,
                   SettingsStore& settingsStore,
                   QWidget* parent)
    : QWidget(parent)
    , m_projectStore(projectStore)
    , m_settingsStore(settingsStore)
{
    setObjectName(QStringLiteral("homePage"));

    auto* pageLayout = new QVBoxLayout(this);
    pageLayout->setContentsMargins(48, 42, 48, 40);
    pageLayout->setSpacing(24);

    auto* title = new QLabel(QStringLiteral("Kazan Cloth Tool"), this);
    title->setObjectName(QStringLiteral("pageTitle"));
    pageLayout->addWidget(title);

    auto* subtitle = new QLabel(
        QStringLiteral("Параллельная C++/Qt 6 версия редактора одежды. Начните с проекта или откройте недавний."),
        this);
    subtitle->setObjectName(QStringLiteral("pageSubtitle"));
    subtitle->setWordWrap(true);
    pageLayout->addWidget(subtitle);

    auto* actionCard = new QFrame(this);
    actionCard->setObjectName(QStringLiteral("card"));
    auto* actionLayout = new QGridLayout(actionCard);
    actionLayout->setContentsMargins(24, 24, 24, 24);
    actionLayout->setHorizontalSpacing(14);
    actionLayout->setVerticalSpacing(14);

    m_newProjectButton = new QPushButton(QStringLiteral("＋  Новый проект"), actionCard);
    m_newProjectButton->setObjectName(QStringLiteral("primaryButton"));
    m_newProjectButton->setMinimumHeight(52);
    m_newProjectButton->setToolTip(QStringLiteral("Создать папку проекта и базовые каталоги"));

    auto* openButton = new QPushButton(QStringLiteral("Открыть проект"), actionCard);
    openButton->setObjectName(QStringLiteral("secondaryButton"));
    openButton->setMinimumHeight(52);

    auto* importButton = new QPushButton(QStringLiteral("Импорт"), actionCard);
    importButton->setObjectName(QStringLiteral("secondaryButton"));
    importButton->setMinimumHeight(52);
    importButton->setToolTip(QStringLiteral("Импорт GTA-ресурсов появится после переноса форматов"));

    actionLayout->addWidget(m_newProjectButton, 0, 0);
    actionLayout->addWidget(openButton, 0, 1);
    actionLayout->addWidget(importButton, 0, 2);
    pageLayout->addWidget(actionCard);

    auto* recentHeader = new QHBoxLayout;
    recentHeader->setContentsMargins(0, 4, 0, 0);
    auto* recentTitle = new QLabel(QStringLiteral("Недавние проекты"), this);
    recentTitle->setObjectName(QStringLiteral("sectionTitle"));
    recentHeader->addWidget(recentTitle);
    recentHeader->addStretch(1);

    auto* removeButton = new QPushButton(QStringLiteral("Убрать из списка"), this);
    removeButton->setObjectName(QStringLiteral("linkButton"));
    removeButton->setEnabled(false);
    recentHeader->addWidget(removeButton);
    pageLayout->addLayout(recentHeader);

    m_recentProjectsList = new QListWidget(this);
    m_recentProjectsList->setObjectName(QStringLiteral("recentProjectsList"));
    m_recentProjectsList->setSelectionMode(QAbstractItemView::SingleSelection);
    m_recentProjectsList->setMinimumHeight(170);
    m_recentProjectsList->setAlternatingRowColors(false);
    m_recentProjectsList->setVisible(false);
    pageLayout->addWidget(m_recentProjectsList);

    connect(m_recentProjectsList, &QListWidget::itemSelectionChanged, this, [this, removeButton] {
        removeButton->setEnabled(m_recentProjectsList != nullptr
                                 && m_recentProjectsList->currentItem() != nullptr);
    });

    m_emptyRecentLabel = new QLabel(
        QStringLiteral("Пока нет недавних проектов.\nСоздайте первый проект — его папка появится здесь."),
        this);
    m_emptyRecentLabel->setObjectName(QStringLiteral("emptyRecent"));
    m_emptyRecentLabel->setAlignment(Qt::AlignCenter);
    m_emptyRecentLabel->setMinimumHeight(170);
    m_emptyRecentLabel->setWordWrap(true);
    pageLayout->addWidget(m_emptyRecentLabel);

    m_statusLabel = new QLabel(this);
    m_statusLabel->setObjectName(QStringLiteral("statusLabel"));
    m_statusLabel->setWordWrap(true);
    pageLayout->addWidget(m_statusLabel);
    pageLayout->addStretch(1);

    connect(m_newProjectButton, &QPushButton::clicked,
            this, &HomePage::createProject);
    connect(openButton, &QPushButton::clicked, this, &HomePage::openProject);
    connect(importButton, &QPushButton::clicked, this, &HomePage::importProject);
    connect(m_recentProjectsList, &QListWidget::itemDoubleClicked,
            this, &HomePage::openRecentProjectItem);
    connect(m_recentProjectsList, &QListWidget::itemActivated,
            this, [this](QListWidgetItem* item) {
                openRecentProjectItem(item);
            });
    connect(removeButton, &QPushButton::clicked, this, &HomePage::removeRecentProject);
    connect(&m_projectStore, &ProjectStore::recentProjectsChanged,
            this, &HomePage::refreshRecentProjects);

    refreshRecentProjects();
}

void HomePage::refreshRecentProjects()
{
    if (m_recentProjectsList == nullptr) {
        return;
    }

    m_recentProjectsList->clear();
    int validProjectCount = 0;
    const QStringList recent = m_projectStore.recentProjects();
    for (const QString& path : recent) {
        const QFileInfo info(path);
        if (!info.exists() || !info.isDir()) {
            continue;
        }

        QString projectName = info.fileName();
        const QFile projectJson(info.filePath(QStringLiteral("project.json")));
        if (projectJson.open(QIODevice::ReadOnly | QIODevice::Text)) {
            const QJsonDocument document = QJsonDocument::fromJson(projectJson.readAll());
            if (document.isObject()) {
                const QString savedName = document.object().value(QStringLiteral("name")).toString();
                if (!savedName.isEmpty()) {
                    projectName = savedName;
                }
            }
        }

        auto* item = new QListWidgetItem(projectName, m_recentProjectsList);
        item->setData(Qt::UserRole, info.absoluteFilePath());
        item->setToolTip(info.absoluteFilePath());
        ++validProjectCount;
    }

    const bool hasProjects = validProjectCount > 0;
    m_recentProjectsList->setVisible(hasProjects);
    m_emptyRecentLabel->setVisible(!hasProjects);
}

void HomePage::createProject()
{
    bool accepted = false;
    const QString name = QInputDialog::getText(
        this,
        QStringLiteral("Новый проект"),
        QStringLiteral("Название проекта:"),
        QLineEdit::Normal,
        QString(),
        &accepted).trimmed();

    if (!accepted || name.isEmpty()) {
        return;
    }

    QString createdPath;
    QString error;
    if (!m_projectStore.createProject(name, m_settingsStore.projectsFolder(),
                                      &createdPath, &error)) {
        QMessageBox::warning(this, QStringLiteral("Не удалось создать проект"), error);
        return;
    }

    showStatus(QStringLiteral("Проект создан: %1")
                   .arg(QDir::toNativeSeparators(createdPath)));
    emit projectOpened(createdPath);
    emit editorRequested();
}

void HomePage::openProject()
{
    const QString path = QFileDialog::getExistingDirectory(
        this,
        QStringLiteral("Открыть проект"),
        m_settingsStore.projectsFolder(),
        QFileDialog::ShowDirsOnly | QFileDialog::DontResolveSymlinks);
    if (path.isEmpty()) {
        return;
    }

    openProjectPath(path);
}

void HomePage::importProject()
{
    QMessageBox::information(
        this,
        QStringLiteral("Импорт пока не перенесён"),
        QStringLiteral("Импорт CodeWalker и GTA-форматов (YDD/YTD и других файлов) "
                       "ещё не перенесён из WPF-версии.\n\n"
                       "В текущем MVP доступны создание и открытие папок проекта. "
                       "Поддержка форматов будет добавлена отдельным этапом."));
}

void HomePage::openRecentProject()
{
    if (m_recentProjectsList == nullptr) {
        return;
    }
    openRecentProjectItem(m_recentProjectsList->currentItem());
}

void HomePage::openRecentProjectItem(QListWidgetItem* item)
{
    if (item == nullptr) {
        return;
    }
    openProjectPath(item->data(Qt::UserRole).toString());
}

bool HomePage::openProjectPath(const QString& path)
{
    QString error;
    if (!m_projectStore.openProject(path, &error)) {
        QMessageBox::warning(this, QStringLiteral("Не удалось открыть проект"), error);
        return false;
    }

    showStatus(QStringLiteral("Открыт проект: %1")
                   .arg(QDir::toNativeSeparators(path)));
    emit projectOpened(QFileInfo(path).absoluteFilePath());
    emit editorRequested();
    return true;
}

void HomePage::removeRecentProject()
{
    if (m_recentProjectsList == nullptr) {
        return;
    }
    QListWidgetItem* item = m_recentProjectsList->currentItem();
    if (item == nullptr) {
        return;
    }

    m_projectStore.forgetProject(item->data(Qt::UserRole).toString());
    showStatus(QStringLiteral("Проект убран из списка."));
}

void HomePage::showStatus(const QString& text, bool isError)
{
    if (m_statusLabel == nullptr) {
        return;
    }
    m_statusLabel->setText(text);
    m_statusLabel->setProperty("isError", isError);
    m_statusLabel->style()->unpolish(m_statusLabel);
    m_statusLabel->style()->polish(m_statusLabel);
}
