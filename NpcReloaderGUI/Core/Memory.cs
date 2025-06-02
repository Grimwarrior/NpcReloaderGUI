// ----- Start of Core/Memory.cs -----
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NpcReloaderGUI.Core; // Needed to access EldenRingConfig
using NpcReloaderGUI;

namespace NpcReloaderGUI.Core
{
    public static class Memory
    {
        // --- Static Properties ---
        public static IntPtr ProcessHandle { get; private set; } = IntPtr.Zero;
        public static Process AttachedProcess { get; private set; } = null;
        public static IntPtr BaseAddress { get; private set; } = IntPtr.Zero;

        // Configurable Elden Ring process names
        public const string EldenRingProcessName_Default = "eldenring";
        public const string EldenRingProcessName_NoEAC = "start_protected_game";

        // Elden Ring AOB Scan Results (Defined ONLY ONCE)
        public static IntPtr EldenRing_WorldChrManPtr { get; private set; } = IntPtr.Zero;
        public static IntPtr EldenRing_CrashFixPtr { get; private set; } = IntPtr.Zero;

        // --- Attachment ---
        public static bool AttachProc(string processName)
        {
            try
            {
                CloseHandle(); // Close any existing handle first

                Process[] processes = Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    AttachedProcess = processes[0];
                    ProcessHandle = Kernel32.OpenProcess(Kernel32.ProcessAccessFlags.All, false, AttachedProcess.Id);

                    if (ProcessHandle != IntPtr.Zero)
                    {
                        BaseAddress = AttachedProcess.MainModule.BaseAddress;
                        NpcReloaderLogic.LogAction?.Invoke($"Process Found: {AttachedProcess.ProcessName} (ID: {AttachedProcess.Id})");
                        NpcReloaderLogic.LogAction?.Invoke($"Base Address: 0x{BaseAddress.ToInt64():X}");
                        return true;
                    }
                    else
                    {
                        int error = Marshal.GetLastWin32Error();
                        NpcReloaderLogic.LogAction?.Invoke($"ERROR: Could not open process handle for {processName}. Win32Error: {error}. Try running as Administrator.");
                        AttachedProcess = null;
                        return false;
                    }
                }
                else
                {
                    NpcReloaderLogic.LogAction?.Invoke($"Process '{processName}' not found.");
                    AttachedProcess = null;
                    ProcessHandle = IntPtr.Zero;
                    BaseAddress = IntPtr.Zero;
                    return false;
                }
            }
            catch (Exception ex)
            {
                NpcReloaderLogic.LogAction?.Invoke($"ERROR Attaching to process '{processName}': {ex.Message}");
                AttachedProcess = null;
                ProcessHandle = IntPtr.Zero;
                BaseAddress = IntPtr.Zero;
                return false;
            }
        }

        public static void CloseHandle()
        {
            if (ProcessHandle != IntPtr.Zero)
            {
                Kernel32.CloseHandle(ProcessHandle);
            }
            // Always reset these regardless of handle state before closing
            ProcessHandle = IntPtr.Zero;
            BaseAddress = IntPtr.Zero;
            AttachedProcess = null;
            EldenRing_WorldChrManPtr = IntPtr.Zero;
            EldenRing_CrashFixPtr = IntPtr.Zero;
        }

        // --- Memory Allocation / Deallocation ---
        public static IntPtr AllocateMemory(int size)
        {
            if (ProcessHandle == IntPtr.Zero) return IntPtr.Zero;
            return Kernel32.VirtualAllocEx(ProcessHandle, IntPtr.Zero, size, Kernel32.AllocationType.Commit | Kernel32.AllocationType.Reserve, Kernel32.MemoryProtection.ExecuteReadWrite);
        }

        public static bool FreeMemory(IntPtr address)
        {
            if (ProcessHandle == IntPtr.Zero || address == IntPtr.Zero) return false;
            return Kernel32.VirtualFreeEx(ProcessHandle, address, 0, Kernel32.AllocationType.Release);
        }

        // --- Basic Read/Write Operations ---
        public static bool WriteBytes(IntPtr address, byte[] data)
        {
            if (ProcessHandle == IntPtr.Zero || data == null) return false;
            return Kernel32.WriteProcessMemory(ProcessHandle, address, data, data.Length, out _);
        }

