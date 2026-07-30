using RestSharp;
using System;
using System.Linq;
using System.Net;

namespace Leauge_Auto_Accept
{
    internal enum SwapKind
    {
        PickOrder,
        Position,
        Champion
    }

    internal sealed class ChampionSelectResult
    {
        public LCUTypes.LolChampSelectSessionV1 Session { get; init; }
        public bool IsActive { get; init; }
        public string Error { get; init; } = "";
    }

    internal sealed class SwapRequestResult
    {
        public bool Success { get; init; }
        public bool StopSequence { get; init; }
        public string Error { get; init; } = "";
    }

    internal static class SwapService
    {
        private static readonly NLog.ILogger Log = NLog.LogManager.GetCurrentClassLogger();
        private const string SessionEndpoint = "lol-champ-select/v1/session";
        private const string PickOrderSwapEndpoint = SessionEndpoint + "/pick-order-swaps";
        private const string PositionSwapEndpoint = SessionEndpoint + "/position-swaps";
        private const string ChampionSwapEndpoint = SessionEndpoint + "/champion-swaps";

        public static ChampionSelectResult GetCurrentSession()
        {
            if (!LCU.isLeagueOpen)
            {
                return new ChampionSelectResult { Error = "League client not running." };
            }

            RestResponse<LCUTypes.LolChampSelectSessionV1> response =
                LCU.clientRequest<LCUTypes.LolChampSelectSessionV1>("GET", SessionEndpoint);

            if (response.IsSuccessStatusCode && response.Data != null)
            {
                return new ChampionSelectResult
                {
                    IsActive = true,
                    Session = response.Data
                };
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new ChampionSelectResult { Error = "Champion select not active." };
            }

            string error = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "LCU authentication failed.",
                HttpStatusCode.Forbidden => "LCU authentication failed.",
                _ when response.ResponseStatus == ResponseStatus.TimedOut => "Champion-select session request timed out.",
                _ when response.ResponseStatus == ResponseStatus.Error => "League client connection failed.",
                _ => $"Champion-select session unavailable ({(int)response.StatusCode})."
            };

            Log.Warn(
                "Champion-select session request failed. statusCode={0} responseStatus={1} error={2}",
                response.StatusCode,
                response.ResponseStatus,
                response.ErrorMessage);

            return new ChampionSelectResult { Error = error };
        }

        public static SwapRequestResult RequestSwap(SwapKind kind, int swapId)
        {
            return SendSwapAction(kind, swapId, "request");
        }

        public static SwapRequestResult ValidateSwapRequest(
            SwapKind kind,
            string expectedSessionKey,
            int targetCellId,
            int swapId)
        {
            ChampionSelectResult current = GetCurrentSession();
            if (!current.IsActive || current.Session == null)
            {
                return new SwapRequestResult
                {
                    StopSequence = true,
                    Error = string.IsNullOrWhiteSpace(current.Error)
                        ? "Champion select not active."
                        : current.Error
                };
            }

            LCUTypes.LolChampSelectSessionV1 session = current.Session;
            string sessionKey = $"{session.GameId}:{session.Id}";
            if (!string.Equals(sessionKey, expectedSessionKey, StringComparison.Ordinal))
            {
                return new SwapRequestResult
                {
                    StopSequence = true,
                    Error = "Champion-select lobby changed."
                };
            }

            if (targetCellId == session.LocalPlayerCellId)
            {
                return new SwapRequestResult { Error = "Cannot request a swap with the local player." };
            }

            LCUTypes.MyTeam localPlayer =
                session.MyTeam?.FirstOrDefault(player => player.CellId == session.LocalPlayerCellId);
            LCUTypes.MyTeam target =
                session.MyTeam?.FirstOrDefault(player => player.CellId == targetCellId);
            if (localPlayer == null)
            {
                return new SwapRequestResult
                {
                    StopSequence = true,
                    Error = "Local player not found in champion select."
                };
            }

            if (target == null)
            {
                return new SwapRequestResult { Error = "Teammate no longer available." };
            }

            bool anotherRequestPending =
                (session.PickOrderSwaps?.Any(swap => IsPending(swap.State)) ?? false)
                || (session.PositionSwaps?.Any(swap => IsPending(swap.State)) ?? false)
                || (session.Trades?.Any(swap => IsPending(swap.State)) ?? false);
            if (anotherRequestPending)
            {
                return new SwapRequestResult { Error = "Another swap request is already pending." };
            }

            if (kind == SwapKind.PickOrder)
            {
                LCUTypes.PickOrderSwap contract =
                    session.PickOrderSwaps?.FirstOrDefault(swap =>
                        swap.CellId == targetCellId && swap.Id == swapId);
                if (contract == null || !IsAvailable(contract.State))
                {
                    return new SwapRequestResult { Error = "Pick-order swap unavailable." };
                }
            }
            else if (kind == SwapKind.Position)
            {
                LCUTypes.PositionSwap contract =
                    session.PositionSwaps?.FirstOrDefault(swap =>
                        swap.CellId == targetCellId && swap.Id == swapId);
                if (contract == null || !IsAvailable(contract.State))
                {
                    return new SwapRequestResult { Error = "Role swap unavailable." };
                }

                string localRole = PositionName(localPlayer.AssignedPosition);
                string targetRole = PositionName(target.AssignedPosition);
                if (localRole == "Unassigned"
                    || targetRole == "Unassigned"
                    || string.Equals(localRole, targetRole, StringComparison.OrdinalIgnoreCase))
                {
                    return new SwapRequestResult { Error = "Role swap unavailable." };
                }
            }
            else
            {
                LCUTypes.ChampionSwap contract =
                    session.Trades?.FirstOrDefault(swap =>
                        swap.CellId == targetCellId && swap.Id == swapId);
                if (contract == null
                    || !IsAvailable(contract.State)
                    || localPlayer.ChampionId <= 0
                    || target.ChampionId <= 0)
                {
                    return new SwapRequestResult { Error = "Champion swap unavailable." };
                }
            }

            return new SwapRequestResult { Success = true };
        }

