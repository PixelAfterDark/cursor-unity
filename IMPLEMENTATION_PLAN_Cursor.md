# Plan Implementacji — CURSOR
**Wersja:** 1.0  
**Engine:** Unity 6 | C#

> Zasada: każdy etap kończy się **działającą, testowalną wersją gry**. Nie przechodzimy dalej, dopóki poprzedni etap nie jest stabilny.

---

## Mapa etapów (overview)

```
ETAP 1 → ETAP 2 → ETAP 3 → ETAP 4 → ETAP 5 → ETAP 6 → ETAP 7 → ETAP 8 → ETAP 9
Szkielet  Core      Collec-   Diffi-    UI /     Save     Upgrade   Wizualne  Juice /
projektu  gameplay  table     culty     Widoki   System   System    Enemy     Polish
          loop      system    Scaler    & HUD
```

Etapy 1–4 to "szary boks" — brak grafiki, tylko działająca logika.  
Etapy 5–9 to warstwa prezentacji i systemów meta-game.

---

## ETAP 1 — Szkielet projektu

**Cel:** Projekt Unity skonfigurowany, systemy fundamentalne działają, można przełączać sceny.

### Zadania

**T1.1 — Setup projektu Unity**
- Nowy projekt Unity 6, URP (2D)
- Struktura folderów: `Scripts/`, `Prefabs/`, `ScriptableObjects/`, `Scenes/`, `Art/`, `Audio/`
- Dwie sceny: `MainMenu`, `GameScene`
- Script Execution Order skonfigurowany (wg GDD)

**T1.2 — SystemsManager**
- Singleton `SystemsManager` — nie niszczy się przy ładowaniu scen (`DontDestroyOnLoad`)
- Publiczne pola referencji do wszystkich systemów (początkowo puste, uzupełniane w kolejnych etapach)
- Inicjalizacja w `Awake()` przed wszystkimi innymi systemami

**T1.3 — EventSystem**
- Statyczna klasa `GameEvents` z Action-based eventami
- Zdefiniowane eventy: `OnPlayerDead`, `OnSessionStop`, `OnEnemyKilled`, `OnCollectablePickedUp`, `OnUpgradePurchased`, `OnGameStateChanged`
- Helper metody: `Subscribe`, `Unsubscribe`, `Emit` dla każdego eventu

**T1.4 — GameManager**
- `enum GameState { Upgrade, Gameplay, Summary }`
- Metoda `ChangeState(GameState newState)` + emitowanie `OnGameStateChanged`
- Obsługa przejść między scenami (`MainMenu` ↔ `GameScene`)
- Na starcie `GameScene` domyślnie ładuje stan `Upgrade`

**T1.5 — Podstawowe UI MainMenu**
- Przyciski: New Game, Options (placeholder), Credits (placeholder), Quit
- New Game → ładuje `GameScene`
- Quit → `Application.Quit()`

✅ **Test etapu:** Kliknięcie New Game ładuje GameScene. GameManager inicjalizuje się bez błędów. Eventy można emitować i odbierać w logach.

---

## ETAP 2 — Core Gameplay Loop

**Cel:** Gracz się porusza, Enemy spawnują, latają, kolizja działa, HP spada. Brak grafiki (placeholder shapes).

### Zadania

**T2.1 — StatsSystem (podstawowe staty)**
- Klasa `StatsSystem` z podstawowymi polami: `playerMaxHp`, `playerCurrentHp`, `playerDmg`, `playerDef`, `playerRadius`, `collectDistance`
- Staty per Enemy type: `enemyBaseHp`, `enemyBaseDmg`, `enemySpeedMin/Max`, `enemyRotSpeedMin/Max`, `enemyRadius`
- API: `GetStat`, `SetStat`, `ModifyStat`
- Wartości domyślne twardo zakodowane na start (przed UpgradeSystem)
- Metoda `ResetSessionStats()` — reset `playerCurrentHp` do `playerMaxHp`

**T2.2 — ObjectPoolManager**
- Generyczny `ObjectPool<T>` gdzie T : MonoBehaviour
- Metody: `Get()`, `Return(obj)`
- Pre-warm przy inicjalizacji (konfigurowalny rozmiar startowy)
- Rejestracja pul per prefab (klucz: `PoolKey` enum lub string)