        public static byte[] ReadBytes(IntPtr address, int count)
        {
            if (ProcessHandle == IntPtr.Zero || count <= 0) return null;
            byte[] buffer = new byte[count];
            if (Kernel32.ReadProcessMemory(ProcessHandle, address, buffer, buffer.Length, out _))
            {
                return buffer;
            }
            // Log error only if read fails
            // NpcReloaderLogic.LogAction?.Invoke($"ERROR: ReadProcessMemory failed at 0x{address.ToInt64():X} for {count} bytes. Win32Error: {Marshal.GetLastWin32Error()}");
            return null;
        }

        public static bool WriteBoolean(IntPtr address, bool value)
        {
            return WriteBytes(address, new byte[] { (byte)(value ? 1 : 0) });
        }

        public static bool WriteInt8(IntPtr address, byte value)
        {
            return WriteBytes(address, new byte[] { value });
        }

        public static bool WriteInt64(IntPtr address, long value)
        {
            return WriteBytes(address, BitConverter.GetBytes(value));
        }

        public static long ReadInt64(IntPtr address)
        {
            byte[] data = ReadBytes(address, 8);
            if (data != null && data.Length == 8)
            {
                return BitConverter.ToInt64(data, 0);
            }
            // NpcReloaderLogic.LogAction?.Invoke($"ERROR: Failed to read Int64 at 0x{address.ToInt64():X}");
            return 0; // Return 0 on failure
        }

        // --- Remote Execution ---
        public static IntPtr ExecuteRemoteFunction(IntPtr functionAddress, IntPtr parameter = default(IntPtr))
        {
            if (ProcessHandle == IntPtr.Zero || functionAddress == IntPtr.Zero) return IntPtr.Zero;

            IntPtr threadHandle = Kernel32.CreateRemoteThread(ProcessHandle, IntPtr.Zero, 0, functionAddress, parameter, 0, out _);
            if (threadHandle == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                NpcReloaderLogic.LogAction?.Invoke($"ERROR: CreateRemoteThread failed. Win32Error: {error}");
            }
            return threadHandle;
        }


        public static bool WaitForThread(IntPtr threadHandle, uint milliseconds = Kernel32.INFINITE)
        {
            if (threadHandle == IntPtr.Zero) return false;
            return Kernel32.WaitForSingleObject(threadHandle, milliseconds) == 0;
        }

        public static void CloseThreadHandle(IntPtr threadHandle)
        {
            if (threadHandle != IntPtr.Zero)
            {
                Kernel32.CloseHandle(threadHandle);
            }
        }