        public static SwapRequestResult CancelSwap(SwapKind kind, int swapId)
        {
            return SendSwapAction(kind, swapId, "cancel");
        }

        private static SwapRequestResult SendSwapAction(SwapKind kind, int swapId, string action)
        {
            string baseEndpoint = kind switch
            {
                SwapKind.PickOrder => PickOrderSwapEndpoint,
                SwapKind.Position => PositionSwapEndpoint,
                _ => ChampionSwapEndpoint
            };
            string endpoint = $"{baseEndpoint}/{swapId}/{action}";

            Log.Info(
                "Sending champion-select swap action. kind={0} swapId={1} action={2}",
                kind,
                swapId,
                action);

            RestResponse response = LCU.clientRequest("POST", endpoint);
            if (response.IsSuccessStatusCode)
            {
                return new SwapRequestResult { Success = true };
            }

            string error = response.StatusCode switch
            {
                HttpStatusCode.NotFound => $"{KindName(kind)} swap unavailable or League changed the endpoint.",
                HttpStatusCode.Unauthorized => "LCU authentication failed.",
                HttpStatusCode.Forbidden => $"{KindName(kind)} swap is not permitted.",
                HttpStatusCode.Conflict => $"A {KindName(kind).ToLowerInvariant()} swap request is already pending.",
                _ when response.ResponseStatus == ResponseStatus.TimedOut => $"{KindName(kind)} swap request timed out.",
                _ when response.ResponseStatus == ResponseStatus.Error => "League client connection failed.",
                _ => $"{KindName(kind)} swap request failed ({(int)response.StatusCode})."
            };

            Log.Warn(
                "Champion-select swap action failed. kind={0} swapId={1} action={2} statusCode={3} responseStatus={4} error={5}",
                kind,
                swapId,
                action,
                response.StatusCode,
                response.ResponseStatus,
                response.ErrorMessage);

            return new SwapRequestResult { Error = error };
        }

        public static string ChampionText(int championId, int pickIntent = 0)
        {
            int displayId = championId > 0 ? championId : pickIntent;
            if (displayId <= 0)
            {
                return "Unselected";
            }

            string name = null;
            try
            {
                name = Data.champsSorted
                    .FirstOrDefault(champion => champion.id == displayId.ToString())
                    ?.name;
            }
            catch (InvalidOperationException)
            {
                // The League connection thread may refresh the shared champion list.
                // Falling back to the real champion ID keeps the panel responsive.
            }

            string value = string.IsNullOrWhiteSpace(name)
                ? $"Champion {displayId}"
                : name;

            return championId <= 0 ? $"Intent: {value}" : value;
        }

        public static string PositionName(string position)
        {
            return position?.Trim().ToLowerInvariant() switch
            {
                "top" => "Top",
                "jungle" => "Jungle",
                "middle" => "Mid",
                "mid" => "Mid",
                "bottom" => "Bottom",
                "bot" => "Bottom",
                "utility" => "Support",
                "support" => "Support",
                "" => "Unassigned",
                null => "Unassigned",
                _ => position.Trim()
            };
        }

        public static bool IsAvailable(string state)
        {
            return string.Equals(state, "AVAILABLE", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsPending(string state)
        {
            return string.Equals(state, "PENDING", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, "REQUESTED", StringComparison.OrdinalIgnoreCase);
        }

        public static string KindName(SwapKind kind)
        {
            return kind switch
            {
                SwapKind.PickOrder => "Pick-order",
                SwapKind.Position => "Position",
                _ => "Champion"
            };
        }
    }
}
