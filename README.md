# Daily Hry Tracker

Desktopová aplikace v C# / MonoGame, která sleduje, jak hraješ denní hry.

**Denní hra** se otevře jako originální web přímo v okně aplikace: nahoře zůstane lišta aplikace a pod ní je vestavěný prohlížeč (WebKit na macOS, WebView2 na Windows). Nic se neotevírá v Safari ani v Chromu a nevzniká žádné další okno. **Křížek vpravo nahoře** web zavře a hru dnes odškrtne, tlačítko **← Přehled** ho zavře bez odškrtnutí. Přihlášení, cookies a statistiky webů zůstávají uložené mezi spuštěními.

| Hra | Web |
|---|---|
| Wordle | nytimes.com/games/wordle |
| Connections | nytimes.com/games/connections |
| Krillion | krillion.io |
| Noillirk | noillirk.com |
| Globle | globle-game.com |
| Worldle | worldle.teuteuf.fr |
| Spotle Movies | spotle.movie |
| Poople | poople.io |
| WikiRace | thewikigame.com |

Každá hra má navíc **nekonečnou** verzi, která běží přímo v aplikaci a generuje pokaždé nové kolo.

## Funkce

- **Heatmapa aktivity** jako na GitHubu (poslední rok, intenzita = počet dohraných her za den)
- **Odškrtávání**: křížek v liště nad webem hru zavře a odškrtne; jde to i ručně zaškrtávátkem na kartě
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

Data se ukládají do `~/Library/Application Support/DailyHryTracker` (macOS), `~/.config/DailyHryTracker` (Linux) nebo `%APPDATA%\DailyHryTracker` (Windows). Data vestavěného prohlížeče jsou na Windows v podsložce `Web`, na macOS je spravuje systémový WebKit.

## Síť a požadavky

- **Denní hry** jsou weby, takže potřebují internet.
- Windows 10/11 mají WebView2 v systému, macOS používá systémový WebKit.
- Na **Linuxu** se web do okna vložit neumí, otevře se v samostatném okně aplikace (potřebuje WebKitGTK, `libwebkit2gtk-4.1`) a po jeho zavření se aplikace zeptá, jestli hru odškrtnout.
- **Nekonečné verze**: Globle/Worldle si poprvé stáhnou obrysy zemí ([Natural Earth](https://www.naturalearthdata.com/), public domain) a uloží si je. WikiRace čte anglickou Wikipedii. Ostatní fungují offline.

## Struktura

```
DailyHryTracker.sln
src/DailyHryTracker/
  Core/      UI vrstva (kreslení, text, tlačítka, textová pole, našeptávač), EmbeddedWeb = web vložený do okna, WebGames = samostatné okno pro Linux
  Data/      profil, ukládání, série, ocenění, přátelé
  Games/     katalog her s adresami webů, nekonečné mini-hry + Data/ (slovníky, země, filmy, otázky)
  Screens/   přehled, statistiky, ocenění, přátelé, nastavení, herní obrazovka
```

Fonty se načítají za běhu ze systému (Arial / Segoe UI / DejaVu Sans), takže není potřeba content pipeline. Vlastní font můžeš dát jako `Fonts/Regular.ttf` a `Fonts/Bold.ttf` vedle aplikace.
