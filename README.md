# Лабораторна робота №2 — Процеси (варіант 4, C#)

Рішення `Lab2.sln` містить дві консольні програми на .NET 8:

|    Проєкт   | Завдання |                                         Що робить                                            |
|-------------|----------|----------------------------------------------------------------------------------------------|
| `MaskWords` |     1    | читає stdin, замінює слова з `@` на `xxxxx` (`--keep-len` — зберігає довжину), пише в stdout |
| `ProcTree`  |     2    | показує дерево процесів від заданого PID або зупиняє все дерево знизу вгору                  |

## Збірка

**Visual Studio:** відкрити `Lab2.sln` → *Build → Build Solution* (Ctrl+Shift+B).

**Для демонстрації (pipe, redirect):

```
build.bat
cd out
```

У папці `out` з'являться `MaskWords.exe`, `ProcTree.exe`, `input.txt` і `spawn-test-tree.bat`.

> У Windows PowerShell 5.1 оператор `>` записує файли в UTF-16, тому `type` покаже «зайві» символи.

## Завдання 1 — MaskWords

```
type input.txt | MaskWords.exe                         :: pipe: stdout "type" -> stdin MaskWords
type input.txt | MaskWords.exe --keep-len              :: зі збереженням довжини
type input.txt | MaskWords.exe > out.txt               :: stdout у файл (перезапис)
type input.txt | MaskWords.exe --keep-len >> out.txt   :: stdout у файл (дописування)
type out.txt
MaskWords.exe --wrong                                  :: помилка йде в stderr, код виходу 2
MaskWords.exe --wrong 2> errors.txt                    :: stderr у файл
MaskWords.exe --wrong > all.txt 2>&1                   :: stdout і stderr в один файл
type input.txt | MaskWords.exe 1>NUL                   :: stdout викинути
type input.txt | MaskWords.exe | find /c "xxxxx"       :: ланцюжок із трьох процесів
MaskWords.exe < input.txt                              :: stdin із файлу
echo %errorlevel%                                      :: код виходу останньої програми
```

Без pipe програма чекає введення з клавіатури: набрати текст, потім **Ctrl+Z** і **Enter** (кінець потоку в Windows).

## Завдання 2 — ProcTree

```
ProcTree.exe list                    :: усі процеси + яка програма дала список (wmic чи PowerShell)
spawn-test-tree.bat                  :: створює тестове дерево і друкує Root PID, наприклад 7100
ProcTree.exe tree 7100               :: показати дерево (підставте свій Root PID)
ProcTree.exe kill 7100 --dry-run     :: показати порядок зупинки, нічого не зупиняючи
ProcTree.exe kill 7100               :: зупинити (з підтвердженням y/N)
ProcTree.exe tree 7100               :: тепер: "process 7100 not found", код 3
ProcTree.exe kill 4                  :: системний процес - відмова
ProcTree.exe help
```

> PID **число без дужок**: у cmd символи `<` і `>` означають перенаправлення.

Список процесів програма отримує, запускаючи `wmic process get ProcessId,ParentProcessId,Name`.
Якщо `wmic` у системі немає (Windows 11 24H2+) або він не повернув жодного процесу, використовується
`powershell ... Get-CimInstance Win32_Process`. Команда `list` показує, яке джерело спрацювало. Зупинка — `taskkill /F /PID <pid>`.
На Linux/macOS програма працює з `ps` і `kill -9`.

Коди виходу: `0` — успіх, `2` — неправильні аргументи, `3` — процес не знайдено, `4` — не вдалося зупинити.

## Запуск із Visual Studio (F5)

У списку поруч із кнопкою запуску є готові профілі аргументів
(`ProcTree`: `tree 4`, `kill 4 --dry-run`, `help`; `MaskWords`: з `--keep-len` і без).
Змінити аргументи: *Project → Properties → Debug → Open debug launch profiles UI → Command line arguments*.
> Для показу pipe і redirect Visual Studio не підходить — тільки командний рядок.
