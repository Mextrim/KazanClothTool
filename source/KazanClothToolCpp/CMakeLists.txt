cmake_minimum_required(VERSION 3.21)

project(KazanClothToolCpp
    VERSION 0.1.0
    DESCRIPTION "Kazan Cloth Tool - parallel Qt 6 MVP"
    LANGUAGES CXX
)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

set(CMAKE_AUTOMOC ON)
set(CMAKE_AUTOUIC ON)
set(CMAKE_AUTORCC ON)

find_package(Qt6 6.2 REQUIRED COMPONENTS Widgets)

qt_add_executable(KazanClothToolCpp WIN32
    src/main.cpp

    src/MainWindow.h
    src/MainWindow.cpp

    src/pages/HomePage.h
    src/pages/HomePage.cpp
    src/pages/EditorPage.h
    src/pages/EditorPage.cpp
    src/pages/SettingsPage.h
    src/pages/SettingsPage.cpp

    src/services/SettingsStore.h
    src/services/SettingsStore.cpp
    src/services/ThemeManager.h
    src/services/ThemeManager.cpp
    src/services/ProjectStore.h
    src/services/ProjectStore.cpp

    src/widgets/SplitActionButton.h
    src/widgets/SplitActionButton.cpp

    resources/app.qrc
)

target_include_directories(KazanClothToolCpp PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}/src
)

target_link_libraries(KazanClothToolCpp PRIVATE
    Qt6::Widgets
)

if (MSVC)
    target_compile_options(KazanClothToolCpp PRIVATE /W4 /permissive- /utf-8)
else()
    target_compile_options(KazanClothToolCpp PRIVATE -Wall -Wextra -pedantic)
endif()

set_target_properties(KazanClothToolCpp PROPERTIES
    WIN32_EXECUTABLE TRUE
    OUTPUT_NAME "KazanClothToolCpp"
)
