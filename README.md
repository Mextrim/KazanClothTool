<div align="center">

  <img src="https://raw.githubusercontent.com/Mextrim/KazanClothTool/main/source/grzyClothTool/Resources/KazanClothTool.png" alt="Kazan Cloth Tool" width="104">

  # ✂️ Kazan Cloth Tool

  ### Редактор одежды и текстур для GTA V

  **Просмотр моделей, редактирование свойств, проверка качества и сборка игровых ресурсов — в одном приложении.**

  <p>
    <a href="https://github.com/Mextrim/KazanClothTool/releases/download/v1.7.0/KazanClothTool-v1.7.0-win-x64.zip">
      <img alt="Скачать Kazan Cloth Tool v1.7.0" src="https://img.shields.io/badge/%E2%86%95%20%D0%A1%D0%BA%D0%B0%D1%87%D0%B0%D1%82%D1%8C%20v1.7.0-0072e5?style=for-the-badge&logo=windows&logoColor=white">
    </a>
    <a href="https://github.com/Mextrim/KazanClothTool/wiki">
      <img alt="Wiki" src="https://img.shields.io/badge/%F0%9F%93%9A%20Wiki-6f42c1?style=for-the-badge&logo=github&logoColor=white">
    </a>
    <a href="https://github.com/Mextrim/KazanClothTool/releases">
      <img alt="Все релизы" src="https://img.shields.io/github/v/release/Mextrim/KazanClothTool?label=Release&style=for-the-badge&logo=github&logoColor=white">
    </a>
    <a href="https://github.com/Mextrim/KazanClothTool/issues">
      <img alt="Issues" src="https://img.shields.io/github/issues/Mextrim/KazanClothTool?style=for-the-badge&logo=github&logoColor=white">
    </a>
  </p>

  <p>
    <img alt="Windows x64" src="https://img.shields.io/badge/Windows_x64-0d6efd?style=flat-square&logo=windows&logoColor=white">
    <img alt=".NET 10" src="https://img.shields.io/badge/.NET_10-512bd4?style=flat-square&logo=dotnet&logoColor=white">
    <img alt="WPF" src="https://img.shields.io/badge/WPF-68217A?style=flat-square">
    <img alt="Количество тем" src="https://img.shields.io/badge/15_%D1%82%D0%B5%D0%BC-6f42c1?style=flat-square">
    <img alt="Языки" src="https://img.shields.io/badge/3_%D1%8F%D0%B7%D1%8B%D0%BAa-e83e8c?style=flat-square">
    <img alt="Сборка" src="https://img.shields.io/badge/FiveM%20%C2%B7%20Alt%3AV%20%C2%B7%20SP-198754?style=flat-square">
    <img alt="Лицензия" src="https://img.shields.io/badge/GPL_v3-198754?style=flat-square">
  </p>

</div>

---