        public static bool ExecuteBufferFunction(byte[] asmBuffer, byte[] argBytes = null, int argLocationInAsmArray = -1)
        {
            if (ProcessHandle == IntPtr.Zero || asmBuffer == null) return false;

            IntPtr allocatedAsm = IntPtr.Zero;
            IntPtr allocatedArg = IntPtr.Zero;
            bool success = false;

            try
            {
                allocatedAsm = AllocateMemory(asmBuffer.Length);
                if (allocatedAsm == IntPtr.Zero)
                {
                    NpcReloaderLogic.LogAction?.Invoke("ERROR: Failed to allocate memory for ASM buffer.");
                    return false;
                }

                if (argBytes != null && argBytes.Length > 0)
                {
                    allocatedArg = AllocateMemory(argBytes.Length);
                    if (allocatedArg == IntPtr.Zero)
                    {
                        NpcReloaderLogic.LogAction?.Invoke("ERROR: Failed to allocate memory for argument bytes.");
                        FreeMemory(allocatedAsm); // Clean up ASM allocation
                        return false;
                    }

                    if (!WriteBytes(allocatedArg, argBytes))
                    {
                        NpcReloaderLogic.LogAction?.Invoke("ERROR: Failed to write argument bytes to allocated memory.");
                        FreeMemory(allocatedArg);
                        FreeMemory(allocatedAsm);
                        return false;
                    }

                    if (argLocationInAsmArray >= 0 && argLocationInAsmArray <= asmBuffer.Length - 8)
                    {
                        byte[] argAddressBytes = BitConverter.GetBytes(allocatedArg.ToInt64());
                        Array.Copy(argAddressBytes, 0, asmBuffer, argLocationInAsmArray, argAddressBytes.Length);
                        // NpcReloaderLogic.LogAction?.Invoke($"Patched ASM at offset {argLocationInAsmArray} with ArgAddr 0x{allocatedArg.ToInt64():X}");
                    }
                    else if (argLocationInAsmArray != -1)
                    {
                        NpcReloaderLogic.LogAction?.Invoke($"WARNING: Invalid argLocationInAsmArray ({argLocationInAsmArray}), cannot patch argument address.");
                    }
                }

                if (!WriteBytes(allocatedAsm, asmBuffer))
                {
                    NpcReloaderLogic.LogAction?.Invoke("ERROR: Failed to write ASM buffer to allocated memory.");
                    if (allocatedArg != IntPtr.Zero) FreeMemory(allocatedArg); // Clean up arg if allocated
                    FreeMemory(allocatedAsm);
                    return false;
                }
                // NpcReloaderLogic.LogAction?.Invoke($"Wrote ASM ({(argBytes != null ? "Patched" : "Unpatched")}) to 0x{allocatedAsm.ToInt64():X}");

                // NpcReloaderLogic.LogAction?.Invoke("Executing remote thread for buffer...");
                IntPtr threadHandle = ExecuteRemoteFunction(allocatedAsm);

                if (threadHandle != IntPtr.Zero)
                {
                    WaitForThread(threadHandle, 5000);
                    CloseThreadHandle(threadHandle);
                    // NpcReloaderLogic.LogAction?.Invoke("Remote thread completed.");
                    success = true;
                }
                else
                {
                    // Error logged within ExecuteRemoteFunction
                    success = false;
                }
            }
            catch (Exception ex)
            {
                NpcReloaderLogic.LogAction?.Invoke($"ERROR in ExecuteBufferFunction: {ex.Message}\n{ex.StackTrace}");
                success = false;
            }
            finally
            {
                if (allocatedAsm != IntPtr.Zero) FreeMemory(allocatedAsm);
                if (allocatedArg != IntPtr.Zero) FreeMemory(allocatedArg);
                // NpcReloaderLogic.LogAction?.Invoke($"Cleaned up allocated memory (ASM: {allocatedAsm != IntPtr.Zero}, Arg: {allocatedArg != IntPtr.Zero}). Success: {success}");
            }

            return success;
        }

        // --- Elden Ring AOB Scanning ---
        //public static bool UpdateEldenRingAobs() // Defined ONLY ONCE
        //{
        //    if (ProcessHandle == IntPtr.Zero || BaseAddress == IntPtr.Zero)
        //    {
        //        NpcReloaderLogic.LogAction?.Invoke("ERROR: Cannot scan AOBs, not attached to process.");
        //        return false;
        //    }

        //    NpcReloaderLogic.LogAction?.Invoke("Starting Elden Ring AOB scan using configuration...");

        //    EldenRing_WorldChrManPtr = IntPtr.Zero; // Reset before scan
        //    EldenRing_CrashFixPtr = IntPtr.Zero;  // Reset before scan

        //    // --- WorldChrMan Scan ---
        //    IntPtr chrManPtrLocation = FindPattern(EldenRingConfig.WorldChrManAob); // Find the instruction location first
        //    if (chrManPtrLocation != IntPtr.Zero)
        //    {
        //        byte[] offsetBytes = ReadBytes((IntPtr)(chrManPtrLocation.ToInt64() + EldenRingConfig.WorldChrManJumpStartOffset), 4);
        //        if (offsetBytes != null && offsetBytes.Length == 4)
        //        {
        //            int relativeOffset = BitConverter.ToInt32(offsetBytes, 0);
        //            // Calculate the address *OF THE POINTER*
        //            IntPtr pointerAddress = (IntPtr)(chrManPtrLocation.ToInt64() + EldenRingConfig.WorldChrManJumpEndOffset + relativeOffset);
        //            NpcReloaderLogic.LogAction?.Invoke($"Found WorldChrMan Pointer Address: 0x{pointerAddress.ToInt64():X}");
        //            // *** ADD THIS DEREFERENCE STEP ***
        //            // Read the actual pointer value from that address
        //            long actualPtrValue = ReadInt64(pointerAddress); // Read the 64-bit pointer
        //            if (actualPtrValue != 0)
        //            {
        //                EldenRing_WorldChrManPtr = (IntPtr)actualPtrValue; // Assign the dereferenced pointer
        //                NpcReloaderLogic.LogAction?.Invoke($"Dereferenced WorldChrMan Pointer: 0x{EldenRing_WorldChrManPtr.ToInt64():X}");
        //            }
        //        else
        //        {
        //            NpcReloaderLogic.LogAction?.Invoke($"ERROR: WorldChrMan pointer at 0x{pointerAddress.ToInt64():X} is NULL.");
        //            // EldenRing_WorldChrManPtr remains Zero
        //        }
        //    }
        //    else
        //    {
        //        NpcReloaderLogic.LogAction?.Invoke($"ERROR: Failed to read WorldChrMan offset bytes.");
        //        // EldenRing_WorldChrManPtr remains Zero
        //    }
        //    }
        //    else
        //    {
        //        NpcReloaderLogic.LogAction?.Invoke("ERROR: WorldChrMan pattern not found!");
        //        // EldenRing_WorldChrManPtr remains Zero
        //    }

