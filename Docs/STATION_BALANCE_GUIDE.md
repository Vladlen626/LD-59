# Station Balance Guide

Короткий рабочий документ для балансировки station loop.

Основа:

- central resource: `RemainingDebt`
- день считается по формуле:

```text
final_debt = initial_debt + total_fines - total_earnings
```

Источники:

- Level 1: Intro to Game Balance
- Level 2: Numeric Relationships
- Level 3: Transitive Mechanics and Cost Curves
- Level 4: Probability and Randomness

## 1. Главные правила

- Игрок не должен выигрывать, всегда нажимая одну кнопку.
- Идеальная игра должна закрывать день или давать заметный прогресс по долгу.
- Пропуск грязного вагона должен быть больнее, чем возврат чистого.
- Баланс не лечит плохую читаемость кейса. Если нарушение плохо видно, надо улучшать кейс, а не цифры.
- Сначала балансируем фиксированные кейсы, потом добавляем randomness.

## 2. Что проверять для каждого дня

Считаем три сценария:

```text
perfect_play = total_rewards
always_allow = clean_cases * reward - dirty_cases * missed_penalty
always_send_back = dirty_cases * reward - clean_cases * wrongful_penalty
```

Требования:

- `perfect_play >= initial_debt`
- `always_allow < 0`
- `always_send_back < 0`

Если любое из этих правил не выполняется, день сломан.

## 3. Цена ошибки

Практические ориентиры:

- `missed_penalty` = `1.5 .. 2.0` правильных кейса
- `wrongful_penalty` = `0.6 .. 1.0` правильного кейса

То есть:

- hidden contraband / forbidden cargo надо бояться;
- перестраховка тоже должна наказываться, но слабее.

## 4. Кривая сложности по дням

Меняем по одной оси за итерацию:

1. Количество кейсов.
2. Долю грязных кейсов.
3. Типы нарушений.
4. Экономический запас дня.
5. Только потом randomness.

Базовый ориентир:

- Day 1: 8 кейсов, 30-40% грязных
- Day 2: 9 кейсов, 35-45% грязных
- Day 3: 10 кейсов, 40-50% грязных

## 5. Randomness

- Не генерировать нарушения полностью случайно.
- Лучше брать из подготовленного пула кейсов.
- Не допускать длинных стриков одинаковых кейсов.
- Не рандомизировать размер штрафа.

Хорошо:

```text
pick from curated scenario pool
```

Плохо:

```text
random forbidden + random mismatch + random marker + random contraband
```

## 6. Workflow настройки дня

1. Определить цель дня: чему он учит.
2. Собрать JSON-кейсы.
3. Посчитать `perfect_play`, `always_allow`, `always_send_back`.
4. Проверить цену одной ошибки.
5. Прогнать несколько ручных сессий.
6. Менять только одну группу параметров за раз.

## 7. Что логировать

- `day_index`
- `initial_debt`
- `final_debt`
- `total_rewards`
- `total_fines`
- `correct_count`
- `false_allow_count`
- `false_send_back_count`
- `scenario_id -> fail rate`

## 8. Текущий базовый день

Файл:

- [station_day.json](/C:/Users/vladl/projects/LD-59/Assets/Resources/Json/station_day.json)

Текущая база:

- `initial_debt = 620`
- `reward = 80`
- `missed_penalty = 150`
- `wrongful_penalty = 60`
- `8` кейсов
- `5` чистых
- `3` грязных

Проверка:

```text
perfect_play = 8 * 80 = 640
always_allow = 5 * 80 - 3 * 150 = -50
always_send_back = 3 * 80 - 5 * 60 = -60
```

Это нормальный стартовый баланс:

- идеальная игра закрывает день;
- тупая стратегия в любую сторону убыточна.

## 9. Быстрые правила

- Если можно выиграть одной кнопкой, баланс сломан.
- Если одна ошибка стоит больше двух правильных кейсов, день слишком жесткий.
- Если нарушение трудно заметить, усиливай читаемость, а не штраф.
- Все числа своди к `RemainingDebt`.