**T2.3 — EnemyData + EnemyController**
- `EnemyData` struct/class: `position`, `direction`, `speed`, `rotSpeed`, `currentHp`, `maxHp`, `type`, `subtype`, `lastHitTime`, `transform ref`
- `EnemyController` — singleton zarządzający listą `List<EnemyData>`
- `Update()`: pętla po wszystkich Enemy — ruch (`pos += dir * speed * dt`), obrót, sprawdzanie granic ViewPort → despawn
- Metody: `RegisterEnemy(EnemyData)`, `UnregisterEnemy(EnemyData)`, `DespawnAll()`
- Granice ViewPort cachowane jako `Rect playArea` (przeliczane z `Camera.main` raz na start + na resize)

**T2.4 — EnemySpawner**
- `StartSpawn()` / `StopSpawn()`
- Burst spawn: instantiuje `spawnBurstCount` Enemy natychmiast przy starcie
- Interval spawn: Coroutine co `spawnInterval` sekund
- Punkt spawnu: losowy punkt na prostokącie poza ViewPort + margines
- Kierunek: `(centrum + Random.insideUnitCircle * centerOffset - spawnPos).normalized`
- Prędkość i rotacja: losowane z zakresów min/max
- Na razie jeden typ Enemy (Type A), kilka typów po T2 jest stabilne
- Rejestruje każdy Enemy w EnemyController

**T2.5 — PlayerController**
- Pozycja gracza = `Camera.main.ScreenToWorldPoint(Input.mousePosition)`, clampowana do `playArea`
- Placeholder sprite (białe kółko)
- `TakeDamage(float dmg)` → `playerCurrentHp -= max(0, dmg - playerDef)`
- Gdy `playerCurrentHp <= 0` → emit `OnPlayerDead`
- `ResetHp()` na start każdej sesji

**T2.6 — Collision Detection**
- W `EnemyController.Update()`: dla każdego aktywnego Enemy sprawdź `sqrMagnitude < radiusSum²`
- Cooldown per Enemy: `Time.time - enemy.lastHitTime > hitCooldown` (np. 0.25s)
- Na kolizji: `PlayerController.TakeDamage(enemyDmg)` + `enemy.currentHp -= playerDmg`
- Gdy `enemy.currentHp <= 0`: emit `OnEnemyKilled(type, position)` → despawn → pula

**T2.7 — Podłączenie GameManager do eventów**
- `OnPlayerDead` → `ChangeState(Summary)`
- Przycisk Stop/Pause na HUD → emit `OnSessionStop` → `ChangeState(Summary)`
- `ChangeState(Gameplay)` → `StartSpawn()`, `ResetHp()`
- `ChangeState(Summary / Upgrade)` → `StopSpawn()`, `DespawnAll()`

✅ **Test etapu:** Gracz (kółko) porusza się za myszką. Enemy (kwadraty placeholder) spawnują poza ekranem, lecą przez ekran i znikają. Kolizja zadaje dmg obu stronom. Śmierć gracza przełącza widok. Logi potwierdzają poprawne eventy.

---

## ETAP 3 — Collectable System

**Cel:** Po śmierci Enemy spawnują przedmioty, gracz je zbiera, waluta jest śledzona.

### Zadania

**T3.1 — CollectableData + CollectableController**
- `CollectableData`: `position`, `currencyType`, `amount`, `state` (Waiting / Debounce / Homing), `debounceTimer`, `transform ref`
- `CollectableController` — zarządza listą `List<CollectableData>`
- `Update()`:
  - Stan `Waiting`: sprawdź odległość do gracza < `collectDistance` → przejdź do `Debounce`
  - Stan `Debounce`: przesuń w kierunku przeciwnym przez `debounceTime` sekund → przejdź do `Homing`
  - Stan `Homing`: przesuń w stronę gracza z `homingSpeed` → gdy odległość < `pickupRadius` → zbierz
- Zebranie: `StatsSystem.ModifyStat(currency_X, +amount)` → emit `OnCollectablePickedUp` → despawn → pula
- Metody: `RegisterCollectable`, `UnregisterCollectable`, `DespawnAll()`

