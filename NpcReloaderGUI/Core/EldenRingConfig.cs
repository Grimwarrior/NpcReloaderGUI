using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace NpcReloaderGUI.Core
{
    /// <summary>
    /// Holds Elden Ring specific configuration values (AOBs, Offsets, Patches).
    /// </summary>
    public static class EldenRingConfig
    {
        // --- WorldChrMan Pointer Info ---
        public static string WorldChrManAob { get; } = "48 8B 05 ?? ?? ?? ?? 48 85 C0 74 0F 48 39 88 ?? ?? ?? ?? 75 06 89 B1";
        public static int WorldChrManJumpStartOffset { get; } = 3; // Offset within AOB to start reading the relative jump address
        public static int WorldChrManJumpEndOffset { get; } = 7;   // Offset within AOB for the end of the jump instruction (used for RIP calculation)

        // --- WorldChrMan Structure Offsets (Used in Reload Logic Assembly) ---
        // IMPORTANT: Mapping these based on common patterns. Verify if this matches your specific assembly code.
        // Assuming Offset1 = Count/Trigger1 Offset, Offset2 = Float/Trigger2 Offset, Offset3 = Pointer List Offset
        public static int WorldChrManStructOffset_Trigger1 { get; } = ParseHex("1E673"); // Offset for "mov dword ptr [rcx+??], 1"
        public static int WorldChrManStructOffset_Trigger2 { get; } = ParseHex("1E668"); // Offset for "mov dword ptr [rcx+??], 41200000h"
        public static int WorldChrManStructOffset_ListHeadPtr { get; } = ParseHex("1E670"); // **NEW/CORRECTED**: Offset to the pointer used for list manipulation (Likely 0x1E660 for ER 1.07+)
                                                                                            // public static int WorldChrManStructOffset_PointerList { get; } = ParseHex("1E678"); // REMOVED or comment out this potentially incorrect one

        // --- Crash Fix Patch Info ---
        public static string CrashFixAob { get; } = "80 65 ?? FD 48 C7 45 ?? 07 00 00 00 ?? 8D 45 48 4C 89 60 ?? 48 83 78 ?? 08 72 03 48 8B 00 66 44 89 20 49 8B 8F ?? ?? ?? ?? 48 8B 01 48 ?? ??";
        public static int CrashFixOffsetDistanceFromEnd { get; } = 3; // How many bytes *before* the end of the found AOB the patch should be applied.
        public static byte[] CrashFixWriteBytes { get; } = ParseHexString("4831D2"); // Bytes for "XOR RDX, RDX"

        // --- Helper Methods ---

        private static int ParseHex(string hexValue)
        {
            try
            {
                return int.Parse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing hex value '{hexValue}': {ex.Message}");
                // Return a default or throw? Returning 0 for now.
                return 0;
            }
        }

        private static byte[] ParseHexString(string hexString)
        {
            if (string.IsNullOrWhiteSpace(hexString) || hexString.Length % 2 != 0)
            {
                Console.WriteLine($"Invalid hex string provided: '{hexString}'");
                return new byte[0]; // Return empty array on error
            }

            try
            {
                return Enumerable.Range(0, hexString.Length / 2)
                                 .Select(x => Convert.ToByte(hexString.Substring(x * 2, 2), 16))
                                 .ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing hex string '{hexString}': {ex.Message}");
                return new byte[0]; // Return empty array on error
            }
        }

        // Helper to get the length of a parsed AOB pattern string
        public static int GetAobLength(string aob)
        {
            if (string.IsNullOrWhiteSpace(aob)) return 0;
            return aob.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
    }
}