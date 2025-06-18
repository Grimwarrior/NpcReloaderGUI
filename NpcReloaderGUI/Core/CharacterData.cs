// NpcReloaderGUI.Core.CharacterData.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NpcReloaderGUI.Core
{
    /// <summary>
    /// Represents a single character with their ID and Name.
    /// </summary>
    public class CharacterInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// Gets the character ID as the formatted string "cXXXX".
        /// </summary>
        public string CharacterIdString => $"c{Id:D4}";

        /// <summary>
        /// Defines how the object is displayed in the ComboBox list.
        /// </summary>
        public override string ToString()
        {
            // --- OLD ---
            // return $"{CharacterIdString} - {Name}";

            // --- NEW ---
            return $"{Name} - {CharacterIdString}";
        }
    }

    /// <summary>
    /// Manages the list of all available characters for selection.
    /// </summary>
    public static class CharacterData
    {
        // A static list to hold all parsed characters, accessible from anywhere.
        public static List<CharacterInfo> AllCharacters { get; private set; }
        private static readonly string UserCharactersFilePath = "user_characters.txt";

        // The raw data block containing all character information.
        private static readonly string _characterDataBlock = @"
            PLAYER = { ID = 0000, NAME = ""Player"" },
            UNKNOWN_C0100 = { ID = 0100, NAME = ""Unknown"" },
            UNKNOWN_C0110 = { ID = 0110, NAME = ""Unknown"" },
            UNKNOWN_C0120 = { ID = 0120, NAME = ""Unknown"" },
            UNKNOWN_C0130 = { ID = 0130, NAME = ""Unknown"" },
            UNKNOWN_C1000 = { ID = 1000, NAME = ""Unknown"" },
            BLAIDD = { ID = 2010, NAME = ""Blaidd the Half-Wolf"" },
            RENNALA_PHASE1 = { ID = 2030, NAME = ""Rennala, Queen of the Full Moon (Phase 1)"" },
            RENNALA_PHASE2 = { ID = 2031, NAME = ""Rennala, Queen of the Full Moon (Phase 2)"" },
            JUVENILE_SCHOLAR = { ID = 2040, NAME = ""Juvenile Scholar"" },
            KINDRED_ROT_YOUNG = { ID = 2041, NAME = ""Kindred of Rot (Young)"" },
            RANNI = { ID = 2050, NAME = ""Ranni the Witch"" },
            TWO_FINGERS = { ID = 2060, NAME = ""The Two Fingers"" },
            THREE_FINGERS = { ID = 2070, NAME = ""The Three Fingers"" },
            BLACK_KNIFE_ASSASSIN = { ID = 2100, NAME = ""Black Knife Assassin"" },
            MALIKETH = { ID = 2110, NAME = ""Beast Clergyman Gurranq / Maliketh the Black Blade"" },
            MALENIA = { ID = 2120, NAME = ""Malenia, Blade of Miquella"" },
            MORGOTT = { ID = 2130, NAME = ""Morgott, the Omen King"" },
            MORGOTT_CORPSE = { ID = 2131, NAME = ""Morgott (Corpse)"" },
            OMEN = { ID = 2140, NAME = ""Omen"" },
            WISP = { ID = 2150, NAME = ""Wisp"" },
            FINGER_READER = { ID = 2160, NAME = ""Finger Reader Crone"" },
            ENIA = { ID = 2170, NAME = ""Enia"" },
            MELINA = { ID = 2180, NAME = ""Melina"" },
            RADAGON = { ID = 2190, NAME = ""Radagon"" },
            MARIKA = { ID = 2191, NAME = ""Marika"" },
            ELDEN_BEAST = { ID = 2200, NAME = ""Elden Beast"" },
            CRAB_GIANT = { ID = 2270, NAME = ""Crab (Giant)"" },
            CRAB = { ID = 2271, NAME = ""Crab"" },
            CRAB_UNDEAD_GIANT = { ID = 2272, NAME = ""Crab (Undead, Giant)"" },
            CRAB_UNDEAD = { ID = 2273, NAME = ""Crab (Undead)"" },
            CRAB_FROZEN_GIANT = { ID = 2274, NAME = ""Crab (Frozen, Giant)"" },
            CRAB_FROZEN = { ID = 2275, NAME = ""Crab (Frozen)"" },
            CRAB_BLIGHTED_GIANT = { ID = 2276, NAME = ""Crab (Blighted, Giant)"" },
            CRAB_BLIGHTED = { ID = 2277, NAME = ""Crab (Blighted)"" },
            CRUCIBLE_KNIGHT = { ID = 2500, NAME = ""Crucible Knight"" },
            EXILE_SOLDIER = { ID = 3000, NAME = ""Exile Soldier"" },
            BANISHED_KNIGHT = { ID = 3010, NAME = ""Banished Knight"" },
            EXILE_SOLDIER_LARGE = { ID = 3020, NAME = ""Exile Soldier (Large)"" },
            COMMANDER = { ID = 3050, NAME = ""Commander Niall / Commander O'Neill"" },
            GIANT_SKELETON = { ID = 3060, NAME = ""Giant Skeleton"" },
            GIANT_SKELETON_BEASTMAN = { ID = 3061, NAME = ""Giant Skeleton (Beastman)"" },
            DOMINULA_DANCER = { ID = 3070, NAME = ""Dominula Dancer / Celebrant"" },
            IMP = { ID = 3080, NAME = ""Imp"" },
            ELEMER = { ID = 3100, NAME = ""Elemer of the Briar / Bell Bearing Hunter"" },
            NIGHTS_CAVALRY = { ID = 3150, NAME = ""Night's Cavalry"" },
            NIGHTS_CAVALRY_HORSE = { ID = 3160, NAME = ""Horse (Night's Cavalry)"" },
            ALBINAURIC_ARCHER = { ID = 3170, NAME = ""Albinauric Archer"" },
            PHILIA = { ID = 3171, NAME = ""Philia, Towering Little Sister"" },
            ARCHERS_WOLF = { ID = 3180, NAME = ""Archer's Wolf"" },
            RED_WOLF = { ID = 3181, NAME = ""Red Wolf"" },
            NOMAD = { ID = 3200, NAME = ""Nomad / Wandering Merchant"" },
            NOMAD_FRENZIED = { ID = 3201, NAME = ""Nomad (Frenzied)"" },
            NOMAD_MULE = { ID = 3210, NAME = ""Nomad's Pack Mule"" },
            DRACONIC_TREE_SENTINEL = { ID = 3250, NAME = ""Draconic Tree Sentinel"" },
            TREE_SENTINEL = { ID = 3251, NAME = ""Tree Sentinel"" },
            LORETTA = { ID = 3252, NAME = ""Royal Knight Loretta"" },
            NOX_MONK = { ID = 3300, NAME = ""Nox Monk"" },
            SILVER_TEAR = { ID = 3320, NAME = ""Silver Tear"" },
            SILVER_TEAR_BALL = { ID = 3330, NAME = ""Silver Tear Ball"" },
            CRYSTALIAN = { ID = 3350, NAME = ""Crystalian"" },
            ANCESTRAL_WARRIOR = { ID = 3360, NAME = ""Ancestral Follower Warrior"" },
            ANCESTRAL_WARRIOR_ROT = { ID = 3361, NAME = ""Ancestral Follower Warrior (Rot)"" },
            ANCESTRAL_SHAMAN = { ID = 3370, NAME = ""Ancestral Follower Shaman"" },
            ANCESTRAL_SHAMAN_ROT = { ID = 3371, NAME = ""Ancestral Follower Shaman (Rot)"" },
            GRAVE_WARDEN = { ID = 3400, NAME = ""Grave Warden Duelist"" },
            MISBEGOTTEN = { ID = 3450, NAME = ""Misbegotten"" },
            MISBEGOTTEN_SCALY = { ID = 3451, NAME = ""Scaly Misbegotten"" },
            LEONINE_MISBEGOTTEN = { ID = 3460, NAME = ""Leonine Misbegotten"" },
            ALBINAURIC = { ID = 3470, NAME = ""Albinauric"" },
            ALBINAURIC_LARGE = { ID = 3471, NAME = ""Albinauric (Large)"" },
            SKELETAL_MILITIAMEN = { ID = 3500, NAME = ""Skeletal Militiamen"" },
            SKELETON = { ID = 3510, NAME = ""Skeleton"" },
            SANGUINE_NOBLE = { ID = 3550, NAME = ""Sanguine Noble"" },
            GODSKIN_APOSTLE = { ID = 3560, NAME = ""Godskin Apostle"" },
            GODSKIN_NOBLE = { ID = 3570, NAME = ""Godskin Noble"" },
            ONYX_LORD = { ID = 3600, NAME = ""Onyx Lord / Alabaster Lord"" },
            ORACLE_ENVOY = { ID = 3610, NAME = ""Oracle Envoy"" },
            ORACLE_ENVOY_LONGHORN = { ID = 3620, NAME = ""Oracle Envoy (Longhorn)"" },
            ORACLE_ENVOY_GREATHORN = { ID = 3630, NAME = ""Oracle Envoy (Greathorn)"" },
            GUARDIAN = { ID = 3650, NAME = ""Guardian"" },
            COMMONER = { ID = 3660, NAME = ""Commoner"" },
            PUTRID_CORPSE = { ID = 3661, NAME = ""Putrid Corpse"" },
            PUTRID_CORPSE_LARGE = { ID = 3662, NAME = ""Putrid Corpse (Large)"" },
            CEMETARY_SHADE = { ID = 3664, NAME = ""Cemetary Shade"" },
            GOSTOC = { ID = 3665, NAME = ""Gostoc"" },
            GOLDMASK = { ID = 3666, NAME = ""Goldmask"" },
            ELDER_ALBINAURIC = { ID = 3670, NAME = ""Elder Albinauric"" },
            DEPRAVED_PERFUMER = { ID = 3700, NAME = ""Depraved Perfumer"" },
            PERFUMER = { ID = 3701, NAME = ""Perfumer"" },
            GLINTSTONE_SORCERER = { ID = 3702, NAME = ""Glintstone Sorcerer"" },
            PAGE = { ID = 3703, NAME = ""Page"" },
            BATTLEMAGE = { ID = 3704, NAME = ""Battlemage"" },
            PRIMEVAL_SORCERER_AZUR = { ID = 3710, NAME = ""Primeval Sorcerer Azur"" },
            PRIMEVAL_SORCERER_LUSAT = { ID = 3720, NAME = ""Primeval Sorcerer Lusat"" },
            GRAVEN_MASS = { ID = 3730, NAME = ""Graven-Mass"" },
            CLAYMAN = { ID = 3750, NAME = ""Clayman"" },
            CLEANROT_KNIGHT = { ID = 3800, NAME = ""Cleanrot Knight"" },
            KINDRED_OF_ROT = { ID = 3810, NAME = ""Kindred of Rot"" },
            MARIONETTE_SOLDIER = { ID = 3850, NAME = ""Marionette Soldier"" },
            AVIONETTE_SOLDIER = { ID = 3860, NAME = ""Avionette Soldier"" },
            FIRE_MONK = { ID = 3900, NAME = ""Fire Monk"" },
            BLACKFLAME_MONK = { ID = 3901, NAME = ""Blackflame Monk"" },
            FIRE_PRELATE = { ID = 3910, NAME = ""Fire Prelate"" },
            GELMIR_MAN_SERPENT = { ID = 3950, NAME = ""Gelmir Man-Serpent"" },
            BEASTMAN_OF_FARUM_AZULA = { ID = 3970, NAME = ""Beastman of Farum Azula"" },
            REVENANT_FOLLOWER = { ID = 4000, NAME = ""Revenant Follower"" },
            ROYAL_REVENANT = { ID = 4020, NAME = ""Royal Revenant"" },
            SLUG = { ID = 4040, NAME = ""Slug"" },
            KAIDEN_SELLSWORD = { ID = 4050, NAME = ""Kaiden Sellsword"" },
            HORSE_KAIDEN = { ID = 4060, NAME = ""Horse (Kaiden)"" },
            WOLF = { ID = 4070, NAME = ""Wolf"" },
            WOLF_WHITE = { ID = 4071, NAME = ""Wolf (White)"" },
            GIANT_RAT = { ID = 4080, NAME = ""Giant Rat"" },
            RAT = { ID = 4090, NAME = ""Rat"" },
            DEMI_HUMAN = { ID = 4100, NAME = ""Demi-Human"" },
            DEMI_HUMAN_HEAVY = { ID = 4101, NAME = ""Demi-Human (Heavy)"" },
            DEMI_HUMAN_SMALL = { ID = 4110, NAME = ""Demi-Human (Small)"" },
            DEMI_HUMAN_CHIEF = { ID = 4120, NAME = ""Demi-Human Chief"" },
            DEMI_HUMAN_QUEEN = { ID = 4130, NAME = ""Demi-Human Queen"" },
            SPIRIT_CALLER_SNAIL = { ID = 4140, NAME = ""Spirit-Caller Snail / Snake Snail / Skeletal Snail"" },
            BASILISK = { ID = 4150, NAME = ""Basilisk"" },
            STRAY_DOG = { ID = 4160, NAME = ""Stray Dog"" },
            STRAY_DOG_1 = { ID = 4161, NAME = ""Stray Dog"" },
            STRAY_DOG_HEAVY = { ID = 4162, NAME = ""Stray Dog (Heavy?)"" },
            STRAY_DOG_HEAVY_1 = { ID = 4163, NAME = ""Stray Dog (Heavy?)"" },
            STRAY_DOG_BLOOD = { ID = 4164, NAME = ""Stray Dog (Blood)"" },
            STRAY_DOG_BLOOD_1 = { ID = 4165, NAME = ""Stray Dog (Blood)"" },
            STRAY_DOG_ROT = { ID = 4166, NAME = ""Stray Dog (Rot)"" },
            STRAY_DOG_ROT_1 = { ID = 4167, NAME = ""Stray Dog (Rot)"" },
            SLIME = { ID = 4170, NAME = ""Slime"" },
            SLIME_BLOOD_LARGE = { ID = 4171, NAME = ""Slime (Blood, Large)"" },
            SPIRIT_JELLYFISH = { ID = 4180, NAME = ""Spirit Jellyfish"" },
            SCARAB_LARGE = { ID = 4190, NAME = ""Scarab (Large)"" },
            SCARAB_FLASK = { ID = 4191, NAME = ""Scarab (Flask)"" },
            SCARAB_LOOT = { ID = 4192, NAME = ""Scarab (Loot)"" },
            MAN_BAT = { ID = 4200, NAME = ""Man-Bat"" },
            OPERATIC_BAT = { ID = 4201, NAME = ""Operatic Bat"" },
            WARHAWK = { ID = 4210, NAME = ""Warhawk"" },
            LAND_OCTOPUS = { ID = 4220, NAME = ""Land Octopus"" },
            LARVAL_OCTOPUS = { ID = 4230, NAME = ""Larval Octopus"" },
            FINGERCREEPER = { ID = 4240, NAME = ""Fingercreeper"" },
            FINGERCREEPER_HUGE = { ID = 4241, NAME = ""Fingercreeper (Huge)"" },
            FINGERCREEPER_SMALL = { ID = 4250, NAME = ""Fingercreeper (Small)"" },
            ERDTREE_BURIAL_WATCHDOG = { ID = 4260, NAME = ""Erdtree Burial Watchdog"" },
            ELDER_LION = { ID = 4270, NAME = ""Elder Lion"" },
            GIANT_ANT = { ID = 4280, NAME = ""Giant Ant"" },
            GIANT_ANT_SHIELD_HEAD = { ID = 4281, NAME = ""Giant Ant (Shield head)"" },
            BLOODHOUND_KNIGHT = { ID = 4290, NAME = ""Bloodhound Knight"" },
            WANDERING_NOBLE = { ID = 4300, NAME = ""Wandering Noble"" },
            UNKNOWN_C4310 = { ID = 4310, NAME = ""?"" },
            SOLDIER_GODRICK = { ID = 4311, NAME = ""Soldier (Godrick)"" },
            SOLDIER_RAYA_LUCARIA = { ID = 4312, NAME = ""Soldier (Raya Lucaria)"" },
            SOLDIER_LEYNDELL = { ID = 4313, NAME = ""Soldier (Leyndell)"" },
            SOLDIER_RADAHN = { ID = 4314, NAME = ""Soldier (Radahn)"" },
            SOLDIER_MAUSOLEUM = { ID = 4315, NAME = ""Soldier (Mausoleum)"" },
            SOLDIER_HALIGTREE = { ID = 4316, NAME = ""Soldier (Haligtree)"" },
            VULGAR_MILITIA_TALL_UNUSED = { ID = 4320, NAME = ""Vulgar Militia (Tall, unused, removed with DLC)"" },
            VULGAR_MILITIA = { ID = 4321, NAME = ""Vulgar Militia"" },
            MAD_PUMPKIN_HEAD = { ID = 4340, NAME = ""Mad Pumpkin Head"" },
            MAD_PUMPKIN_HEAD_SPIRIT = { ID = 4341, NAME = ""Mad Pumpkin Head (smaller, Spirit Summon)"" },
            CASTLE_KNIGHT = { ID = 4350, NAME = ""Castle Knight"" },
            KNIGHT_GODRICK = { ID = 4351, NAME = ""Knight (Godrick)"" },
            KNIGHT_RAYA_LUCARIA = { ID = 4352, NAME = ""Knight (Raya Lucaria)"" },
            KNIGHT_LEYNDELL = { ID = 4353, NAME = ""Knight (Leyndell)"" },
            KNIGHT_RADAHN = { ID = 4354, NAME = ""Knight (Radahn)"" },
            KNIGHT_MAUSOLEUM = { ID = 4355, NAME = ""Knight (Mausoleum)"" },
            KNIGHT_HALIGTREE = { ID = 4356, NAME = ""Knight (Haligtree)"" },
            UNKNOWN_C4360 = { ID = 4360, NAME = ""?"" },
            HORSE_GODRICK_KNIGHT = { ID = 4361, NAME = ""Horse (Godrick Knight)"" },
            HORSE_CUCKOO_KNIGHT = { ID = 4362, NAME = ""Horse (Cuckoo Knight)"" },
            HORSE_LEYNDELL_KNIGHT = { ID = 4363, NAME = ""Horse (Leyndell Knight)"" },
            HORSE_REDMANE_KNIGHT = { ID = 4364, NAME = ""Horse (Redmane Knight)"" },
            HORSE_MAUSOLEUM_KNIGHT = { ID = 4365, NAME = ""Horse (Mausoleum Knight)"" },
            UNKNOWN_C4370 = { ID = 4370, NAME = ""?"" },
            FOOT_SOLDIER_GODRICK = { ID = 4371, NAME = ""Foot Soldier (Godrick)"" },
            FOOT_SOLDIER_RAYA_LUCARIA = { ID = 4372, NAME = ""Foot Soldier (Raya Lucaria)"" },
            FOOT_SOLDIER_LEYNDELL = { ID = 4373, NAME = ""Foot Soldier (Leyndell)"" },
            FOOT_SOLDIER_RADAHN = { ID = 4374, NAME = ""Foot soldier (Radahn)"" },
            FOOT_SOLDIER_MAUSOLEUM = { ID = 4375, NAME = ""Foot Soldier (Mausoleum)"" },
            FOOT_SOLDIER_HALIGTREE = { ID = 4376, NAME = ""Foot Soldier (Haligtree)"" },
            HIGHWAYMAN = { ID = 4377, NAME = ""Highwayman"" },
            METEORITE_SCAVENGER = { ID = 4380, NAME = ""Meteorite Scavenger"" },
            GUILTY = { ID = 4381, NAME = ""Guilty"" },
            MINER = { ID = 4382, NAME = ""Miner"" },
            GLINTSTONE_MINER_SORCERER = { ID = 4383, NAME = ""Glintstone Miner (Sorcerer)"" },
            GLINTSTONE_MINER_LARGE = { ID = 4384, NAME = ""Glintstone Miner (Large)"" },
            FUNGAL_SORCERER = { ID = 4385, NAME = ""Fungal Sorcerer"" },
            GIANT_CRAYFISH = { ID = 4420, NAME = ""Giant Crayfish"" },
            SENTRY_STONE = { ID = 4430, NAME = ""Sentry Stone"" },
            LAND_SQUIRT = { ID = 4440, NAME = ""Land Squirt"" },
            GIANT_LAND_SQUIRT = { ID = 4441, NAME = ""Giant Land Squirt"" },
            GIANT_LAND_SQUIRT_ROT = { ID = 4442, NAME = ""Giant Land Squirt (Rot)"" },
            WANDERING_MAUSOLEUM = { ID = 4450, NAME = ""Wandering Mausoleum"" },
            FLAME_CHARIOT_GUNNER = { ID = 4460, NAME = ""Flame Chariot Gunner"" },
            ABDUCTOR_VIRGIN = { ID = 4470, NAME = ""Abductor Virgin"" },
            MIRANDA_BLOSSOM = { ID = 4480, NAME = ""Miranda Blossom"" },
            MIRANDA_SPROUT = { ID = 4481, NAME = ""Miranda Sprout (?)"" },
            MIRANDA_BLOSSOM_ROT = { ID = 4482, NAME = ""Miranda Blossom (Rot)"" },
            MIRANDA_BLOSSOM_1 = { ID = 4483, NAME = ""Miranda Blossom (?)"" },
            LIVING_JAR = { ID = 4490, NAME = ""Living Jar"" },
            LITTLE_LIVING_JAR = { ID = 4491, NAME = ""Little Living Jar"" },
            GREAT_JAR = { ID = 4492, NAME = ""Great Jar"" },
            FLYING_DRAGON_AGHEEL_GREYLL = { ID = 4500, NAME = ""Flying Dragon Agheel / Greyll"" },
            DECAYING_EKZYKES = { ID = 4501, NAME = ""Decaying Ekzykes"" },
            GLINTSTONE_DRAGON_ADULA_SMARAG = { ID = 4502, NAME = ""Glintstone Dragon Adula / Smarag"" },
            BOREALIS_THE_FREEZING_FOG = { ID = 4503, NAME = ""Borealis the Freezing Fog"" },
            ELDER_DRAGON_GREYOLL = { ID = 4504, NAME = ""Elder Dragon Greyoll"" },
            DRAGON_DRAGONBARROW = { ID = 4505, NAME = ""Dragon (Dragonbarrow)"" },
            ANCIENT_DRAGON = { ID = 4510, NAME = ""Ancient Dragon"" },
            LICHDRAGON_FORTISSAX = { ID = 4511, NAME = ""Lichdragon Fortissax"" },
            DRAGONLORD_PLACIDUSAX = { ID = 4520, NAME = ""Dragonlord Placidusax"" },
            GIANT_DOG = { ID = 4550, NAME = ""Giant Dog"" },
            GIANT_CROW = { ID = 4560, NAME = ""Giant Crow"" },
            GIANT_CROW_BLOOD = { ID = 4561, NAME = ""Giant Crow (Blood)"" },
            WORMFACE = { ID = 4570, NAME = ""Wormface"" },
            WORMFACE_GIANT = { ID = 4580, NAME = ""Wormface (Giant)"" },
            TROLL = { ID = 4600, NAME = ""Troll"" },
            TROLL_KNIGHT_HEADLESS = { ID = 4601, NAME = ""Troll Knight (Headless)"" },
            TROLL_MOUNTAINTOPS = { ID = 4602, NAME = ""Troll (Mountaintops)"" },
            STONEDIGGER_TROLL = { ID = 4603, NAME = ""Stonedigger Troll"" },
            WAR_COUNSELOR_IJI = { ID = 4604, NAME = ""War Counselor Iji"" },
            ASTEL_NATURALBORN_OF_THE_VOID = { ID = 4620, NAME = ""Astel, Naturalborn of the Void"" },
            RUNEBEAR = { ID = 4630, NAME = ""Runebear"" },
            ULCERATED_TREE_SPIRIT = { ID = 4640, NAME = ""Ulcerated Tree Spirit"" },
            DRAGONKIN_SOLDIER = { ID = 4650, NAME = ""Dragonkin Soldier"" },
            GUARDIAN_GOLEM = { ID = 4660, NAME = ""Guardian Golem"" },
            ANCESTOR_SPIRIT_REGAL = { ID = 4670, NAME = ""Ancestor Spirit / Regal Ancestor Spirit"" },
            FALLINGSTAR_BEAST = { ID = 4680, NAME = ""Fallingstar Beast"" },
            GRAFTED_SCION = { ID = 4690, NAME = ""Grafted Scion"" },
            RYKARD_LORD_OF_BLASPHEMY = { ID = 4710, NAME = ""Rykard, Lord of Blasphemy"" },
            RYKARD_LORD_OF_BLASPHEMY_CORPSE = { ID = 4711, NAME = ""Rykard, Lord of Blasphemy (Corpse head)"" },
            GODFREY_FIRST_ELDEN_LORD = { ID = 4720, NAME = ""Godfrey, First Elden Lord"" },
            HOARAH_LOUX = { ID = 4721, NAME = ""Hoarah Loux"" },
            STARSCOURGE_RADAHN = { ID = 4730, NAME = ""Starscourge Radahn"" },
            GODRICK_THE_GRAFTED = { ID = 4750, NAME = ""Godrick the Grafted"" },
            GODRICK_THE_GRAFTED_CORPSE = { ID = 4751, NAME = ""Godrick the Grafted (Corpse)"" },
            FIRE_GIANT = { ID = 4760, NAME = ""Fire Giant"" },
            VALIANT_GARGOYLE = { ID = 4770, NAME = ""Valiant Gargoyle / Black Blade Kindred/ Broken Gargoyle"" },
            MOHG = { ID = 4800, NAME = ""Mohg, Lord of Blood / Mohg the Omen"" },
            ERDTREE_AVATAR = { ID = 4810, NAME = ""Erdtree Avatar"" },
            PUTRID_AVATAR = { ID = 4811, NAME = ""Putrid Avatar"" },
            OMENKILLER = { ID = 4820, NAME = ""Omenkiller"" },
            MAGMA_WYRM = { ID = 4910, NAME = ""Magma Wyrm"" },
            GREAT_WYRM_THEODORIX = { ID = 4911, NAME = ""Great Wyrm Theodorix"" },
            TIBIA_MARINER = { ID = 4950, NAME = ""Tibia Mariner"" },
            GIANT_SKELETON_1 = { ID = 4960, NAME = ""Giant Skeleton"" },
            DEATHBIRD = { ID = 4980, NAME = ""Deathbird"" },
            COMMANDER_GAIUS = { ID = 5000, NAME = ""Commander Gaius"" },
            GOLDEN_HIPPOPOTAMUS = { ID = 5010, NAME = ""Golden Hippopotamus"" },
            HIPPOPOTAMUS = { ID = 5011, NAME = ""Hippopotamus"" },
            PUTRESCENT_KNIGHT = { ID = 5020, NAME = ""Putrescent Knight"" },
            ROMINA = { ID = 5030, NAME = ""Romina, Saint of the Bud"" },
            CURSEBLADE = { ID = 5040, NAME = ""Curseblade"" },
            SAGE_MIDRA = { ID = 5050, NAME = ""Sage Midra"" },
            MIDRA_FRENZIED = { ID = 5051, NAME = ""Midra, Lord of Frenzied Flame"" },
            LAMPREY_LARGE = { ID = 5060, NAME = ""Lamprey (Large)"" },
            LAMPREY = { ID = 5061, NAME = ""Lamprey"" },
            DEATH_KNIGHT = { ID = 5070, NAME = ""Death Knight"" },
            BLOODFIEND = { ID = 5080, NAME = ""Bloodfiend"" },
            CHIEF_BLOODFIEND = { ID = 5081, NAME = ""Chief Bloodfiend"" },
            GRAVEBIRD = { ID = 5090, NAME = ""Gravebird"" },
            SPIRIT_WORM = { ID = 5110, NAME = ""Spirit Worm"" },
            BAYLE_THE_DREAD = { ID = 5120, NAME = ""Bayle the Dread"" },
            MESSMER = { ID = 5130, NAME = ""Messmer the Impaler"" },
            MESSMER_CUTSCENE = { ID = 5131, NAME = ""Messmer the Impaler (cutscene model)"" },
            MESSMER_SERPENT_CUTSCENE = { ID = 5132, NAME = ""Messmer the Impaler (cutscene model, Base Serpent)"" },
            BASE_SERPENT_ONE_EYED = { ID = 5140, NAME = ""Base Serpent Messmer's One-eyed serpent"" },
            BASE_SERPENT_MULTI_EYED = { ID = 5141, NAME = ""Base Serpent Messmer's Multi-eyed serpent"" },
            FIRE_KNIGHT = { ID = 5160, NAME = ""Fire Knight"" },
            FURNACE_GOLEM = { ID = 5170, NAME = ""Furnace Golem"" },
            UNKNOWN_C5180 = { ID = 5180, NAME = ""?"" },
            UNKNOWN_C5181 = { ID = 5181, NAME = ""?"" },
            SCORPION_LARGE = { ID = 5190, NAME = ""Scorpion (Large)"" },
            SCORPION_GIANT = { ID = 5192, NAME = ""Scorpion (Giant)"" },
            SCORPION_SMALL = { ID = 5193, NAME = ""Scorpion (Small)"" },
            SCORPION = { ID = 5194, NAME = ""Scorpion"" },
            METYR_MOTHER_OF_FINGERS = { ID = 5200, NAME = ""Metyr, Mother of Fingers"" },
            DIVINE_BEAST_DANCING_LION = { ID = 5210, NAME = ""Divine Beast Dancing Lion"" },
            PROMISED_CONSORT_RADAHN = { ID = 5220, NAME = ""Promised Consort Radahn"" },
            MIQUELLAS_ARMS = { ID = 5221, NAME = ""Miquella's arms (?)"" },
            SCADUTREE_AVATAR = { ID = 5230, NAME = ""Scadutree Avatar"" },
            SHADOWFOLK = { ID = 5240, NAME = ""Shadowfolk"" },
            SHADOWFOLK_GIANT_CLEAVER = { ID = 5241, NAME = ""Shadowfolk (Giant/cleaver)"" },
            HORNED_WARRIOR = { ID = 5250, NAME = ""Horned Warrior/Divine Bird Warrior/Divine Beast Warrior"" },
            HORNSENT_PRIEST = { ID = 5251, NAME = ""Hornsent Priest"" },
            GOLEM_SMITH = { ID = 5260, NAME = ""Golem Smith"" },
            GOLEM_SMITH_SMALLER = { ID = 5261, NAME = ""Golem Smith (smaller)"" },
            JAR_INNARDS = { ID = 5270, NAME = ""Jar Innards"" },
            JAR_INNARDS_LARGE = { ID = 5271, NAME = ""Jar Innards (Large)"" },
            AGING_UNTOUCHABLE = { ID = 5280, NAME = ""Aging Untouchable"" },
            RELLANA_TWIN_MOON_KNIGHT = { ID = 5300, NAME = ""Rellana, Twin Moon Knight"" },
            INQUISITOR = { ID = 5311, NAME = ""Inquisitor"" },
            JORI_ELDER_INQUISITOR = { ID = 5312, NAME = ""Jori, Elder Inquisitor"" },
            GREATER_INQUISITOR = { ID = 5320, NAME = ""Greater Inquisitor"" },
            ST_TRINA = { ID = 5330, NAME = ""St. Trina"" },
            UNKNOWN_C5340 = { ID = 5340, NAME = ""?"" },
            BASILISK_EYES = { ID = 5350, NAME = ""Basilisk Eyes (DLC Catacombs)"" },
            SKELETON_BEASTMAN_DLC = { ID = 5360, NAME = ""Skeleton/Beastman Skeleton (Shadowfolk/DLC?)"" },
            ANCIENT_DRAGON_SENESSAX = { ID = 5370, NAME = ""Ancient Dragon Senessax"" },
            MIRANDA_BLOSSOM_2 = { ID = 5380, NAME = ""Miranda Blossom"" },
            MIRANDA_SPROUT_1 = { ID = 5381, NAME = ""Miranda Sprout"" },
            TROLL_1 = { ID = 5390, NAME = ""Troll"" },
            TROLL_KNIGHT = { ID = 5391, NAME = ""Troll Knight"" },
            EAGLE = { ID = 5401, NAME = ""Eagle"" },
            DEER = { ID = 5410, NAME = ""Deer"" },
            RED_BEAR = { ID = 5421, NAME = ""Red Bear"" },
            OWL = { ID = 5430, NAME = ""Owl"" },
            BOAR = { ID = 5440, NAME = ""Boar"" },
            GOAT = { ID = 5450, NAME = ""Goat"" },
            GULL = { ID = 5460, NAME = ""Gull?"" },
            DRAGONFLY = { ID = 5470, NAME = ""Dragonfly"" },
            DRAGONFLY_LARGE = { ID = 5471, NAME = ""Dragonfly (Large)"" },
            DRAGONFLY_GIANT = { ID = 5472, NAME = ""Dragonfly (Giant)"" },
            TURTLE_ANCIENT = { ID = 5480, NAME = ""Turtle (Ancient)"" },
            SPRINGHARE = { ID = 5490, NAME = ""Springhare"" },
            MAGMA_SLIME = { ID = 5500, NAME = ""Magma Slime"" },
            PUTRID_CORPSE_LARGE_1 = { ID = 5511, NAME = ""Putrid Corpse (Large)"" },
            PUTRID_CORPSE_1 = { ID = 5512, NAME = ""Putrid Corpse"" },
            CEMETARY_SHADE_1 = { ID = 5513, NAME = ""Cemetary Shade"" },
            STRAY_DOG_2 = { ID = 5520, NAME = ""Stray Dog"" },
            STRAY_DOG_3 = { ID = 5521, NAME = ""Stray Dog"" },
            STRAY_DOG_HEAVY_2 = { ID = 5522, NAME = ""Stray Dog (Heavy?)"" },
            STray_DOG_HEAVY_3 = { ID = 5523, NAME = ""Stray Dog (Heavy?)"" },
            STRAY_DOG_BLOOD_2 = { ID = 5524, NAME = ""Stray Dog (Blood)"" },
            STRAY_DOG_BLOOD_3 = { ID = 5525, NAME = ""Stray Dog (Blood)"" },
            STRAY_DOG_ROT_2 = { ID = 5526, NAME = ""Stray Dog (Rot)"" },
            STRAY_DOG_ROT_3 = { ID = 5527, NAME = ""Stray Dog (Rot)"" },
            BAT = { ID = 5530, NAME = ""Bat"" },
            WOLF_LARGE = { ID = 5540, NAME = ""Wolf (Large)"" },
            WOLF_1 = { ID = 5541, NAME = ""Wolf"" },
            FINGERCREEPER_LARGE = { ID = 5550, NAME = ""Fingercreeper (Large)"" },
            FINGERCREEPER_GIANT_1 = { ID = 5551, NAME = ""Fingercreeper (Giant)"" },
            FINGERCREEPER_SMALL_1 = { ID = 5560, NAME = ""Fingercreeper (Small)"" },
            MARIONETTE = { ID = 5570, NAME = ""Marionette"" },
            JAGGED_PEAK_DRAKE = { ID = 5580, NAME = ""Jagged Peak Drake"" },
            PUTRID_FLESH = { ID = 5590, NAME = ""Putrid Flesh"" },
            PUTRID_FLESH_LARGE = { ID = 5591, NAME = ""Putrid Flesh (Large)"" },
            SKELETON_1 = { ID = 5600, NAME = ""Skeleton"" },
            TIBIA_MARINER_1 = { ID = 5620, NAME = ""Tibia Mariner"" },
            GIANT_CRAYFISH_1 = { ID = 5630, NAME = ""Giant Crayfish"" },
            CRAB_1 = { ID = 5640, NAME = ""Crab"" },
            CRAB_SMALL = { ID = 5641, NAME = ""Crab (Small)"" },
            MESSMER_FOOT_SOLDIER = { ID = 5651, NAME = ""Messmer Foot Soldier"" },
            SHADOW_MILITIAMEN = { ID = 5661, NAME = ""Shadow Militiamen"" },
            WHEELED_BALLISTA = { ID = 5680, NAME = ""Wheeled Ballista"" },
            FLAMETHROWER = { ID = 5690, NAME = ""Flamethrower"" },
            DEMI_HUMAN_1 = { ID = 5700, NAME = ""Demi-Human"" },
            DEMI_HUMAN_LARGE = { ID = 5701, NAME = ""Demi-Human (Large)"" },
            DEMI_HUMAN_SMALL_1 = { ID = 5710, NAME = ""Demi-Human (Small)"" },
            DEMI_HUMAN_CHIEF_1 = { ID = 5720, NAME = ""Demi-Human Chief"" },
            DEMI_HUMAN_QUEEN_1 = { ID = 5730, NAME = ""Demi-Human Queen"" },
            KINDRED_OF_ROT_1 = { ID = 5740, NAME = ""Kindred of Rot"" },
            WARRIOR_JAR = { ID = 5750, NAME = ""Warrior Jar"" },
            WARRIOR_JAR_SMALL = { ID = 5751, NAME = ""Warrior Jar (Small)"" },
            RAT_1 = { ID = 5760, NAME = ""Rat"" },
            GIANT_RAT_1 = { ID = 5761, NAME = ""Giant Rat"" },
            RUNEBEAR_1 = { ID = 5780, NAME = ""Runebear"" },
            GUARDIAN_GOLEM_1 = { ID = 5790, NAME = ""Guardian Golem"" },
            CRUCIBLE_KNIGHT_DEVONIA = { ID = 5800, NAME = ""Crucible Knight Devonia"" },
            DEMI_HUMAN_SWORDMASTER_ONZE = { ID = 5810, NAME = ""Demi-Human Swordmaster Onze"" },
            GREAT_RED_BEAR = { ID = 5820, NAME = ""Great Red Bear"" },
            MESSMER_SOLDIER = { ID = 5830, NAME = ""Messmer Soldier"" },
            BLACK_KNIGHT = { ID = 5840, NAME = ""Black Knight"" },
            GIANT_RAM = { ID = 5850, NAME = ""Giant Ram"" },
            GHOSTFLAME_DRAGON = { ID = 5860, NAME = ""Ghostflame Dragon"" },
            IMP_1 = { ID = 5870, NAME = ""Imp"" },
            IMP_LARGE = { ID = 5871, NAME = ""Imp (Large)"" },
            IMP_GIANT = { ID = 5872, NAME = ""Imp (Giant)"" },
            UNDEAD_GRAVETENDER = { ID = 5880, NAME = ""Undead Gravetender"" },
            BLACK_KNIGHT_HORSE = { ID = 5890, NAME = ""Black Knight Horse"" },
            MAN_FLY = { ID = 5900, NAME = ""Man-Fly"" },
            HORNSENT_GRANDAM = { ID = 5910, NAME = ""Hornsent Grandam"" },
            MAGMA_WYRM_1 = { ID = 5920, NAME = ""Magma Wyrm"" },
            GIANT_SKELETON_2 = { ID = 5930, NAME = ""Giant Skeleton"" },
            GIANT_SKELETON_LARGE = { ID = 5931, NAME = ""Giant Skeleton (Large)"" },
            ABNORMAL_STONE_CLUSTER = { ID = 5940, NAME = ""Abnormal Stone Cluster"" },
            LEONINE_MISBEGOTTEN_1 = { ID = 5950, NAME = ""Leonine Misbegotten"" },
            ULCERATED_TREE_SPIRIT_1 = { ID = 5960, NAME = ""Ulcerated Tree Spirit"" },
            ABDUCTOR_VIRGIN_1 = { ID = 5970, NAME = ""Abductor Virgin"" },
            OMENKILLER_1 = { ID = 5980, NAME = ""Omenkiller"" },
            BASILISK_1 = { ID = 5990, NAME = ""Basilisk"" },
            UNKNOWN_C6000 = { ID = 6000, NAME = ""?"" },
            HAWK = { ID = 6001, NAME = ""Hawk"" },
            DEER_1 = { ID = 6010, NAME = ""Deer"" },
            UNKNOWN_C6030 = { ID = 6030, NAME = ""?"" },
            BEAR = { ID = 6031, NAME = ""Bear"" },
            OWL_1 = { ID = 6040, NAME = ""Owl"" },
            BOAR_1 = { ID = 6050, NAME = ""Boar"" },
            GOAT_1 = { ID = 6060, NAME = ""Goat"" },
            GUILLEMOT = { ID = 6070, NAME = ""Guillemot"" },
            UNKNOWN_C6071 = { ID = 6071, NAME = ""?"" },
            MESSENGER_PIDGEON_UNUSED = { ID = 6072, NAME = ""Messenger Pidgeon (unused)"" },
            GREAT_DRAGONFLY = { ID = 6080, NAME = ""Great Dragonfly"" },
            GREAT_DRAGONFLY_GREEN = { ID = 6081, NAME = ""Great Dragonfly (Green)"" },
            GREAT_DRAGONFLY_CAELID = { ID = 6082, NAME = ""Great Dragonfly (Caelid)"" },
            TURTLE = { ID = 6090, NAME = ""Turtle"" },
            MIRIEL_PASTOR_OF_VOWS = { ID = 6091, NAME = ""Miriel, Pastor of Vows"" },
            SPRINGHARE_1 = { ID = 6100, NAME = ""Springhare"" },
            SCARAB = { ID = 6201, NAME = ""Scarab"" },
            ALBINAURIC_ARCHER_1 = { ID = 6210, NAME = ""Albinauric Archer"" },
            ARCHERS_WOLF_1 = { ID = 6220, NAME = ""Archer's Wolf"" },
            PERFUMER_1 = { ID = 6231, NAME = ""Perfumer"" },
            GLINTSTONE_SORCERER_1 = { ID = 6232, NAME = ""Glintstone Sorcerer"" },
            PAGE_1 = { ID = 6233, NAME = ""Page"" },
            LIGHTNING_ORB = { ID = 6240, NAME = ""Lightning Orb?"" },
            TREE_SENTINEL_1 = { ID = 6251, NAME = ""Tree Sentinel"" },
            DEATHBIRD_1 = { ID = 6260, NAME = ""Deathbird"" },
            ROYAL_REVENANT_1 = { ID = 6270, NAME = ""Royal Revenant"" },
            MISBEGOTTEN_1 = { ID = 6290, NAME = ""Misbegotten"" },
            SCALY_MISBEGOTTEN = { ID = 6291, NAME = ""Scaly Misbegotten"" },
            SNAIL = { ID = 6300, NAME = ""Snail"" },
            FALLINGSTAR_BEAST_1 = { ID = 6310, NAME = ""Fallingstar Beast"" },
            WANDERING_NOBLE_1 = { ID = 6320, NAME = ""Wandering Noble"" },
            FALLEN_HAWK_COMPANY = { ID = 7000, NAME = ""Fallen Hawk Company"" },
            ANCIENT_HERO_OF_ZAMOR = { ID = 7100, NAME = ""Ancient Hero of Zamor"" },
            TORRENT = { ID = 8000, NAME = ""Torrent, Spectral Steed"" },
            BALLISTA = { ID = 8100, NAME = ""Ballista"" },
            BALLISTA_LARGE = { ID = 8101, NAME = ""Ballista (Large)"" },
            FLAMETHROWER_1 = { ID = 8110, NAME = ""Flamethrower"" },
            CHARIOT = { ID = 8120, NAME = ""Chariot"" },
            MALENIA_CUTSCENE = { ID = 8900, NAME = ""Malenia, Blade of Miquella (cutscene model)"" },
            MALIKETH_CUTSCENE = { ID = 8901, NAME = ""Beast Clergyman Gurranq / Maliketh the Black Blade (cutscene model)"" },
            MIQUELLA_CUTSCENE = { ID = 8902, NAME = ""Miquella the Kind (cutscene)"" },
        ";

        // Static constructor to parse the data when the class is first used.
        static CharacterData()
        {
            ParseAllCharacters();
        }

        private static void ParseAllCharacters()
        {
            AllCharacters = new List<CharacterInfo>();
            // This regex finds all occurrences of { ID = 1234, NAME = "Some Name" }
            var regex = new Regex(@"{\s*ID\s*=\s*(\d+)\s*,\s*NAME\s*=\s*""([^""]+)""\s*}", RegexOptions.Multiline);
            
            var matches = regex.Matches(_characterDataBlock);

            foreach (Match match in matches)
            {
                if (match.Groups.Count == 3)
                {
                    try
                    {
                        int id = int.Parse(match.Groups[1].Value);
                        string name = match.Groups[2].Value;
                        AllCharacters.Add(new CharacterInfo { Id = id, Name = name });
                    }
                    catch (Exception ex)
                    {
                        // Log errors if parsing fails for a line
                        NpcReloaderLogic.LogAction?.Invoke($"Failed to parse character line: {match.Value}. Error: {ex.Message}");
                    }
                }
            }

            // Sort the list alphabetically by name for a better user experience.
            AllCharacters = AllCharacters.OrderBy(c => c.Name).ToList();
        }

        // +++ ADD THIS NEW METHOD to load from user_characters.txt +++
        private static void LoadUserCharacters()
        {
            if (!File.Exists(UserCharactersFilePath))
            {
                return; // No user file, nothing to do.
            }

            try
            {
                var lines = File.ReadAllLines(UserCharactersFilePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = line.Split(new[] { ',' }, 2); // Split on the first comma only
                    if (parts.Length == 2 && int.TryParse(parts[0], out int id))
                    {
                        string name = parts[1];
                        // Avoid adding duplicates from the user file if the ID already exists
                        if (!AllCharacters.Any(c => c.Id == id))
                        {
                            AllCharacters.Add(new CharacterInfo { Id = id, Name = name });
                        }
                    }
                }
                NpcReloaderLogic.LogAction?.Invoke($"Loaded {lines.Length} custom character entries.");
            }
            catch (Exception ex)
            {
                NpcReloaderLogic.LogAction?.Invoke($"ERROR loading user characters from '{UserCharactersFilePath}': {ex.Message}");
            }
        }

        // +++ ADD THIS NEW PUBLIC METHOD to save a new entry +++
        public static void AddUserCharacter(CharacterInfo newChar)
        {
            // 1. Add to the in-memory list first
            AllCharacters.Add(newChar);
            AllCharacters = AllCharacters.OrderBy(c => c.Name).ToList(); // Re-sort

            // 2. Save to the user file for persistence
            try
            {
                // Format: "5290,Great Shinobi Owl"
                string lineToAppend = $"{newChar.Id},{newChar.Name}{Environment.NewLine}";
                File.AppendAllText(UserCharactersFilePath, lineToAppend);
            }
            catch (Exception ex)
            {
                NpcReloaderLogic.LogAction?.Invoke($"ERROR saving new character to '{UserCharactersFilePath}': {ex.Message}");
            }
        }
    }
}