# Game Design Document — CURSOR
**Wersja:** 1.0  
**Data:** 2025  
**Engine:** Unity 6 | Język: C# | Perspektywa: 2D

---

## Spis treści
1. [Wizja gry](#1-wizja-gry)
2. [Mechaniki rozgrywki](#2-mechaniki-rozgrywki)
3. [Struktura scen i widoków](#3-struktura-scen-i-widoków)
4. [Pętla gameplayu (Game Loop)](#4-pętla-gameplayu)
5. [Gracz (Player)](#5-gracz-player)
6. [Przeciwnicy (Enemies)](#6-przeciwnicy-enemies)
7. [Collectable (Przedmioty do zebrania)](#7-collectable)
8. [System Upgradów](#8-system-upgradów)
9. [Statystyki (StatsSystem)](#9-statystyki-statssystem)
10. [Systemy techniczne](#10-systemy-techniczne)
11. [Optymalizacja](#11-optymalizacja)
12. [Zapis i wczytywanie (SaveSystem)](#12-zapis-i-wczytywanie-savesystem)
13. [Juice i feel](#13-juice-i-feel)
14. [UI i UX](#14-ui-i-ux)
15. [Architektura systemów](#15-architektura-systemów)

---

## 1. Wizja gry

### Opis ogólny
**Cursor** to gra incremental 2D, w której gracz wciela się w bohatera będącego mieczem sterowanym bezpośrednio ruchem myszy. Sesje gameplayowe są krótkie i intensywne — gracz uderza w przelatujących przez ekran przeciwników, zbiera zostawiane przez nich przedmioty i wraca do ekranu upgradów, by ulepszyć statystyki i zacząć kolejną, trudniejszą sesję.

### Referencja
**Nodebuster** (Steam) — incremental z krótką pętlą sesji, rosnącą skalą, systemem upgradów opartym na drzewie ulepszeń.

### Tone & Feel
- Prosty, czytelny 2D art
- Duża ilość efektów wizualnych (particle, animacje trafień) dla satysfakcji z rozgrywki
- Szybkie, responsywne sterowanie (gracz = kursor myszy, zero opóźnień)
- Wyraźna eskalacja trudności i nagrody między sesjami

### Platforma
PC (Windows), sterowanie: mysz

---

## 2. Mechaniki rozgrywki

### Sterowanie
- Gracz (miecz) śledzi pozycję kursora myszy w czasie rzeczywistym, 1:1, bez żadnego lagowania.
- Brak klawiszowych akcji — jedyną interakcją jest ruch myszy.
- Obszar rozgrywki jest ograniczony do ViewPort (kamera statyczna, gracz nie może wyjść poza granice).

### Kolizje
- Kolizja między Playerem a Enemy jest wykrywana **bez użycia fizyki Unity** (Rigidbody/Collider).
- Stosowane podejście: **Distance-based overlap check** w EnemyController — dla każdego wroga co klatkę sprawdzana jest odległość do pozycji gracza; jeśli odległość < suma promieni, dochodzi do kolizji.
- Collectable zbierany przez sprawdzenie odległości collectDistane do pozycji gracza w CollectableController.
- Brak fizycznych Rigidbody — wszystkie obiekty to "dummy" transformy zarządzane przez controllery.

### Walka
- Gdy Player znajduje się w zasięgu uderzenia Enemy:
  - Player zadaje obrażenia Enemy (playerDmg, modyfikowane przez staty)
  - Enemy zadaje obrażenia Playerowi (enemyBaseDmg, modyfikowane przez playerDef)
  - Cooldown kolizji — każda para Player-Enemy może zadać obrażenia nie częściej niż raz na X ms (np. 250ms), aby uniknąć jednoklatkowego ogromnego dmg.
- Gdy HP Enemy spadnie do 0: despawn Enemy → spawn Collectable.
- Gdy HP Player spadnie do 0: koniec sesji → widok Summary.

### Zbieranie Collectable
- Gdy gracz znajdzie się w odległości `collectDistance` od Collectable:
  1. Collectable wykonuje krótki **debounce** — przesuwa się w kierunku przeciwnym do gracza (efekt "odskok").
  2. Następnie wykonuje **homing** — szybko leci do pozycji gracza.
  3. Po dotarciu do gracza: zbiera wartość, despawn → powrót do puli.

---

## 3. Struktura scen i widoków

### Sceny Unity

#### Scena: MainMenu
Startowa scena gry. Zawiera:
- Przycisk **New Game** — ładuje GameScene, startuje od zera (brak save lub reset z poziomu menu)
- Przycisk **Continue** — widoczny tylko gdy istnieje plik zapisu; ładuje GameScene z wczytanym postępem
- Przycisk **Options** — otwiera panel opcji (głośność muzyki, SFX, fullscreen, rozdzielczość)
- Przycisk **Credits** — otwiera panel credits
- Przycisk **Quit** — zamknięcie gry

#### Scena: GameScene
Jedna scena z przełączanymi **widokami (states)**. Kamera statyczna, obszar rozgrywki stały.

---

### Widoki GameScene (Game States)

#### Widok: Upgrade
- Wyświetla **Node Tree** upgradów — sieć węzłów reprezentujących dostępne ulepszenia.
- Węzły odblokowane świecą się / są klikalne; zablokowane są wyszarzone.
- Kliknięcie w węzeł: pokazuje szczegóły upgrade'u (aktualna wartość stat, następna wartość, koszt w Collectable).
- Przycisk **Upgrade** (jeśli spełniony warunek kosztu) — wydaje Collectable, podnosi level, modyfikuje StatsSystem, odblokowuje kolejne węzły.
- Przycisk **Start Session** — przechodzi do widoku Gameplay.
- Przycisk **Options** — panel opcji.
- Przycisk **Main Menu** — powrót do MainMenu (z auto-save).

#### Widok: Gameplay
- Obszar rozgrywki z tłem/granicami.
- HUD: pasek HP gracza, liczniki zebranych Collectable (każdy typ osobno), aktualny czas sesji.
- Przycisk **Pause/Stop** — zatrzymuje sesję i przechodzi do widoku Summary.
- Gracz widoczny na ekranie jako sprite miecza podążający za kursorem.
- Spawnowanie i ruch Enemy + Collectable aktywne tylko w tym widoku.

#### Widok: Summary
- Podsumowanie zakończonej sesji:
  - Czas trwania sesji
  - Liczba zabitych Enemy (per typ)
  - Zebrane Collectable (per typ, ilość)
  - Zadane i otrzymane obrażenia
  - Powód końca sesji (HP = 0 / ręczne zatrzymanie)
- Przycisk **New Session** — od razu startuje kolejną sesję (widok Gameplay), bez wchodzenia w Upgrade.
- Przycisk **Go to Upgrade** — przechodzi do widoku Upgrade.

---

## 4. Pętla gameplayu

```
[MainMenu]
    │
    ├── New Game / Continue
    │
    ▼
[GameScene → Upgrade View]
    │
    ├── Upgraduj statystyki (wydaj Collectable)
    │
    └── Start Session
              │
              ▼
        [Gameplay View]
              │
              ├── Zbijaj Enemy, zbieraj Collectable
              │
              └── HP = 0 lub Stop
                        │
                        ▼
                  [Summary View]
                        │
                        ├── New Session → [Gameplay View]
                        └── Upgrade → [Upgrade View]
```

---

## 5. Gracz (Player)

### Właściwości
| Właściwość | Opis |
|---|---|
| Pozycja | = pozycja kursora myszy (world space), clampowana do granic ViewPort |
| Sprite | miecz; renderowany zawsze na wierzchu |
| Hitbox | okrąg o promieniu `playerRadius` |

### Statystyki (z StatsSystem)
| Stat | Opis | Domyślna wartość |
|---|---|---|
| `playerMaxHp` | Maksymalne HP | 100 |
| `playerCurrentHp` | Bieżące HP | = playerMaxHp na start sesji |
| `playerDmg` | Obrażenia zadawane Enemy | 10 |
| `playerDef` | Redukcja obrażeń od Enemy (flat lub %) | 0 |
| `playerRadius` | Promień hitboxu do wykrywania kolizji | 0.5 |
| `collectDistance` | Odległość zbierania Collectable | 2.0 |

### Zachowanie
- Na początku każdej sesji HP reset do `playerMaxHp`.
- Gracz nie może wyjść poza granice ViewPort.
- Gdy HP ≤ 0: emituje event `OnPlayerDead`, GameManager zmienia stan na Summary.

---

## 6. Przeciwnicy (Enemies)

### Typy Enemy
Planowane **3–4 typy** Enemy, każdy z **3–4 podtypami**.

**Typy różnią się KOLOREM** — kolor to główny identyfikator wizualny typu na ekranie. Przykładowa paleta:

| Typ | Kolor | Opis | Specyfika |
|---|---|---|---|
| Type A | Niebieski | Podstawowy, mały | Mało HP, szybki, często spawnowany |
| Type B | Zielony | Średni | Balansowany HP/DMG |
| Type C | Czerwony | Duży | Dużo HP, wolny, duże obrażenia |
| Type D | Fioletowy | Specjalny / rzadki | TBD — np. split na 2 mniejsze po śmierci |

**Podtypy różnią się KSZTAŁTEM GEOMETRYCZNYM** — ta sama figura może być w kolorze dowolnego typu, tworząc macierz kombinacji. Przykładowe kształty podtypów:

| Podtyp | Kształt | Mnożnik HP | Mnożnik DMG | Mnożnik Dropa |
|---|---|---|---|---|
| Subtype 1 | Trójkąt | x0.8 | x1.2 | x0.8 |
| Subtype 2 | Kwadrat | x1.0 | x1.0 | x1.0 |
| Subtype 3 | Sześciokąt | x1.4 | x0.9 | x1.4 |
| Subtype 4 | Okrąg | x1.8 | x0.7 | x1.8 |

Każda kombinacja Typ × Podtyp to unikalny enemy (np. Czerwony Sześciokąt = powolny, bardzo wytrzymały, duży drop). Sprite enemy to geometryczna figura w danym kolorze — czytelne i skalowalne bez potrzeby wielu asset'ów.

### Statystyki Enemy (baza per typ, z StatsSystem)
| Stat | Opis |
|---|---|
| `enemyBaseHp` | Bazowe HP wroga |
| `enemyBaseDmg` | Bazowe obrażenia zadawane graczowi |
| `enemySpeedMin/Max` | Zakres prędkości ruchu (losowana przy spawnie) |
| `enemyRotSpeedMin/Max` | Zakres prędkości obrotu (losowana przy spawnie) |
| `enemyRadius` | Promień hitboxu |
| `enemyCollectableType` | Jaki typ Collectable dropuje |
| `enemyCollectableCount` | Ile Collectable dropuje |

### Stany wizualne Enemy
Enemy posiada 3 stany wizualne zależne od aktualnego HP:
- **Stan 1**: HP > 66% maxHP → sprite/kolor bazowy
- **Stan 2**: HP ≤ 66% maxHP i > 33% → sprite/kolor uszkodzony
- **Stan 3**: HP ≤ 33% maxHP → sprite/kolor krytyczny

Zmiana stanu: podmiana sprite'a lub materiału (bez dodatkowych komponentów).

### Ruch Enemy
- Kierunek: ze spawn pointa (poza ViewPort) w stronę **centrum sceny + losowy offset** (np. `Vector2.zero + Random.insideUnitCircle * centerOffset`), tak by Enemy nie zawsze leciały w sam środek.
- Prędkość: stała wartość losowana raz przy spawnie z zakresu `[speedMin, speedMax]`.
- Obrót: stały obrót wokół własnej osi, wartość kątowa losowana raz przy spawnie z `[rotSpeedMin, rotSpeedMax]`.
- Gdy Enemy wyleci poza ViewPort (z marginesem): despawn → powrót do puli.

### Spawn Enemy
- Punkt spawnu losowany na okręgu/prostokącie **poza** ViewPort (ze wszystkich 4 stron).
- Kierunek prędkości obliczany: `(targetPoint - spawnPoint).normalized * speed`.

---

## 7. Collectable

### Typy Collectable (Waluty)
**3–4 rodzaje** walut, każda z własną ikoną i kolorem. Każdy Upgrade przypisuje sobie dokładnie **jedną walutę** jako koszt — gracz musi zebrać odpowiednią ilość tej konkretnej waluty, by ulepszyć dany węzeł.

Waluty różnią się rzadkością dropu i tym które typy Enemy je upuszczają:

| ID | Nazwa robocza | Kolor ikony | Źródło (głównie) | Zastosowanie |
|---|---|---|---|---|
| Currency_A | Shard | Niebieski | Type A (częsty) | Podstawowe upgrady |
| Currency_B | Core | Zielony | Type B (średni) | Średnie upgrady |
| Currency_C | Crystal | Czerwony | Type C (rzadki) | Zaawansowane upgrady |
| Currency_D | Essence | Fioletowy | Type D (bardzo rzadki) | Endgame upgrady |

> Każdy Enemy dropuje **jeden konkretny typ waluty** zdefiniowany w jego konfiguracji. Typ dropa to właściwość Enemy Type, nie podtypu.

### Powiązanie Enemy → Drop
| Typ Enemy | Dropowana waluta |
|---|---|
| Type A (Niebieski) | Currency_A (Shard) |
| Type B (Zielony) | Currency_B (Core) |
| Type C (Czerwony) | Currency_C (Crystal) |
| Type D (Fioletowy) | Currency_D (Essence) |

### Trwałość walut
Waluty **kumulują się między sesjami** — nie resetują się po śmierci ani po zakończeniu sesji. Gracz zbiera je przez wiele sesji i wydaje w widoku Upgrade. SaveSystem zapisuje stan walut po każdej sesji i przy powrocie do Main Menu.

### Zachowanie Collectable
1. **Spawn**: pojawiają się w punkcie śmierci Enemy, rozrzucone losowo w małym promieniu (`spawnScatter`).
2. **Oczekiwanie**: leżą w miejscu i czekają.
3. **Trigger zbierania**: gdy gracz znajdzie się w `collectDistance`.
4. **Debounce**: krótki ruch w kierunku odwrotnym od gracza (np. `debounceForce`, `debounceTime`).
5. **Homing**: po debounce → szybki ruch w stronę gracza (`homingSpeed`).
6. **Zebranie**: gdy odległość < `pickupRadius` → dodaje wartość do licznika gracza (kumulatywnie, persystentnie) → despawn → pula.

### Parametry Collectable
| Param | Opis |
|---|---|
| `collectDistance` | Odległość triggera (pobierana z StatsSystem) |
| `debounceForce` | Siła odskoku |
| `debounceTime` | Czas fazy odskok |
| `homingSpeed` | Prędkość lotu do gracza |
| `pickupRadius` | Odległość finalnego zebrania |
| `spawnScatter` | Promień rozrzutu przy spawnie |

---

## 8. System Upgradów

### Koncepcja
Upgrady zorganizowane w **Node Tree** — wizualne drzewo węzłów rozgałęziające się we wszystkich kierunkach od centrum. Każdy węzeł reprezentuje jeden Upgrade z poziomami 0–maxLevel.

### Reguły odblokowywania
- Na starcie: **tylko jeden węzeł odblokowany** — centralny węzeł korzenia drzewa, widoczny na środku ekranu.
- Pozostałe węzły są **niewidoczne** do momentu odblokowania.
- Podniesienie węzła z poziomu 0 na **poziom 1** → węzły przypisane w `unlocksOnLevel1` stają się widoczne i aktywne.
- Wyższe poziomy (2, 3...) nie odblokowują nowych węzłów — tylko wzmacniają daną statystykę.
- Każdy węzeł przechowuje **listę referencji do children węzłów** (`List<Upgrade> unlocksOnLevel1`). Węzły liście (końcowe) mają tę listę pustą.
- Drzewo rozgałęzia się we wszystkich kierunkach — lewo, prawo, góra, dół, ukośnie — tworząc organiczną sieć.

### Wizualizacja Node Tree
- **Węzeł odblokowany (level 0)**: widoczny, klikowalny, podświetlony, połączony linią z rodzicem
- **Węzeł odblokowany (level 1+)**: widoczny, jasny kolor, pokazuje aktualny level
- **Węzeł zablokowany**: niewidoczny (`SetActive(false)`)
- **Linie łączące**: rysowane dynamicznie między węzłami (Unity UI Line lub własny renderer); linia do zablokowanego węzła niewidoczna
- Całość na **scrollowalnym Canvas** — gracz może panoramować widok gdy drzewo urośnie

### Layout Node Tree — ręczny
Pozycje węzłów (`nodePosition: Vector2`) są **ustawiane ręcznie w Unity Inspector / ScriptableObject** przez designera. Brak procedurального rozmieszczania.

Implikacje dla implementacji:
- Każdy `Upgrade` to **ScriptableObject** z polem `Vector2 nodePosition` ustawianym ręcznie.
- `UpgradeNodeUI` (prefab węzła na Canvas) pozycjonuje się na podstawie tej wartości przy inicjalizacji widoku.
- Połączenia parent→child rysowane są po pozycjach węzłów — designer układa drzewo wizualnie i zapisuje pozycje w SO.
- Zalecane narzędzie do edycji: własny prosty **EditorTool** lub ręczne ustawianie wartości w Inspectorze (wystarczające dla projektu tej skali).
- Designer może w dowolnym momencie dodać nowy węzeł, ustawić mu pozycję i przypisać go jako child istniejącego węzła — bez zmian w kodzie.

### Struktura Upgrade (ScriptableObject)
Każdy Upgrade to **ScriptableObject** — łatwy do tworzenia i edycji w Unity Editor bez zmian w kodzie.

```
UpgradeNodeSO : ScriptableObject {
    string upgradeId            // Unikalny identyfikator (do zapisu/wczytu)
    string upgradeName          // Nazwa wyświetlana
    string description          // Opis efektu
    int maxLevel                // np. 5
    StatType targetStat         // Której statystyki dotyczy
    float[] valuesPerLevel      // Wartości dodawane per level [0, 10, 25, 45, 70, 100]
    int[] costsPerLevel         // Koszt per level [0, 10, 30, 60, 100, 150]
    CurrencyType costCurrency   // Jaka waluta jest kosztem
    List<UpgradeNodeSO> unlocksOnLevel1  // Child węzły odblokowywane po level 1
    Vector2 nodePosition        // Pozycja na Canvas — ustawiana ręcznie w Inspector
}
```

Runtime state (nie w SO, trzymane w UpgradeSystem):
```
UpgradeRuntimeState {
    string upgradeId
    int currentLevel
    bool isUnlocked
}
```

### Powiązanie Upgrade → Waluta
Każdy upgrade przypisuje sobie jedną z walut jako koszt. Przykłady:
- Podstawowe upgrady (playerDmg, playerMaxHp) → kosztują **Currency_A** (Shard, najczęstszy)
- Średnie upgrady (playerDef, collectDistance) → kosztują **Currency_B** (Core)
- Zaawansowane upgrady (spawn modifiers, enemy weakness) → **Currency_C** (Crystal)
- Endgame upgrady → **Currency_D** (Essence)

### Przykładowe Upgrady (do rozwinięcia)
| Upgrade | Stat | Efekt |
|---|---|---|
| Sword Sharpness | playerDmg | +X dmg per poziom |
| Iron Will | playerMaxHp | +X maxHP per poziom |
| Guard | playerDef | +X obrony per poziom |
| Collector | collectDistance | +X zasięg zbierania |
| Enemy Weakness | enemyBaseDmg | -X% dmg od enemy |
| Harvest | enemyCollectableCount | +X% więcej collectabli z enemy |

### Aplikowanie Upgrade
Po kliknięciu Upgrade i zapłaceniu kosztu:
1. `currentLevel++`
2. Wywołanie `StatsSystem.ApplyUpgrade(targetStat, valuesPerLevel[currentLevel])`
3. Odblokowanie child węzłów (jeśli level == 1)
4. Odświeżenie UI Node Tree

---

## 9. Statystyki (StatsSystem)

StatsSystem przechowuje **wszystkie** wartości liczbowe gry jako jedno centralne źródło prawdy.

### Kategorie statystyk

#### Player Stats
| Stat | Typ | Opis |
|---|---|---|
| `playerMaxHp` | float | Maksymalne HP gracza |
| `playerDmg` | float | Obrażenia zadawane |
| `playerDef` | float | Redukcja obrażeń |
| `playerRadius` | float | Promień hitboxu gracza |
| `collectDistance` | float | Zasięg zbierania |

#### Enemy Stats (per typ)
| Stat | Typ | Opis |
|---|---|---|
| `enemyBaseHp[type]` | float | Bazowe HP per typ |
| `enemyBaseDmg[type]` | float | Bazowe DMG per typ |
| `enemySpeedMin/Max[type]` | float | Zakres prędkości |
| `enemyRotSpeedMin/Max[type]` | float | Zakres prędkości obrotu |
| `enemyRadius[type]` | float | Promień hitboxu |

#### Spawn Stats
| Stat | Typ | Opis |
|---|---|---|
| `spawnBurstCount` | int | Ile Enemy spawnuje burst na start |
| `spawnInterval` | float | Co ile sekund dospawnowanie |
| `spawnIntervalCount` | int | Ile Enemy per interwał |
| `spawnTypeWeights[type]` | float[] | Wagi spawn probability per typ |
| `spawnSubtypeWeights[type][subtype]` | float[] | Wagi per podtyp |

#### Collectable / Waluta Stats
| Stat | Typ | Opis |
|---|---|---|
| `collectableCountModifier` | float | Mnożnik ilości dropowanych collectabli |
| `currency_A_count` | int | Bieżąca ilość Shard gracza |
| `currency_B_count` | int | Bieżąca ilość Core gracza |
| `currency_C_count` | int | Bieżąca ilość Crystal gracza |
| `currency_D_count` | int | Bieżąca ilość Essence gracza |

### API StatsSystem
```csharp
float GetStat(StatType stat)
void SetStat(StatType stat, float value)
void ModifyStat(StatType stat, float delta)        // addytywnie
void MultiplyyStat(StatType stat, float multiplier) // multiplikatywnie
void ApplyUpgrade(StatType stat, float value)
void ResetSessionStats()  // reset tylko statystyk per-sesja (HP)
```

---

## 10. Systemy techniczne

### GameManager
- **Singleton**, główny orchestrator gry.
- Zarządza stanem gry: `enum GameState { Upgrade, Gameplay, Summary }`
- Na zmianę stanu:
  - `→ Gameplay`: StartSpawn, reset HP gracza, aktywuj HUD
  - `→ Summary`: StopSpawn, despawn wszystkich Enemy i Collectable, pokaż Summary UI
  - `→ Upgrade`: pokaż widok Upgrade, ukryj resztę
- Nasłuchuje eventów: `OnPlayerDead`, `OnSessionStop`

### SystemsManager
- **Singleton**, przechowuje referencje do wszystkich systemów.
- Dostęp: `SystemsManager.Instance.StatsSystem`, `SystemsManager.Instance.UpgradeSystem`, itd.
- Inicjalizowany przed wszystkimi innymi systemami (Script Execution Order).

### EventSystem
- Globalny system publish-subscribe.
- Zdarzenia (przykłady):
  - `OnPlayerDead`
  - `OnSessionStop`
  - `OnEnemyKilled(EnemyType type, Vector2 position)`
  - `OnCollectablePickedUp(CollectableType type, int amount)`
  - `OnUpgradePurchased(Upgrade upgrade)`
  - `OnGameStateChanged(GameState newState)`
- Implementacja: statyczna klasa z Action/delegate lub własny EventBus ze słownikiem eventów.

### ObjectPooling
- Jeden centralny `ObjectPoolManager` lub osobne pule per typ obiektu.
- Pule: `EnemyPool[type][subtype]`, `CollectablePool[type]`
- API:
  - `T Get<T>(PoolType type)` — pobierz obiekt z puli (aktywuje go)
  - `void Return(GameObject obj)` — zwróć do puli (deaktywuje)
- Pre-warm pul na starcie GameScene z konfigurowalnymi rozmiarami początkowymi.

### EnemyController
- Jeden obiekt zarządzający **wszystkimi aktywnymi Enemy**.
- W `Update()` iteruje po liście aktywnych Enemy i:
  - Aktualizuje ich pozycję: `pos += direction * speed * deltaTime`
  - Aktualizuje obrót: `rotation += rotSpeed * deltaTime`
  - Sprawdza czy wyleciały poza ViewPort → despawn
  - Sprawdza kolizje z Playerem (distance check) → wywołuje damage
- Metody: `RegisterEnemy(EnemyData)`, `UnregisterEnemy(EnemyData)`, `DespawnAll()`

### EnemySpawner
- Spawning sterowany przez GameManager (StartSpawn / StopSpawn).
- **Burst spawn** na starcie sesji: natychmiast spawni `spawnBurstCount` Enemy.
- **Interval spawn**: co `spawnInterval` sekund spawni `spawnIntervalCount` Enemy.
- Wybór typu: losowanie ważone wg `spawnTypeWeights`.
- Wybór podtypu: losowanie ważone wg `spawnSubtypeWeights[type]`.
- Punkt spawnu: losowy punkt na obwodzie prostokąta poza ViewPort (margines = `spawnMargin`).

### DifficultyScaler
System skalowania trudności **w trakcie sesji** — im dłużej trwa sesja, tym silniejsi przeciwnicy.

#### Mechanizm
- `sessionTime` — czas w sekundach od startu bieżącej sesji (reset na każdej nowej sesji).
- Co `difficultyTickInterval` sekund (np. co 30s) przeliczany jest `difficultyMultiplier`.
- Wzrost może być liniowy lub krzywoliniowy (np. `1.0 + sessionTime / scaleDivisor`).

#### Wpływ na parametry Enemy (wszystkie per-enemy, w momencie spawnu):
| Parametr | Wzrost |
|---|---|
| `enemyHp` | `baseHp * difficultyMultiplier` |
| `enemyDmg` | `baseDmg * difficultyMultiplier` |
| `enemySpeed` | ograniczony wzrost, `baseSpeed * min(multiplier, speedCap)` |
| `spawnIntervalCount` | więcej Enemy per interwał w czasie |
| `spawnInterval` | krótszy interwał spawnu |

#### Konfiguracja (wystawiona w inspektorze)
```csharp
float difficultyTickInterval = 30f;   // Co ile sekund rośnie trudność
float scaleDivisor = 120f;            // Dzielnik — wolniejszy wzrost = większa wartość
float speedCap = 2.0f;                // Maksymalny mnożnik prędkości
float hpCap = 10.0f;                  // Maksymalny mnożnik HP
```

#### Wyświetlanie
- Opcjonalnie: wyświetl aktualny `difficultyMultiplier` na HUD jako "Wave X" lub "Threat Level"
- W Summary: pokaż najwyższy osiągnięty mnożnik trudności w sesji

### CollectableController
- Jeden obiekt zarządzający **wszystkimi aktywnymi Collectable**.
- W `Update()`:
  - Faza debounce: przesuwa Collectable w kierunku odwrotnym od gracza
  - Faza homing: przesuwa w kierunku gracza z prędkością `homingSpeed`
  - Sprawdza odległość gracza (collectDistance) → wyzwala debounce
  - Sprawdza finalny pickupRadius → zbiera
- Metody: `RegisterCollectable(CollectableData)`, `UnregisterCollectable(CollectableData)`, `DespawnAll()`

### CollectableSpawner
- Wywoływany przez EnemyController po śmierci Enemy.
- Spawni `N * collectableCountModifier` Collectable danego typu.
- Każdy Collectable jest rozmieszczany losowo w promieniu `spawnScatter` od pozycji śmierci Enemy.

### PlayerController
- Aktualizuje pozycję gracza do pozycji kursora (z clamping do granic).
- Reaguje na kolizje zarejestrowane przez EnemyController.
- Gdy HP ≤ 0: emituje `OnPlayerDead`.
- Metody: `TakeDamage(float dmg)`, `Heal(float amount)`, `ResetHp()`

### UpgradeSystem
- Wczytuje wszystkie `UpgradeNodeSO` z listy (przypisanej w Inspektorze lub z Resources).
- Trzyma słownik `Dictionary<string, UpgradeRuntimeState>` — runtime state per upgradeId.
- Na starcie: tylko węzeł korzenia ma `isUnlocked = true`, reszta `false`.
- Metoda `TryPurchaseUpgrade(string upgradeId)`:
  1. Sprawdza `isUnlocked == true` i `currentLevel < maxLevel`
  2. Sprawdza czy gracz ma wystarczającą ilość `costCurrency`
  3. Odejmuje koszt z StatsSystem
  4. `currentLevel++`
  5. Wywołuje `StatsSystem.ModifyStat(targetStat, valuesPerLevel[currentLevel])`
  6. Jeśli `currentLevel == 1`: ustawia `isUnlocked = true` dla wszystkich child węzłów z `unlocksOnLevel1`
  7. Emituje `OnUpgradePurchased`
- Po załadowaniu save'a: przywraca `currentLevel` i `isUnlocked` dla każdego upgradeId z pliku zapisu.

---

## 11. Optymalizacja

### Zasady ogólne
- **Brak Rigidbody** — wszystkie obiekty są "dumb transforms" bez fizyki Unity.
- **Brak indywidualnych Update()** w Enemy i Collectable — zarządzane centralnie przez Controller.
- **Object Pooling** dla Enemy i Collectable — brak instantiate/destroy w trakcie sesji.
- **Struct vs Class** — dane Enemy i Collectable jako structs lub małe klasy bez dziedziczenia (cache-friendly).

### Wykrywanie kolizji
- EnemyController trzyma tablicę/listę `EnemyData[]` — iteracja po ciągłej tablicy (cache-friendly).
- Sprawdzanie odległości: `(enemyPos - playerPos).sqrMagnitude < (radiusSum * radiusSum)` — unikamy sqrt.
- Kolizja z cooldownem: per-enemy float `lastHitTime`, sprawdzanie `Time.time - lastHitTime > hitCooldown`.

### Granice ViewPort
- Granice ViewPort przeliczone raz na start i cachowane jako `Rect playArea`.
- Despawn Enemy gdy `position.x < playArea.xMin - margin` etc.

### Renderowanie
- Sprite'y Enemy: SpriteRenderer z materialem wspierającym instancowanie (GPU Instancing jeśli wiele identycznych).
- Minimalna liczba draw calls — atlasy sprite'ów per typ.

---

## 12. Zapis i wczytywanie (SaveSystem)

### Slot
- Jeden slot zapisu.
- Plik: `Application.persistentDataPath/save.json`

### Dane do zapisu
```json
{
  "version": "1.0",
  "timestamp": "2025-...",
  "currencies": {
    "currency_A": 150,
    "currency_B": 30,
    "currency_C": 5,
    "currency_D": 0
  },
  "stats": {
    "playerMaxHp": 120,
    "playerDmg": 15,
    "playerDef": 2
  },
  "upgrades": [
    { "id": "sword_sharpness", "level": 2, "isUnlocked": true },
    { "id": "iron_will", "level": 1, "isUnlocked": true },
    { "id": "guard", "level": 0, "isUnlocked": true },
    { "id": "collector", "level": 0, "isUnlocked": false }
  ]
}
```

> Waluty są zapisywane po każdej sesji — kumulują się i nigdy nie resetują automatycznie.

### Kiedy zapisywać
- Przy przejściu do widoku Upgrade (po każdej sesji)
- Przy powrocie do Main Menu
- Przy zakupie Upgrade

### Wczytywanie
- Na starcie MainMenu: sprawdź czy plik istnieje → pokaż/ukryj przycisk Continue.
- Przy Continue lub New Game → GameScene: załaduj dane do StatsSystem i UpgradeSystem.
- New Game: usuń plik zapisu lub zainicjuj wartościami domyślnymi.

---

## 13. Juice i feel

### JuiceSystem — odpowiedzialność
Centralny system zarządzający efektami wizualnymi i dźwiękowymi. Nasłuchuje eventów z EventSystem.

### Efekty wizualne
| Zdarzenie | Efekt |
|---|---|
| Trafienie Enemy | Flash koloru na sprite'ie Enemy, mała eksplozja particle |
| Zmiana stanu HP Enemy (Stan 2, 3) | Animacja "crack" lub zmiana koloru + particle dymu |
| Śmierć Enemy | Większa eksplozja particle, ewentualnie screen shake (mały) |
| Spawn Enemy | Pojawienie się z "pop" animacją (scale 0 → 1) |
| Zebranie Collectable | Particle burst w kolorze collectabla, floating text "+1" |
| Homing Collectable | Trail renderer za Collectable podczas lotu |
| Player dostaje dmg | Flash tła na czerwono (vignette), pasek HP drżenie |
| Game Over | Slow-mo efekt przed przejściem do Summary |
| Debounce Collectable | Squash & stretch sprite'a Collectable |

### SFX
| Zdarzenie | Dźwięk |
|---|---|
| Trafienie Enemy | Uderzenie (metal/impact) |
| Śmierć Enemy | Eksplozja / pop |
| Zebranie Collectable | Coin/chime (per typ) |
| Zakup Upgrade | "Level up" sound |
| Przejście do Gameplay | Woosh / game start |
| Player dostaje dmg | Hit sound |
| Game Over | Defeat jingle |

### Muzyka
- Main Menu: ambientowa/spokojniejsza
- Gameplay: dynamiczna, narastająca
- Opcje głośności muzyki i SFX osobno (zapisywane w PlayerPrefs)

---

## 14. UI i UX

### Main Menu UI
- Logo gry
- Przyciski: New Game, Continue (warunkowy), Options, Credits, Quit
- Tło animowane (np. pętlująca animacja)

### HUD (widok Gameplay)
- Pasek HP gracza (lewy górny róg)
- Liczniki walut (prawy górny róg, ikona + liczba per waluta: Shard / Core / Crystal / Essence)
- Timer sesji (górny środek)
- **Threat Level** — aktualny poziom trudności (np. "Threat: 1.5x") obok timera
- Przycisk Stop/Pause (prawy górny, ikonka)

### Upgrade View UI
- Node Tree (center/main area) — scrollowalny canvas z węzłami połączonymi liniami
- Węzeł odblokowany: jasny, klikowalny; zablokowany: ciemny/przezroczysty
- Panel szczegółów (po kliknięciu węzła): nazwa, opis, aktualna wartość stat, następna wartość, koszt, przycisk Upgrade
- Sidebar: bieżące zasoby (Collectable per typ)
- Przyciski: Start Session, Options, Main Menu

### Summary View UI
- Tytuł: "Session Complete" / "Game Over"
- Lista statystyk sesji:
  - Czas trwania sesji
  - Liczba zabitych Enemy (per typ)
  - Zebrane waluty (per typ: Shard, Core, Crystal, Essence)
  - Zadane i otrzymane obrażenia
  - Najwyższy osiągnięty Threat Level
  - Powód końca sesji (HP = 0 / ręczne zatrzymanie)
- Przyciski: New Session, Go to Upgrade

### Options Panel (overlay)
- Slider głośności muzyki
- Slider głośności SFX
- Toggle fullscreen
- Dropdown rozdzielczości
- Przycisk Close/Back

---

## 15. Architektura systemów

### Diagram zależności

```
SystemsManager (Singleton)
├── GameManager
├── StatsSystem
├── UpgradeSystem
├── SaveSystem
├── ObjectPoolManager
├── EventSystem
├── EnemySpawner
├── EnemyController
├── DifficultyScaler
├── CollectableSpawner
├── CollectableController
├── PlayerController
└── JuiceSystem
```

### Przepływ danych — przykład śmierci Enemy

```
EnemyController.Update()
  → kolizja wykryta → Enemy.TakeDamage()
  → Enemy.hp <= 0
  → EventSystem.Emit(OnEnemyKilled, type, position)
      ├── CollectableSpawner.OnEnemyKilled() → spawnuje Collectable
      ├── JuiceSystem.OnEnemyKilled() → particle + sfx
      └── StatsSystem / UI updates
  → EnemyController.UnregisterEnemy()
  → ObjectPool.Return(enemyGO)
```

### Przepływ danych — zbieranie Collectable

```
CollectableController.Update()
  → gracz w collectDistance → trigger debounce + homing
  → gracz w pickupRadius
  → StatsSystem.ModifyStat(collectable_X_count, +amount)
  → EventSystem.Emit(OnCollectablePickedUp, type, amount)
      ├── JuiceSystem → efekt + floating text
      └── UI → aktualizacja licznika
  → CollectableController.UnregisterCollectable()
  → ObjectPool.Return(collectableGO)
```

### Script Execution Order (Unity)
1. SystemsManager
2. EventSystem
3. StatsSystem
4. GameManager
5. ObjectPoolManager
6. SaveSystem
7. DifficultyScaler
8. EnemyController, CollectableController
9. EnemySpawner, CollectableSpawner
10. PlayerController
11. JuiceSystem

---

## Appendix: Otwarte decyzje do podjęcia

| Temat | Pytanie |
|---|---|
| Art style | Pixel art? Vector? Jaka paleta kolorów dla tła i UI? |
| Game Over | Czy jest kontynuacja z penalty, czy zawsze full reset HP na start sesji? |
| Cooldown kolizji | Dokładna wartość cooldownu trafienia (sugestia: 250ms)? |
| Screen shake | Czy stosujemy i jak mocny? |
| Typ D enemy | Jaka mechanika specjalna? (split, eksplozja, teleport?) |
| Node Tree layout | Ile węzłów łącznie w finalnym drzewie? Ręczny układ czy proceduralne pozycjonowanie? |
| Skalowanie trudności | Liniowe czy krzywoliniowe? (np. `1 + t/120` vs krzywa kwadratowa) |
| Zapis walut | Czy ilości walut resetują się między sesjami, czy kumulują się między sesjami? |
| Summary — Threat Level | Czy pokazywać poziom trudności (mnożnik) na ekranie Summary? |

## Appendix: Rozwiązane decyzje (log)

| Temat | Decyzja |
|---|---|
| Typy Enemy | Kolor = typ (A=Niebieski, B=Zielony, C=Czerwony, D=Fioletowy) |
| Podtypy Enemy | Kształt geometryczny = podtyp (Trójkąt, Kwadrat, Sześciokąt, Okrąg) |
| Waluty Upgrade | 3–4 waluty; każdy upgrade przypisuje sobie jedną walutę jako koszt |
| Drop Enemy | Każdy Typ Enemy dropuje konkretną walutę (A→Shard, B→Core, C→Crystal, D→Essence) |
| Trwałość walut | Waluty kumulują się między sesjami, zapisywane przez SaveSystem, nigdy nie resetują |
| Progresja trudności | Enemy skalują się w trakcie sesji przez DifficultyScaler (timer-based multiplier) |
| Node Tree — odblokowywanie | Węzły niewidoczne dopóki zablokowane; odblokowanie po osiągnięciu lvl 1 przez rodzica |
| Node Tree — layout | Ręczny — pozycje węzłów ustawiane w Inspectorze w ScriptableObject |
| Node Tree — dane | Każdy Upgrade to ScriptableObject; runtime state (level, isUnlocked) trzymany osobno w UpgradeSystem |