**T3.2 — CollectableSpawner**
- Nasłuchuje `OnEnemyKilled(type, position)`
- Pobiera z EnemyData: ile (`enemyCollectableCount * collectableCountModifier`) i jakiego typu
- Spawni Collectable w pętli, każdy z losowym offsetem w promieniu `spawnScatter` od `position`
- Rejestruje każdy w CollectableController

**T3.3 — Walutowe staty w StatsSystem**
- Dodanie pól: `currency_A`, `currency_B`, `currency_C`, `currency_D` (int, persystentne)
- `ModifyStat` dla walut: tylko addytywnie, nigdy poniżej 0
- Metoda `SpendCurrency(CurrencyType type, int amount) : bool` — zwraca false jeśli za mało

✅ **Test etapu:** Enemy umiera → pojawia się kilka kółek (placeholder) → po zbliżeniu gracza odskok + homing → zebranie inkrementuje walutę w logach.

---

## ETAP 4 — DifficultyScaler

**Cel:** Im dłużej trwa sesja, tym silniejsi Enemy.

### Zadania

**T4.1 — Session Timer**
- `sessionTime` float w GameManager — reset na starcie każdej sesji, inkrementowany w `Update()` tylko w stanie `Gameplay`
- Dostępny przez `GameManager.Instance.SessionTime`

**T4.2 — DifficultyScaler**
- Subskrybuje `OnGameStateChanged` → reset przy starcie Gameplay
- Co `difficultyTickInterval` sekund (Coroutine) przelicza `difficultyMultiplier = 1f + sessionTime / scaleDivisor`
- Cappy: `hpMultiplier = min(difficultyMultiplier, hpCap)`, `speedMultiplier = min(difficultyMultiplier, speedCap)`
- Publiczne property: `HpMultiplier`, `SpeedMultiplier`, `DmgMultiplier`
- EnemySpawner pobiera te wartości przy każdym spawnie i aplikuje do EnemyData

**T4.3 — Konfiguracja w Inspektorze**
- `difficultyTickInterval` = 30f
- `scaleDivisor` = 120f
- `hpCap` = 10f, `speedCap` = 2.0f, `dmgCap` = 5.0f
- Wszystkie jako `[SerializeField]` pola w DifficultyScaler

✅ **Test etapu:** Po 30 sekundach sesji Enemy są zauważalnie szybsi/wytrzymalsi. Multiplier widoczny w logach.

---

## ETAP 5 — UI, Widoki i HUD

**Cel:** Wszystkie trzy widoki GameScene działają wizualnie. MainMenu gotowe funkcjonalnie.

### Zadania

**T5.1 — GameScene View Manager**
- `ViewManager` zarządza trzema panelami Canvas: `UpgradePanel`, `GameplayPanel`, `SummaryPanel`
- Nasłuchuje `OnGameStateChanged` → aktywuje właściwy panel, dezaktywuje pozostałe
- Animacja przejścia (prosty fade lub instant)

**T5.2 — HUD (Gameplay Panel)**
- Pasek HP: `Slider` lub custom bar, aktualizowany przez event lub polling PlayerController
- Liczniki walut: 4x (ikona + TextMeshPro liczba), aktualizowane na `OnCollectablePickedUp`
- Timer sesji: TextMeshPro, aktualizowany co klatkę z `GameManager.SessionTime`
- Threat Level: TextMeshPro "Threat: x1.0", aktualizowany co tick DifficultyScaler
- Przycisk Stop → emit `OnSessionStop`

**T5.3 — Summary Panel**
- Wyświetla: czas sesji, walutę zebraną w tej sesji (per typ), powód końca
- `SessionStats` struct zbierający dane w trakcie sesji (kills, currencies earned, dmg dealt/taken)
- Uzupełniany przez eventy w trakcie Gameplay, czyszczony na starcie każdej sesji
- Przyciski: New Session (`ChangeState(Gameplay)`), Go to Upgrade (`ChangeState(Upgrade)`)

