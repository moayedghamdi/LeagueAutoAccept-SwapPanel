using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Leauge_Auto_Accept
{
    internal class SizeHandler
    {
        public static int minWidth = 120;
        public static int minHeight = 30;

        public static int WindowWidth = minWidth;
        public static int WindowHeight = minHeight;
        public static int WidthCenter = WindowWidth / 2;
        public static int HeightCenter = WindowHeight / 2;

        public static void initialize()
        {
            // Hide cursor/caret
            Console.CursorVisible = false;

            // Set the console size
#pragma warning disable CA1416 // Validate platform compatibility
            int targetWidth = Math.Min(minWidth, Console.LargestWindowWidth);
            int targetHeight = Math.Min(minHeight, Console.LargestWindowHeight);
            if (Console.BufferWidth < targetWidth || Console.BufferHeight < targetHeight)
            {
                Console.SetBufferSize(
                    Math.Max(Console.BufferWidth, targetWidth),
                    Math.Max(Console.BufferHeight, targetHeight));
            }
            Console.SetWindowSize(targetWidth, targetHeight);
#pragma warning restore CA1416 // Validate platform compatibility

            WindowWidth = Console.WindowWidth;
            WindowHeight = Console.WindowHeight;
            CalculateCenter();
        }

        public static void SizeReader()
        {
            while (true)
            {
                int currentWidth = Console.WindowWidth;
                int currentHeight = Console.WindowHeight;
                if (WindowWidth != currentWidth || WindowHeight != currentHeight)
                {
                    WindowWidth = currentWidth;
                    WindowHeight = currentHeight;
                    CalculateCenter();
                    handleResize();
                }
                Thread.Sleep(1000);
            }
        }

        public static void handleResize()
        {
            // Hide cursor because for some reason it shows up every single time
            Console.CursorVisible = false;

            // Adapt to new size
            UI.totalRows = WindowHeight - 2;

            // Handle the console being too small
            if (WindowWidth < minWidth)
            {
                UI.consoleTooSmallMessage("width");
            }
            else if (WindowHeight < minHeight)
            {
                UI.consoleTooSmallMessage("height");
            }
            else if (UI.currentWindow == "consoleTooSmallMessage")
            {
                UI.reloadWindow("previous");
            }
            else
            {
                UI.reloadWindow("current");
            }
        }

        public static void CalculateCenter()
        {
            WidthCenter = WindowWidth / 2;
            HeightCenter = WindowHeight / 2;
        }

        public static void resizeBasedOnChampsCount()
        {
            int totalOptions = Data.champsSorted.Count + 2; // 2 calulcates "Unselected" and "None"
            int longestChampionName = Data.champsSorted
                .Select(champion => champion.name?.Length ?? 0)
                .DefaultIfEmpty(16)
                .Max();

            UI.columnSize = Math.Max(20, longestChampionName + 4);
#pragma warning disable CA1416 // Validate platform compatibility
            int availableColumns = Math.Max(1, Console.LargestWindowWidth / UI.columnSize);
#pragma warning restore CA1416 // Validate platform compatibility
            int columnCount = Math.Min(6, availableColumns);
            int neededWidth = UI.columnSize * columnCount;
            int neededHeight = (int)Math.Ceiling(totalOptions / (double)columnCount) + 2;

            minWidth = Math.Max(120, neededWidth);
            minHeight = Math.Max(30, neededHeight);

            if (WindowWidth < minWidth || WindowHeight < minHeight)
            {
                initialize();
            }
        }
    }
}
