using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Leauge_Auto_Accept
{
    public class itemList
    {
        public string name { get; set; }
        public string id { get; set; }
        public bool free { get; set; }
    }

    public class spellList
    {
        public string name { get; set; }
        public string id { get; set; }
        public List<string> gameModes { get; set; } = new();
    }

    internal class Data
    {
        private static readonly NLog.ILogger Log = NLog.LogManager.GetCurrentClassLogger();

        public static List<itemList> champsSorted = new List<itemList>();
        public static List<itemList> runesList = new List<itemList>();
        public static List<spellList> spellsSorted = new List<spellList>();

        public static long currentSummonerId = 0;
        public static string currentChatId = "";

        public static void loadSummonerId()
        {
            Log.Debug("currentSummonerId={0}", currentSummonerId);
            if (currentSummonerId == 0)
            {
                Log.Info("Loading summonerId from service");
                Print.printCentered("Getting summoner ID...", 15);
                var currentSummoner = LCU.clientRequestUntilSuccess<LCUTypes.LolSummonerV1CurrentSummoner>("GET", "lol-summoner/v1/current-summoner");
                Console.Clear();
                currentSummonerId = currentSummoner.Data.SummonerId;
            }
        }

        public static void loadPlayerChatId()
        {
            var chatProfileResp = LCU.clientRequest<LCUTypes.LolChatMeV1>("GET", "lol-chat/v1/me");
            currentChatId = chatProfileResp.Data.Id;
            currentSummonerId = chatProfileResp.Data.SummonerId;
        }

        public static void loadChampionsList()
        {
            Console.Clear();

            Log.Debug("champsSorted.Count={0}", champsSorted?.Count);
            if (!champsSorted.Any())
            {
                Log.Info("Loading available champions list from service");
                loadSummonerId();

                List<itemList> champs = new List<itemList>();

                Print.printCentered("Getting champions and ownership list...", 15);
                var ownedChampsResp = LCU.clientRequestUntilSuccess<LCUTypes.LolChampionsInventoriesChampionsMinimalV1[]>("GET", $"lol-champions/v1/inventories/{currentSummonerId}/champions-minimal");
                Console.Clear();

                champs = ownedChampsResp.Data
                    .Where(x => x.Name != "None")
                    .Select(x => {
                        bool isAvailable = x.Ownership.Owned || x.Ownership.XboxGPReward || x.FreeToPlay;
                        return new itemList() { name = x.Name, id = x.Id.ToString(), free = isAvailable };
                    })
                    .OrderBy(x => x.name)
                    .ToList();

                champsSorted = champs;
            }

            SizeHandler.resizeBasedOnChampsCount();
            Console.Clear();
        }

        public static void loadRunesList()
        {
            Console.Clear();

            Log.Debug("runesList.Count={0}", runesList?.Count);
            if (!runesList.Any())
            {
                loadSummonerId();

                List<itemList> list = new List<itemList>();

                Print.printCentered("Getting runes list...", 15);
                var runesResp = LCU.clientRequestUntilSuccess<LCUTypes.LolPerksPagesV1[]>("GET", "lol-perks/v1/pages");
                Console.Clear();

                runesList = runesResp.Data
                    .Where(x => x.IsValid == true)
                    .Select(x => new itemList() { name = (string)x.Name, id = x.Id.ToStringInvariant(), free = true })
                    .ToList();
            }

            Console.Clear();
        }

        public static void loadSpellsList()
        {
            Console.Clear();

            Log.Debug("spellsSorted.Count={0}", spellsSorted?.Count);
            if (!spellsSorted.Any())
            {
                Print.printCentered("Getting a list of available summoner spells...", 15);
                var spellsResp = LCU.clientRequest<JsonArray>("GET", "lol-game-data/assets/v1/summoner-spells.json");
                Console.Clear();

                // Add to list with names and available gamemodes
                var spellsTmp = new List<spellList>(20);
                foreach (var spell in spellsResp.Data)
                {
                    if (spell == null)
                        continue;

                    var gameModes = spell["gameModes"]?.Deserialize<List<string>>() ?? new List<string>();

                    // Skip if array is spell has no available gamemodes
                    if (gameModes.Count == 0)
                        continue;

                    spellsTmp.Add(new spellList()
                    {
                        name = (string)spell["name"],
                        id = spell["id"].ToString(),
                        gameModes = gameModes
                    });
                }

                // Remove spells from the list for gamemodes where the user has no choice
                foreach (var spell in spellsTmp.ToList())
                {
                    foreach (var mode in spell.gameModes.ToList())
                    {
                        int count = 0;

                        foreach (var otherSpell in spellsTmp)
                        {
                            if (otherSpell.gameModes.Contains(mode))
                                count++;
                        }
                        if (count <= 2)
                            spell.gameModes.Remove(mode);
                    }
                    if (spell.gameModes.Count == 0)
                        spellsTmp.Remove(spell);
                }
                spellsSorted = spellsTmp;
            }
        }
    }
}
