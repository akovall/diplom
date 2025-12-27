# Analytics Page — Рекомендации по статистике

## Обзор структуры данных

На основе базы данных доступны следующие сущности:
- **Projects**: Id, Title, Description, CreatedAt, IsArchived
- **Users**: Id, Username, FullName, JobTitle, Role, IsActive
- **Tasks**: Id, Title, Description, Status, Priority, CreatedAt, Deadline, EstimatedHours, ProjectId, AssigneeId
- **TimeLogs**: Id, StartTime, EndTime, Comment, IsManual, TaskId, UserId

---

## 📊 Для обычного пользователя (Employee)

### 1. Личная продуктивность

| Метрика | Описание | Визуализация |
|---------|----------|--------------|
| **Logged Hours Today** | Сумма времени залогированного сегодня | Большое число |
| **Logged Hours This Week** | Время за текущую неделю | Число + сравнение с прошлой неделей (↑5%) |
| **Daily Activity Chart** | Распределение залогированного времени по дням | Bar Chart (Пн-Вс) |

### 2. Статус задач

| Метрика | Описание | Визуализация |
|---------|----------|--------------|
| **My Tasks Overview** | Количество задач по статусам (To Do, In Progress, Done) | Donut Chart |
| **Overdue Tasks** | Задачи с просроченным дедлайном | Число + список с красным индикатором |
| **Upcoming Deadlines** | Задачи с дедлайном в ближайшие 7 дней | Timeline / List |

### 3. Эффективность выполнения

| Метрика | Описание | Визуализация |
|---------|----------|--------------|
| **Estimated vs Actual Hours** | Сравнение оценочного времени с фактическим | Bar Chart (сравнение) |
| **Tasks Completed This Week** | Количество завершённых задач | Число + тренд |
| **Average Time Per Task** | Среднее время на выполнение задачи | Число |

---

## 👔 Для руководителя (Manager/Admin)

### 1. Обзор команды

| Метрика | Описание | Визуализация |
|---------|----------|--------------|
| **Team Workload** | Распределение залогированного времени по сотрудникам | Horizontal Bar Chart |
| **Active Users This Week** | Количество активных пользователей | Число |
| **Team Hours Overview** | Сумма часов команды за период (день/неделя/месяц) | KPI Card |

### 2. Аналитика проектов

| Метрика | Описание | Визуализация |
|---------|----------|--------------|
| **Hours by Project** | Распределение времени по проектам | Pie Chart / Bar Chart |
| **Project Progress** | % завершённых задач в каждом проекте | Progress Bars |
| **Top Projects by Activity** | Проекты с наибольшей активностью | Ranked List |

### 3. Анализ задач

| Метрика | Описание | Визуализация |
|---------|----------|--------------|
| **Tasks by Status (Team)** | Общее распределение статусов задач | Stacked Bar Chart |
| **Tasks by Priority** | Распределение по приоритетам (High/Medium/Low) | Donut Chart |
| **Overdue Tasks Summary** | Сводка просроченных задач по исполнителям | Table |
| **Bottleneck Detection** | Задачи долго находящиеся в статусе "In Progress" | List с warning |

### 4. Трендовая аналитика

| Метрика | Описание | Визуализация |
|---------|----------|--------------|
| **Weekly Hours Trend** | Тренд залогированных часов за последние N недель | Line Chart |
| **Task Completion Velocity** | Скорость завершения задач (tasks/week) | Line Chart |
| **Estimation Accuracy** | % точности оценок (EstimatedHours vs Actual) | Gauge / Percentage |

---

## 🎯 Рекомендуемая структура страницы Analytics

### Общий Layout

```
┌─────────────────────────────────────────────────────────────┐
│  📅 Period Selector: [Today] [This Week] [This Month] [Custom] │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────────┐    │
│  │ 24.5h   │  │ 12      │  │ 3       │  │ 89%         │    │
│  │ Logged  │  │ Tasks   │  │ Overdue │  │ On Time     │    │
│  └─────────┘  └─────────┘  └─────────┘  └─────────────┘    │
│                                                             │
│  ┌─────────────────────────┐  ┌─────────────────────────┐  │
│  │  Daily Activity         │  │  Tasks by Status        │  │
│  │  [████▓▓██▓▓▓▓█]       │  │     ◯ Done: 45%         │  │
│  │  Mon Tue Wed Thu Fri    │  │     ◯ In Progress: 30% │  │
│  │                         │  │     ◯ To Do: 25%       │  │
│  └─────────────────────────┘  └─────────────────────────┘  │
│                                                             │
│  ┌─────────────────────────┐  ┌─────────────────────────┐  │
│  │  Hours by Project       │  │  Weekly Trend           │  │
│  │  [███████████]          │  │  📈 +12% vs last week  │  │
│  └─────────────────────────┘  └─────────────────────────┘  │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 🛠️ Приоритет реализации

### Phase 1 (MVP)
1. Logged Hours Today/Week — простой расчёт
2. My Tasks Overview (Donut Chart) — по статусам
3. Estimated vs Actual comparison — для текущего пользователя

### Phase 2 (Enhanced)
4. Daily Activity Chart — распределение по дням
5. Overdue Tasks Alert — список с индикаторами
6. Period Selector — фильтрация по дате

### Phase 3 (Manager Dashboard)
7. Team Workload — только для роли Admin/Manager
8. Hours by Project — группировка по ProjectId
9. Weekly Trend Chart — историческая аналитика
10. Estimation Accuracy Gauge — точность планирования

---

## 📝 Примечания к реализации

### Фильтрация по роли
```csharp
// Для обычного пользователя — только его данные
WHERE UserId = @CurrentUserId

// Для Manager/Admin — все данные команды
WHERE 1=1 (или без фильтра по UserId)
```

### Расчёт времени
```csharp
// Длительность TimeLog
Duration = EndTime - StartTime

// Для активного таймера (EndTime = null)
Duration = DateTime.Now - StartTime
```

### Сравнение EstimatedHours vs Actual
```csharp
ActualHours = SUM(TimeLog.Duration) WHERE TaskId = @TaskId
Accuracy = EstimatedHours / ActualHours * 100
```
