#include "SettingsPage.h"

#include "../services/SettingsStore.h"
#include "../services/ThemeManager.h"

#include <QComboBox>
#include <QDir>
#include <QFileDialog>
#include <QFrame>
#include <QHBoxLayout>
#include <QLabel>
#include <QLineEdit>
#include <QPushButton>
#include <QStyle>
#include <QVBoxLayout>

SettingsPage::SettingsPage(SettingsStore& settingsStore,
                           ThemeManager& themeManager,
                           QWidget* parent)
    : QWidget(parent)
    , m_settingsStore(settingsStore)
    , m_themeManager(themeManager)
{
    setObjectName(QStringLiteral("settingsPage"));

    auto* pageLayout = new QVBoxLayout(this);
    pageLayout->setContentsMargins(48, 42, 48, 40);
    pageLayout->setSpacing(24);

    auto* title = new QLabel(QStringLiteral("Настройки"), this);
    title->setObjectName(QStringLiteral("pageTitle"));
    pageLayout->addWidget(title);

    auto* subtitle = new QLabel(
        QStringLiteral("Персонализируйте рабочее пространство и выберите папку для новых проектов."),
        this);
    subtitle->setObjectName(QStringLiteral("pageSubtitle"));
    subtitle->setWordWrap(true);
    pageLayout->addWidget(subtitle);

    auto* appearanceCard = new QFrame(this);
    appearanceCard->setObjectName(QStringLiteral("card"));
    auto* appearanceLayout = new QVBoxLayout(appearanceCard);
    appearanceLayout->setContentsMargins(24, 24, 24, 24);
    appearanceLayout->setSpacing(12);

    auto* appearanceTitle = new QLabel(QStringLiteral("Оформление"), appearanceCard);
    appearanceTitle->setObjectName(QStringLiteral("sectionTitle"));
    appearanceLayout->addWidget(appearanceTitle);

    auto* themeRow = new QHBoxLayout;
    auto* themeLabel = new QLabel(QStringLiteral("Тема"), appearanceCard);
    themeLabel->setObjectName(QStringLiteral("mutedLabel"));
    m_themeCombo = new QComboBox(appearanceCard);
    m_themeCombo->setObjectName(QStringLiteral("themeCombo"));
    m_themeCombo->setMinimumWidth(240);
    for (const QString& theme : m_themeManager.availableThemes()) {
        m_themeCombo->addItem(theme, theme);
    }
    const int currentThemeIndex = m_themeCombo->findData(m_themeManager.currentTheme());
    m_themeCombo->setCurrentIndex(currentThemeIndex >= 0 ? currentThemeIndex : 0);
    themeRow->addWidget(themeLabel);
    themeRow->addWidget(m_themeCombo, 1);
    appearanceLayout->addLayout(themeRow);

    auto* themeHint = new QLabel(
        QStringLiteral("Доступные темы: Dark, Light, Ocean, Sunset, Forest, Neon, Aurora, Ruby, Cobalt, Sand, Rose и Mono."),
        appearanceCard);
    themeHint->setObjectName(QStringLiteral("mutedLabel"));
    appearanceLayout->addWidget(themeHint);

    auto* languageRow = new QHBoxLayout;
    auto* languageLabel = new QLabel(QStringLiteral("Язык интерфейса"), appearanceCard);
    languageLabel->setObjectName(QStringLiteral("mutedLabel"));
    m_languageCombo = new QComboBox(appearanceCard);
    m_languageCombo->setObjectName(QStringLiteral("languageCombo"));
    m_languageCombo->setMinimumWidth(240);
    m_languageCombo->addItem(QStringLiteral("Русский"), QStringLiteral("ru"));
    m_languageCombo->addItem(QStringLiteral("Українська"), QStringLiteral("uk"));
    m_languageCombo->addItem(QStringLiteral("English"), QStringLiteral("en"));
    const int currentLanguageIndex = m_languageCombo->findData(m_settingsStore.language());
    m_languageCombo->setCurrentIndex(currentLanguageIndex >= 0 ? currentLanguageIndex : 0);
    languageRow->addWidget(languageLabel);
    languageRow->addWidget(m_languageCombo, 1);
    appearanceLayout->addLayout(languageRow);

    pageLayout->addWidget(appearanceCard);

    auto* projectsCard = new QFrame(this);
    projectsCard->setObjectName(QStringLiteral("card"));
    auto* projectsLayout = new QVBoxLayout(projectsCard);
    projectsLayout->setContentsMargins(24, 24, 24, 24);
    projectsLayout->setSpacing(12);

    auto* projectsTitle = new QLabel(QStringLiteral("Папка проектов"), projectsCard);
    projectsTitle->setObjectName(QStringLiteral("sectionTitle"));
    projectsLayout->addWidget(projectsTitle);

    auto* projectsRow = new QHBoxLayout;
    m_projectsFolderEdit = new QLineEdit(m_settingsStore.projectsFolder(), projectsCard);
    m_projectsFolderEdit->setObjectName(QStringLiteral("projectsFolderEdit"));
    m_projectsFolderEdit->setReadOnly(true);
    m_projectsFolderEdit->setToolTip(QDir::toNativeSeparators(m_settingsStore.projectsFolder()));
    auto* chooseFolderButton = new QPushButton(QStringLiteral("Выбрать папку"), projectsCard);
    chooseFolderButton->setObjectName(QStringLiteral("secondaryButton"));
    projectsRow->addWidget(m_projectsFolderEdit, 1);
    projectsRow->addWidget(chooseFolderButton);
    projectsLayout->addLayout(projectsRow);

    auto* projectsHint = new QLabel(
        QStringLiteral("Новые проекты из Home будут создаваться в этой папке."),
        projectsCard);
    projectsHint->setObjectName(QStringLiteral("mutedLabel"));
    projectsHint->setWordWrap(true);
    projectsLayout->addWidget(projectsHint);
    pageLayout->addWidget(projectsCard);

    m_statusLabel = new QLabel(this);
    m_statusLabel->setObjectName(QStringLiteral("statusLabel"));
    m_statusLabel->setWordWrap(true);
    pageLayout->addWidget(m_statusLabel);
    pageLayout->addStretch(1);

    connect(chooseFolderButton, &QPushButton::clicked,
            this, &SettingsPage::chooseProjectsFolder);
    connect(m_themeCombo, &QComboBox::currentIndexChanged,
            this, &SettingsPage::themeSelectionChanged);
    connect(m_languageCombo, &QComboBox::currentIndexChanged,
            this, &SettingsPage::languageSelectionChanged);
}