**T5.4 — Upgrade Panel (placeholder)**
- Na razie: pusty panel z przyciskiem "Start Session" i "Main Menu"
- Node Tree zostanie dodany w Etapie 7
- Wyświetla aktualne stany walut (te same liczniki co HUD)

**T5.5 — MainMenu finalizacja**
- Przycisk Continue: aktywny tylko gdy `SaveSystem.SaveExists()` (na razie hardcode false, podłączony w Etapie 6)
- Wszystkie przyciski działają poprawnie

✅ **Test etapu:** Pełna pętla UI działa: MainMenu → GameScene (Upgrade) → Gameplay (HUD widoczny, waluta się aktualizuje, timer leci) → Summary (poprawne stats) → z powrotem do Upgrade lub nowa sesja.

---

## ETAP 6 — SaveSystem

**Cel:** Progres jest zapisywany i wczytywany. Continue działa. Waluty przeżywają restart gry.

### Zadania

**T6.1 — SaveData model**
- `SaveData` klasa serializowalna do JSON:
  - `string version`
  - `Dictionary<string, int> currencies` (currency_A, B, C, D)
  - `Dictionary<string, float> stats` (tylko te zmodyfikowane przez upgrady)
  - `List<UpgradeSaveEntry> upgrades` — każdy entry: `{ id, level, isUnlocked }`

**T6.2 — SaveSystem implementacja**
- `SaveSystem` singleton
- `void Save()` — serializuje aktualne dane ze StatsSystem + UpgradeSystem do JSON → zapis do `Application.persistentDataPath/save.json`
- `SaveData Load()` — deserializacja z pliku, null jeśli brak
- `bool SaveExists()` — sprawdza czy plik istnieje
- `void DeleteSave()` — usuwa plik (New Game)

**T6.3 — Podłączenie SaveSystem**
- Automatyczny save przy: `ChangeState(Upgrade)`, powrocie do MainMenu
- Wczytanie po załadowaniu GameScene: `StatsSystem.LoadFrom(saveData)`, `UpgradeSystem.LoadFrom(saveData)` (UpgradeSystem stub na razie)
- MainMenu: `Continue` widoczny jeśli `SaveSystem.SaveExists()`
- New Game: `SaveSystem.DeleteSave()` → reset StatsSystem do defaultów

**T6.4 — Migracja walut między sesjami**
- Waluty w StatsSystem NIE resetują się przy `ResetSessionStats()`
- `ResetSessionStats()` resetuje tylko: `playerCurrentHp`
- Po sesji: walutę zebrana w sesji jest już w StatsSystem (dodawana live) → save automatycznie ją utrwali

✅ **Test etapu:** Zacznij grę, zbierz trochę waluty, zamknij grę, uruchom ponownie → Continue dostępne → waluta zachowana. New Game kasuje postęp.

---

## ETAP 7 — Upgrade System

**Cel:** Działające drzewo upgradów z wizualnym Node Tree na Canvas.

### Zadania

**T7.1 — UpgradeNodeSO**
- `UpgradeNodeSO : ScriptableObject`
- Pola: `upgradeId`, `upgradeName`, `description`, `maxLevel`, `targetStat` (enum `StatType`), `valuesPerLevel[]`, `costsPerLevel[]`, `costCurrency`, `unlocksOnLevel1[]` (List<UpgradeNodeSO>), `nodePosition` (Vector2)
- Stwórz kilka przykładowych SO w edytorze (5-8 węzłów), ustawiając ręcznie pozycje i połączenia

**T7.2 — UpgradeSystem**
- `UpgradeSystem` singleton
- Wczytuje wszystkie `UpgradeNodeSO` z listy przypisanej w Inspektorze
- `Dictionary<string, UpgradeRuntimeState>` — runtime state: `currentLevel`, `isUnlocked`
- Inicjalizacja: tylko węzeł korzenia `isUnlocked = true`, reszta `false`
- `TryPurchaseUpgrade(string upgradeId)`:
  1. Walidacja (isUnlocked, level < max, wystarczająca waluta)
  2. `StatsSystem.SpendCurrency(type, cost)`
  3. `currentLevel++`
  4. `StatsSystem.ModifyStat(targetStat, valuesPerLevel[currentLevel])`
  5. Jeśli `currentLevel == 1`: unlock children
  6. Emit `OnUpgradePurchased`
