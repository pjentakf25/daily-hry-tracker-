using DailyHryTracker.Core;

namespace DailyHryTracker.Games.Data;

public class Country
{
    public string Iso3;
    public string NameCs;
    public string NameEn;
    public string[] Aliases; // normalizované varianty pro hledání
    public double Lat, Lon;
    public string Continent;
}

public static class Countries
{
    // ISO3; české názvy; anglické názvy; lat; lon; kontinent
    const string Raw = @"
AFG;Afghánistán;Afghanistan;33.9;67.7;Asie
ALB;Albánie;Albania;41.2;20.2;Evropa
DZA;Alžírsko;Algeria;28.0;1.7;Afrika
AGO;Angola;Angola;-11.2;17.9;Afrika
ARG;Argentina;Argentina;-38.4;-63.6;Jižní Amerika
ARM;Arménie;Armenia;40.1;45.0;Asie
AUS;Austrálie;Australia;-25.3;133.8;Oceánie
AUT;Rakousko;Austria;47.5;14.6;Evropa
AZE;Ázerbájdžán;Azerbaijan;40.1;47.6;Asie
BHS;Bahamy;Bahamas;24.3;-76.0;Severní Amerika
BGD;Bangladéš;Bangladesh;23.7;90.4;Asie
BLR;Bělorusko;Belarus;53.7;28.0;Evropa
BEL;Belgie;Belgium;50.5;4.5;Evropa
BLZ;Belize;Belize;17.2;-88.5;Severní Amerika
BEN;Benin;Benin;9.3;2.3;Afrika
BTN;Bhútán;Bhutan;27.5;90.4;Asie
BOL;Bolívie;Bolivia;-16.3;-63.6;Jižní Amerika
BIH;Bosna a Hercegovina|Bosna;Bosnia and Herzegovina|Bosnia;43.9;17.7;Evropa
BWA;Botswana;Botswana;-22.3;24.7;Afrika
BRA;Brazílie;Brazil;-14.2;-51.9;Jižní Amerika
BRN;Brunej;Brunei;4.5;114.7;Asie
BGR;Bulharsko;Bulgaria;42.7;25.5;Evropa
BFA;Burkina Faso;Burkina Faso;12.2;-1.6;Afrika
BDI;Burundi;Burundi;-3.4;29.9;Afrika
KHM;Kambodža;Cambodia;12.6;105.0;Asie
CMR;Kamerun;Cameroon;7.4;12.4;Afrika
CAN;Kanada;Canada;56.1;-106.3;Severní Amerika
CAF;Středoafrická republika;Central African Republic;6.6;20.9;Afrika
TCD;Čad;Chad;15.5;18.7;Afrika
CHL;Chile;Chile;-35.7;-71.5;Jižní Amerika
CHN;Čína;China;35.9;104.2;Asie
COL;Kolumbie;Colombia;4.6;-74.3;Jižní Amerika
COG;Kongo|Republika Kongo;Republic of the Congo|Congo;-0.2;15.8;Afrika
COD;Demokratická republika Kongo|DR Kongo|Kongo DR;Democratic Republic of the Congo|DR Congo;-4.0;21.8;Afrika
CRI;Kostarika;Costa Rica;9.7;-83.8;Severní Amerika
CIV;Pobřeží slonoviny;Ivory Coast|Cote d'Ivoire;7.5;-5.5;Afrika
HRV;Chorvatsko;Croatia;45.1;15.2;Evropa
CUB;Kuba;Cuba;21.5;-77.8;Severní Amerika
CYP;Kypr;Cyprus;35.1;33.4;Evropa
CZE;Česko|Česká republika;Czechia|Czech Republic;49.8;15.5;Evropa
DNK;Dánsko;Denmark;56.3;9.5;Evropa
DJI;Džibutsko;Djibouti;11.8;42.6;Afrika
DOM;Dominikánská republika;Dominican Republic;18.7;-70.2;Severní Amerika
ECU;Ekvádor;Ecuador;-1.8;-78.2;Jižní Amerika
EGY;Egypt;Egypt;26.8;30.8;Afrika
SLV;Salvador;El Salvador;13.8;-88.9;Severní Amerika
GNQ;Rovníková Guinea;Equatorial Guinea;1.7;10.3;Afrika
ERI;Eritrea;Eritrea;15.2;39.8;Afrika
EST;Estonsko;Estonia;58.6;25.0;Evropa
SWZ;Eswatini|Svazijsko;Eswatini|Swaziland;-26.5;31.5;Afrika
ETH;Etiopie;Ethiopia;9.1;40.5;Afrika
FJI;Fidži;Fiji;-17.7;178.1;Oceánie
FIN;Finsko;Finland;61.9;25.7;Evropa
FRA;Francie;France;46.2;2.2;Evropa
GAB;Gabon;Gabon;-0.8;11.6;Afrika
GMB;Gambie;Gambia;13.4;-15.3;Afrika
GEO;Gruzie;Georgia;42.3;43.4;Asie
DEU;Německo;Germany;51.2;10.5;Evropa
GHA;Ghana;Ghana;7.9;-1.0;Afrika
GRC;Řecko;Greece;39.1;21.8;Evropa
GRL;Grónsko;Greenland;71.7;-42.6;Severní Amerika
GTM;Guatemala;Guatemala;15.8;-90.2;Severní Amerika
GIN;Guinea;Guinea;9.9;-9.7;Afrika
GNB;Guinea-Bissau;Guinea-Bissau;12.0;-15.2;Afrika
GUY;Guyana;Guyana;4.9;-58.9;Jižní Amerika
HTI;Haiti;Haiti;19.0;-72.3;Severní Amerika
HND;Honduras;Honduras;15.2;-86.2;Severní Amerika
HUN;Maďarsko;Hungary;47.2;19.5;Evropa
ISL;Island;Iceland;64.96;-19.0;Evropa
IND;Indie;India;20.6;79.0;Asie
IDN;Indonésie;Indonesia;-0.8;113.9;Asie
IRN;Írán;Iran;32.4;53.7;Asie
IRQ;Irák;Iraq;33.2;43.7;Asie
IRL;Irsko;Ireland;53.4;-8.2;Evropa
ISR;Izrael;Israel;31.0;34.9;Asie
ITA;Itálie;Italy;41.9;12.6;Evropa
JAM;Jamajka;Jamaica;18.1;-77.3;Severní Amerika
JPN;Japonsko;Japan;36.2;138.3;Asie
JOR;Jordánsko;Jordan;30.6;36.2;Asie
KAZ;Kazachstán;Kazakhstan;48.0;66.9;Asie
KEN;Keňa;Kenya;-0.02;37.9;Afrika
KOS;Kosovo;Kosovo;42.6;20.9;Evropa
KWT;Kuvajt;Kuwait;29.3;47.5;Asie
KGZ;Kyrgyzstán;Kyrgyzstan;41.2;74.8;Asie
LAO;Laos;Laos;19.9;102.5;Asie
LVA;Lotyšsko;Latvia;56.9;24.6;Evropa
LBN;Libanon;Lebanon;33.9;35.9;Asie
LSO;Lesotho;Lesotho;-29.6;28.2;Afrika
LBR;Libérie;Liberia;6.4;-9.4;Afrika
LBY;Libye;Libya;26.3;17.2;Afrika
LTU;Litva;Lithuania;55.2;23.9;Evropa
LUX;Lucembursko;Luxembourg;49.8;6.1;Evropa
MDG;Madagaskar;Madagascar;-18.8;46.9;Afrika
MWI;Malawi;Malawi;-13.3;34.3;Afrika
MYS;Malajsie;Malaysia;4.2;102.0;Asie
MLI;Mali;Mali;17.6;-4.0;Afrika
MRT;Mauritánie;Mauritania;21.0;-10.9;Afrika
MEX;Mexiko;Mexico;23.6;-102.6;Severní Amerika
MDA;Moldavsko;Moldova;47.4;28.4;Evropa
MNG;Mongolsko;Mongolia;46.9;103.8;Asie
MNE;Černá Hora;Montenegro;42.7;19.4;Evropa
MAR;Maroko;Morocco;31.8;-7.1;Afrika
MOZ;Mosambik;Mozambique;-18.7;35.5;Afrika
MMR;Myanmar|Barma;Myanmar|Burma;21.9;96.0;Asie
NAM;Namibie;Namibia;-22.96;18.5;Afrika
NPL;Nepál;Nepal;28.4;84.1;Asie
NLD;Nizozemsko|Holandsko;Netherlands|Holland;52.1;5.3;Evropa
NCL;Nová Kaledonie;New Caledonia;-20.9;165.6;Oceánie
NZL;Nový Zéland;New Zealand;-40.9;174.9;Oceánie
NIC;Nikaragua;Nicaragua;12.9;-85.2;Severní Amerika
NER;Niger;Niger;17.6;8.1;Afrika
NGA;Nigérie;Nigeria;9.1;8.7;Afrika
PRK;Severní Korea|KLDR;North Korea;40.3;127.5;Asie
MKD;Severní Makedonie|Makedonie;North Macedonia|Macedonia;41.6;21.7;Evropa
NOR;Norsko;Norway;60.5;8.5;Evropa
OMN;Omán;Oman;21.5;55.9;Asie
PAK;Pákistán;Pakistan;30.4;69.3;Asie
PSX;Palestina;Palestine;31.9;35.2;Asie
PAN;Panama;Panama;8.5;-80.8;Severní Amerika
PNG;Papua Nová Guinea;Papua New Guinea;-6.3;143.96;Oceánie
PRY;Paraguay;Paraguay;-23.4;-58.4;Jižní Amerika
PER;Peru;Peru;-9.2;-75.0;Jižní Amerika
PHL;Filipíny;Philippines;12.9;121.8;Asie
POL;Polsko;Poland;51.9;19.1;Evropa
PRT;Portugalsko;Portugal;39.4;-8.2;Evropa
PRI;Portoriko;Puerto Rico;18.2;-66.6;Severní Amerika
QAT;Katar;Qatar;25.4;51.2;Asie
ROU;Rumunsko;Romania;45.9;25.0;Evropa
RUS;Rusko;Russia;61.5;105.3;Evropa
RWA;Rwanda;Rwanda;-1.9;29.9;Afrika
SAU;Saúdská Arábie;Saudi Arabia;23.9;45.1;Asie
SEN;Senegal;Senegal;14.5;-14.5;Afrika
SRB;Srbsko;Serbia;44.0;21.0;Evropa
SLE;Sierra Leone;Sierra Leone;8.5;-11.8;Afrika
SVK;Slovensko;Slovakia;48.7;19.7;Evropa
SVN;Slovinsko;Slovenia;46.2;15.0;Evropa
SLB;Šalamounovy ostrovy;Solomon Islands;-9.6;160.2;Oceánie
SOM;Somálsko;Somalia;5.2;46.2;Afrika
ZAF;Jihoafrická republika|JAR;South Africa;-30.6;22.9;Afrika
KOR;Jižní Korea;South Korea;35.9;127.8;Asie
SDS;Jižní Súdán;South Sudan;6.9;31.3;Afrika
ESP;Španělsko;Spain;40.5;-3.7;Evropa
LKA;Srí Lanka;Sri Lanka;7.9;80.8;Asie
SDN;Súdán;Sudan;12.9;30.2;Afrika
SUR;Surinam;Suriname;3.9;-56.0;Jižní Amerika
SWE;Švédsko;Sweden;60.1;18.6;Evropa
CHE;Švýcarsko;Switzerland;46.8;8.2;Evropa
SYR;Sýrie;Syria;34.8;39.0;Asie
TWN;Tchaj-wan;Taiwan;23.7;121.0;Asie
TJK;Tádžikistán;Tajikistan;38.9;71.3;Asie
TZA;Tanzanie;Tanzania;-6.4;34.9;Afrika
THA;Thajsko;Thailand;15.9;100.99;Asie
TLS;Východní Timor;East Timor|Timor-Leste;-8.9;125.7;Asie
TGO;Togo;Togo;8.6;0.8;Afrika
TTO;Trinidad a Tobago;Trinidad and Tobago;10.7;-61.2;Severní Amerika
TUN;Tunisko;Tunisia;33.9;9.5;Afrika
TUR;Turecko;Turkey|Turkiye;38.96;35.2;Asie
TKM;Turkmenistán;Turkmenistan;38.97;59.6;Asie
UGA;Uganda;Uganda;1.4;32.3;Afrika
UKR;Ukrajina;Ukraine;48.4;31.2;Evropa
ARE;Spojené arabské emiráty|SAE;United Arab Emirates|UAE;23.4;53.8;Asie
GBR;Spojené království|Velká Británie|Británie|Anglie;United Kingdom|UK|Great Britain|England;55.4;-3.4;Evropa
USA;Spojené státy|USA|Amerika;United States|USA|America;37.1;-95.7;Severní Amerika
URY;Uruguay;Uruguay;-32.5;-55.8;Jižní Amerika
UZB;Uzbekistán;Uzbekistan;41.4;64.6;Asie
VUT;Vanuatu;Vanuatu;-15.4;166.96;Oceánie
VEN;Venezuela;Venezuela;6.4;-66.6;Jižní Amerika
VNM;Vietnam;Vietnam;14.1;108.3;Asie
SAH;Západní Sahara;Western Sahara;24.2;-12.9;Afrika
YEM;Jemen;Yemen;15.6;48.5;Asie
ZMB;Zambie;Zambia;-13.1;27.8;Afrika
ZWE;Zimbabwe;Zimbabwe;-19.0;29.2;Afrika
SGP;Singapur;Singapore;1.35;103.8;Asie
BHR;Bahrajn;Bahrain;26.0;50.6;Asie
MLT;Malta;Malta;35.9;14.4;Evropa
MUS;Mauricius;Mauritius;-20.3;57.6;Afrika
CPV;Kapverdy;Cape Verde|Cabo Verde;16.0;-24.0;Afrika
COM;Komory;Comoros;-11.9;43.9;Afrika
SYC;Seychely;Seychelles;-4.7;55.5;Afrika
MDV;Maledivy;Maldives;3.2;73.2;Asie
AND;Andorra;Andorra;42.5;1.5;Evropa
LIE;Lichtenštejnsko;Liechtenstein;47.2;9.6;Evropa
MCO;Monako;Monaco;43.7;7.4;Evropa
SMR;San Marino;San Marino;43.9;12.5;Evropa
VAT;Vatikán;Vatican|Vatican City;41.9;12.45;Evropa
WSM;Samoa;Samoa;-13.8;-172.1;Oceánie
TON;Tonga;Tonga;-21.2;-175.2;Oceánie
BRB;Barbados;Barbados;13.2;-59.5;Severní Amerika
";

