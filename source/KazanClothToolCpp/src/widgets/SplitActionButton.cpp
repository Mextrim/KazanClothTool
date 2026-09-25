#include "SplitActionButton.h"

#include <QButtonGroup>
#include <QHBoxLayout>
#include <QPushButton>
#include <QSizePolicy>

SplitActionButton::SplitActionButton(const QString& leftText,
                                     const QString& rightText,
                                     QWidget* parent)
    : QWidget(parent)
    , m_leftButton(new QPushButton(leftText, this))
    , m_rightButton(new QPushButton(rightText, this))
    , m_buttonGroup(new QButtonGroup(this))
{
    setObjectName(QStringLiteral("splitActionButton"));
    setMinimumHeight(54);
    setSizePolicy(QSizePolicy::Expanding, QSizePolicy::Fixed);

    m_leftButton->setObjectName(QStringLiteral("splitLeft"));
    m_rightButton->setObjectName(QStringLiteral("splitRight"));
    m_leftButton->setCheckable(true);
    m_rightButton->setCheckable(true);
    m_leftButton->setToolTip(QStringLiteral("Показать женскую одежду"));
    m_rightButton->setToolTip(QStringLiteral("Показать мужскую одежду"));

    m_buttonGroup->setExclusive(true);
    m_buttonGroup->addButton(m_leftButton, 0);
    m_buttonGroup->addButton(m_rightButton, 1);

    auto* layout = new QHBoxLayout(this);
    layout->setContentsMargins(4, 4, 4, 4);
    layout->setSpacing(4);
    layout->addWidget(m_leftButton);
    layout->addWidget(m_rightButton);

    connect(m_buttonGroup, &QButtonGroup::idClicked,
            this, &SplitActionButton::setCurrentIndex);
    setCurrentIndex(0);
}

int SplitActionButton::currentIndex() const
{
    return m_currentIndex;
}

void SplitActionButton::setCurrentIndex(int index)
{
    if (index < 0 || index > 1) {
        return;
    }

    const bool changed = m_currentIndex != index;
    m_currentIndex = index;
    updateButtons();
    if (changed) {
        emit currentIndexChanged(index);
    }
}

void SplitActionButton::updateButtons()
{
    m_leftButton->setChecked(m_currentIndex == 0);
    m_rightButton->setChecked(m_currentIndex == 1);
}
