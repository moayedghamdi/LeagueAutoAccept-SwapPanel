﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Leauge_Auto_Accept
{
    internal class UI
    {
        private static readonly object swapPanelRenderLock = new();

        public static string currentWindow = "";
        public static string previousWindow = "";
        public static int currentChampPicker = 0;
        public static int currentSpellSlot = 0;

        public static int totalChamps = 0;
        public static int totalRunes = 0;
        public static int totalSpells = 0;

        // normal/+grid/pages/nocursor/messageEdit
        public static string windowType = "";
        public static int messageIndex = 0; //index for the message currently being edit

        public static int totalRows = SizeHandler.WindowHeight - 2;
        public static int columnSize = 20;
        public static int topPad = 0;
        public static int leftPad = 0;
        public static int numOptions = 0;
        public static int maxPos = 0;
        public static int currentPage = 0;
        public static int totalPages = 0;

        private static int[] cursorPositionValue = { 0, 0 };
        public static int[] cursorPosition
        {
            get { return cursorPositionValue; }
            set
            {
                cursorPositionValue = value;
                Console.SetCursorPosition(value[0], value[1]);
            }
        }

        private static bool showCursorValue = false;
        public static bool showCursor
        {
            get { return showCursorValue; }
            set
            {
                if (showCursorValue != value)
                {
                    Console.CursorVisible = value;
                }
                showCursorValue = value;
            }
        }   

        public static void initializingWindow()
        {
            Print.canMovePos = false;
            Console.Clear();
            currentWindow = "initializing";
            windowType = "nocursor";
            showCursor = false;

            Print.printCentered("Initializing...", SizeHandler.HeightCenter);
        }

        public static void leagueClientIsClosedMessage()
        {
            Print.canMovePos = false;
            currentWindow = "leagueClientIsClosedMessage";
            windowType = "nocursor";
            showCursor = false;

            Console.Clear();

            Print.printCentered("League client cannot be found.", SizeHandler.HeightCenter);
        }

        public static void consoleTooSmallMessage(string direction)
        {
            // Remember what was previously open
            if (currentWindow != "consoleTooSmallMessage")
            {
                previousWindow = currentWindow;
            }

            currentWindow = "consoleTooSmallMessage";
            windowType = "nocursor";
            showCursor = false;

            Console.Clear();

            if (direction == "width")
            {
                Print.printCentered("Console width is too small. Please resize it.", SizeHandler.HeightCenter);
                Print.printCentered("Minimum width:" + SizeHandler.minWidth + " | Current width:" + SizeHandler.WindowWidth);
            }
            else
            {
                Print.printCentered("Console height is too small. Please resize it.", SizeHandler.HeightCenter);
                Print.printCentered("Minimum height:" + SizeHandler.minHeight + " | Current height:" + SizeHandler.WindowHeight);
            }
        }

        public static void reloadWindow(string windowToReload = "current")
        {
            if (windowToReload == "current")
            {
                windowToReload = currentWindow;
            }
            else
            {
                windowToReload = previousWindow;
            }
            switch (windowToReload)
            {
                case "mainScreen":
                    mainScreen();
                    break;
                case "settingsMenu":
                    settingsMenu();
                    break;
                case "delayMenu":
                    delayMenu();
                    break;
                case "leagueClientIsClosedMessage":
                    leagueClientIsClosedMessage();
                    break;
                case "infoMenu":
                    infoMenu();
                    break;
                case "exitMenu":
                    exitMenu();
                    break;
                case "champSelector":
                    champSelector();
                    break;
                case "runeSelector":
                    runeSelector();
                    break;
                case "spellSelector":
                    spellSelector();
                    break;
                case "chatMessagesWindow":
                    chatMessagesWindow();
                    break;
                case "swapPanel":
                    swapPanel();
                    break;
                case "initializing":
                    initializingWindow();
                    break;
            }
        }

        public static void mainScreen()
        {
            Print.canMovePos = false;
            Navigation.currentPos = Navigation.lastPosMainNav;
            Navigation.consolePosLast = Navigation.lastPosMainNav;

            currentWindow = "mainScreen";
            windowType = "normal";
            showCursor = false;
            topPad = SizeHandler.HeightCenter - 1;
            leftPad = SizeHandler.WidthCenter - 25;

            Console.Clear();

            // Define logo
            string[] logo =
            {
                @"  _                                                 _                                     _   ",
                @" | |                                     /\        | |            /\                     | |  ",
                @" | |     ___  __ _  __ _ _   _  ___     /  \  _   _| |_ ___      /  \   ___ ___ ___ _ __ | |_ ",
                @" | |    / _ \/ _` |/ _` | | | |/ _ \   / /\ \| | | | __/ _ \    / /\ \ / __/ __/ _ \ '_ \| __|",
                @" | |___|  __/ (_| | (_| | |_| |  __/  / ____ \ |_| | || (_) |  / ____ \ (_| (_|  __/ |_) | |_ ",
                @" |______\___|\__,_|\__, |\__,_|\___| /_/    \_\__,_|\__\___/  /_/    \_\___\___\___| .__/ \__|",
                @"                    __/ |                                                          | |        ",
                @"                   |___/                                                           |_|        "
            };

            // Print logo
            for (int i = 0; i < logo.Length; i++)
            {
                Print.printCentered(logo[i], SizeHandler.HeightCenter - 12 + i);
            }

            // Define options
            string[] optionName = {
                "Select primary champion",
                " Rune page",
                "Primary backup champion",
                " Rune page",
                "Select secondary champion",
                " Rune page",
                "Secondary backup champion",
                " Rune page",
                "Select a ban",
                "Select summoner spell 1",
                "Select summoner spell 2",
                "Instant chat messages",
                "Target Riot ID",
                "Auto-ban target hover",
                "Auto-dodge target (penalties apply)",
                "Enable auto accept"
            };
            string[] optionValue = {
                Settings.currentChamp[0],
                Settings.currentChampRunes[0],
                Settings.currentBackupChamp[0],
                Settings.currentBackupChampRunes[0],
                Settings.secondaryChamp[0],
                Settings.secondaryChampRunes[0],
                Settings.secondaryBackupChamp[0],
                Settings.secondaryBackupChampRunes[0],
                Settings.currentBan[0],
                Settings.currentSpell1[0],
                Settings.currentSpell2[0],
                Settings.chatMessagesEnabled ? "Enabled, " + Settings.chatMessages.Count : "Disabled",
                string.IsNullOrWhiteSpace(Settings.targetRiotId)
                    ? "Not set"
                    : Settings.targetRiotId,
                Settings.autoBanTargetHover ? "Enabled" : "Disabled",
                Settings.autoDodgeTarget ? "Enabled" : "Disabled",
                MainLogic.isAutoAcceptOn ? "Enabled" : "Disabled"
            };

            numOptions = optionName.Length;
            maxPos = numOptions + 4; //Settings + Swap Panel + Arena + Info

            // Print options
            for (int i = 0; i < optionName.Length; i++)
            {
                Print.printCentered(addDotsInBetween(optionName[i], optionValue[i]), topPad + i);
            }

            // Print the bottom buttons that are not actual settings
            Print.printWhenPossible("  Info", SizeHandler.HeightCenter + numOptions, leftPad + 41);
            Print.printWhenPossible("  Arena", SizeHandler.HeightCenter + numOptions, leftPad + 29);
            Print.printWhenPossible("  Swap Panel", SizeHandler.HeightCenter + numOptions, leftPad + 14);
            Print.printWhenPossible("  Settings", SizeHandler.HeightCenter + numOptions, leftPad + 1);

            Print.printWhenPossible("v" + Updater.appVersion, SizeHandler.WindowHeight - 1, 0, false);

            Navigation.handlePointerMovementPrint();

            Print.canMovePos = true;
        }

        public static void swapPanel(bool resetNavigation = true)
        {
            lock (swapPanelRenderLock)
            {
                if (!resetNavigation && currentWindow != "swapPanel")
                {
                    return;
                }

                renderSwapPanel(resetNavigation);
            }
        }

        private static void renderSwapPanel(bool resetNavigation)
        {
            Print.canMovePos = false;
            if (resetNavigation)
            {
                Navigation.currentPos = 0;
                Navigation.consolePosLast = 0;
            }

            currentWindow = "swapPanel";
            windowType = "normal";
            showCursor = false;
            topPad = 7;
            leftPad = Math.Max(0, SizeHandler.WidthCenter - 55);
            maxPos = 10;

            SwapPanelState state = SwapController.GetState();
            Console.Clear();

            Print.printCentered("Champion Select Swap Panel", 1);
            Print.printCentered(
                $"League client: {(state.IsConnected ? "Connected" : "Disconnected")} | " +
                $"Champion select: {(state.IsChampionSelectActive ? "Active" : "Not active")}",
                2);

            string localChampion = SwapService.ChampionText(
                state.LocalChampionId,
                state.LocalChampionPickIntent);
            Print.printCentered(
                $"Local player | Pick: {(state.LocalPickOrder > 0 ? $"#{state.LocalPickOrder}" : "N/A")} | " +
                $"Role: {state.LocalRole} | Champion: {localChampion}",
                3);
            Print.printCentered(
                $"Selected: {state.Teammates.Count(teammate => teammate.Selected)} | " +
                "Use Enter to toggle a teammate or activate a command.",
                4);

            for (int row = 0; row < 4; row++)
            {
                if (row >= state.Teammates.Count)
                {
                    printSwapOption(row, "[ ] Waiting for teammate...");
                    continue;
                }

                SwapTeammateState teammate = state.Teammates[row];
                bool pickOrderEligible = state.IsChampionSelectActive
                    && !state.IsSequenceRunning
                    && state.LocalPickOrder > 0
                    && teammate.PickOrderSwapEligible
                    && state.LocalPickOrder != teammate.PickOrder;
                bool roleEligible = state.IsChampionSelectActive
                    && !state.IsSequenceRunning
                    && teammate.RoleSwapEligible
                    && state.LocalRole != "Unassigned"
                    && !string.Equals(
                        state.LocalRole,
                        teammate.Role,
                        StringComparison.OrdinalIgnoreCase);
                bool championEligible = state.IsChampionSelectActive
                    && !state.IsSequenceRunning
                    && state.LocalChampionId > 0
                    && teammate.ChampionSwapEligible;
                bool pending = teammate.IsPending
                    || state.PendingCellId == teammate.CellId;

                string pickOrderStatus = pickOrderEligible
                    ? "Yes"
                    : string.IsNullOrWhiteSpace(teammate.PickOrderSwapState)
                        ? "N/A"
                        : teammate.PickOrderSwapState;
                string roleStatus = roleEligible
                    ? "Yes"
                    : string.IsNullOrWhiteSpace(teammate.PositionSwapState)
                        ? "N/A"
                        : teammate.PositionSwapState;
                string championStatus = championEligible
                    ? "Yes"
                    : string.IsNullOrWhiteSpace(teammate.ChampionSwapState)
                        ? "N/A"
                        : teammate.ChampionSwapState;
                string champion = SwapService.ChampionText(
                    teammate.ChampionId,
                    teammate.ChampionPickIntent);

                printSwapOption(
                    row,
                    $"[{(teammate.Selected ? 'x' : ' ')}] {teammate.Label,-10} | " +
                    $"{(teammate.PickOrder > 0 ? $"#{teammate.PickOrder}" : "N/A"),-3} | " +
                    $"{champion,-18} | Order: {pickOrderStatus,-9} | " +
                    $"Pos: {roleStatus,-9} | Champ: {championStatus,-9}" +
                    (pending ? " | Pending" : ""));
            }

            bool hasPendingSwap = state.Teammates.Any(teammate => teammate.IsPending);
            bool hasPickOrderSelection = state.LocalPickOrder > 0
                && state.Teammates.Any(
                    teammate => teammate.Selected
                        && teammate.PickOrderSwapEligible
                        && state.LocalPickOrder != teammate.PickOrder);
            bool hasRoleSelection = state.LocalRole != "Unassigned"
                && state.Teammates.Any(
                    teammate => teammate.Selected
                        && teammate.RoleSwapEligible
                        && !string.Equals(
                            state.LocalRole,
                            teammate.Role,
                            StringComparison.OrdinalIgnoreCase));
            bool hasChampionSelection = state.LocalChampionId > 0
                && state.Teammates.Any(
                    teammate => teammate.Selected && teammate.ChampionSwapEligible);

            printSwapOption(4, "Select All Eligible Teammates");
            printSwapOption(5, "Clear Selection");
            printSwapOption(
                6,
                "Request Pick-Order Swap" +
                (state.IsChampionSelectActive
                    && !state.IsSequenceRunning
                    && !hasPendingSwap
                    && hasPickOrderSelection
                    ? ""
                    : " [disabled]"));
            printSwapOption(
                7,
                "Request Position Swap" +
                (state.IsChampionSelectActive
                    && !state.IsSequenceRunning
                    && !hasPendingSwap
                    && hasRoleSelection
                    ? ""
                    : " [disabled]"));
            printSwapOption(
                8,
                "Request Champion Swap" +
                (state.IsChampionSelectActive
                    && !state.IsSequenceRunning
                    && !hasPendingSwap
                    && hasChampionSelection
                    ? ""
                    : " [disabled]"));
            printSwapOption(
                9,
                "Cancel Active Sequence" + (state.IsSequenceRunning ? "" : " [disabled]"));

            Print.printCentered($"Status: {state.StatusMessage}", topPad + maxPos + 1);
            Print.printCentered("Esc: return to main screen", topPad + maxPos + 3);

            Print.canMovePos = true;
            Navigation.handlePointerMovementPrint();
        }

        private static void printSwapOption(int position, string text)
        {
            const int availableWidth = 108;
            string output = text.Length > availableWidth
                ? text.Substring(0, availableWidth)
                : text;
            Print.printWhenPossible(
                output.PadRight(availableWidth),
                topPad + position,
                leftPad + 2);
        }

        public static void toggleAutoAcceptSettingUI(int pos)
        {
            Print.printWhenPossible(MainLogic.isAutoAcceptOn ? ". Enabled" : " Disabled", topPad + pos, leftPad + 38);
        }

        public static void arenaMenu()
        {
            Print.canMovePos = false;
            Navigation.currentPos = 0;
            Navigation.consolePosLast = 0;

            currentWindow = "arenaMenu";
            windowType = "normal";
            showCursor = false;
            topPad = SizeHandler.HeightCenter - 4;
            leftPad = SizeHandler.WidthCenter - 25;
            maxPos = 7;

            Console.Clear();
            
            string[] optionName = {
                "Enable Bravery",
                "Ban crowd favourite champion",
                "Crowd favourite 1st",
                "Crowd favourite 2nd",
                "Crowd favourite 3rd",
                "Crowd favourite 4th",
                "Crowd favourite 5th",
            };
            
            string[] optionValue = {
                Settings.bravery ? "Yes" : "No",
                Settings.banCrowdFavourite ? "Yes" : "No",
                Settings.crowdFavouraiteChamp1[0],
                Settings.crowdFavouraiteChamp2[0],
                Settings.crowdFavouraiteChamp3[0],
                Settings.crowdFavouraiteChamp4[0],
                Settings.crowdFavouraiteChamp5[0],
            };
            
            // Print options
            for (int i = 0; i < optionName.Length; i++)
            {
                Print.printCentered(addDotsInBetween(optionName[i], optionValue[i]), topPad + i);
            }
            
            Navigation.handlePointerMovementPrint();
            Print.canMovePos = true;
            arenaMenuDesc(0);
        }
        
        public static void arenaMenuDesc(int item)
        {
            switch (item)
            {
                case 0:
                    Print.printCentered("Enable or disable bravery for arena games.", topPad + maxPos + 2);
                    Print.printCentered("This will pick bravery in arena games over your selected champion", topPad + maxPos + 3);
                    break;
                case 1:
                    Print.printCentered("Enable or disable banning one of the selected crowd favourite champion", topPad + maxPos + 2);
                    Print.printCentered("If true and the ban matches one of the crowd favourite champions, It will ban None.", topPad + maxPos + 3);
                    break;
                case 2:
                    Print.printCentered("Select the first crowd favourite champion to be picked in arena", topPad + maxPos + 2);
                    Print.printCentered("This will be picked over bravery.", topPad + maxPos + 3);
                    break;
                case 3:
                    Print.printCentered("Select the second crowd favourite champion to be picked in arena", topPad + maxPos + 2);
                    Print.printCentered("This will be picked over bravery.", topPad + maxPos + 3);
                    break;
                case 4:
                    Print.printCentered("Select the third crowd favourite champion to be picked in arena", topPad + maxPos + 2);
                    Print.printCentered("This will be picked over bravery.", topPad + maxPos + 3);
                    break;
                case 5:
                    Print.printCentered("Select the fourth crowd favourite champion to be picked in arena", topPad + maxPos + 2);
                    Print.printCentered("This will be picked over bravery.", topPad + maxPos + 3);
                    break;
                case 6:
                    Print.printCentered("Select the fifth crowd favourite champion to be picked in arena", topPad + maxPos + 2);
                    Print.printCentered("This will be picked over bravery.", topPad + maxPos + 3);
                    break;
            }
        }
        
        public static void arenaMenuUpdateUI(int item)
        {
            // Select item to toggle from settings

            string outputText = item switch
            {
                0 => Settings.bravery ? " Yes" : ". No",
                1 => Settings.banCrowdFavourite ? "Yes" : ". No",
                2 => Settings.crowdFavouraiteChamp1[0],
                3 => Settings.crowdFavouraiteChamp2[0],
                4 => Settings.crowdFavouraiteChamp3[0],
                5 => Settings.crowdFavouraiteChamp4[0],
                6 => Settings.crowdFavouraiteChamp5[0],
                _ => throw new NotImplementedException(), //added to suppress CS8509
            };
            Print.printWhenPossible(outputText, item + topPad, SizeHandler.WidthCenter + 22 - outputText.Length);
        }

        public static void settingsMenu()
        {
            Print.canMovePos = false;
            Navigation.currentPos = 0;
            Navigation.consolePosLast = 0;

            currentWindow = "settingsMenu";
            windowType = "normal";
            showCursor = false;
            topPad = SizeHandler.HeightCenter - 4;
            leftPad = SizeHandler.WidthCenter - 25;
            maxPos = 10;

            Console.Clear();

            // Define options
            string[] optionName = {
                "Save settings/config",
                "Preload data",
                "Instalock pick",
                "Instalock ban",
                "Disable update check",
                "Automatically trade pick order",
                "Instantly hover pick",
                "Automatically restart queue",
                "Cancel queue after dodge",
                "Delay settings"
            };

            string[] optionValue = {
                Settings.saveSettings ? "Yes" : "No",
                Settings.preloadData ? "Yes" : "No",
                Settings.instaLock ? "Yes" : "No",
                Settings.instaBan ? "Yes" : "No",
                Settings.disableUpdateCheck ? "Yes" : "No",
                Settings.autoPickOrderTrade ? "Yes" : "No",
                Settings.instantHover ? "Yes" : "No",
                Settings.autoRestartQueue ? "Yes" : "No",
                Settings.cancelQueueAfterDodge ? "Yes" : "No",
                ""
            };

            // Print options
            for (int i = 0; i < optionName.Length; i++)
            {
                Print.printCentered(addDotsInBetween(optionName[i], optionValue[i]), topPad + i);
            }

            Navigation.handlePointerMovementPrint();

            Print.canMovePos = true;

            settingsMenuDesc(0);
        }

        public static void settingsMenuDesc(int item)
        {
            // settings descrptions
            switch (item)
            {
                case 0:
                    Print.printCentered("Save settings for the next time you open the app.", topPad + maxPos + 2);
                    Print.printCentered("This will create a settings file in the %AppData% folder.");
                    break;
                case 1:
                    Print.printCentered("Preload all data the app will need on launch.", topPad + maxPos + 2);
                    Print.printCentered("This includes champions list, summoner spells list and more.");
                    break;
                case 2:
                    Print.printCentered("Instanly lock in when it's your turn to pick.", topPad + maxPos + 2);
                    Print.printCentered("This will bypass the lock in delay setting.");
                    break;
                case 3:
                    Print.printCentered("Instanly lock in when it's your turn to ban.", topPad + maxPos + 2);
                    Print.printCentered("This will bypass the lock in delay setting.");
                    break;
                case 4:
                    Print.printCentered("Disable update check on startup.", topPad + maxPos + 2);
                    Print.printCentered("");
                    break;
                case 5:
                    Print.printCentered("Automatically trade pick order when someone requests to.", topPad + maxPos + 2);
                    Print.printCentered("");
                    break;
                case 6:
                    Print.printCentered("Instantly hover champion as soon as joining champ select.", topPad + maxPos + 2);
                    Print.printCentered("In draft pick, it will hover before you are normally able to.");
                    break;
                case 7:
                    Print.printCentered("Automatically restart queue every few minutes.", topPad + maxPos + 2);
                    Print.printCentered("Default is 5 mintues, can be configured in the delays settings.");
                    break;
                case 8:
                    Print.printCentered("Automatically cancel the queue after someone dodges the lobby.", topPad + maxPos + 2);
                    Print.printCentered("");
                    break;
                case 9:
                    Print.printCentered("Adjust different delays.", topPad + maxPos + 2);
                    Print.printCentered("");
                    break;
            }
        }

        public static void settingsMenuUpdateUI(int item)
        {
            // Select item to toggle from settings

            string outputText = item switch
            {
                0 => Settings.saveSettings ? " Yes" : ". No",
                1 => Settings.preloadData ? " Yes" : ". No",
                2 => Settings.instaLock ? " Yes" : ". No",
                3 => Settings.instaBan ? " Yes" : ". No",
                4 => Settings.disableUpdateCheck ? " Yes" : ". No",
                5 => Settings.autoPickOrderTrade ? " Yes" : ". No",
                6 => Settings.instantHover ? " Yes" : ". No",
                7 => Settings.autoRestartQueue ? " Yes" : ". No",
                8 => Settings.cancelQueueAfterDodge ? " Yes" : ". No",
                _ => ""
            };
            Print.printWhenPossible(outputText, item + topPad, SizeHandler.WidthCenter + 22 - outputText.Length);
        }



        public static void delayMenu()
        {
            Print.canMovePos = false;
            Navigation.currentPos = 0;
            Navigation.consolePosLast = 0;

            currentWindow = "delayMenu";
            windowType = "normal";
            showCursor = false;
            topPad = SizeHandler.HeightCenter - 3;
            leftPad = SizeHandler.WidthCenter - 25;
            maxPos = 8;

            Console.Clear();

            // Define options
            string[] optionName = {
                "Pick hover delay upon phase start",
                "Pick lock delay upon phase start",
                "Pick lock delay before phase end",
                "Ban hover delay upon phase start",
                "Ban lock delay upon phase start",
                "Ban lock delay before phase end",
                "Max queue time before restart",
                "Chat Messages Delay"
            };
            string[] optionValue = {
                Settings.pickStartHoverDelay.ToString(),
                Settings.pickStartlockDelay.ToString(),
                Settings.pickEndlockDelay.ToString(),
                Settings.banStartHoverDelay.ToString(),
                Settings.banStartlockDelay.ToString(),
                Settings.banEndlockDelay.ToString(),
                Settings.queueMaxTime.ToString(),
                Settings.chatMessagesDelay.ToString()
            };

            // Print options
            for (int i = 0; i < optionName.Length; i++)
            {
                Print.printCentered(addDotsInBetween(optionName[i], optionValue[i]), topPad + i);
            }

            Navigation.handlePointerMovementPrint();

            Print.canMovePos = true;

            delayMenuDesc(0);
        }

        public static void delayMenuDesc(int item)
        {
            // settings descrptions
            switch (item)
            {
                case 0:
                    Print.printCentered("Delay after which to hover your pick.", topPad + maxPos + 2);
                    Print.printCentered("Default is 10000.");
                    break;
                case 1:
                    Print.printCentered("Delay after which to lock in your pick, after you are able to.", topPad + maxPos + 2);
                    Print.printCentered("Default is 999999999.");
                    break;
                case 2:
                    Print.printCentered("Time to lock in before your time runs out.", topPad + maxPos + 2);
                    Print.printCentered("Do not set too low (<300), it will cause you to dodge. Default is 1000.");
                    break;
                case 3:
                    Print.printCentered("Delay after which to hover your ban.", topPad + maxPos + 2);
                    Print.printCentered("Default is 1500.");
                    break;
                case 4:
                    Print.printCentered("Delay after which to lock in your pick, after you are able to.", topPad + maxPos + 2);
                    Print.printCentered("Default is 999999999.");
                    break;
                case 5:
                    Print.printCentered("Time to lock in before your time runs out", topPad + maxPos + 2);
                    Print.printCentered("Default is 1000.");
                    break;
                case 6:
                    Print.printCentered("How long should the queue be before cancelling and restarting it?", topPad + maxPos + 2);
                    Print.printCentered("Default is 300000.");
                    break;
                case 7:
                    Print.printCentered("Delay after which the chat messages will be sent", topPad + maxPos + 2);
                    Print.printCentered("Default is 100.");
                    break;
            }
        }

        public static void delayMenuUpdateUI(int item)
        {
            // Select item to toggle from settings

            string outputText = item switch
            {
                0 => (" " + Settings.pickStartHoverDelay).PadLeft(10, '.'),
                1 => (" " + Settings.pickStartlockDelay).PadLeft(10, '.'),
                2 => (" " + Settings.pickEndlockDelay).PadLeft(10, '.'),
                3 => (" " + Settings.banStartHoverDelay).PadLeft(10, '.'),
                4 => (" " + Settings.banStartlockDelay).PadLeft(10, '.'),
                5 => (" " + Settings.banEndlockDelay).PadLeft(10, '.'),
                6 => (" " + Settings.queueMaxTime).PadLeft(10, '.'),
                7 => (" " + Settings.chatMessagesDelay).PadLeft(10, '.'),
                _ => ""
            };
            Print.printWhenPossible(outputText, item + topPad, SizeHandler.WidthCenter + 22 - outputText.Length);
        }

        public static void infoMenu()
        {
            Print.canMovePos = false;
            Navigation.currentPos = 0;
            Navigation.consolePosLast = 0;

            currentWindow = "infoMenu";
            windowType = "nocursor";
            showCursor = false;

            Console.Clear();

            Print.printCentered(addDotsInBetween("Made by", "sweetriverfish"), SizeHandler.HeightCenter - 5);
            Print.printCentered(addDotsInBetween("Version", Updater.appVersion));

            Print.printCentered("Source code:", SizeHandler.HeightCenter -2);
            Print.printCentered(" github.com/sweetriverfish/LeagueAutoAccept");

            Print.printCentered("Contributors:", SizeHandler.HeightCenter + 1);

            string[] ContributorsList = {
                "IxPrumxI",
                "ivelmiskin3",
                "mrmark1998",
                "mrcyclo",
                "considerate-mouth",
                "DanielBiondi",
                "ericnewton76"
            };

            Random rng = new Random();
            int n = ContributorsList.Length;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                string value = ContributorsList[k];
                ContributorsList[k] = ContributorsList[n];
                ContributorsList[n] = value;
            }

            for (int i = 0; i < ContributorsList.Length; i++)
            {
                Print.printCentered(ContributorsList[i]);
            }
        }

        public static void exitMenu()
        {
            Print.canMovePos = false;
            Navigation.currentPos = 0;
            Navigation.consolePosLast = 0;

            currentWindow = "exitMenu";
            windowType = "sideways";
            showCursor = false;
            topPad = SizeHandler.HeightCenter + 1;
            leftPad = SizeHandler.WidthCenter - 19;
            maxPos = 2;

            Console.Clear();

            Print.printCentered("Are you sure you want to close this app?", topPad - 2);
            Print.printWhenPossible((" No").PadLeft(32, ' '), topPad, leftPad + 3, false);
            Print.printWhenPossible("Yes ", topPad, leftPad + 3, false);

            Navigation.handlePointerMovementPrint();

            Print.canMovePos = true;
        }

        public static void champSelector()
        {
            Print.canMovePos = false;

            totalRows = SizeHandler.WindowHeight - 2;

            currentWindow = "champSelector";
            windowType = "grid";
            showCursor = false;

            Navigation.currentInput = "";

            Console.Clear();

            if (!Data.loadChampionsList())
            {
                Print.printCentered("Failed to load champions from League.", SizeHandler.HeightCenter - 1);
                Print.printCentered("Press Escape to return.");
                return;
            }

            displayChamps();
            updateCurrentFilter();
        }

        private static void displayChamps()
        {
            Console.CursorVisible = false;
            Navigation.currentPos = 0;
            Navigation.consolePosLast = 0;

            topPad = 0;
            leftPad = 0;

            Console.SetCursorPosition(0, 0);

            List<itemList> champsFiltered = new List<itemList>();
            if ("unselected".Contains(Navigation.currentInput.ToLower()))
            {
                champsFiltered.Add(new itemList() { name = "Unselected", id = "0" });
            }
            if (currentChampPicker == 4)
            {
                if ("none".Contains(Navigation.currentInput.ToLower()))
                {
                    champsFiltered.Add(new itemList() { name = "None", id = "-1" });
                }
            }
            foreach (var champ in Data.champsSorted)
            {
                if (champ.name.ToLower().Contains(Navigation.currentInput.ToLower()))
                {
                    // Make sure the champ is free or if it's for a ban before adding it to the list
                     if (champ.free || currentChampPicker == 4 && int.Parse(champ.id) < 10000)
                    {
                        champsFiltered.Add(new itemList() { name = champ.name, id = champ.id });
                    }
                }
            }

            totalChamps = champsFiltered.Count;
            maxPos = totalChamps;

            int currentRow = 0;
            string[] champsOutput = new string[totalRows];

            foreach (var champ in champsFiltered)
            {
                string line = "   " + champ.name;
                line = line.PadRight(columnSize, ' ');

                champsOutput[currentRow] += line;

                currentRow++;
                if (currentRow >= totalRows)
                {
                    currentRow = 0;
                }
            }

            foreach (var line in champsOutput)
            {
                string lineNew;
                if (line != null)
                {
                    lineNew = line.Remove(line.Length - 1);
                    lineNew = lineNew.PadRight(119, ' ');
                }
                else
                {
                    lineNew = "".PadRight(119, ' ');
                }
                Print.printWhenPossible(lineNew);
            }
            Navigation.handlePointerMovementPrint();
            Print.canMovePos = true;
            displayCursorIfNeeded();
        }

        public static void runeSelector()
        {
            Print.canMovePos = false;

            totalRows = SizeHandler.WindowHeight - 2;

            currentWindow = "runeSelector";
            windowType = "grid";
            showCursor = false;

            Navigation.currentInput = "";

            Console.Clear();

            if (!Data.loadRunesList())
            {
                Print.printCentered("Failed to load rune pages from League.", SizeHandler.HeightCenter - 1);
                Print.printCentered("Press Escape to return.");
                return;
            }

            displayRunes();
            updateCurrentFilter();
        }

        private static void displayRunes()
        {
            Console.CursorVisible = false;
            Navigation.currentPos = 0;
            Navigation.consolePosLast = 0;

            topPad = 0;
            leftPad = 0;

            Console.SetCursorPosition(0, 0);

            List<itemList> runesFiltered = new List<itemList>();
            if ("unselected".Contains(Navigation.currentInput.ToLower()))
            {
                runesFiltered.Add(new itemList() { name = "Unselected", id = "0" });
            }
            foreach (var rune in Data.runesList)
            {
                if (rune.name.ToLower().Contains(Navigation.currentInput.ToLower()))
                {
                    runesFiltered.Add(new itemList() { name = rune.name, id = rune.id });
                }
            }

            totalRunes = runesFiltered.Count;
            maxPos = totalRunes;

            int currentRow = 0;
            string[] runeoutput = new string[totalRows];

            foreach (var rune in runesFiltered)
            {
                string line = "   " + rune.name;
                line = line.PadRight(columnSize, ' ');

                runeoutput[currentRow] += line;

                currentRow++;
                if (currentRow >= totalRows)
                {
                    currentRow = 0;
                }
            }

            foreach (var line in runeoutput)
            {
                string lineNew;
                if (line != null)
                {
                    lineNew = line.Remove(line.Length - 1);
                    lineNew = lineNew.PadRight(119, ' ');
                }
                else
                {
                    lineNew = "".PadRight(119, ' ');
                }
                Print.printWhenPossible(lineNew);
            }
            Navigation.handlePointerMovementPrint();
            Print.canMovePos = true;
            displayCursorIfNeeded();
        }

        public static void spellSelector()
        {
            Print.canMovePos = false;

            totalRows = SizeHandler.WindowHeight - 2;

            currentWindow = "spellSelector";
            windowType = "grid";
            showCursor = false;

            Navigation.currentInput = "";

            Console.Clear();

            if (!Data.loadSpellsList())
            {
                Print.printCentered("Failed to load summoner spells from League.", SizeHandler.HeightCenter - 1);
                Print.printCentered("Press Escape to return.");
                return;
            }

            displaySpells();
            updateCurrentFilter();
        }

        private static void displaySpells()
        {
            Console.CursorVisible = false;
            Navigation.currentPos = 0;
            Navigation.consolePosLast = 0;

            topPad = 0;
            leftPad = 0;

            Console.SetCursorPosition(0, 0);

            List<itemList> spellsFiltered = new List<itemList>();
            if ("unselected".Contains(Navigation.currentInput.ToLower()))
            {
                spellsFiltered.Add(new itemList() { name = "Unselected", id = "0" });
            }
            foreach (var spell in Data.spellsSorted)
            {
                if (spell.name.ToLower().Contains(Navigation.currentInput.ToLower()))
                {
                    spellsFiltered.Add(new itemList() { name = spell.name, id = spell.id });
                }
            }

            totalSpells = spellsFiltered.Count;
            maxPos = totalSpells;

            int currentRow = 0;
            string[] spelloutput = new string[totalRows];

            foreach (var spell in spellsFiltered)
            {
                string line = "   " + spell.name;
                line = line.PadRight(columnSize, ' ');

                spelloutput[currentRow] += line;

                currentRow++;
                if (currentRow >= totalRows)
                {
                    currentRow = 0;
                }
            }

            foreach (var line in spelloutput)
            {
                string lineNew;
                if (line != null)
                {
                    lineNew = line.Remove(line.Length - 1);
                    lineNew = lineNew.PadRight(119, ' ');
                }
                else
                {
                    lineNew = "".PadRight(119, ' ');
                }
                Print.printWhenPossible(lineNew);
            }
            Navigation.handlePointerMovementPrint();
            Print.canMovePos = true;
            displayCursorIfNeeded();
        }

        public static void updateCurrentFilter()
        {
            showCursor = true;
            if (currentWindow == "champSelector")
            {
                displayChamps();
            }
            else if (currentWindow == "spellSelector")
            {
                displaySpells();
            }
             else if (currentWindow == "runeSelector")
            {
                displayRunes();
            }

            Navigation.currentPos = 0;
            Console.CursorVisible = false;
            string consoleLine = "Search: " + Navigation.currentInput;
            Print.printCentered(consoleLine, Console.WindowHeight - 1, false, true);

            Console.SetCursorPosition(0, 0);
            Console.CursorVisible = true;
            updateCursorPosition();
        }

        public static void printHeart()
        {
            int[] position = { 54, 10 };

            string[] lines = {
                "  oooo   oooo",
                " o    o o    o ",
                "o      o      o",
                " o           o",
                "  o         o",
                "   o       o",
                "    o     o",
                "     o   o",
                "      o o",
                "       o"
            };

            foreach (string line in lines)
            {
                Console.SetCursorPosition(position[0], position[1]++);
                Console.WriteLine(line);
            }

            updateCursorPosition();
        }

        private static string addDotsInBetween(string firstString, string secondString, int totalLength = 44)
        {
            int firstStringLength = firstString.Length + 1;
            int secondStringLength = secondString.Length + 1;
            int dotsCount = totalLength - firstStringLength - secondStringLength;

            // Make sure there's a minimum of 2 dots
            if (dotsCount < 2)
            {
                int allowedLength = totalLength - firstStringLength - 3;

                if (allowedLength < 0)
                {
                    allowedLength = 0; // avoid negative substring length
                }

                if (secondString.Length > allowedLength)
                {
                    secondString = secondString.Substring(0, allowedLength) + "~";
                }

                dotsCount = 2;
            }

            return firstString + " " + new string('.', dotsCount) + " " + secondString;
        }

        public static void chatMessagesWindow(int pageToLoad = 0)
        {
            Print.canMovePos = false;
            Console.Clear();
            Navigation.currentPos = 0;
            Navigation.consolePosLast = 0;

            currentWindow = "chatMessagesWindow";
            windowType = "pages";
            showCursor = false;
            topPad = 1;
            leftPad = 2;
            maxPos = Settings.chatMessages.Count + 1; // +1 for "new message" row
            int messageWidth = SizeHandler.WindowWidth - (leftPad * 2) - 6; // calclate the amount of characters to display of each messages before cropping it
            totalRows = SizeHandler.WindowHeight - 4; // calculate rows per page
            if (maxPos > totalRows)
            {
                maxPos = totalRows;
            }

            {
                double totalPagesTmp = ((double)Settings.chatMessages.Count + 1) / (double)totalRows;
                int totalPagesTmp2 = (int)Math.Ceiling(totalPagesTmp);
                totalPages = totalPagesTmp2;
            }

            int currentConsoleRow = topPad;
            int currentMessagePrint = 0;
            int startingIndex = pageToLoad * totalRows;

            // Print all messages
            foreach (var message in Settings.chatMessages)
            {
                if (startingIndex > 0)
                {
                    startingIndex--;
                    continue;
                }

                if (currentMessagePrint + 1 > totalRows)
                {
                    break;
                }

                // Limit messages to console width, crop and add an ellipsis at the end if the message is too long
                string messageOutput = message.Length > messageWidth ? message.Substring(0, messageWidth - 3) + "..." : message;
                Print.printWhenPossible(messageOutput, currentConsoleRow++, leftPad + 3, false);
                currentMessagePrint++;
            }

            // Add a button to create a new message
            if (!(currentMessagePrint + 1 > totalRows)) // +1 for "new message" row
            {
                Print.printWhenPossible("[new message]", currentConsoleRow++, leftPad + 3, false);
            }

            // Print pages count, if needed
            if (totalPages > 1)
            {
                string pagesPrint = Print.centerString("Current page: " + (pageToLoad + 1) + " / " + totalPages)[0];
                pagesPrint = Print.replaceAt(pagesPrint, "<- previous page", leftPad + 3);
                pagesPrint = Print.replaceAt(pagesPrint, "next page ->", SizeHandler.WindowWidth - 17);
                Print.printWhenPossible(pagesPrint, SizeHandler.WindowHeight - 2, 0, false);
            }

            Print.canMovePos = true;
            Navigation.handlePointerMovementPrint();
        }

        public static void chatMessagesEdit()
        {
            Print.canMovePos = false;
            Console.Clear();
            Navigation.currentPos = 0;
            Navigation.consolePosLast = 0;

            currentWindow = "chatMessagesEdit";
            windowType = "messageEdit";
            showCursor = true;
            topPad = SizeHandler.HeightCenter - 2;
            leftPad = SizeHandler.WidthCenter;
            maxPos = 3;

            if (Settings.chatMessages.Count > messageIndex)
            {
                Navigation.currentInput = Settings.chatMessages[messageIndex];
            }
            else
            {
                Navigation.currentInput = "";
            }

            updateMessageEdit();

            Print.printCentered("Save          Delete         Cancel", topPad + 3, false);


            Print.canMovePos = true;
            Navigation.handlePointerMovementPrint();
            updateCursorPosition();
        }

        public static void targetRiotIdEdit()
        {
            Print.canMovePos = false;
            Console.Clear();
            Navigation.currentPos = 0;
            Navigation.consolePosLast = 0;

            currentWindow = "targetRiotIdEdit";
            windowType = "messageEdit";
            showCursor = true;
            topPad = SizeHandler.HeightCenter;
            leftPad = SizeHandler.WidthCenter;
            maxPos = 2;
            Navigation.currentInput = Settings.targetRiotId;

            Print.printCentered("Target Riot ID (GameName#TagLine)", topPad - 2, false);
            updateMessageEdit();
            Print.printCentered("Save          Cancel", topPad + 3, false);

            Print.canMovePos = true;
            Navigation.handlePointerMovementPrint();
            updateCursorPosition();
        }

        public static void showTargetRiotIdValidationError()
        {
            Print.printCentered(
                "Enter a complete Riot ID such as GameName#TagLine, or leave it empty.",
                topPad + 5,
                false,
                true);
            updateCursorPosition();
        }

        public static void updateMessageEdit()
        {
            string message = Navigation.currentInput;
            int chunkLength = 100; // Length of each chunk

            List<string> chunks = new List<string>();

            // Extract chunks from the input message
            for (int i = 0; i < message.Length; i += chunkLength)
            {
                int length = Math.Min(chunkLength, message.Length - i);
                chunks.Add(message.Substring(i, length));
            }

            string chunk1 = chunks.Count > 0 ? chunks[0] : "";
            string chunk2 = chunks.Count > 1 ? chunks[1] : "";

            Console.CursorVisible = false;

            // make sure the second line is wiped if it has something
            if (chunk2.Length == 0)
            {
                Print.printCentered(chunk2, topPad + 1, false, true);
            }

            // print first line
            Print.printCentered(chunk1, topPad, false, true);

            // only print second line if needed
            if (chunk2.Length > 0)
            {
                Print.printCentered(chunk2, topPad + 1, false, true);
            }
            displayCursorIfNeeded();
        }

        public static void updateCursorPosition()
        {
            Console.SetCursorPosition(cursorPosition[0], cursorPosition[1]);
        }

        public static void displayCursorIfNeeded()
        {
            if (showCursor)
            {
                Console.CursorVisible = true;
            }
        }
    }
}




