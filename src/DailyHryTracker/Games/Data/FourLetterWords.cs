namespace DailyHryTracker.Games.Data;

/// <summary>Slovník anglických slov o 4 písmenech pro Poople.</summary>
public static class FourLetterWords
{
    public static readonly HashSet<string> All = new((
        "able ache acid acne acre aged aide aids aims airy ajar akin alas ally alms aloe also alto alum amid ammo amok " +
        "anew ankh ants apex arch area aria arid arms army arts aunt aura auto avid away awed awry axes axis axle babe " +
        "baby back bade bags bail bait bake bald bale balk ball balm band bane bang bank bans barb bard bare bark barn " +
        "bars base bash bask bass bath bats bead beak beam bean bear beat beds beef been beep beer bees beet begs bell " +
        "belt bend bent best bets bias bibs bide bike bile bill bind bins bird bite bits blab bled blew blip blob bloc " +
        "blog blot blow blue blur boar boat bobs body boil bold bolt bomb bond bone bony book boom boon boor boos boot " +
        "bore born boss both bout bowl bows boys brag bran bras brat bray bred brew brim brow buck buds buff bugs bulb " +
        "bulk bull bump bums bunk buns buoy burn burp burr bury bush bust busy butt buys buzz byte cabs cafe cage cake " +
        "calf call calm came camp cane cans cape caps card care carp cars cart case cash cask cast cats cave cell cent " +
        "chap chat chef chew chic chin chip chop chow chug chum cite city clad clam clan clap claw clay clip clog clot " +
        "club clue coal coat coax cobs code coed coil coin coke cola cold colt comb come cone cook cool coop cope cops " +
        "copy cord core cork corn cost cosy cots coup cove cowl cows crab crew crib crop crow crux cube cubs cued cues " +
        "cuff cull cult cups curb curd cure curl curt cusp cute cuts dabs dads daft dais dame damp dams dank dare dark " +
        "darn dart dash data date dawn days daze dead deaf deal dean dear debt deck deed deem deep deer defy dell dens " +
        "dent deny desk dial dice died dies diet digs dill dime dine ding dins dint dips dire dirt disc dish disk dive " +
        "dock doer does dogs dole doll dome done doom door dope dork dorm dose dote dots dour dove down doze dozy drab " +
        "drag dram draw drew drip drop drug drum dual dubs duck duct dude duel dues duet duke dull duly dumb dump dune " +
        "dung dunk dupe dusk dust duty each earl earn ears ease east easy eats ebbs echo edge edgy edit eels eggs egos " +
        "else emit ends envy epic even ever evil exam exit eyed eyes face fact fade fads fail fair fake fall fame fang " +
        "fans fare farm fast fate fawn faze fear feat feed feel fees feet fell felt fend fern feud fibs fife figs file " +
        "fill film find fine fins fire firm fish fist fits five fizz flag flak flap flat flaw flax flea fled flee flew " +
        "flex flip flit flog flop flow flue flux foal foam foes fogs foil fold folk fond font food fool foot fops fore " +
        "fork form fort foul four fowl foxy fray free fret frog from fuel full fume fumy fund funk furl fury fuse fuss " +
        "fuzz gaga gags gain gait gala gale gall game gang gaps garb gash gasp gate gave gawk gaze gear geek gels gems " +
        "gene gent germ gets gift gigs gild gill gilt girl gist give glad glee glen glib glob glow glue glum glut gnat " +
        "gnaw goad goal goat gods goes gold golf gone gong good goof goon goop gore gory gosh gout gown grab gram gray " +
        "grew grey grid grim grin grip grit grow grub gulf gull gulp gums guns guru gush gust guts guys gyms hack hail " +
        "hair hale half hall halo halt hams hand hang hard hare harm harp hash hate hats haul have hawk haze hazy head " +
        "heal heap hear heat heck heed heel heir held hell helm help hemp hems hens herb herd here hero hers hide high " +
        "hike hill hilt hind hint hips hire hiss hits hive hoax hobo hock hoes hogs hold hole holy home hone honk hood " +
        "hoof hook hoop hoot hope hops horn hose host hour hove howl hubs huge hugs hula hulk hull hump hums hung hunk " +
        "hunt hurl hurt hush husk huts hymn hype iced icon idea idle idly idol iffy inch info into ions iris iron isle " +
        "itch item jabs jack jade jail jamb jams jars jaws jazz jeep jeer jell jerk jest jets jibe jigs jilt jinx jobs " +
        "jock jogs join joke jolt jots jowl joys judo jugs juke jump junk jury just jute keel keen keep kegs kelp kept " +
        "keys kick kids kill kiln kilt kind king kink kiss kite kits knee knew knit knob knot know labs lace lack lacy " +
        "lads lady laid lain lair lake lamb lame lamp land lane laps lard lark lash lass last late lava lawn laws lazy " +
        "lead leaf leak lean leap left legs lend lens lent less lest liar lice lick lids lied lies lieu life lift like " +
        "lily limb lime limp line link lint lion lips lisp list live load loaf loam loan lobe lock loft logo logs lone " +
        "long look loom loon loop loot lord lore lose loss lost lots loud lout love lows luck lull lump lung lure lurk " +
        "lush lust lute mace made maid mail maim main make male mall malt mane many maps mare mark mars mash mask mass " +
        "mast mate math maze mead meal mean meat meek meet meld melt memo mend menu meow mere mesh mess mewl mice mild " +
        "mile milk mill mime mind mine mint mire miss mist mite mitt moan moat mobs mock mode mold mole molt monk mood " +
        "moon moor moot mope mops more moss most moth move mown much muck muds muff mugs mule mull mums murk muse mush " +
        "musk must mute mutt myth nabs nags nail name nape naps navy near neat neck need neon nerd nest nets news newt " +
        "next nice nick nine nips node nods none nook noon nope norm nose nosy note noun nuke null numb nuns nuts oafs " +
        "oaks oars oath oats obey odds odes ogle ogre oils oily okay omen omit once ones only onto ooze oozy opal open " +
        "opts oral orbs ores ouch ours oust outs oval oven over owed owes owls owns pace pack pact pads page paid pail " +
        "pain pair pale pall palm pals pane pang pans pant papa pare park part pass past pate path pats pave pawn paws " +
        "pays peak peal pear peas peat peck peek peel peep peer pegs pelt pens peon perk perm pest pets pews pick pied " +
        "pier pies pigs pike pile pill pine ping pink pins pint pipe pits pity plan play plea pled plod plop plot plow " +
        "ploy plug plum plus pock pods poem poet poke poky pole poll polo pomp pond pong pony pooh pool poop poor pope " +
        "pops pore pork port pose posh post posy pots pour pout pram pray prep prey prim prod prom prop pros prow puck " +
        "puff pugs puke pull pulp puma pump punk puns punt puny pupa pups pure purr push puts putt quad quay quip quit " +
        "quiz race rack racy raft rage rags raid rail rain rake ramp rams rang rank rant raps rapt rare rash rasp rate " +
        "rats rave rays raze read real ream reap rear redo reed reef reek reel rein rely rend rent rest ribs rice rich " +
        "ride rife rift rigs rile rims rind ring rink riot ripe rips rise risk rite road roam roar robe robs rock rode " +
        "rods role roll romp roof rook room root rope rose rosy rots rout rove rows rubs ruby rude rued rugs ruin rule " +
        "rump rums rune rung runs runt ruse rush rust ruts sack safe saga sage said sail sake sale salt same sand sane " +
        "sang sank saps sash save saws says scab scam scan scar seal seam sear seas seat sect seed seek seem seen seep " +
        "seer sees self sell semi send sent sets sewn shag sham shed shin ship shoe shoo shop shot show shun shut sick " +
        "side sift sigh sign silk sill silo silt sing sink sins sips sire site sits size skew skid skim skin skip skis " +
        "slab slam slap slat slaw sled slew slid slim slip slit slob slop slot slow slug slum slur smog snag snap snip " +
        "snob snot snow snub snug soak soap soar sobs sock soda sofa soft soil sold sole solo some song sons soon soot " +
        "sore sort soul soup sour sown spam span spar spat spec sped spin spit spot spry spud spun spur stab stag star " +
        "stay stem step stew stir stop stow stub stud stun such suck suds suit sulk sumo sump sung sunk suns sure surf " +
        "swab swam swan swap sway swim tabs tack taco tact tags tail take tale talk tall tame tamp tank tans tape taps " +
        "tart task taxi teak teal team tear teas teem teen tell temp tend tens tent term test text than that thaw thee " +
        "them then they thin this thud thug thus tick tide tidy tied tier ties tile till tilt time tint tiny tips tire " +
        "toad toes tofu toga toil told toll tomb tome tone tong tons took tool toot tops tore torn toss tote tour tout " +
        "town toys tram trap tray tree trek trim trio trip trod trot true tsar tuba tube tubs tuck tuft tugs tuna tune " +
        "turf turn tusk tutu twig twin twit type typo ugly undo unit unto upon urge used user uses vain vale vane vans " +
        "vary vase vast veal veer veil vein vent verb very vest veto vets vial vibe vice view vile vine visa void vole " +
        "volt vote vows wade wads waft wage wags waif wail wait wake walk wall wand want ward ware warm warn warp wars " +
        "wart wary wash wasp watt wave wavy waxy ways weak wean wear webs weed week weep weld well welt went wept were " +
        "west wets what when whim whip whiz whom wick wide wife wigs wild will wilt wily wimp wind wine wing wink wins " +
        "wipe wire wiry wise wish wisp with wits woes woke wolf womb wont wood woof wool word wore work worm worn wove " +
        "wrap wren writ yank yaps yard yarn yawn yeah year yell yelp yeti yoga yolk yore your yowl yuck yule zany zaps " +
        "zeal zero zest zinc zing zips zone zoom zoos ").Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => w.ToUpperInvariant()));
}
