#include "EditorPage.h"

#include "../services/SettingsStore.h"
#include "../widgets/SplitActionButton.h"

#include <QAbstractItemView>
#include <QAction>
#include <QDir>
#include <QFileDialog>
#include <QFrame>
#include <QHBoxLayout>
#include <QLabel>
#include <QListWidget>
#include <QListWidgetItem>
#include <QMenu>
#include <QMessageBox>
#include <QPushButton>
#include <QToolButton>
#include <QVBoxLayout>

EditorPage::EditorPage(SettingsStore& settingsStore, QWidget* parent)
    : QWidget(parent)
    , m_settingsStore(settingsStore)
{
    setObjectName(QStringLiteral("editorPage"));

    auto* pageLayout = new QVBoxLayout(this);
    pageLayout->setContentsMargins(48, 42, 48, 40);
    pageLayout->setSpacing(22);

    auto* title = new QLabel(QStringLiteral("Редактор одежды"), this);
    title->setObjectName(QStringLiteral("pageTitle"));
    pageLayout->addWidget(title);

    auto* subtitle = new QLabel(
        QStringLiteral("Выберите категорию и работайте с содержимым проекта. "
                       "В этой версии список намеренно пуст до появления импорта форматов."),
        this);
    subtitle->setObjectName(QStringLiteral("pageSubtitle"));
    subtitle->setWordWrap(true);
    pageLayout->addWidget(subtitle);

    auto* projectBar = new QFrame(this);
    projectBar->setObjectName(QStringLiteral("projectBar"));
    auto* projectLayout = new QHBoxLayout(projectBar);
    projectLayout->setContentsMargins(16, 12, 12, 12);
    projectLayout->setSpacing(12);

    auto* projectCaption = new QLabel(QStringLiteral("Папка проекта:"), projectBar);
    projectCaption->setObjectName(QStringLiteral("mutedLabel"));
    m_projectPathLabel = new QLabel(QStringLiteral("не выбрана"), projectBar);
    m_projectPathLabel->setObjectName(QStringLiteral("projectPath"));
    m_projectPathLabel->setTextInteractionFlags(Qt::TextSelectableByMouse);
    m_projectPathLabel->setWordWrap(true);

    m_folderButton = new QToolButton(projectBar);
    m_folderButton->setObjectName(QStringLiteral("folderButton"));
    m_folderButton->setText(QStringLiteral("Выбрать папку"));
    m_folderButton->setToolButtonStyle(Qt::ToolButtonTextBesideIcon);
    m_folderButton->setPopupMode(QToolButton::InstantPopup);
    m_folderButton->setToolTip(QStringLiteral("Выбрать папку текущего проекта"));

    auto* folderMenu = new QMenu(m_folderButton);
    m_chooseFolderAction = folderMenu->addAction(QStringLiteral("Выбрать папку"));
    m_folderButton->setMenu(folderMenu);

    projectLayout->addWidget(projectCaption);
    projectLayout->addWidget(m_projectPathLabel, 1);
    projectLayout->addWidget(m_folderButton);
    pageLayout->addWidget(projectBar);

    auto* categoryCard = new QFrame(this);
    categoryCard->setObjectName(QStringLiteral("card"));
    auto* categoryLayout = new QVBoxLayout(categoryCard);
    categoryLayout->setContentsMargins(24, 22, 24, 24);
    categoryLayout->setSpacing(14);

    auto* categoryHeader = new QHBoxLayout;
    auto* categoryTitle = new QLabel(QStringLiteral("Категория одежды"), categoryCard);
    categoryTitle->setObjectName(QStringLiteral("sectionTitle"));
    categoryHeader->addWidget(categoryTitle);
    categoryHeader->addStretch(1);
    m_deleteButton = new QPushButton(QStringLiteral("Удалить выбранное"), categoryCard);
    m_deleteButton->setObjectName(QStringLiteral("dangerButton"));
    m_deleteButton->setEnabled(false);
    m_deleteButton->setToolTip(QStringLiteral("Удаление доступно после выбора элемента"));
    categoryHeader->addWidget(m_deleteButton);
    categoryLayout->addLayout(categoryHeader);

    m_categorySelector = new SplitActionButton(
        QStringLiteral("ЖЕНСКАЯ ОДЕЖДА"),
        QStringLiteral("МУЖСКАЯ ОДЕЖДА"),
        categoryCard);
    categoryLayout->addWidget(m_categorySelector);

    auto* categoryMeta = new QHBoxLayout;
    m_categoryDescription = new QLabel(categoryCard);
    m_categoryDescription->setObjectName(QStringLiteral("mutedLabel"));
    m_womenCountLabel = new QLabel(QStringLiteral("0 элементов"), categoryCard);
    m_menCountLabel = new QLabel(QStringLiteral("0 элементов"), categoryCard);
    m_womenCountLabel->setObjectName(QStringLiteral("countBadge"));
    m_menCountLabel->setObjectName(QStringLiteral("countBadge"));
    categoryMeta->addWidget(m_categoryDescription, 1);
    categoryMeta->addWidget(m_womenCountLabel);
    categoryMeta->addWidget(m_menCountLabel);
    categoryLayout->addLayout(categoryMeta);
    pageLayout->addWidget(categoryCard);

    m_emptyState = new QFrame(this);
    m_emptyState->setObjectName(QStringLiteral("emptyState"));
    auto* emptyLayout = new QVBoxLayout(m_emptyState);
    emptyLayout->setContentsMargins(36, 42, 36, 42);
    emptyLayout->setSpacing(12);

    auto* emptyTitle = new QLabel(QStringLiteral("Одежда не добавлена"), m_emptyState);
    emptyTitle->setObjectName(QStringLiteral("emptyTitle"));
    emptyTitle->setAlignment(Qt::AlignCenter);
    emptyLayout->addWidget(emptyTitle);

    auto* emptyText = new QLabel(
        QStringLiteral("Здесь появятся выбранные предметы одежды.\n"
                       "Импорт и чтение GTA-форматов пока не перенесены из WPF-версии."),
        m_emptyState);
    emptyText->setObjectName(QStringLiteral("emptyText"));
    emptyText->setAlignment(Qt::AlignCenter);
    emptyText->setWordWrap(true);
    emptyLayout->addWidget(emptyText);

    auto* emptyHint = new QLabel(
        QStringLiteral("Подсказка: выберите папку проекта в верхней панели, "
                       "когда будете готовы добавить ресурсы."),
        m_emptyState);
    emptyHint->setObjectName(QStringLiteral("emptyHint"));
    emptyHint->setAlignment(Qt::AlignCenter);
    emptyHint->setWordWrap(true);
    emptyLayout->addWidget(emptyHint);
    pageLayout->addWidget(m_emptyState, 1);

    m_itemsList = new QListWidget(this);
    m_itemsList->setObjectName(QStringLiteral("itemsList"));
    m_itemsList->setSelectionMode(QAbstractItemView::SingleSelection);
    m_itemsList->setVisible(false);
    pageLayout->addWidget(m_itemsList, 1);

    connect(m_chooseFolderAction, &QAction::triggered,
            this, &EditorPage::chooseProjectFolder);
    connect(m_categorySelector, &SplitActionButton::currentIndexChanged,
            this, &EditorPage::categoryChanged);
    connect(m_deleteButton, &QPushButton::clicked,
            this, &EditorPage::deleteSelectedItem);
    connect(m_itemsList, &QListWidget::itemSelectionChanged,
            this, &EditorPage::updateEmptyState);
    connect(m_itemsList, &QListWidget::itemDoubleClicked,
            this, [this](QListWidgetItem*) {
                // The MVP has no garment model yet; double-click is intentionally inert.
            });

    categoryChanged(m_categorySelector->currentIndex());
    updateEmptyState();
}