        //    // --- Crash Fix Scan ---
        //    IntPtr crashFixAobResult = FindPattern(EldenRingConfig.CrashFixAob);
        //    if (crashFixAobResult != IntPtr.Zero)
        //    {
        //        int patternLength = EldenRingConfig.GetAobLength(EldenRingConfig.CrashFixAob);
        //        if (patternLength > 0 && EldenRingConfig.CrashFixOffsetDistanceFromEnd < patternLength)
        //        {
        //            // Calculate final address - Use explicit IntPtr cast
        //            EldenRing_CrashFixPtr = (IntPtr)(crashFixAobResult.ToInt64() + patternLength - EldenRingConfig.CrashFixOffsetDistanceFromEnd);
        //            NpcReloaderLogic.LogAction?.Invoke($"Found CrashFix Location: 0x{EldenRing_CrashFixPtr.ToInt64():X}");
        //        }
        //        else
        //        {
        //            NpcReloaderLogic.LogAction?.Invoke($"ERROR: Invalid CrashFix pattern length or offset. Cannot calculate address.");
        //        }
        //    }
        //    else
        //    {
        //        NpcReloaderLogic.LogAction?.Invoke("ERROR: CrashFix pattern not found!");
        //    }

        //    // Determine overall success
        //    bool scanSuccess = EldenRing_WorldChrManPtr != IntPtr.Zero && EldenRing_CrashFixPtr != IntPtr.Zero;
        //    if (!scanSuccess)
        //    {
        //        NpcReloaderLogic.LogAction?.Invoke("AOB Scan failed to find all required pointers.");
        //    }
        //    else
        //    {
        //        NpcReloaderLogic.LogAction?.Invoke("AOB Scan successful.");
        //    }

        //    return scanSuccess;
        //}


        //public static bool UpdateEldenRingAobs() // Defined ONLY ONCE
        //{
        //    if (ProcessHandle == IntPtr.Zero || BaseAddress == IntPtr.Zero)
        //    {
        //        NpcReloaderLogic.LogAction?.Invoke("ERROR: Cannot scan AOBs, not attached to process.");
        //        return false;
        //    }

        //    NpcReloaderLogic.LogAction?.Invoke("Starting Elden Ring AOB scan using configuration...");

        //    EldenRing_WorldChrManPtr = IntPtr.Zero; // Reset before scan
        //    EldenRing_CrashFixPtr = IntPtr.Zero;  // Reset before scan

        //    // --- WorldChrMan Scan ---
        //    IntPtr chrManPtrLocation = FindPattern(EldenRingConfig.WorldChrManAob); // Find the instruction location first
        //    if (chrManPtrLocation != IntPtr.Zero)
        //    {
        //        byte[] offsetBytes = ReadBytes((IntPtr)(chrManPtrLocation.ToInt64() + EldenRingConfig.WorldChrManJumpStartOffset), 4);
        //        if (offsetBytes != null && offsetBytes.Length == 4)
        //        {
        //            int relativeOffset = BitConverter.ToInt32(offsetBytes, 0);
        //            // Calculate the address *OF THE POINTER* (e.g., 0x7FF6D1BC5F88)
        //            IntPtr pointerAddress = (IntPtr)(chrManPtrLocation.ToInt64() + EldenRingConfig.WorldChrManJumpEndOffset + relativeOffset);
        //            NpcReloaderLogic.LogAction?.Invoke($"Found WorldChrMan Function Pointer Location: 0x{pointerAddress.ToInt64():X}"); // Updated log message

