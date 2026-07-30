using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Leauge_Auto_Accept
{
    internal sealed class SwapTeammateState
    {
        public int CellId { get; init; }
        public string Label { get; init; } = "";
        public string Role { get; init; } = "";
        public int ChampionId { get; init; }
        public int ChampionPickIntent { get; init; }
        public int? PositionSwapId { get; init; }
        public string PositionSwapState { get; init; } = "";
        public int? ChampionSwapId { get; init; }
        public string ChampionSwapState { get; init; } = "";
        public bool Selected { get; set; }
        public bool IsPending { get; init; }

        public bool RoleSwapEligible =>
            PositionSwapId.HasValue
            && SwapService.IsAvailable(PositionSwapState)
            && !string.IsNullOrWhiteSpace(Role)
            && !string.Equals(Role, "Unassigned", StringComparison.OrdinalIgnoreCase);

        public bool ChampionSwapEligible =>
            ChampionSwapId.HasValue
            && SwapService.IsAvailable(ChampionSwapState)
            && ChampionId > 0;
    }

    internal sealed class SwapPanelState
    {
        public bool IsConnected { get; init; }
        public bool IsChampionSelectActive { get; init; }
        public string SessionKey { get; init; } = "";
        public int LocalPlayerCellId { get; init; } = -1;
        public string LocalRole { get; init; } = "Unassigned";
        public int LocalChampionId { get; init; }
        public int LocalChampionPickIntent { get; init; }
        public IReadOnlyList<SwapTeammateState> Teammates { get; init; } = Array.Empty<SwapTeammateState>();
        public bool IsSequenceRunning { get; set; }
        public int? PendingCellId { get; set; }
        public SwapKind? PendingKind { get; set; }
        public string StatusMessage { get; set; } = "Waiting for champion select.";
    }

    internal static class SwapController
    {
        private enum SequenceOutcome
        {
            Accepted,
            DeclinedOrUnavailable,
            TimedOut,
            SessionEnded
        }

        private static readonly NLog.ILogger Log = NLog.LogManager.GetCurrentClassLogger();
        private static readonly object StateLock = new();
        private static SwapPanelState CurrentState = new();
        private static CancellationTokenSource SequenceCancellation;
        private static int? ActiveSwapId;
        private const int PollIntervalMilliseconds = 1000;
        private const int SwapTimeoutMilliseconds = 20000;

        public static void Run()
        {
            while (true)
            {
                try
                {
                    ChampionSelectResult result = SwapService.GetCurrentSession();
                    if (result.IsActive && result.Session != null)
                    {
                        ApplySession(result.Session);
                    }
                    else
                    {
                        ApplyInactiveState(LCU.isLeagueOpen, result.Error);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, "Unexpected error while updating champion-select swap state.");
                    ApplyInactiveState(LCU.isLeagueOpen, "Unexpected champion-select response.");
                }

                Thread.Sleep(PollIntervalMilliseconds);
            }
        }

        public static SwapPanelState GetState()
        {
            lock (StateLock)
            {
                return CloneState(CurrentState);
            }
        }

        public static void ToggleTeammate(int rowIndex)
        {
            lock (StateLock)
            {
                if (CurrentState.IsSequenceRunning)
                {
                    CurrentState.StatusMessage = "Cancel the active sequence before changing selection.";
                }
                else if (rowIndex < 0 || rowIndex >= CurrentState.Teammates.Count)
                {
                    CurrentState.StatusMessage = "No teammate is available in that row.";
                }
                else
                {
                    SwapTeammateState teammate = CurrentState.Teammates[rowIndex];
                    bool shouldSelect = !teammate.Selected;

                    foreach (SwapTeammateState item in CurrentState.Teammates)
                    {
                        item.Selected = false;
                    }

                    teammate.Selected = shouldSelect;
                    CurrentState.StatusMessage = shouldSelect
                        ? $"{teammate.Label} selected; previous selection cleared."
                        : $"{teammate.Label} cleared.";
                }
            }

            RefreshPanel();
        }

        public static void SelectAll()
        {
            lock (StateLock)
            {
                if (CurrentState.IsSequenceRunning)
                {
                    CurrentState.StatusMessage = "Cancel the active sequence before changing selection.";
                }
                else
                {
                    int selected = 0;
                    foreach (SwapTeammateState teammate in CurrentState.Teammates)
                    {
                        teammate.Selected = IsEligible(teammate, SwapKind.Position)
                            || IsEligible(teammate, SwapKind.Champion);
                        if (teammate.Selected)
                        {
                            selected++;
                        }
                    }

                    CurrentState.StatusMessage = selected == 0
                        ? "No eligible teammates are currently available."
                        : $"Selected {selected} eligible teammate(s).";
                }
            }

            RefreshPanel();
        }

        public static void ClearSelection()
        {
            lock (StateLock)
            {
                if (CurrentState.IsSequenceRunning)
                {
                    CurrentState.StatusMessage = "Cancel the active sequence before changing selection.";
                }
                else
                {
                    foreach (SwapTeammateState teammate in CurrentState.Teammates)
                    {
                        teammate.Selected = false;
                    }

                    CurrentState.StatusMessage = "Selection cleared.";
                }
            }

            RefreshPanel();
        }

        public static void StartSequence(SwapKind kind)
        {
            string sessionKey;
            List<int> selectedCellIds;

            lock (StateLock)
            {
                if (!CurrentState.IsConnected)
                {
                    CurrentState.StatusMessage = "League client not running.";
                    RefreshPanelAfterLock();
                    return;
                }

                if (!CurrentState.IsChampionSelectActive)
                {
                    CurrentState.StatusMessage = "Champion select not active.";
                    RefreshPanelAfterLock();
                    return;
                }

                if (CurrentState.IsSequenceRunning)
                {
                    CurrentState.StatusMessage = "A swap sequence is already running.";
                    RefreshPanelAfterLock();
                    return;
                }

                if (CurrentState.Teammates.Any(teammate => teammate.IsPending))
                {
                    CurrentState.StatusMessage = "Another swap request is already pending.";
                    RefreshPanelAfterLock();
                    return;
                }

                selectedCellIds = CurrentState.Teammates
                    .Where(teammate => teammate.Selected && IsEligible(teammate, kind))
                    .Select(teammate => teammate.CellId)
                    .ToList();

                if (selectedCellIds.Count == 0)
                {
                    CurrentState.StatusMessage = $"No selected teammate has an available {SwapService.KindName(kind).ToLowerInvariant()} swap.";
                    RefreshPanelAfterLock();
                    return;
                }

                sessionKey = CurrentState.SessionKey;
                SequenceCancellation = new CancellationTokenSource();
                CurrentState.IsSequenceRunning = true;
                CurrentState.PendingKind = kind;
                CurrentState.StatusMessage = $"Starting {SwapService.KindName(kind).ToLowerInvariant()} swap sequence...";
            }

            RefreshPanel();
            _ = Task.Run(() => RunSequence(kind, sessionKey, selectedCellIds, SequenceCancellation.Token));
        }

        public static void CancelSequence()
        {
            CancellationTokenSource cancellation;
            lock (StateLock)
            {
                cancellation = SequenceCancellation;
                if (!CurrentState.IsSequenceRunning || cancellation == null)
                {
                    CurrentState.StatusMessage = "No swap sequence is running.";
                    RefreshPanelAfterLock();
                    return;
                }

                CurrentState.StatusMessage = "Cancelling swap sequence...";
            }

            cancellation.Cancel();
            RefreshPanel();
        }

        private static void RunSequence(
            SwapKind kind,
            string sessionKey,
            IReadOnlyList<int> selectedCellIds,
            CancellationToken cancellationToken)
        {
            bool accepted = false;
            int attempted = 0;

            try
            {
                foreach (int cellId in selectedCellIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    SwapTeammateState teammate;
                    string localRole;
                    int localChampionId;
                    lock (StateLock)
                    {
                        if (!SessionMatches(sessionKey))
                        {
                            SetStatusLocked("Stopped: champion select ended or the lobby changed.");
                            return;
                        }

                        teammate = CurrentState.Teammates.FirstOrDefault(item => item.CellId == cellId);
                        if (teammate == null || !IsEligible(teammate, kind))
                        {
                            continue;
                        }

                        int? swapId = kind == SwapKind.Position
                            ? teammate.PositionSwapId
                            : teammate.ChampionSwapId;
                        if (!swapId.HasValue)
                        {
                            continue;
                        }

                        ActiveSwapId = swapId;
                        CurrentState.PendingCellId = cellId;
                        CurrentState.PendingKind = kind;
                        CurrentState.StatusMessage =
                            $"Requesting {SwapService.KindName(kind).ToLowerInvariant()} swap with {teammate.Label}...";
                        localRole = CurrentState.LocalRole;
                        localChampionId = CurrentState.LocalChampionId;
                    }

                    attempted++;
                    RefreshPanel();

                    SwapRequestResult validation = SwapService.ValidateSwapRequest(
                        kind,
                        sessionKey,
                        cellId,
                        ActiveSwapId.Value);
                    if (!validation.Success)
                    {
                        SetStatus(validation.Error);
                        ClearPendingRequest();
                        if (validation.StopSequence)
                        {
                            return;
                        }
                        continue;
                    }

                    SwapRequestResult request = SwapService.RequestSwap(kind, ActiveSwapId.Value);
                    if (!request.Success)
                    {
                        SetStatus(request.Error);
                        ClearPendingRequest();
                        continue;
                    }

                    SequenceOutcome outcome = WaitForOutcome(
                        kind,
                        sessionKey,
                        cellId,
                        ActiveSwapId.Value,
                        localRole,
                        localChampionId,
                        teammate.Role,
                        teammate.ChampionId,
                        cancellationToken);

                    if (outcome == SequenceOutcome.Accepted)
                    {
                        accepted = true;
                        SetStatus($"{SwapService.KindName(kind)} swap accepted by {teammate.Label}.");
                        break;
                    }

                    if (outcome == SequenceOutcome.SessionEnded)
                    {
                        SetStatus("Stopped: champion select ended or the lobby changed.");
                        return;
                    }

                    if (outcome == SequenceOutcome.TimedOut)
                    {
                        SwapService.CancelSwap(kind, ActiveSwapId.Value);
                        SetStatus($"{SwapService.KindName(kind)} swap with {teammate.Label} timed out; trying next.");
                    }
                    else
                    {
                        SetStatus($"{SwapService.KindName(kind)} swap with {teammate.Label} declined or unavailable; trying next.");
                    }

                    ClearPendingRequest();
                }

                if (!accepted)
                {
                    SetStatus(attempted == 0
                        ? $"No selected {SwapService.KindName(kind).ToLowerInvariant()} swaps remained eligible."
                        : $"{SwapService.KindName(kind)} swap sequence completed without acceptance.");
                }
            }
            catch (OperationCanceledException)
            {
                int? swapId;
                lock (StateLock)
                {
                    swapId = ActiveSwapId;
                }

                if (swapId.HasValue)
                {
                    SwapService.CancelSwap(kind, swapId.Value);
                }

                SetStatus("Swap sequence cancelled.");
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Unexpected error in champion-select swap sequence. kind={0}", kind);
                SetStatus("Unexpected error while processing swap sequence.");
            }
            finally
            {
                lock (StateLock)
                {
                    CurrentState.IsSequenceRunning = false;
                    CurrentState.PendingCellId = null;
                    CurrentState.PendingKind = null;
                    ActiveSwapId = null;
                    SequenceCancellation?.Dispose();
                    SequenceCancellation = null;
                }

                RefreshPanel();
            }
        }

        private static SequenceOutcome WaitForOutcome(
            SwapKind kind,
            string sessionKey,
            int targetCellId,
            int swapId,
            string previousLocalRole,
            int previousLocalChampionId,
            string previousTargetRole,
            int previousTargetChampionId,
            CancellationToken cancellationToken)
        {
            DateTime started = DateTime.UtcNow;
            bool sawPending = false;

            while ((DateTime.UtcNow - started).TotalMilliseconds < SwapTimeoutMilliseconds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Thread.Sleep(250);

                lock (StateLock)
                {
                    if (!SessionMatches(sessionKey))
                    {
                        return SequenceOutcome.SessionEnded;
                    }

                    SwapTeammateState teammate =
                        CurrentState.Teammates.FirstOrDefault(item => item.CellId == targetCellId);
                    if (teammate == null)
                    {
                        return SequenceOutcome.DeclinedOrUnavailable;
                    }

                    string state = kind == SwapKind.Position
                        ? teammate.PositionSwapState
                        : teammate.ChampionSwapState;
                    int? currentSwapId = kind == SwapKind.Position
                        ? teammate.PositionSwapId
                        : teammate.ChampionSwapId;

                    sawPending |= SwapService.IsPending(state);

                    bool valuesSwapped = kind == SwapKind.Position
                        ? string.Equals(CurrentState.LocalRole, previousTargetRole, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(teammate.Role, previousLocalRole, StringComparison.OrdinalIgnoreCase)
                        : previousLocalChampionId > 0
                            && previousTargetChampionId > 0
                            && CurrentState.LocalChampionId == previousTargetChampionId
                            && teammate.ChampionId == previousLocalChampionId;

                    if (valuesSwapped || string.Equals(state, "ACCEPTED", StringComparison.OrdinalIgnoreCase))
                    {
                        return SequenceOutcome.Accepted;
                    }

                    if (IsTerminalFailureState(state))
                    {
                        return SequenceOutcome.DeclinedOrUnavailable;
                    }

                    double elapsedMilliseconds = (DateTime.UtcNow - started).TotalMilliseconds;
                    if ((sawPending || elapsedMilliseconds >= 3000)
                        && (!currentSwapId.HasValue
                            || currentSwapId.Value != swapId
                            || SwapService.IsAvailable(state)))
                    {
                        return SequenceOutcome.DeclinedOrUnavailable;
                    }
                }
            }

            return SequenceOutcome.TimedOut;
        }

        private static void ApplySession(LCUTypes.LolChampSelectSessionV1 session)
        {
            LCUTypes.MyTeam localPlayer =
                session.MyTeam?.FirstOrDefault(player => player.CellId == session.LocalPlayerCellId);
            if (localPlayer == null)
            {
                ApplyInactiveState(true, "Local player not found in champion select.");
                return;
            }

            string sessionKey = $"{session.GameId}:{session.Id}";
            List<SwapTeammateState> teammates = new();
            int teammateNumber = 0;

            foreach (LCUTypes.MyTeam player in session.MyTeam ?? Array.Empty<LCUTypes.MyTeam>())
            {
                if (player.CellId == session.LocalPlayerCellId)
                {
                    continue;
                }

                teammateNumber++;
                LCUTypes.PositionSwap positionSwap =
                    session.PositionSwaps?.FirstOrDefault(swap => swap.CellId == player.CellId);
                LCUTypes.ChampionSwap championSwap =
                    session.Trades?.FirstOrDefault(swap => swap.CellId == player.CellId);
                string role = SwapService.PositionName(player.AssignedPosition);
                string label = role == "Unassigned" ? $"Teammate {teammateNumber}" : role;

                teammates.Add(new SwapTeammateState
                {
                    CellId = player.CellId,
                    Label = label,
                    Role = role,
                    ChampionId = player.ChampionId,
                    ChampionPickIntent = player.ChampionPickIntent,
                    PositionSwapId = positionSwap?.Id,
                    PositionSwapState = positionSwap?.State ?? "",
                    ChampionSwapId = championSwap?.Id,
                    ChampionSwapState = championSwap?.State ?? "",
                    IsPending = SwapService.IsPending(positionSwap?.State)
                        || SwapService.IsPending(championSwap?.State)
                });
            }

            bool shouldRefresh;
            bool sessionChanged;
            lock (StateLock)
            {
                sessionChanged = CurrentState.SessionKey != sessionKey;
                Dictionary<int, bool> previousSelection =
                    CurrentState.SessionKey == sessionKey
                        ? CurrentState.Teammates.ToDictionary(item => item.CellId, item => item.Selected)
                        : new Dictionary<int, bool>();

                foreach (SwapTeammateState teammate in teammates)
                {
                    teammate.Selected =
                        previousSelection.TryGetValue(teammate.CellId, out bool selected) && selected;
                }

                SwapPanelState updated = new()
                {
                    IsConnected = true,
                    IsChampionSelectActive = true,
                    SessionKey = sessionKey,
                    LocalPlayerCellId = session.LocalPlayerCellId,
                    LocalRole = SwapService.PositionName(localPlayer.AssignedPosition),
                    LocalChampionId = localPlayer.ChampionId,
                    LocalChampionPickIntent = localPlayer.ChampionPickIntent,
                    Teammates = teammates,
                    IsSequenceRunning = CurrentState.IsSequenceRunning,
                    PendingCellId = CurrentState.PendingCellId,
                    PendingKind = CurrentState.PendingKind,
                    StatusMessage = CurrentState.SessionKey == sessionKey
                        ? CurrentState.StatusMessage
                        : "Champion select detected."
                };

                shouldRefresh = DisplaySignature(CurrentState) != DisplaySignature(updated);
                CurrentState = updated;
            }

            if (sessionChanged)
            {
                Log.Info(
                    "Champion select detected. localCellId={0} teammateCount={1} positionSwapContracts={2} championSwapContracts={3}",
                    session.LocalPlayerCellId,
                    teammates.Count,
                    session.PositionSwaps?.Count ?? 0,
                    session.Trades?.Count ?? 0);
            }

            if (shouldRefresh)
            {
                RefreshPanel();
            }
        }

        private static void ApplyInactiveState(bool connected, string error)
        {
            bool shouldRefresh;
            bool championSelectEnded;
            lock (StateLock)
            {
                championSelectEnded = CurrentState.IsChampionSelectActive;
                SwapPanelState updated = new()
                {
                    IsConnected = connected,
                    StatusMessage = string.IsNullOrWhiteSpace(error)
                        ? connected ? "Champion select not active." : "League client not running."
                        : error,
                    IsSequenceRunning = CurrentState.IsSequenceRunning,
                    PendingCellId = CurrentState.PendingCellId,
                    PendingKind = CurrentState.PendingKind
                };

                shouldRefresh = DisplaySignature(CurrentState) != DisplaySignature(updated);
                CurrentState = updated;
            }

            if (championSelectEnded)
            {
                Log.Info("Champion select ended or became unavailable.");
            }

            if (shouldRefresh)
            {
                RefreshPanel();
            }
        }

        private static bool IsEligible(SwapTeammateState teammate, SwapKind kind)
        {
            if (kind == SwapKind.Position)
            {
                return teammate.RoleSwapEligible
                    && !string.Equals(CurrentState.LocalRole, "Unassigned", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(CurrentState.LocalRole, teammate.Role, StringComparison.OrdinalIgnoreCase);
            }

            return CurrentState.LocalChampionId > 0
                && teammate.ChampionSwapEligible;
        }

        private static bool SessionMatches(string sessionKey)
        {
            return CurrentState.IsChampionSelectActive
                && string.Equals(CurrentState.SessionKey, sessionKey, StringComparison.Ordinal);
        }

        private static bool IsTerminalFailureState(string state)
        {
            return string.Equals(state, "DECLINED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, "CANCELLED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, "CANCELED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, "EXPIRED", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state, "INVALID", StringComparison.OrdinalIgnoreCase);
        }

        private static void ClearPendingRequest()
        {
            lock (StateLock)
            {
                CurrentState.PendingCellId = null;
                ActiveSwapId = null;
            }

            RefreshPanel();
        }

        private static void SetStatus(string status)
        {
            Log.Info("Swap sequence status: {0}", status);
            lock (StateLock)
            {
                SetStatusLocked(status);
            }

            RefreshPanel();
        }

        private static void SetStatusLocked(string status)
        {
            CurrentState.StatusMessage = status;
        }

        private static string DisplaySignature(SwapPanelState state)
        {
            string teammates = string.Join(
                "|",
                state.Teammates.Select(item =>
                    $"{item.CellId}:{item.Role}:{item.ChampionId}:{item.ChampionPickIntent}:{item.PositionSwapId}:{item.PositionSwapState}:{item.ChampionSwapId}:{item.ChampionSwapState}:{item.Selected}"));

            return $"{state.IsConnected}:{state.IsChampionSelectActive}:{state.SessionKey}:{state.LocalRole}:{state.LocalChampionId}:{state.LocalChampionPickIntent}:{state.IsSequenceRunning}:{state.PendingCellId}:{state.PendingKind}:{state.StatusMessage}:{teammates}";
        }

        private static SwapPanelState CloneState(SwapPanelState state)
        {
            return new SwapPanelState
            {
                IsConnected = state.IsConnected,
                IsChampionSelectActive = state.IsChampionSelectActive,
                SessionKey = state.SessionKey,
                LocalPlayerCellId = state.LocalPlayerCellId,
                LocalRole = state.LocalRole,
                LocalChampionId = state.LocalChampionId,
                LocalChampionPickIntent = state.LocalChampionPickIntent,
                Teammates = state.Teammates
                    .Select(item => new SwapTeammateState
                    {
                        CellId = item.CellId,
                        Label = item.Label,
                        Role = item.Role,
                        ChampionId = item.ChampionId,
                        ChampionPickIntent = item.ChampionPickIntent,
                        PositionSwapId = item.PositionSwapId,
                        PositionSwapState = item.PositionSwapState,
                        ChampionSwapId = item.ChampionSwapId,
                        ChampionSwapState = item.ChampionSwapState,
                        Selected = item.Selected,
                        IsPending = item.IsPending
                    })
                    .ToList(),
                IsSequenceRunning = state.IsSequenceRunning,
                PendingCellId = state.PendingCellId,
                PendingKind = state.PendingKind,
                StatusMessage = state.StatusMessage
            };
        }

        private static void RefreshPanelAfterLock()
        {
            _ = Task.Run(RefreshPanel);
        }

        private static void RefreshPanel()
        {
            if (UI.currentWindow == "swapPanel")
            {
                UI.swapPanel(false);
            }
        }
    }
}
