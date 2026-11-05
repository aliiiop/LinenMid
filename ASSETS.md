# Использованные ассеты

## Сторонние ассеты

| Что | Автор | Источник | Лицензия | Где используется |
|---|---|---|---|---|
| Y Bot (модель) | Adobe Mixamo | https://www.mixamo.com | Mixamo Terms (бесплатно для проектов) | Игрок, боты-герои, герой в меню |
| Mutant (модель) | Adobe Mixamo | https://www.mixamo.com | Mixamo Terms | Миньоны, нейтралы, Pitmonster |
| Rifle Idle, Rifle Run | Adobe Mixamo | https://www.mixamo.com | Mixamo Terms | Анимации игрока и ботов |
| Idle, Zombie Walk | Adobe Mixamo | https://www.mixamo.com | Mixamo Terms | Анимации миньонов и нейтралов |
| Plasma lazer pistol gun shot 1 (#427396) | erokia | https://freesound.org/people/erokia/sounds/427396/ | **ПРОВЕРИТЬ на странице** | Выстрелы игрока, ботов, башен |
| Reload (#688866) | dacmod1 | https://freesound.org/people/dacmod1/sounds/688866/ | **ПРОВЕРИТЬ на странице** | Перезарядка |
| Coin C 05 (#350868) | cabled_mess | https://freesound.org/people/cabled_mess/sounds/350868/ | **ПРОВЕРИТЬ на странице** | Подбор предметов, покупка, лечение |
| Cinematic energy impact grainy voltage (#749788) | rescopicsound | https://freesound.org/people/rescopicsound/sounds/749788/ | **ПРОВЕРИТЬ на странице** | Попадание, способность Q, разрушение башни |
| Cyberpunk Moonlight Sonata | Joth | https://opengameart.org/content/cyberpunk-moonlight-sonata | CC0 | Музыка меню и матча |
| Liberation Sans (шрифт) | Red Hat / TextMesh Pro | входит в TMP Essential Resources | SIL Open Font License | Весь текст интерфейса |

> Звуки с freesound: открой каждую ссылку и впиши лицензию (CC0 или CC BY). Если CC BY, автор уже указан в титрах игры.

## Собственные ассеты (сделаны нами)

| Что | Где лежит | Исходник |
|---|---|---|
| Карта Neon City (линии, джунгли, базы, город вокруг) | `Assets/Scenes/Game.unity` | `Assets/Editor/MidlineBuilder.Game.cs` (вся расстановка описана кодом) |
| Башни, Ancient, магазин, фонтан, прыжковые платформы | сцена Game | `MidlineBuilder.Game.cs` |
| Винтовка героя (из примитивов) | префабы `BotHero`, объект Player | `MidlineBuilder.cs`, метод `AddRifle` |
| Процедурные текстуры: неоновая сетка, окна небоскрёбов, полосы, мягкая точка | `Assets/Textures/*.png` | `MidlineBuilder.cs`, методы `GridPixel`, `WindowPixel`, `StripePixel`, `DotPixel` |
| Материалы (неон, здания, щиты, лучи) | `Assets/Materials` | `MidlineBuilder.cs`, метод `BuildAssets` |
| Эффекты частиц (искры, взрыв, кольцо Q, лечение) | `Assets/Prefabs` | `MidlineBuilder.cs`, `MakeBurstFx`, `MakeRingFx` |
| Animator Controller игрока и миньона | `Assets/Animations/*.controller` | `MidlineBuilder.cs`, `MakeAnimator` |
| Интерфейс (HUD, магазин, пауза, меню, титры) | сцены MainMenu и Game | `Assets/Editor/MidlineBuilder.UI.cs` |
| Прицел | — | `Sources/Crosshair.xcf` |
| Все скрипты игры | `Assets/Scripts` | — |