- `LoadFrom(SaveData)` — przywraca levele i isUnlocked z save

**T7.3 — Node Tree UI**
- `UpgradeNodeUI` prefab: okrągły/kwadratowy button z TextMeshPro (nazwa, level), kolor wg stanu
- `UpgradeTreeView` MonoBehaviour:
  - Przy inicjalizacji: dla każdego SO → instantiuj `UpgradeNodeUI` na Canvas w pozycji `nodePosition`
  - Węzły `isUnlocked == false`: `SetActive(false)`
  - Rysowanie linii między węzłami: `UILineRenderer` (własny skrypt lub asset) parent→child
- Kliknięcie węzła → otwiera `UpgradeDetailPanel` (sidebar)

**T7.4 — UpgradeDetailPanel**
- Wyświetla: nazwa, opis, current level / max level, aktualna wartość staty, następna wartość, koszt (ikona waluty + liczba)
- Przycisk Upgrade: aktywny jeśli `isUnlocked && level < max && wystarczająca waluta`
- Po zakupie: odświeża panel i tree (aktywuje nowe węzły, rysuje nowe linie)

**T7.5 — Integracja z SaveSystem**
- `UpgradeSystem.GetSaveData()` → lista `UpgradeSaveEntry`
- Podłączenie do `SaveSystem.Save()`

✅ **Test etapu:** Widok Upgrade pokazuje węzły na Canvas. Kliknięcie root węzła, zapłata walutą, level rośnie, nowe węzły się pojawiają. Po restarcie gry drzewo wczytane poprawnie ze save'a.

---

## ETAP 8 — Wizualne Enemy (Typ × Podtyp)

**Cel:** Enemy wyglądają zgodnie z GDD — kolor per typ, kształt per podtyp, 3 stany HP.

### Zadania

**T8.1 — Sprite'y Enemy**
- Stwórz 4 kształty geometryczne (Trójkąt, Kwadrat, Sześciokąt, Okrąg) — mogą być proste sprite'y lub generowane proceduralne w Unity
- Stwórz materiały/palety per typ: Niebieski (A), Zielony (B), Czerwony (C), Fioletowy (D)
- Macierz: 4 typy × 4 podtypy = 16 kombinacji sprite + kolor

**T8.2 — HP States**
- W `EnemyData`: property `HpState` → 1 / 2 / 3 wg progów 66% / 33%
- `EnemyController.Update()`: gdy HP State zmienił się od ostatniej klatki → `SpriteRenderer.color` lub podmiana materiału
- Stan 1: kolor bazowy, Stan 2: przyciemniony (~70% brightness), Stan 3: mocno przyciemniony + lekki czerwony tint

**T8.3 — EnemySpawner — wszystkie typy i podtypy**
- Konfiguracja weighted random dla 4 typów i 4 podtypów w Inspectorze (`spawnTypeWeights[]`, `spawnSubtypeWeights[][]`)
- EnemySpawner wybiera typ i podtyp → pobiera odpowiedni prefab z ObjectPool → ustawia sprite i kolor

**T8.4 — Player Sprite**
- Sprite miecza zamiast placeholdera
- Obrót: skierowany w kierunku ostatniego ruchu myszy (opcjonalnie)

✅ **Test etapu:** Na ekranie widać kolorowe geometryczne kształty. Uszkodzone Enemy ciemnieją. Różne typy Enemy spawnują z różnymi wagami.

---

## ETAP 9 — Juice i Polish

**Cel:** Gra jest przyjemna wizualnie i audio. Każda akcja ma feedback.

### Zadania

**T9.1 — JuiceSystem — setup**
- `JuiceSystem` singleton subskrybujący eventy: `OnEnemyKilled`, `OnCollectablePickedUp`, `OnPlayerDead`, `OnUpgradePurchased`
- Osobna metoda per event: `OnEnemyKilledJuice(type, position)` itp.

