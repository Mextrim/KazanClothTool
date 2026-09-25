#pragma once

#include <QString>
#include <QWidget>

class SettingsStore;
class SplitActionButton;
class QFrame;
class QAction;
class QLabel;
class QListWidget;
class QPushButton;
class QToolButton;

/** MVP editor surface with a category selector and an intentional empty state. */
class EditorPage final : public QWidget
{
    Q_OBJECT

public:
    explicit EditorPage(SettingsStore& settingsStore, QWidget* parent = nullptr);

    QString projectPath() const;
    void setProjectPath(const QString& path);

signals:
    void projectFolderChanged(const QString& path);

private slots:
    void chooseProjectFolder();
    void deleteSelectedItem();
    void updateEmptyState();
    void categoryChanged(int index);

private:
    SettingsStore& m_settingsStore;
    SplitActionButton* m_categorySelector = nullptr;
    QToolButton* m_folderButton = nullptr;
    QAction* m_chooseFolderAction = nullptr;
    QLabel* m_projectPathLabel = nullptr;
    QLabel* m_categoryDescription = nullptr;
    QLabel* m_womenCountLabel = nullptr;
    QLabel* m_menCountLabel = nullptr;
    QListWidget* m_itemsList = nullptr;
    QFrame* m_emptyState = nullptr;
    QPushButton* m_deleteButton = nullptr;
    QString m_projectPath;
};