        //            // Read the actual function address value from that location (e.g., 0x7FF3B56566D0 - the code address)
        //            long functionAddressValue = ReadInt64(pointerAddress); // Renamed for clarity
        //            if (functionAddressValue != 0)
        //            {
        //                IntPtr chrManFunctionAddress = (IntPtr)functionAddressValue; // This is the address of the function itself
        //                NpcReloaderLogic.LogAction?.Invoke($"Dereferenced to WorldChrMan Function Address: 0x{chrManFunctionAddress.ToInt64():X}"); // Updated log message

        //                // ****** ADD THE EXECUTION STEP HERE ******
        //                NpcReloaderLogic.LogAction?.Invoke("Executing WorldChrMan function to get base DATA pointer...");
        //                EldenRing_WorldChrManPtr = (IntPtr)functionAddressValue; // This IS your WorldChrMan DATA pointer

        //                if (EldenRing_WorldChrManPtr != IntPtr.Zero)
        //                {
        //                    NpcReloaderLogic.LogAction?.Invoke($"Successfully obtained WorldChrMan DATA Pointer: 0x{EldenRing_WorldChrManPtr.ToInt64():X}"); // Updated log message
        //                }
        //                else
        //                {
        //                    NpcReloaderLogic.LogAction?.Invoke($"ERROR: Failed to obtain WorldChrMan DATA pointer by executing function.");
        //                }
        //            }
        //            else
        //            {
        //                NpcReloaderLogic.LogAction?.Invoke($"ERROR: WorldChrMan function pointer at 0x{pointerAddress.ToInt64():X} is NULL.");
        //                // EldenRing_WorldChrManPtr remains Zero
        //            }
        //        }
        //        else
        //        {
        //            NpcReloaderLogic.LogAction?.Invoke($"ERROR: Failed to read WorldChrMan offset bytes.");
        //            // EldenRing_WorldChrManPtr remains Zero
        //        }
        //    }
        //    else
        //    {
        //        NpcReloaderLogic.LogAction?.Invoke("ERROR: WorldChrMan pattern not found!");
        //        // EldenRing_WorldChrManPtr remains Zero
        //    }

        //    // --- Crash Fix Scan ---
        //    IntPtr crashFixAobResult = FindPattern(EldenRingConfig.CrashFixAob);
        //    if (crashFixAobResult != IntPtr.Zero)
        //    {
        //        int patternLength = EldenRingConfig.GetAobLength(EldenRingConfig.CrashFixAob);
        //        if (patternLength > 0 && EldenRingConfig.CrashFixOffsetDistanceFromEnd < patternLength)
        //        {
        //            // Calculate final address - Use explicit IntPtr cast
        //            EldenRing_CrashFixPtr = (IntPtr)(crashFixAobResult.ToInt64() + patternLength - EldenRingConfig.CrashFixOffsetDistanceFromEnd);
        //            NpcReloaderLogic.LogAction?.Invoke($"Found CrashFix Location: 0x{EldenRing_CrashFixPtr.ToInt64():X}");
        //        }
        //        else
        //        {
        //            NpcReloaderLogic.LogAction?.Invoke($"ERROR: Invalid CrashFix pattern length or offset. Cannot calculate address.");
        //        }
        //    }
        //    else
        //    {
        //        NpcReloaderLogic.LogAction?.Invoke("ERROR: CrashFix pattern not found!");
        //    }

        //    // Determine overall success
        //    bool scanSuccess = EldenRing_WorldChrManPtr != IntPtr.Zero && EldenRing_CrashFixPtr != IntPtr.Zero; // Ensure this still checks the data pointer
        //    if (!scanSuccess)
        //    {
        //        NpcReloaderLogic.LogAction?.Invoke("AOB Scan failed to find all required pointers.");
        //    }
        //    else
        //    {
        //        NpcReloaderLogic.LogAction?.Invoke("AOB Scan successful.");
        //    }

        //    return scanSuccess;
        //}