    public static readonly List<Country> All = Parse();

    static List<Country> Parse()
    {
        var list = new List<Country>();
        foreach (var line in Raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var p = line.Split(';');
            if (p.Length < 6) continue;
            var cs = p[1].Split('|');
            var en = p[2].Split('|');
            list.Add(new Country
            {
                Iso3 = p[0],
                NameCs = cs[0],
                NameEn = en[0],
                Aliases = cs.Concat(en).Select(TextUtil.Normalize).Distinct().ToArray(),
                Lat = double.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture),
                Lon = double.Parse(p[4], System.Globalization.CultureInfo.InvariantCulture),
                Continent = p[5],
            });
        }
        return list;
    }

    public static Country Find(string input)
    {
        var n = TextUtil.Normalize(input);
        if (n.Length == 0) return null;
        return All.FirstOrDefault(c => c.Aliases.Contains(n));
    }

    /// <summary>Návrhy pro našeptávač – země, jejichž název (cz/en) začíná nebo obsahuje zadaný text.</summary>
    public static List<Country> Suggest(string input, int max = 6)
    {
        var n = TextUtil.Normalize(input);
        if (n.Length == 0) return new List<Country>();
        return All.Where(c => c.Aliases.Any(a => a.StartsWith(n)))
            .Concat(All.Where(c => c.Aliases.Any(a => a.Contains(n))))
            .Distinct().Take(max).ToList();
    }

    public static double DistanceKm(Country a, Country b) => DistanceKm(a.Lat, a.Lon, b.Lat, b.Lon);

    public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        double dLat = Rad(lat2 - lat1), dLon = Rad(lon2 - lon1);
        double h = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * R * Math.Asin(Math.Min(1, Math.Sqrt(h)));
    }

    /// <summary>Počáteční azimut z a do b ve stupních (0 = sever, 90 = východ).</summary>
    public static double Bearing(Country a, Country b)
    {
        double φ1 = Rad(a.Lat), φ2 = Rad(b.Lat), Δλ = Rad(b.Lon - a.Lon);
        double y = Math.Sin(Δλ) * Math.Cos(φ2);
        double x = Math.Cos(φ1) * Math.Sin(φ2) - Math.Sin(φ1) * Math.Cos(φ2) * Math.Cos(Δλ);
        return (Math.Atan2(y, x) * 180 / Math.PI + 360) % 360;
    }

    static double Rad(double d) => d * Math.PI / 180;
}
