using Microsoft.Xna.Framework;

namespace DailyHryTracker.Games;

public static class GameCatalog
{
    public static readonly List<GameInfo> All = new()
    {
        new GameInfo
        {
            Id = "wordle", Url = "https://www.nytimes.com/games/wordle/index.html", Name = "Wordle", Tagline = "Uhodni anglické slovo o 5 písmenech",
            Help = "Máš 6 pokusů. Zelená = správné písmeno na správném místě, žlutá = písmeno ve slově je, ale jinde, šedá = ve slově není.",
            Color = new Color(83, 141, 78), Create = () => new WordleGame(),
        },
        new GameInfo
        {
            Id = "connections", Url = "https://www.nytimes.com/games/connections", Name = "Connections", Tagline = "Najdi 4 skupiny po 4 slovech",
            Help = "Vyber 4 slova, která spolu souvisí, a potvrď. Můžeš udělat nejvýš 4 chyby.",
            Color = new Color(186, 129, 197), Create = () => new ConnectionsGame(),
        },
        new GameInfo
        {
            Id = "krillion", Url = "https://krillion.io/", Name = "Krillion", Tagline = "Vzácné odpovědi = víc bodů",
            Help = "7 otázek, na každou 25 sekund. Čím méně obvyklou (ale správnou) odpověď napíšeš, tím víc bodů.",
            Color = new Color(64, 156, 255), Create = () => new KrillionGame(false),
        },
        new GameInfo
        {
            Id = "noillirk", Url = "https://noillirk.com/", Name = "Noillirk", Tagline = "Krillion naruby – boduje to nejčastější",
            Help = "7 otázek, na každou 25 sekund. Napiš to, co by napsala většina lidí – nejčastější odpovědi mají nejvíc bodů.",
            Color = new Color(255, 120, 90), Create = () => new KrillionGame(true),
        },
        new GameInfo
        {
            Id = "globle", Url = "https://globle-game.com/", Name = "Globle", Tagline = "Najdi tajnou zemi podle vzdálenosti",
            Help = "Napiš libovolnou zemi. Ukáže se vzdálenost k hledané zemi a barva – čím teplejší, tím blíž.",
            Color = new Color(230, 90, 60), Create = () => new GlobleGame(),
        },
        new GameInfo
        {
            Id = "worldle", Url = "https://worldle.teuteuf.fr/", Name = "Worldle", Tagline = "Poznej zemi podle tvaru",
            Help = "Uhodni zemi podle siluety. Na každý pokus dostaneš vzdálenost, směr a blízkost v %. Máš 6 pokusů.",
            Color = new Color(60, 180, 160), Create = () => new WorldleGame(),
        },
        new GameInfo
        {
            Id = "spotle", Url = "https://spotle.movie/", Name = "Spotle Movies", Tagline = "Uhodni film podle vlastností",
            Help = "Napiš film. U každého pokusu uvidíš, jestli sedí rok, žánr, režisér, země, délka a hodnocení (šipky = vyšší/nižší). Máš 10 pokusů.",
            Color = new Color(30, 185, 84), Create = () => new SpotleGame(),
        },
        new GameInfo
        {
            Id = "poople", Url = "https://poople.io/", Name = "Poople", Tagline = "Slovní žebřík až k POOP",
            Help = "Měň vždy jedno písmeno, každé mezislovo musí být skutečné anglické slovo o 4 písmenech. Cíl: POOP. Čím méně kroků, tím líp.",
            Color = new Color(150, 100, 60), Create = () => new PoopleGame(),
        },
        new GameInfo
        {
            Id = "wikirace", Url = "https://www.thewikigame.com/", Name = "WikiRace", Tagline = "Doklikej se z článku A do článku B",
            Help = "Klikej na odkazy v článcích anglické Wikipedie a dostaň se na cílový článek na co nejméně kliků. Potřebuje internet.",
            Color = new Color(200, 200, 200), NeedsNetwork = true, Create = () => new WikiRaceGame(),
        },
    };

    public static GameInfo Get(string id) => All.First(g => g.Id == id);
}
