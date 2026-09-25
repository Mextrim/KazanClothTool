#pragma once

#include <QString>
#include <QWidget>

class SettingsStore;
class ThemeManager;
class QComboBox;
class QLabel;
class QLineEdit;

/** Theme and project-folder preferences. */
class SettingsPage final : public QWidget
{
    Q_OBJECT

public:
    SettingsPage(SettingsStore& settingsStore,
                 ThemeManager& themeManager,
                 QWidget* parent = nullptr);

private slots:
    void chooseProjectsFolder();
    void themeSelectionChanged(int index);
    void languageSelectionChanged(int index);

private:
    void showStatus(const QString& text, bool isError = false);

    SettingsStore& m_settingsStore;
    ThemeManager& m_themeManager;
    QComboBox* m_themeCombo = nullptr;
    QComboBox* m_languageCombo = nullptr;
    QLineEdit* m_projectsFolderEdit = nullptr;
    QLabel* m_statusLabel = nullptr;
};
