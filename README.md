# Daily Hry Tracker

Desktopová aplikace v C# / MonoGame, která sleduje, jak hraješ denní hry. Všech 9 her běží přímo v aplikaci, žádné odkazy do prohlížeče:

| Hra | Popis |
|---|---|
| Wordle | anglické slovo o 5 písmenech, 6 pokusů |
| Connections | 16 slov → 4 skupiny, max. 4 chyby |
| Krillion | 7 otázek × 25 s, vzácné odpovědi = víc bodů |
| Noillirk | Krillion naruby, nejvíc bodů má nejčastější odpověď |
| Globle | hledání země podle vzdálenosti (tepelná mapa) |
| Worldle | poznávání země podle siluety (vzdálenost, směr, %) |
| Spotle Movies | hádání filmu podle roku, žánru, režiséra, země, délky a hodnocení |
| Poople | slovní žebřík ke slovu POOP |
| WikiRace | proklikávání anglickou Wikipedií z článku A do článku B |

Každá hra má **denní** verzi (pro všechny stejná, odvozená od data) a **nekonečnou** verzi s náhodnými koly.

## Funkce

- **Heatmapa aktivity** jako na GitHubu (poslední rok, intenzita = počet dohraných her za den)
- **Odškrtávání**: hry dohrané v aplikaci se odškrtnou samy, ručně si můžeš odškrtnout i hru, kterou jsi hrál jinde
- **Série**: celková série i série pro každou hru zvlášť
- **Ocenění** (38): série 3–365 dní, perfektní dny a týdny, počty her, mistrovské výkony v jednotlivých hrách, přátelé
- **Statistiky**: úspěšnost, průměrné a nejlepší skóre, nekonečné hry, graf za posledních 30 dní
- **Přátelé a žebříčky přes soubory**, bez serveru:
  1. Na záložce *Přátelé* klikni na **Exportovat můj soubor**. Na ploše se vytvoří `DailyHry-<jméno>.dht.json`.
  2. Pošli ho kamarádovi (chat, e-mail…).
  3. Kamarád soubor přetáhne do okna aplikace nebo ho dá do složky přátel.
  4. V žebříčku se objeví tvoje jméno a statistiky i s datem, ke kterému platí. Pro aktuální data stačí poslat nový export; nahradí ten starý.

## Stažení

Hotové buildy jsou v sekci **Releases** (Windows, macOS Apple Silicon / Intel, Linux). Stačí rozbalit zip a spustit `DailyHryTracker`.
Na macOS je aplikace nepodepsaná, takže ji při prvním spuštění otevři přes pravé tlačítko → Otevřít, nebo spusť `xattr -cr DailyHryTracker` ve složce.

## Spuštění ze zdrojáků

Potřebuješ .NET 9 SDK.

```bash
dotnet run --project src/DailyHryTracker
```

Data se ukládají do `~/.config/DailyHryTracker` (macOS/Linux) nebo `%APPDATA%\DailyHryTracker` (Windows).

## Síť

- **Globle/Worldle** si při prvním spuštění stáhnou obrysy zemí ([Natural Earth](https://www.naturalearthdata.com/), public domain, ~800 kB) a uloží si je. Bez internetu hrají v náhradním režimu (středy zemí, nápovědy).
- **WikiRace** potřebuje internet (MediaWiki API).
- Ostatní hry fungují úplně offline.

## Struktura

```
DailyHryTracker.sln
src/DailyHryTracker/
  Core/      UI vrstva (kreslení, text, tlačítka, textová pole, našeptávač)
  Data/      profil, ukládání, série, ocenění, přátelé
  Games/     jednotlivé mini-hry + Data/ (slovníky, země, filmy, otázky)
  Screens/   přehled, statistiky, ocenění, přátelé, nastavení, herní obrazovka
```

Fonty se načítají za běhu ze systému (Arial / Segoe UI / DejaVu Sans), takže není potřeba content pipeline. Vlastní font můžeš dát jako `Fonts/Regular.ttf` a `Fonts/Bold.ttf` vedle aplikace.
