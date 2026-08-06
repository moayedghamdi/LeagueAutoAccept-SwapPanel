using System;
using System.Collections.Generic;

namespace Leauge_Auto_Accept
{
    internal static class SwiftplayService
    {
        private static readonly NLog.ILogger Log = NLog.LogManager.GetCurrentClassLogger();
        private const string PlayerSlotsEndpoint = "lol-lobby/v1/lobby/members/localMember/player-slots";
        private static DateTime nextAttemptUtc = DateTime.MinValue;

        public static void ApplyConfiguredSlots()
        {
            if (DateTime.UtcNow < nextAttemptUtc)
            {
                return;
            }

            var lobbyResponse = LCU.clientRequest<LCUTypes.LolLobbyV2Lobby>("GET", "lol-lobby/v2/lobby");
            var lobby = lobbyResponse.Data;
            if (!lobbyResponse.IsSuccessful || lobby?.GameConfig == null || lobby.LocalMember == null)
            {
                DelayRetry();
                return;
            }

            if (!lobby.GameConfig.ShowQuickPlaySlotSelection)
            {
                nextAttemptUtc = DateTime.MinValue;
                return;
            }

            var currentSlots = lobby.LocalMember.PlayerSlots;
            if (currentSlots == null || currentSlots.Count == 0)
            {
                Log.Debug("Swiftplay lobby has no local player slots yet.");
                DelayRetry();
                return;
            }

            int primaryChampionId = ParseChampionId(Settings.currentChamp[1]);
            int secondaryChampionId = ParseChampionId(Settings.secondaryChamp[1]);

            var updatedSlots = new List<LCUTypes.SwiftplayPlayerSlot>(currentSlots.Count);
            bool changed = false;

            for (int index = 0; index < currentSlots.Count; index++)
            {
                var slot = currentSlots[index];
                int desiredChampionId = GetDesiredChampionId(
                    slot.PositionPreference,
                    index,
                    lobby.LocalMember.FirstPositionPreference,
                    lobby.LocalMember.SecondPositionPreference,
                    primaryChampionId,
                    secondaryChampionId);

                bool primarySlot = IsPrimarySlot(
                    slot.PositionPreference,
                    index,
                    lobby.LocalMember.FirstPositionPreference,
                    lobby.LocalMember.SecondPositionPreference);
                int desiredSpell1 = ParseSpellId(primarySlot
                    ? Settings.swiftplayPrimarySpell1[1]
                    : Settings.swiftplaySecondarySpell1[1]);
                int desiredSpell2 = ParseSpellId(primarySlot
                    ? Settings.swiftplayPrimarySpell2[1]
                    : Settings.swiftplaySecondarySpell2[1]);

                if (desiredChampionId > 0 && desiredChampionId != slot.ChampionId)
                {
                    // A skin ID belongs to one champion. Reset to that champion's base skin
                    // while preserving the slot's position and perks.
                    slot = slot with
                    {
                        ChampionId = desiredChampionId,
                        SkinId = desiredChampionId * 1000
                    };
                    changed = true;
                }

                if (desiredSpell1 > 0 && desiredSpell1 != slot.Spell1)
                {
                    slot = slot with { Spell1 = desiredSpell1 };
                    changed = true;
                }

                if (desiredSpell2 > 0 && desiredSpell2 != slot.Spell2)
                {
                    slot = slot with { Spell2 = desiredSpell2 };
                    changed = true;
                }

                updatedSlots.Add(slot);
            }

            if (!changed)
            {
                nextAttemptUtc = DateTime.MinValue;
                return;
            }

            var updateResponse = LCU.clientRequest("PUT", PlayerSlotsEndpoint, updatedSlots);
            if (updateResponse.IsSuccessful)
            {
                Log.Info("Applied configured champions and spells to {0} Swiftplay player slot(s).", updatedSlots.Count);
                nextAttemptUtc = DateTime.MinValue;
            }
            else
            {
                Log.Warn(
                    "Swiftplay slot update was rejected. endpoint={0} statusCode={1} responseStatus={2}",
                    PlayerSlotsEndpoint,
                    updateResponse.StatusCode,
                    updateResponse.ResponseStatus);
                DelayRetry();
            }
        }

        private static int GetDesiredChampionId(
            string slotPosition,
            int slotIndex,
            string firstPosition,
            string secondPosition,
            int primaryChampionId,
            int secondaryChampionId)
        {
            if (!string.IsNullOrWhiteSpace(slotPosition))
            {
                if (string.Equals(slotPosition, firstPosition, StringComparison.OrdinalIgnoreCase))
                {
                    return primaryChampionId;
                }

                if (string.Equals(slotPosition, secondPosition, StringComparison.OrdinalIgnoreCase))
                {
                    return secondaryChampionId;
                }
            }

            // The client normally supplies positions. Slot order is the safe fallback while
            // the lobby is still populating its position-preference fields.
            return slotIndex == 0 ? primaryChampionId : secondaryChampionId;
        }

        private static bool IsPrimarySlot(
            string slotPosition,
            int slotIndex,
            string firstPosition,
            string secondPosition)
        {
            if (!string.IsNullOrWhiteSpace(slotPosition))
            {
                if (string.Equals(slotPosition, firstPosition, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(slotPosition, secondPosition, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return slotIndex == 0;
        }

        private static int ParseChampionId(string value)
        {
            return int.TryParse(value, out int championId) && championId > 0 ? championId : 0;
        }

        private static int ParseSpellId(string value)
        {
            return int.TryParse(value, out int spellId) && spellId > 0 ? spellId : 0;
        }

        private static void DelayRetry()
        {
            nextAttemptUtc = DateTime.UtcNow.AddSeconds(10);
        }
    }
}
