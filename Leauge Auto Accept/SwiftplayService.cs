using System;
using System.Collections.Generic;

namespace Leauge_Auto_Accept
{
    internal static class SwiftplayService
    {
        private static readonly NLog.ILogger Log = NLog.LogManager.GetCurrentClassLogger();
        private const string PlayerSlotsEndpoint = "lol-lobby/v1/lobby/members/localMember/player-slots";
        private static DateTime nextAttemptUtc = DateTime.MinValue;

        public static void ApplyConfiguredChampions()
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
            if (primaryChampionId == 0 && secondaryChampionId == 0)
            {
                return;
            }

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

                if (desiredChampionId > 0 && desiredChampionId != slot.ChampionId)
                {
                    // A skin ID belongs to one champion. Reset to that champion's base skin
                    // while preserving the slot's position, perks, and summoner spells.
                    slot = slot with
                    {
                        ChampionId = desiredChampionId,
                        SkinId = desiredChampionId * 1000
                    };
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
                Log.Info("Applied configured champions to {0} Swiftplay player slot(s).", updatedSlots.Count);
                nextAttemptUtc = DateTime.MinValue;
            }
            else
            {
                Log.Warn(
                    "Swiftplay champion selection was rejected. endpoint={0} statusCode={1} responseStatus={2}",
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

        private static int ParseChampionId(string value)
        {
            return int.TryParse(value, out int championId) && championId > 0 ? championId : 0;
        }

        private static void DelayRetry()
        {
            nextAttemptUtc = DateTime.UtcNow.AddSeconds(10);
        }
    }
}
