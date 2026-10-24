# ValheimGreek

Клиентский мод для Valheim: подбор лежащего лута открывает короткую викторину по греческому. Остальная игра не меняется — бой, рубка, крафт, строительство, еда, сон, парус, сундуки, ягоды и могилы работают как в ваниле.

Словарь берётся из соседнего проекта `Greek3` (`../Greek3/words`). В игре показывается случайная карточка, не связанная с предметом.

## Как это выглядит

1. Автоподбор выключен: предметы остаются на земле.
2. Взаимодействие с кучей открывает окно: русский prompt (например «иду») и 4 греческих варианта.
3. Верный ответ подсвечивается зелёным, неверный — красным; выбранное слово озвучивается.
4. У каждой кучи свой счётчик ошибок. Первая попытка — 1 верный ответ. Ошибка увеличивает долг: следующая попытка требует уже на один верный подряд больше. Escape закрывает окно и долг не трогает.

Выключить мод можно в **Настройки → Геймплей → Греческая викторина**. После **ОК** лут снова ванильный, пока галочку не вернут.

Клавиши в окне: `1`–`4` или клик, `Esc` — оставить предмет.

## Установка

Нужны Valheim и [BepInEx 5.x](https://github.com/BepInEx/BepInEx).

Скопируйте в `BepInEx/plugins/ValheimGreek/`:

- `ValheimGreek.dll`
- `ValheimGreek.Core.dll`
- `dictionary.json`

Запускайте игру так, чтобы подхватился BepInEx (из Steam, не двойным кликом по `.app` на macOS).

### macOS

На Apple Silicon BepInEx 5 патчит игру через Rosetta. Скрипт `run_bepinex.sh` рядом с `valheim.app` должен запускать `arch -x86_64`. Hardened Runtime у приложения мешает инъекции: после обновления Steam может вернуть подпись, тогда мод снова не загрузится.

Озвучка — системный голос el-GR (обычно Melina). Без него варианты не произносятся.

### Steam Deck / Linux

Сборка x86_64, BepInEx ставится как обычно. Озвучки `say` / AVSpeech там нет.

## Сборка

Рядом с репозиторием ожидается `Greek3/words`. Скопируйте `Directory.Build.props.example` в `Directory.Build.props` и пропишите пути к Valheim, BepInEx и Greek3.

```bash
export PATH="$HOME/.dotnet:$PATH" DOTNET_ROOT="$HOME/.dotnet" DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet test tests/ValheimGreek.Core.Tests/ValheimGreek.Core.Tests.csproj -c Release
dotnet build src/ValheimGreek/ValheimGreek.csproj -c Release
```

После успешной сборки DLL и словарь копируются в `BepInEx/plugins/ValheimGreek/`. Игру нужно перезапустить из Steam.

Экспорт словаря вручную:

```bash
dotnet run --project src/ValheimGreek.Export -c Release -- /path/to/Greek3/words dictionary.json
```

## Конфиг

`BepInEx/config/com.valheimgreek.mod.cfg`

| Ключ | По умолчанию | Смысл |
|------|----------------|--------|
| `Enabled` | `true` | Выкл — автолут и подбор как в ваниле. То же, что галочка в настройках. |
| `DisableAutoPickup` | `true` | Предметы лежат на земле. Работает только при `Enabled`. |
| `DictionaryFile` | `dictionary.json` | Файл рядом с DLL или абсолютный путь. |

Если словарь пустой или в нём меньше четырёх различимых слов, подбор не блокируется.

## Структура

- `src/ValheimGreek.Core` — парсер Greek3, нормализация, сборка вопроса (без Unity).
- `src/ValheimGreek.Export` — консоль: Markdown → `dictionary.json`.
- `src/ValheimGreek` — плагин BepInEx / Harmony.
- `tests/ValheimGreek.Core.Tests` — разбор карточек и викторина.

Подробности патчей и формата карточек — в `PLAN.md`.