void SettingsPage::chooseProjectsFolder()
{
    const QString path = QFileDialog::getExistingDirectory(
        this,
        QStringLiteral("Папка проектов"),
        m_settingsStore.projectsFolder(),
        QFileDialog::ShowDirsOnly | QFileDialog::DontResolveSymlinks);
    if (path.isEmpty()) {
        return;
    }

    m_settingsStore.setProjectsFolder(path);
    const bool saved = m_settingsStore.save();
    m_projectsFolderEdit->setText(m_settingsStore.projectsFolder());
    m_projectsFolderEdit->setToolTip(QDir::toNativeSeparators(m_settingsStore.projectsFolder()));
    showStatus(saved ? QStringLiteral("Папка проектов сохранена.")
                     : QStringLiteral("Не удалось сохранить settings.json."),
               !saved);
}

void SettingsPage::themeSelectionChanged(int index)
{
    if (index < 0 || m_themeCombo == nullptr) {
        return;
    }

    const QString theme = m_themeCombo->currentData().toString();
    if (m_themeManager.setTheme(theme)) {
        showStatus(QStringLiteral("Тема применена: %1").arg(theme));
    } else {
        showStatus(QStringLiteral("Не удалось применить тему."), true);
    }
}

void SettingsPage::languageSelectionChanged(int index)
{
    if (index < 0 || m_languageCombo == nullptr) {
        return;
    }

    m_settingsStore.setLanguage(m_languageCombo->currentData().toString());
    const bool saved = m_settingsStore.save();
    showStatus(saved ? QStringLiteral("Язык интерфейса сохранён.")
                     : QStringLiteral("Не удалось сохранить язык интерфейса."),
               !saved);
}

void SettingsPage::showStatus(const QString& text, bool isError)
{
    if (m_statusLabel == nullptr) {
        return;
    }
    m_statusLabel->setText(text);
    m_statusLabel->setProperty("isError", isError);
    m_statusLabel->style()->unpolish(m_statusLabel);
    m_statusLabel->style()->polish(m_statusLabel);
}