**T9.2 — Particle Effects**
- Hit Enemy: małe particle burst w kolorze Enemy type (Particle System z puli)
- Śmierć Enemy: większy burst + rozbicie na fragmenty
- Spawn Enemy: pop-in (scale 0→1 animacja, np. DOTween lub LeanTween)
- Zebranie Collectable: mały burst w kolorze waluty
- Homing Collectable: Trail Renderer
- Player dostaje dmg: vignette flash (UI Image na całym ekranie, alpha 0→0.3→0 bardzo szybko)

**T9.3 — Floating Text**
- Po zebraniu Collectable: "+N" text w kolorze waluty, unosi się i zanika
- Pool tekstów (TextMeshPro) zarządzany przez JuiceSystem

**T9.4 — Screen Shake**
- Lekki shake przy śmierci Enemy (szczególnie większych typów)
- Mocniejszy shake przy Game Over
- Implementacja: Coroutine przesuwająca kamerę o losowy offset, tłumiony w czasie

**T9.5 — SFX**
- AudioManager singleton z pulą `AudioSource`
- Dźwięki: hit_enemy, enemy_death, collectable_pickup_A/B/C/D, upgrade_purchase, game_over, session_start
- Słuchaj eventów → odtwórz odpowiedni clip

**T9.6 — Muzyka**
- Dwa tracki: MainMenu, Gameplay
- Crossfade przy przejściu scen
- Slider głośności w Options podłączony do `AudioMixer`

**T9.7 — Options Panel**
- Slider muzyki → `AudioMixer.SetFloat("MusicVolume", ...)`
- Slider SFX → `AudioMixer.SetFloat("SFXVolume", ...)`
- Toggle fullscreen
- Ustawienia zapisywane w `PlayerPrefs`

✅ **Test etapu:** Gra jest "juicy" — każda akcja ma wizualny i audio feedback. Session start/end brzmi i wygląda dobrze.

---

## Kolejność priorytetów — skrót

| Etap | Co daje | Bez tego nie działa |
|---|---|---|
| 1 — Szkielet | Fundament | Wszystko |
| 2 — Core Loop | Grywalna sesja | Etap 3, 4, 5 |
| 3 — Collectable | Progres waluty | Etap 6, 7 |
| 4 — Difficulty | Napięcie w sesji | Etap 8 (pełne typy) |
| 5 — UI/Widoki | Pełna pętla gry | Etap 6 (Continue) |
| 6 — Save | Persystencja | Etap 7 (load upgrade) |
| 7 — Upgrade | Meta-game | Etap 8, 9 |
| 8 — Wizualne | Czytelność | Etap 9 |
| 9 — Juice | Feel & polish | — |

---

## Zależności krytyczne (co od czego zależy)

```
StatsSystem ←── PlayerController
           ←── EnemySpawner (odczyt statów Enemy)
           ←── UpgradeSystem (zapis zmian)
           ←── CollectableController (zapis waluty)
           ←── SaveSystem (odczyt/zapis)

ObjectPoolManager ←── EnemySpawner
                  ←── CollectableSpawner
                  ←── JuiceSystem (particle, floating text)

GameManager ←── EnemySpawner (Start/StopSpawn)
            ←── EnemyController (DespawnAll)
            ←── CollectableController (DespawnAll)
            ←── ViewManager (zmiana widoków)
            ←── SaveSystem (auto-save na zmianie stanu)

UpgradeSystem ←── StatsSystem (aplikowanie statów)
              ←── SaveSystem (load/save)
              ←── UpgradeTreeView (UI)
```

---

## Notatki dla Claude Code

- Zacznij od **T1.1–T1.5** i nie ruszaj się dalej bez potwierdzenia że szkielet działa
- Każdy system jako osobny plik `.cs`, jeden MonoBehaviour / klasa per plik
- Nazwy plików = nazwy klas (konwencja Unity)
- Wszystkie `[SerializeField]` pola widoczne w Inspektorze — hardkodowane wartości tylko tymczasowo
- Prefaby Enemy i Collectable bez żadnych Collider/Rigidbody komponentów
- Żadnych `FindObjectOfType` w runtime — zawsze przez `SystemsManager.Instance`
- Logowanie DEBUG: każdy kluczowy event loguje się z tagiem `[SystemName]` na etapach 1–4, wyłączone w etapach 5+