> [!TIP]
> ### 📖 Полная документация — в Wiki
> Здесь описаны только основы. **Подробное объяснение всех функций** — в [Wiki проекта](https://github.com/Mextrim/KazanClothTool/wiki):
> редактор, свойства одежды, текстуры, 3D-просмотр, сборка ресурсов, горячие клавиши, форматы файлов и решение проблем.

**Поддерживаемые платформы:** GTA V · FiveM · Rage MP · Alt:V · Singleplayer

---

## 📑 Содержание

<table>
<tr>
<td width="50%" valign="top">

- [✨ Возможности](#-возможности)
- [📸 Скриншоты](#-скриншоты)
- [🚀 Быстрый старт](#-быстрый-старт)
- [🧰 Типы сборки](#-типы-сборки)
- [🗂 Типы проектов](#-типы-проектов)

</td>
<td width="50%" valign="top">

- [⌨️ Горячие клавиши](#️-горячие-клавиши)
- [🧊 3D-просмотр](#-3d-просмотр)
- [🎨 Темы и языки](#-темы-и-языки)
- [⚙️ Настройки](#️-настройки)
- [📐 Ограничения](#-ограничения)
- [🛠 Сборка из исходников](#-сборка-из-исходников)

</td>
</tr>
</table>

---

## ✨ Возможности

<table>
<tr>
<td width="50%" valign="top">

### 🧥 Редактор одежды
Интерактивный 3D-просмотр и полное редактирование одежды.

- Группировка по **группам** и **тегам**
- Отдельные списки для мужской и женской одежды
- Массовый выбор, копирование, перемещение
- Переупорядочивание перетаскиванием с автонумерацией
- Дублирование вещи на противоположный пол
- **Зарезервированные слоты** — безопасная замена вещей по номеру

</td>
<td width="50%" valign="top">

### 🖼 Работа с текстурами
Полный цикл: от `.png` до готовой `.ytd` в ресурсе.

- Импорт `.ytd`, `.dds`, `.png`, `.jpg`
- **Встроенные текстуры** (normal, specular) прямо внутри модели
- Оптимизация размера и сжатия **при сборке**
- Экспорт текстур в `DDS`, `PNG`, `YTD`
- Массовое действие над выделенными текстурами

</td>
</tr>
<tr>
<td valign="top">

### 🧪 Контроль качества
Предупреждения до того, как игрок увидит проблему.

- Проверка **полигонов** по уровням LOD
- Проверка **разрешения** текстур (diffuse / normal / specular)
- Проверка степени двойки, числа mip-уровней, сжатия
- Подсветка проблемных вещей в списке

</td>
<td valign="top">

### 🧩 Инспектор дубликатов
Находит одинаковые вещи и помогает их убрать.

- Группировка по «сигнатуре» модели
- Удаление всех дублей кроме первого
- Переход к вещи прямо из инспектора

</td>
</tr>
<tr>
<td valign="top">

### 📦 Сборка ресурсов
Три готовых формата из одного проекта.

- **FiveM** — `fxmanifest.lua` + `stream/`
- **Alt:V** — `resource.toml` + `stream/*.rpf`
- **Singleplayer** — `dlc.rpf` + `content.xml` + `setup2.xml`
- Разделение аддонов по отдельным ресурсам
- Автоматическая генерация `.meta` и `creaturemetadata`

</td>
<td valign="top">

### 🗂 Проекты и перенос
- **Автономные** проекты (файлы копируются внутрь)
- **Внешние** проекты (файлы остаются на месте)
- Автосохранение каждую минуту
- Импорт и экспорт в `.kctproject`
- Резервные копии и откат при неудачном импорте

</td>
</tr>
<tr>
<td valign="top">

### 🧊 3D-просмотр
Живая модель сразу при выборе вещи.

- Вращение мышью, масштаб колесом
- Автоматическая смена модели персонажа
- Экспорт всех текстур в PNG
- Управление каблуками и масштабом волос

</td>
<td valign="top">

### 🎨 Интерфейс
- **18 тем** оформления
- **3 языка**: русский, украинский, английский
- Смена темы и языка мгновенно, без перезапуска
- Журнал событий в приложении

</td>
</tr>
</table>

---

## 📸 Скриншоты

<p align="center"><sub>← Прокрутите вправо, чтобы посмотреть все скриншоты →</sub></p>

<table cellpadding="8">
  <tr>
    <td width="760" align="center" nowrap>
      <a href="https://github.com/user-attachments/assets/a9d6f69f-3ea7-4658-b69a-77aac6dc61d2">
        <img src="https://github.com/user-attachments/assets/a9d6f69f-3ea7-4658-b69a-77aac6dc61d2" alt="Kazan Cloth Tool — скриншот 1" width="760">
      </a>
    </td>
    <td width="760" align="center" nowrap>
      <a href="https://github.com/user-attachments/assets/b585e76d-776b-43dd-b02d-4f08fdea815d">
        <img src="https://github.com/user-attachments/assets/b585e76d-776b-43dd-b02d-4f08fdea815d" alt="Kazan Cloth Tool — скриншот 2" width="760">
      </a>
    </td>
    <td width="760" align="center" nowrap>
      <a href="https://github.com/user-attachments/assets/cb28051a-7611-4891-8878-31dfdf69b814">
        <img src="https://github.com/user-attachments/assets/cb28051a-7611-4891-8878-31dfdf69b814" alt="Kazan Cloth Tool — скриншот 3" width="760">
      </a>
    </td>
    <td width="760" align="center" nowrap>
      <a href="https://github.com/user-attachments/assets/142de650-afe2-4490-ad4e-0665a7ac3a2e">
        <img src="https://github.com/user-attachments/assets/142de650-afe2-4490-ad4e-0665a7ac3a2e" alt="Kazan Cloth Tool — скриншот 4" width="760">
      </a>
    </td>
  </tr>
</table>

<details>
<summary><strong>🖥 Интерактивная страница проекта (Bootstrap 5)</strong></summary>

GitHub не выполняет JavaScript и CSS внутри `README.md`, поэтому полная интерактивная версия — на отдельной странице: [`docs/index.html`](docs/index.html).

[Открыть страницу проекта](https://mextrim.github.io/KazanClothTool/) · [исходник страницы](docs/index.html)

<iframe src="https://mextrim.github.io/KazanClothTool/" width="100%" height="640" style="border:1px solid #e4e8ee;border-radius:16px" title="Kazan Cloth Tool — страница проекта" loading="lazy"></iframe>

<p align="center">
  <sub>Если фрейм не отображается — <a href="https://mextrim.github.io/KazanClothTool/">откройте страницу проекта</a> напрямую.</sub>
</p>

</details>

---

## 🚀 Быстрый старт

> [!NOTE]
> **Portable-сборка для Windows x64 — установка не требуется.**
> Скачайте архив, распакуйте его в удобную папку и запустите приложение.

<table>
<tr>
<td width="60%" valign="top">

1. **Скачайте** portable-сборку из [GitHub Releases](https://github.com/Mextrim/KazanClothTool/releases).
2. **Распакуйте** архив в любую папку на компьютере.
3. **Запустите** `KazanClothTool.exe`.
4. При первом запуске укажите **папку проектов** — её можно поменять позже в настройках.

</td>
<td width="40%" align="center" valign="middle">

<a href="https://github.com/Mextrim/KazanClothTool/releases/download/v1.7.0/KazanClothTool-v1.7.0-win-x64.zip">
  <img alt="Скачать Kazan Cloth Tool v1.7.0" src="https://img.shields.io/badge/%E2%86%95%20%D0%A1%D0%BA%D0%B0%D1%87%D0%B0%D1%82%D1%8C%20v1.7.0-0072e5?style=for-the-badge&logo=windows&logoColor=white">
</a>

<br><br>

`~91 МБ` · self-contained · `.NET 10`

</td>
</tr>
</table>

Готовая сборка находится в папке `KazanClothTool`:

```text
KazanClothTool\
└── KazanClothTool.exe
```

### 🛠 Три способа начать работу

<table>
<tr>
<td width="33%" valign="top">

**🆕 Новый проект**

Создайте пустой проект и добавьте `.ydd` вручную.

*Подходит, если вы верстаете одежду с нуля.*

</td>
<td width="33%" valign="top">

**📥 Открыть набор одежды**

Укажите `.meta`-файлы аддона — программа сама разберёт структуру.

*Подходит, если уже есть готовый аддон.*

</td>
<td width="33%" valign="top">

**📤 Импорт проекта**

Откройте `.kctproject`, полученный от другого автора.

*Подходит, чтобы продолжить чужую работу.*

</td>
</tr>
</table>

---

## 🧰 Типы сборки

Один и тот же проект собирается под три платформы. Тип выбирается в окне сборки.

<table>
<tr>
<th width="14%">Платформа</th>
<th width="30%">Что получится</th>
<th width="28%">Ключевые файлы</th>
<th>Особенности</th>
</tr>
<tr>
<td valign="top"><strong>FiveM</strong></td>
<td valign="top">Папка ресурса, готовая к запуску в `resources/`</td>
<td valign="top"><code>fxmanifest.lua</code><br><code>stream/*.ymt</code><br><code>*.meta</code></td>
<td valign="top">Поддерживает <strong>группы</strong> (вложенные папки), файлы от первого лица (<code>.yld</code> и <code>_1.ydd</code>), метаданные каблуков и волос</td>
</tr>
<tr>
<td valign="top"><strong>Alt:V</strong></td>
<td valign="top">Ресурс с `resource.toml`</td>
<td valign="top"><code>resource.toml</code><br><code>stream.toml</code><br><code>stream/*_male.rpf</code></td>
<td valign="top">Расширение текстур <strong>сохраняется как есть</strong>; группы не используются; файлы физики и от первого лица не переносятся</td>
</tr>
<tr>
<td valign="top"><strong>Singleplayer</strong></td>
<td valign="top">Готовый <code>dlc.rpf</code> для папки <code>mods</code></td>
<td valign="top"><code>dlc.rpf</code><br><code>content.xml</code><br><code>setup2.xml</code></td>
<td valign="top">Создаётся настоящий RPF-архив. Рекомендуется <strong>один аддон на сборку</strong></td>
</tr>
</table>

> [!WARNING]
> Папка вывода должна быть **пустой** либо уже быть папкой сборки Kazan Cloth Tool — программа откажется очищать чужую непустую папку. Это защита от потери ваших файлов.

Подробности, точные имена файлов и структура папок — в [Wiki: Сборка ресурсов](https://github.com/Mextrim/KazanClothTool/wiki/Сборка-ресурсов).

---

## 🗂 Типы проектов

<table>
<tr>
<th width="20%">Тип</th>
<th width="40%">Автономный</th>
<th>Внешний</th>
</tr>
<tr>
<td valign="top"><strong>Что происходит с файлами</strong></td>
<td valign="top">Копируются в <code>project_assets</code> внутри проекта</td>
<td valign="top">Остаются в исходных папках, пути сохраняются как есть</td>
</tr>
<tr>
<td valign="top"><strong>Можно перенести</strong></td>
<td valign="top">✅ Да — проект переносится целиком</td>
<td valign="top">❌ Нет — исходные файлы нельзя перемещать и удалять</td>
</tr>
<tr>
<td valign="top"><strong>Расход места</strong></td>
<td valign="top">Больше — дублируются</td>
<td valign="top">Меньше — копий нет</td>
</tr>
<tr>
<td valign="top"><strong>Файл проекта</strong></td>
<td valign="top"><code>autosave.json</code></td>
<td valign="top"><code>autosave.external.json</code></td>
</tr>
<tr>
<td valign="top"><strong>Когда выбирать</strong></td>
<td valign="top">Проект передают коллегам, архивируют, переносят на другой ПК</td>
<td valign="top">Много вещей, важно экономить место, исходники под контролем</td>
</tr>
</table>

> [!TIP]
> При **импорте** `.kctproject` всегда создаётся автономный проект. При **открытии набора одежды** по умолчанию предлагается внешний.

---

## ⌨️ Горячие клавиши

<table>
<tr>
<th width="34%">Комбинация</th>
<th>Действие</th>
</tr>
<tr>
<td><code>Ctrl</code> + <code>S</code></td>
<td>Сохранить проект</td>
</tr>
<tr>
<td><code>Delete</code></td>
<td>Удалить выделенное — с выбором: <strong>Удалить</strong> / <strong>Заменить</strong> / <strong>Отмена</strong></td>
</tr>
<tr>
<td><code>Shift</code> + <code>Delete</code></td>
<td>Удалить выделенное сразу, без подтверждения</td>
</tr>
<tr>
<td><code>Ctrl</code> + <code>Delete</code></td>
<td>Заменить на зарезервированный слот, сохранив нумерацию</td>
</tr>
<tr>
<td><code>Ctrl</code> + <code>C</code></td>
<td>Скопировать текст сообщения из диалогового окна</td>
</tr>
<tr>
<td>Двойной клик по строке статуса</td>
<td>Открыть журнал событий</td>
</tr>
<tr>
<td>Двойной клик по вещи в списке</td>
<td>Показать модель в 3D-просмотре</td>
</tr>
<tr>
<td>Мышь <code>◂</code> колесо <code>▸</code> в 3D</td>
<td>Вращение и масштаб модели</td>
</tr>
</table>

---

## 🧊 3D-просмотр

> [!IMPORTANT]
> Для 3D-просмотра нужно один раз указать **папку установки GTA V**.
> Это необязательно: **сборка ресурсов работает и без неё**.

1. Откройте **Настройки → Рабочие папки → Папка GTA V → Изменить**.
2. Укажите папку, в которой лежит **`GTA5.exe`**.
3. Вернитесь в редактор и нажмите **«3D-ПРОСМОТР»**.

Модель персонажа переключается сама: мужская одежда — `mp_m_freemode_01`, женская — `mp_f_freemode_01`.

Зашифрованные вещи в 3D не отображаются — это ограничение исходных файлов.

---

## 🎨 Темы и языки

**18 тем** — переключаются мгновенно, выбор сохраняется автоматически.

<table>
<tr>
<td width="33%" valign="top">

**Светлые**

- Bootstrap 5 *(по умолчанию)*
- **Material UI** *(новинка)*
- **Horizon UI**
- Чистая (Purity UI)
- Светлая
- Песок
- Роза
- Моно

</td>
<td width="33%" valign="top">

**Тёмные**

- **DesignCode**
- Графит
- Material Design
- Океан
- Закат
- Лес
- Неон

</td>
<td width="33%" valign="top">

**Акцентные**

- Аврора
- Рубин
- Кобальт

<br>

*Тема Bootstrap 5*: синий `#0d6efd`, серый `#6c757d`, плоские карточки, радиус `0.375rem`.
<br>
*Тема Material UI*: официальный MUI — синий `#0072e5`, фиолетовый `#a259ff`, холст `#f3f6f9`, формы Material 3.
<br><br>
*Тема Horizon UI*: Soft UI дашборд, индиго `#4318ff`, голубой `#36bffa`, фон `#f4f7fe`, крупные скругления и мягкие тени.
<br><br>
*Тема DesignCode*: почти чёрный индиго `#050715`, карточки `#120d27`, ледяной голубой `#9ed0ee`, акцент `#2f6bff`.

</td>
</tr>
</table>

**3 языка интерфейса:** русский · українська · English.
Переключение мгновенно переводит открытые окна, без перезапуска.

---

## ⚙️ Настройки

Настройки хранятся в отдельном файле и не затрагивают проекты:

```text
%LOCALAPPDATA%\KazanClothTool\settings.json
```

<table>
<tr>
<th width="34%">Параметр</th>
<th>Значение</th>
</tr>
<tr>
<td>🌐 <strong>Язык</strong></td>
<td>Русский, украинский, английский</td>
</tr>
<tr>
<td>🎨 <strong>Тема</strong></td>
<td>18 тем оформления, сохраняется автоматически</td>
</tr>
<tr>
<td>📁 <strong>Папка проектов</strong></td>
<td>Общая папка для всех проектов и автосохранений</td>
</tr>
<tr>
<td>🎮 <strong>Папка GTA V</strong></td>
<td>Нужна только для 3D-просмотра</td>
</tr>
<tr>
<td>🔺 <strong>Лимит полигонов</strong></td>
<td>Высокий / средний / низкий LOD — по умолчанию <code>35000 / 20000 / 10000</code></td>
</tr>
<tr>
<td>🖼 <strong>Макс. разрешение текстур</strong></td>
<td>Diffuse, normal, specular — по умолчанию <code>1024</code></td>
</tr>
<tr>
<td>🏷 <strong>Помечать новые вещи</strong></td>
<td>Показывать бейдж «НОВ» у добавленных вещей</td>
</tr>
<tr>
<td>📄 <strong>Показывать путь к вещи</strong></td>
<td>Выводить путь к файлу в панели свойств</td>
</tr>
<tr>
<td>🗑 <strong>Автоудаление файлов</strong></td>
<td>Удалять исходные файлы вместе с вещью. Включайте осознанно!</td>
</tr>
</table>

Изменение лимитов сразу перепроверяет все вещи и текстуры текущего проекта.

> [!NOTE]
> Пользовательские проекты и автосохранения **не удаляются** при очистке папки сборки.

Полный список параметров — в [Wiki: Настройки](https://github.com/Mextrim/KazanClothTool/wiki/Настройки).

---

## 📐 Ограничения

<table>
<tr>
<th width="46%">Параметр</th>
<th>Значение</th>
</tr>
<tr>
<td>Вещей одного типа в одном аддоне</td>
<td><strong>128</strong> (0–127)</td>
</tr>
<tr>
<td>Текстур на одну вещь</td>
<td><strong>26</strong> (слоты <code>a</code>…<code>z</code>)</td>
</tr>
<tr>
<td>Слотов одежды (компоненты)</td>
<td>12: <code>head</code>, <code>berd</code>, <code>hair</code>, <code>uppr</code>, <code>lowr</code>, <code>hand</code>, <code>feet</code>, <code>teef</code>, <code>accs</code>, <code>task</code>, <code>decl</code>, <code>jbib</code></td>
</tr>
<tr>
<td>Пропов (аксессуары)</td>
<td>13: <code>p_head</code> … <code>p_ph_r_hand</code></td>
</tr>
<tr>
<td>Частота автосохранения</td>
<td>каждые <strong>60 секунд</strong> после изменений</td>
</tr>
<tr>
<td>Недавних проектов в списке</td>
<td>до <strong>6</strong></td>
</tr>
<tr>
<td>Имя проекта</td>
<td>3–50 символов, только <code>a-z</code>, <code>0-9</code>, <code>_</code></td>
</tr>
</table>

---

## 🧰 Платформы и форматы

<p align="center">
  <img alt="GTA V" src="https://img.shields.io/badge/GTA%20V-0d6efd?style=for-the-badge">
  <img alt="FiveM" src="https://img.shields.io/badge/FiveM-198754?style=for-the-badge">
  <img alt="Rage MP" src="https://img.shields.io/badge/Rage%20MP-6f42c1?style=for-the-badge">
  <img alt="Alt:V" src="https://img.shields.io/badge/Alt%3AV-e83e8c?style=for-the-badge">
  <img alt="Singleplayer" src="https://img.shields.io/badge/Singleplayer-6c757d?style=for-the-badge">
</p>

| Формат | Назначение |
| --- | --- |
| `.ydd` | Модель одежды |
| `.ytd` | Текстуры одежды |
| `.ymt` | Описание набора одежды |
| `.yld` | Физика ткани (cloth simulation) |
| `.meta` | Привязка аддона к модели персонажа |
| `.dds`, `.png`, `.jpg` | Работа с изображениями и текстурами |
| `.kctproject` | Переносимый архив проекта |
| `.json` | Файл проекта (`autosave.json`) |

---

## 🛠 Сборка из исходников

<details>
<summary><strong>Развернуть инструкцию для разработчиков</strong></summary>

Для сборки WPF-приложения требуется **Windows** и **.NET 10 SDK**.

```powershell
dotnet restore .\source\grzyClothTool.sln
dotnet build .\source\grzyClothTool.sln -c Release
dotnet publish .\source\grzyClothTool\grzyClothTool.csproj -c Release -r win-x64 --self-contained true
```

### Структура репозитория

| Каталог | Назначение |
| --- | --- |
| [`KazanClothTool`](KazanClothTool) | Portable-сборка приложения для Windows x64 |
| [`source/grzyClothTool`](source/grzyClothTool) | Основное WPF-приложение |
| [`source/grzyClothTool.Shared`](source/grzyClothTool.Shared) | Общий код и контракты |
| [`source/CodeWalker`](source/CodeWalker) | Работа с 3D-моделями, RPF-архивами и ресурсами GTA |
| [`source/KazanClothToolCpp`](source/KazanClothToolCpp) | Параллельный C++/Qt MVP |
| [`docs`](docs) | Страница проекта для GitHub Pages |

Подробное описание исходников — в [source/README.md](source/README.md).

</details>

---

## 💬 Обратная связь

Нашли ошибку или хотите предложить улучшение? Будем рады вашим идеям.

<table>
<tr>
<td width="50%" align="center">

[**Сообщить об ошибке**](https://github.com/Mextrim/KazanClothTool/issues/new)

</td>
<td width="50%" align="center">

[**Задать вопрос**](https://github.com/Mextrim/KazanClothTool/issues)

</td>
</tr>
</table>

---

## 📄 Лицензия

Исходный код распространяется по лицензии [GNU General Public License v3.0](source/LICENSE).

Проект является форком [grzyClothTool](https://github.com/grzybeek/grzyClothTool) © [grzybeek](https://github.com/grzybeek) — оригинальный автор. Поскольку оригинал распространяется по GPL-3.0, этот форк и все модифицированные сборки также остаются под GPL-3.0.

---

## 👥 Авторы и оформление

© 2026 **MeX** · Программное обеспечение переделал **MeX** под чутким руководством **Evelentdev**

Интерфейс использует иконки **Font Awesome** через **FontAwesome.Sharp**.

---

<div align="center">
  <sub>Сделано для удобной работы с одеждой и ресурсами GTA V</sub>
</div>