        // Inside NpcReloaderGUI.Core.Memory.cs
        public static bool UpdateEldenRingAobs()
        {
            if (ProcessHandle == IntPtr.Zero || BaseAddress == IntPtr.Zero)
            {
                NpcReloaderLogic.LogAction?.Invoke("ERROR: Cannot scan AOBs, not attached to process.");
                return false;
            }

            NpcReloaderLogic.LogAction?.Invoke("Starting Elden Ring AOB scan using configuration...");

            EldenRing_WorldChrManPtr = IntPtr.Zero; // Reset before scan
            EldenRing_CrashFixPtr = IntPtr.Zero;  // Reset before scan

            // --- WorldChrMan Scan ---
            // This finds the address of the static pointer (e.g., eldenring.exe + 0x3D65F88)
            IntPtr staticWorldChrManPtrLocation_InstructionStart = FindPattern(EldenRingConfig.WorldChrManAob);
            if (staticWorldChrManPtrLocation_InstructionStart != IntPtr.Zero)
            {
                // Read the 4-byte relative offset from the instruction
                byte[] offsetBytes = ReadBytes((IntPtr)(staticWorldChrManPtrLocation_InstructionStart.ToInt64() + EldenRingConfig.WorldChrManJumpStartOffset), 4);
                if (offsetBytes != null && offsetBytes.Length == 4)
                {
                    int relativeOffset = BitConverter.ToInt32(offsetBytes, 0);
                    // Calculate the absolute address where the WCM_DATA_PTR is stored
                    // This is STATIC_POINTER_LOCATION (e.g., eldenring.exe + 0x3D65F88)
                    IntPtr addressWhereWcmDataPtrIsStored = (IntPtr)(staticWorldChrManPtrLocation_InstructionStart.ToInt64() + EldenRingConfig.WorldChrManJumpEndOffset + relativeOffset);
                    NpcReloaderLogic.LogAction?.Invoke($"Found Address Where WCM Data Pointer is Stored: 0x{addressWhereWcmDataPtrIsStored.ToInt64():X}");

                    // Read the 8-byte WCM_DATA_PTR from that location
                    long wcmDataPtrValue = ReadInt64(addressWhereWcmDataPtrIsStored);
                    if (wcmDataPtrValue != 0)
                    {
                        EldenRing_WorldChrManPtr = (IntPtr)wcmDataPtrValue; // This IS THE DIRECT DATA POINTER
                        NpcReloaderLogic.LogAction?.Invoke($"Successfully Read WorldChrMan DATA Pointer: 0x{EldenRing_WorldChrManPtr.ToInt64():X}");
                    }
                    else
                    {
                        NpcReloaderLogic.LogAction?.Invoke($"ERROR: WorldChrMan DATA Pointer at 0x{addressWhereWcmDataPtrIsStored.ToInt64():X} is NULL.");
                    }
                }
                else
                {
                    NpcReloaderLogic.LogAction?.Invoke($"ERROR: Failed to read WorldChrMan relative offset bytes.");
                }
            }
            else
            {
                NpcReloaderLogic.LogAction?.Invoke("ERROR: WorldChrMan pattern (for static pointer location) not found!");
            }

            // --- Crash Fix Scan --- (This seems to be working based on your log)
            IntPtr crashFixAobResult = FindPattern(EldenRingConfig.CrashFixAob);
            if (crashFixAobResult != IntPtr.Zero)
            {
                int patternLength = EldenRingConfig.GetAobLength(EldenRingConfig.CrashFixAob);
                if (patternLength > 0 && EldenRingConfig.CrashFixOffsetDistanceFromEnd < patternLength)
                {
                    EldenRing_CrashFixPtr = (IntPtr)(crashFixAobResult.ToInt64() + patternLength - EldenRingConfig.CrashFixOffsetDistanceFromEnd);
                    NpcReloaderLogic.LogAction?.Invoke($"Found CrashFix Location: 0x{EldenRing_CrashFixPtr.ToInt64():X}");
                }
                else
                {
                    NpcReloaderLogic.LogAction?.Invoke($"ERROR: Invalid CrashFix pattern length or offset.");
                }
            }
            else
            {
                NpcReloaderLogic.LogAction?.Invoke("ERROR: CrashFix pattern not found!");
            }

            // Determine overall success
            bool scanSuccess = EldenRing_WorldChrManPtr != IntPtr.Zero && EldenRing_CrashFixPtr != IntPtr.Zero;
            if (!scanSuccess)
            {
                NpcReloaderLogic.LogAction?.Invoke("AOB Scan failed to find all required pointers.");
            }
            else
            {
                NpcReloaderLogic.LogAction?.Invoke("AOB Scan successful.");
            }
            return scanSuccess;
        }