QString EditorPage::projectPath() const
{
    return m_projectPath;
}

void EditorPage::setProjectPath(const QString& path)
{
    m_projectPath = QDir::cleanPath(path.trimmed());
    if (m_projectPath.isEmpty() || m_projectPath == QLatin1String(".")) {
        m_projectPath.clear();
        m_projectPathLabel->setText(QStringLiteral("не выбрана"));
    } else {
        m_projectPathLabel->setText(QDir::toNativeSeparators(m_projectPath));
    }
    m_projectPathLabel->setToolTip(m_projectPath);
    emit projectFolderChanged(m_projectPath);
}

void EditorPage::chooseProjectFolder()
{
    const QString initialPath = m_projectPath.isEmpty()
        ? m_settingsStore.projectsFolder()
        : m_projectPath;
    const QString path = QFileDialog::getExistingDirectory(
        this,
        QStringLiteral("Выбрать папку проекта"),
        initialPath,
        QFileDialog::ShowDirsOnly | QFileDialog::DontResolveSymlinks);
    if (!path.isEmpty()) {
        setProjectPath(path);
    }
}

void EditorPage::deleteSelectedItem()
{
    if (m_itemsList == nullptr || m_itemsList->currentRow() < 0) {
        return;
    }

    const QMessageBox::StandardButton answer = QMessageBox::question(
        this,
        QStringLiteral("Удалить предмет?"),
        QStringLiteral("Удалить выбранный предмет из текущего проекта?"),
        QMessageBox::Yes | QMessageBox::No,
        QMessageBox::No);
    if (answer == QMessageBox::Yes) {
        delete m_itemsList->takeItem(m_itemsList->currentRow());
        updateEmptyState();
    }
}

void EditorPage::updateEmptyState()
{
    const bool isEmpty = m_itemsList == nullptr || m_itemsList->count() == 0;
    if (m_emptyState != nullptr) {
        m_emptyState->setVisible(isEmpty);
    }
    if (m_itemsList != nullptr) {
        m_itemsList->setVisible(!isEmpty);
        m_deleteButton->setEnabled(!isEmpty && m_itemsList->currentItem() != nullptr);
    }
}

void EditorPage::categoryChanged(int index)
{
    if (m_categoryDescription == nullptr) {
        return;
    }

    if (index == 0) {
        m_categoryDescription->setText(QStringLiteral("Женская одежда"));
    } else {
        m_categoryDescription->setText(QStringLiteral("Мужская одежда"));
    }
    updateEmptyState();
}
