#pragma once

#include <QString>
#include <QWidget>

class QButtonGroup;
class QPushButton;

/** A two-sided selector with independent, checkable actions. */
class SplitActionButton final : public QWidget
{
    Q_OBJECT

public:
    explicit SplitActionButton(const QString& leftText,
                               const QString& rightText,
                               QWidget* parent = nullptr);

    int currentIndex() const;
    void setCurrentIndex(int index);

signals:
    void currentIndexChanged(int index);

private:
    void updateButtons();

    QPushButton* m_leftButton = nullptr;
    QPushButton* m_rightButton = nullptr;
    QButtonGroup* m_buttonGroup = nullptr;
    int m_currentIndex = -1;
};
