using System.Globalization;
using DailyHryTracker.Core;

namespace DailyHryTracker.Games.Data;

public class Movie
{
    public string Title;
    public int Year;
    public string Genre;
    public string Director;
    public string Country;
    public int Runtime;
    public double Rating;
    public string Norm;
}

public static class Movies
{
    // název; rok; hlavní žánr; režisér; země; délka (min); hodnocení (přibližně, 0–10)
    const string Raw = @"
The Shawshank Redemption;1994;Drama;Frank Darabont;USA;142;9.3
The Godfather;1972;Krimi;Francis Ford Coppola;USA;175;9.2
The Dark Knight;2008;Akční;Christopher Nolan;USA;152;9.0
Pulp Fiction;1994;Krimi;Quentin Tarantino;USA;154;8.9
Forrest Gump;1994;Drama;Robert Zemeckis;USA;142;8.8
Inception;2010;Sci-fi;Christopher Nolan;USA;148;8.8
Fight Club;1999;Drama;David Fincher;USA;139;8.8
The Matrix;1999;Sci-fi;Lana a Lilly Wachowski;USA;136;8.7
Goodfellas;1990;Krimi;Martin Scorsese;USA;146;8.7
Interstellar;2014;Sci-fi;Christopher Nolan;USA;169;8.7
Se7en;1995;Thriller;David Fincher;USA;127;8.6
The Silence of the Lambs;1991;Thriller;Jonathan Demme;USA;118;8.6
Saving Private Ryan;1998;Válečný;Steven Spielberg;USA;169;8.6
Schindler's List;1993;Drama;Steven Spielberg;USA;195;9.0
Spirited Away;2001;Animovaný;Hayao Miyazaki;Japonsko;125;8.6
The Green Mile;1999;Drama;Frank Darabont;USA;189;8.6
Parasite;2019;Thriller;Bong Joon-ho;Jižní Korea;132;8.5
The Lion King;1994;Animovaný;Roger Allers;USA;88;8.5
Gladiator;2000;Akční;Ridley Scott;USA;155;8.5
The Departed;2006;Krimi;Martin Scorsese;USA;151;8.5
The Prestige;2006;Thriller;Christopher Nolan;USA;130;8.5
Back to the Future;1985;Sci-fi;Robert Zemeckis;USA;116;8.5
Whiplash;2014;Drama;Damien Chazelle;USA;106;8.5
Alien;1979;Sci-fi;Ridley Scott;USA;117;8.5
Django Unchained;2012;Western;Quentin Tarantino;USA;165;8.5
The Intouchables;2011;Komedie;Olivier Nakache;Francie;112;8.5
Psycho;1960;Horor;Alfred Hitchcock;USA;109;8.5
Terminator 2: Judgment Day;1991;Akční;James Cameron;USA;137;8.6
Casablanca;1942;Romantický;Michael Curtiz;USA;102;8.5
Apocalypse Now;1979;Válečný;Francis Ford Coppola;USA;147;8.4
WALL-E;2008;Animovaný;Andrew Stanton;USA;98;8.4
The Shining;1980;Horor;Stanley Kubrick;USA;146;8.4
Joker;2019;Drama;Todd Phillips;USA;122;8.4
Avengers: Endgame;2019;Akční;Anthony a Joe Russo;USA;181;8.4
Coco;2017;Animovaný;Lee Unkrich;USA;105;8.4
Oldboy;2003;Thriller;Park Chan-wook;Jižní Korea;101;8.4
Toy Story;1995;Animovaný;John Lasseter;USA;81;8.3
Amadeus;1984;Drama;Miloš Forman;USA;160;8.4
One Flew Over the Cuckoo's Nest;1975;Drama;Miloš Forman;USA;133;8.7
Good Will Hunting;1997;Drama;Gus Van Sant;USA;126;8.3
Reservoir Dogs;1992;Krimi;Quentin Tarantino;USA;99;8.3
2001: A Space Odyssey;1968;Sci-fi;Stanley Kubrick;USA;149;8.3
Inglourious Basterds;2009;Válečný;Quentin Tarantino;USA;153;8.4
Up;2009;Animovaný;Pete Docter;USA;96;8.3
Amélie;2001;Romantický;Jean-Pierre Jeunet;Francie;122;8.3
Heat;1995;Krimi;Michael Mann;USA;170;8.3
Jurassic Park;1993;Dobrodružný;Steven Spielberg;USA;127;8.2
Raiders of the Lost Ark;1981;Dobrodružný;Steven Spielberg;USA;115;8.4
Star Wars;1977;Sci-fi;George Lucas;USA;121;8.6
The Empire Strikes Back;1980;Sci-fi;Irvin Kershner;USA;124;8.7
The Lord of the Rings: The Fellowship of the Ring;2001;Fantasy;Peter Jackson;Nový Zéland;178;8.9
The Lord of the Rings: The Return of the King;2003;Fantasy;Peter Jackson;Nový Zéland;201;9.0
Pan's Labyrinth;2006;Fantasy;Guillermo del Toro;Mexiko;118;8.2
Titanic;1997;Romantický;James Cameron;USA;194;7.9
Avatar;2009;Sci-fi;James Cameron;USA;162;7.9
The Truman Show;1998;Komedie;Peter Weir;USA;103;8.2
Shutter Island;2010;Thriller;Martin Scorsese;USA;138;8.2
The Wolf of Wall Street;2013;Komedie;Martin Scorsese;USA;180;8.2
No Country for Old Men;2007;Krimi;Joel a Ethan Coen;USA;122;8.2
Fargo;1996;Krimi;Joel a Ethan Coen;USA;98;8.1
The Big Lebowski;1998;Komedie;Joel a Ethan Coen;USA;117;8.1
Mad Max: Fury Road;2015;Akční;George Miller;Austrálie;120;8.1
Blade Runner;1982;Sci-fi;Ridley Scott;USA;117;8.1
Blade Runner 2049;2017;Sci-fi;Denis Villeneuve;USA;164;8.0
Dune;2021;Sci-fi;Denis Villeneuve;USA;155;8.0
Arrival;2016;Sci-fi;Denis Villeneuve;USA;116;7.9
Gone Girl;2014;Thriller;David Fincher;USA;149;8.1
The Social Network;2010;Drama;David Fincher;USA;120;7.8
Kill Bill: Vol. 1;2003;Akční;Quentin Tarantino;USA;111;8.2
Trainspotting;1996;Drama;Danny Boyle;Velká Británie;93;8.1
Slumdog Millionaire;2008;Drama;Danny Boyle;Velká Británie;120;8.0
The Grand Budapest Hotel;2014;Komedie;Wes Anderson;USA;99;8.1
Monty Python and the Holy Grail;1975;Komedie;Terry Gilliam;Velká Británie;91;8.2
Life of Brian;1979;Komedie;Terry Jones;Velká Británie;94;8.0
Groundhog Day;1993;Komedie;Harold Ramis;USA;101;8.0
Home Alone;1990;Komedie;Chris Columbus;USA;103;7.7
Harry Potter and the Philosopher's Stone;2001;Fantasy;Chris Columbus;Velká Británie;152;7.6
Harry Potter and the Deathly Hallows: Part 2;2011;Fantasy;David Yates;Velká Británie;130;8.1
Pirates of the Caribbean: The Curse of the Black Pearl;2003;Dobrodružný;Gore Verbinski;USA;143;8.1
Shrek;2001;Animovaný;Andrew Adamson;USA;90;7.9
Finding Nemo;2003;Animovaný;Andrew Stanton;USA;100;8.2
Inside Out;2015;Animovaný;Pete Docter;USA;95;8.1
Ratatouille;2007;Animovaný;Brad Bird;USA;111;8.1
Frozen;2013;Animovaný;Chris Buck;USA;102;7.4
Spider-Man: Into the Spider-Verse;2018;Animovaný;Peter Ramsey;USA;117;8.4
My Neighbor Totoro;1988;Animovaný;Hayao Miyazaki;Japonsko;86;8.1
Princess Mononoke;1997;Animovaný;Hayao Miyazaki;Japonsko;134;8.3
Seven Samurai;1954;Akční;Akira Kurosawa;Japonsko;207;8.6
Your Name;2016;Animovaný;Makoto Shinkai;Japonsko;106;8.4
Get Out;2017;Horor;Jordan Peele;USA;104;7.8
The Exorcist;1973;Horor;William Friedkin;USA;122;8.1
Jaws;1975;Thriller;Steven Spielberg;USA;124;8.1
E.T. the Extra-Terrestrial;1982;Sci-fi;Steven Spielberg;USA;115;7.9
The Sixth Sense;1999;Thriller;M. Night Shyamalan;USA;107;8.2
A Clockwork Orange;1971;Krimi;Stanley Kubrick;Velká Británie;136;8.2
Full Metal Jacket;1987;Válečný;Stanley Kubrick;Velká Británie;116;8.3
1917;2019;Válečný;Sam Mendes;Velká Británie;119;8.2
Dunkirk;2017;Válečný;Christopher Nolan;Velká Británie;106;7.8
Oppenheimer;2023;Drama;Christopher Nolan;USA;180;8.3
Barbie;2023;Komedie;Greta Gerwig;USA;114;6.8
La La Land;2016;Muzikál;Damien Chazelle;USA;128;8.0
The Sound of Music;1965;Muzikál;Robert Wise;USA;172;8.1
Grease;1978;Muzikál;Randal Kleiser;USA;110;7.2
Mamma Mia!;2008;Muzikál;Phyllida Lloyd;USA;108;6.5
Bohemian Rhapsody;2018;Drama;Bryan Singer;Velká Británie;134;7.9
Top Gun: Maverick;2022;Akční;Joseph Kosinski;USA;130;8.2
Die Hard;1988;Akční;John McTiernan;USA;132;8.2
The Terminator;1984;Sci-fi;James Cameron;USA;107;8.1
Rocky;1976;Drama;John G. Avildsen;USA;120;8.1
Taxi Driver;1976;Krimi;Martin Scorsese;USA;114;8.2
Everything Everywhere All at Once;2022;Sci-fi;Daniel Kwan a Daniel Scheinert;USA;139;7.8
Pelíšky;1999;Komedie;Jan Hřebejk;Česko;115;8.6
Kolja;1996;Drama;Jan Svěrák;Česko;105;7.9
Vratné lahve;2007;Komedie;Jan Svěrák;Česko;103;7.5
Obecná škola;1991;Komedie;Jan Svěrák;Česko;100;7.8
Ostře sledované vlaky;1966;Komedie;Jiří Menzel;Československo;93;7.9
Vesničko má středisková;1985;Komedie;Jiří Menzel;Československo;98;7.7
Marečku, podejte mi pero!;1976;Komedie;Oldřich Lipský;Československo;84;7.8
Limonádový Joe;1964;Western;Oldřich Lipský;Československo;99;7.4
Tři oříšky pro Popelku;1973;Pohádka;Václav Vorlíček;Československo;86;7.9
Pyšná princezna;1952;Pohádka;Bořivoj Zeman;Československo;94;7.8
S čerty nejsou žerty;1984;Pohádka;Hynek Bočan;Československo;93;7.8
Hoří, má panenko;1967;Komedie;Miloš Forman;Československo;71;7.6
Lásky jedné plavovlásky;1965;Romantický;Miloš Forman;Československo;90;7.5
Kulový blesk;1979;Komedie;Ladislav Smoljak;Československo;84;7.9
Jára Cimrman ležící, spící;1983;Komedie;Ladislav Smoljak;Československo;86;7.9
Dědictví aneb Kurvaseneříká;1992;Komedie;Věra Chytilová;Česko;121;7.5
Musíme si pomáhat;2000;Válečný;Jan Hřebejk;Česko;117;7.9
Tmavomodrý svět;2001;Válečný;Jan Svěrák;Česko;112;7.6
Samotáři;2000;Komedie;David Ondříček;Česko;103;7.5
Amy;2015;Dokument;Asif Kapadia;Velká Británie;128;7.8
";

    public static readonly List<Movie> All = Raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(l => l.Split(';'))
        .Where(p => p.Length == 7)
        .Select(p => new Movie
        {
            Title = p[0], Year = int.Parse(p[1]), Genre = p[2], Director = p[3], Country = p[4],
            Runtime = int.Parse(p[5]), Rating = double.Parse(p[6], CultureInfo.InvariantCulture),
            Norm = TextUtil.Normalize(p[0]),
        }).ToList();

    public static List<Movie> Suggest(string input, int max = 7)
    {
        var n = TextUtil.Normalize(input);
        if (n.Length == 0) return new List<Movie>();
        return All.Where(m => m.Norm.StartsWith(n))
            .Concat(All.Where(m => m.Norm.Contains(n)))
            .Distinct().Take(max).ToList();
    }
}