        // --- AOB Scanning Helper Methods (MUST be INSIDE the Memory class) ---
        private static IntPtr FindPattern(string pattern)
        {
            if (ProcessHandle == IntPtr.Zero || AttachedProcess == null || string.IsNullOrWhiteSpace(pattern)) return IntPtr.Zero;

            byte[] patternBytes = ParsePattern(pattern, out bool[] mask);
            if (patternBytes == null || patternBytes.Length == 0) return IntPtr.Zero;

            // NpcReloaderLogic.LogAction?.Invoke($"Scanning for pattern: {pattern}");

            long currentAddress = BaseAddress.ToInt64();
            long maxAddressToScan = 0x7FFFFFFFFFFF;

            while (currentAddress < maxAddressToScan)
            {
                if (!Kernel32.VirtualQueryEx(ProcessHandle, (IntPtr)currentAddress, out Kernel32.MEMORY_BASIC_INFORMATION mbi, (uint)Marshal.SizeOf(typeof(Kernel32.MEMORY_BASIC_INFORMATION))))
                {
                    break;
                }

                long regionSize = mbi.RegionSize.ToInt64();
                IntPtr regionBaseAddress = mbi.BaseAddress;

                // Simplified readability check
                bool isReadable = mbi.State == Kernel32.MEM_COMMIT_STATE &&
                                  (mbi.Protect & Kernel32.PAGE_GUARD) == 0 &&
                                  (mbi.Protect != (uint)Kernel32.MemoryProtection.NoAccess); // Any protection except NoAccess should be potentially readable

                if (isReadable && regionSize > 0)
                {
                    byte[] regionData = ReadBytes(regionBaseAddress, (int)Math.Min(regionSize, Int32.MaxValue)); // Read up to 2GB

                    if (regionData != null)
                    {
                        for (int i = 0; (i = FindBytePattern(regionData, i, patternBytes, mask)) != -1; i += 1)
                        {
                            // Use explicit IntPtr cast for address calculation
                            IntPtr foundAddress = (IntPtr)(regionBaseAddress.ToInt64() + i);
                            // NpcReloaderLogic.LogAction?.Invoke($"Pattern match found at address: 0x{foundAddress.ToInt64():X}");
                            return foundAddress;
                        }
                    }
                }

                if (regionSize == 0) break;
                long nextAddress = regionBaseAddress.ToInt64() + regionSize;
                if (nextAddress <= currentAddress) break; // Prevent infinite loop on weird region sizes/overlaps
                currentAddress = nextAddress;
            }

            // NpcReloaderLogic.LogAction?.Invoke("Pattern not found in scanned regions.");
            return IntPtr.Zero;
        }

        private static byte[] ParsePattern(string pattern, out bool[] mask)
        {
            mask = null;
            if (string.IsNullOrWhiteSpace(pattern)) return null;

            List<byte> patternBytes = new List<byte>();
            List<bool> patternMask = new List<bool>();
            string[] parts = pattern.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string part in parts)
            {
                if (part == "?" || part == "??")
                {
                    patternBytes.Add(0);
                    patternMask.Add(false);
                }
                else
                {
                    try
                    {
                        patternBytes.Add(Convert.ToByte(part, 16));
                        patternMask.Add(true);
                    }
                    catch
                    {
                        NpcReloaderLogic.LogAction?.Invoke($"ERROR: Invalid byte '{part}' in pattern '{pattern}'");
                        return null;
                    }
                }
            }

            if (patternBytes.Count == 0) return null;

            mask = patternMask.ToArray();
            return patternBytes.ToArray();
        }

        private static int FindBytePattern(byte[] haystack, int startIndex, byte[] needle, bool[] mask)
        {
             if (haystack == null || needle == null || mask == null || needle.Length != mask.Length || needle.Length == 0 || startIndex < 0)
                return -1;

            for (int i = startIndex; i <= haystack.Length - needle.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (mask[j] && haystack[i + j] != needle[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    return i;
                }
            }
            return -1;
        }

    } // End of Memory class
} // End of namespace
// ----- End of Core/Memory.cs -----