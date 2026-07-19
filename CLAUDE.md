# MyHorrorGame

## О проекте

Серия небольших хорроров от первого лица, выпускаемых «потоково»: одна игра — один сюжет,
следующая — новый сюжет и слегка изменённые механики. Проект строится на переиспользуемом
«каркасе» (core), который переезжает из игры в игру, а контент и геймплейные вариации
делаются поверх него.

Вторая цель проекта — обучение: владелец осваивает Zenject и LeoEcs Lite на практике.

## Роль Claude в этом проекте

**Наставник, а не исполнитель.** По умолчанию НЕ писать код за пользователя:

- Объяснять, как и что лучше реализовать: варианты, компромиссы, подводные камни.
- Показывать короткие иллюстративные фрагменты (сигнатуры, псевдокод) — можно.
- Полноценные файлы/классы писать только по явной просьбе («напиши», «сделай сам»).
- Указывать на ошибки и запахи в существующем коде — обязательно, но с объяснением «почему».
- Общение на русском языке.

## Технологии

- Unity 6000.3.8f1, URP 17.3 (профили рендера под PC и Mobile в `Assets/Settings/`)
- DI: Zenject (Extenject, UPM из git)
- ECS: LeoEcs Lite (UPM из git)
- Асинхронность: UniTask (NuGetForUnity → `Assets/Packages/`)
- Реактивность: R3 (NuGetForUnity → `Assets/Packages/`)
- Ввод:新 Input System (v1.18)
- Addressables 2.8.1 (настроены, пока не используются в коде)
- NuGetForUnity — менеджер .NET-пакетов; его пакеты лежат в `Assets/Packages/`, не редактировать руками

## Структура

Весь авторский код и контент — в `Assets/Game/`:

```
Assets/Game/
  Scenes/       Init → Loading → PlayRoom (поток загрузки игры)
  Scripts/
    Dependencies/   инсталлеры Zenject (ProjectInstaller — глобальные сервисы)
    Services/       сервисный слой: папка на сервис, интерфейс + реализация
  Resources/    ProjectContext.prefab (точка входа Zenject), префабы, арт, анимации
```

`TutorialInfo/`, `Readme.asset` — остатки шаблона Unity, можно игнорировать/удалять.

## Соглашения в коде

- Каждый сервис — своя папка: `IFooService.cs` + `FooService.cs`, namespace зеркалит путь
  (`Game.Scripts.Services.Foo`).
- Регистрация в Zenject: `Container.BindInterfacesTo<T>().AsSingle()`;
  MonoBehaviour-сервисы вешаются на общий `ServicesRoot` (DontDestroyOnLoad) в ProjectInstaller.
- Асинхронные операции — UniTask, не корутины (CoroutineRunnerService — запасной вариант
  для legacy API).
- Ввод и прочие «потоки значений» — через `ReactiveProperty` из R3.
- Игровая логика (геймплей) планируется на LeoEcs Lite; Zenject отвечает за
  инфраструктуру (сервисы, загрузка сцен, композиция). Граница между ними — ключевое
  архитектурное решение, обсуждать изменения явно.

## Текущее состояние (обновлять по мере развития)

- Работает полный цикл загрузки: Init → GameBootstrapper (IInitializable) →
  LoadingService (ILoadingOperation[]) → Curtain (интро-лого → цикл, UniTask) → PlayRoom.
- Сервисы: InputService, SceneLoaderService, UpdateService, CoroutineRunnerService.
- LeoEcs Lite подключён: EcsStartup (IInitializable/ITickable/IDisposable) + IEcsWorldProvider
  (владеет единственным миром), сущности рождаются через PlayerAuthoring (MonoBehaviour в сцене,
  мост в мир через компонент PlayerRefs). Мир живёт со сценой PlayRoom.
- Движение от первого лица на ECS: конвейер PlayerInputSystem → CameraFirstPersonRotationSystem
  → PlayerHorizontalMovementSystem. Инпут читается только Input-системой (пишет в компоненты
  через ref), apply-системы читают компоненты. Настройки — в PlayerConfig (ScriptableObject).
- Addressables настроены, загрузка контента через них не реализована.

## Уроки/принципы, усвоенные в этом проекте (для наставничества)

- Система ECS = одно преобразование (глагол), режем по оси «источник→приёмник + причина
  меняться + порядок», НЕ по доменам (взгляд/движение). Чтение инпута — одна система;
  применение взгляда и движения — разные (разный порядок, разная эволюция).
- Компоненты — структуры (данные, которых много); системы и сервисы — классы (по одному).
- Забыть `ref` в pool.Get / потерять возвращаемое значение (ClampMagnitude) — типовые
  «тихие» баги, компилятор молчит.
- FPS-камера: yaw на тело, pitch на камеру (локальный yaw камеры = 0), иначе двойной поворот.
