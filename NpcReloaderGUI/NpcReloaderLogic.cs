// ----- Start of NpcReloaderLogic.cs -----
using System;
using System.Text;
using System.Windows.Forms;
using SoulsAssetPipeline;
using NpcReloaderGUI.Core; // Use Core namespace

namespace NpcReloaderGUI
{
    public static class NpcReloaderLogic // Defined ONLY ONCE
    {
        // --- Static Fields ---
        private static SoulsGames? _lastAttachedGame = null;
        private static bool _isAttached = false;
        public static Action<string> LogAction { get; set; } = message => System.Diagnostics.Debug.WriteLine(message); // Default to Debug Output

        // --- Private Helper Methods ---
        private static void Log(string message) => LogAction?.Invoke(message);

        private static void ShowInjectionFailed(SoulsGames gameType, string details = "")
        {
            string message = $"Process injection failed for {gameType}.\n\n" +
                             "Make sure the game is running and this application has permission " +
                             "to control processes (running as administrator might be required).";
            if (gameType == SoulsGames.ER)
            {
                message += "\n\nFor Elden Ring, make sure EasyAntiCheat is NOT enabled (e.g., use Seamless Co-op or Offline Launcher) as it prevents all process memory writing.";
            }
            if (!string.IsNullOrEmpty(details))
            {
                message += $"\n\nDetails: {details}";
            }

            Log($"ERROR: Injection Failed for {gameType}. {details}");
            MessageBox.Show(message, "Injection Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static string GetProcessName(SoulsGames game) => game switch
        {
            SoulsGames.DS1R => "DarkSoulsRemastered",
            SoulsGames.DS3 => "DarkSoulsIII",
            SoulsGames.SDT => "sekiro",
            SoulsGames.ER => Core.Memory.EldenRingProcessName_Default,
            _ => null
        };

        // --- Public Methods ---
        public static bool AttachToGame(SoulsGames gameType)
        {
            if (_isAttached && _lastAttachedGame == gameType && Core.Memory.ProcessHandle != IntPtr.Zero && Core.Memory.AttachedProcess != null && !Core.Memory.AttachedProcess.HasExited)
            {
                Log($"Already attached to {gameType}.");
                return true;
            }

            if (_isAttached) // Detach if switching or process died
            {
                Log($"Detaching from previous process ({_lastAttachedGame})...");
                Core.Memory.CloseHandle();
                _isAttached = false;
                _lastAttachedGame = null;
            }

            Log($"Attempting to attach to {gameType}...");
            string processName = GetProcessName(gameType);
            if (string.IsNullOrEmpty(processName))
            {
                Log($"ERROR: Unknown or unsupported game type {gameType} for attaching.");
                MessageBox.Show($"Attaching not supported for {gameType}.", "Unsupported Game", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            try
            {
                bool attached = Core.Memory.AttachProc(processName);

                if (!attached && gameType == SoulsGames.ER)
                {
                    Log($"Primary attach failed for Elden Ring, trying '{Core.Memory.EldenRingProcessName_NoEAC}'...");
                    attached = Core.Memory.AttachProc(Core.Memory.EldenRingProcessName_NoEAC);
                }

                if (attached && Core.Memory.ProcessHandle != IntPtr.Zero)
                {
                    Log($"Successfully attached to {Core.Memory.AttachedProcess?.ProcessName} (PID: {Core.Memory.AttachedProcess?.Id}).");
                    _isAttached = true;
                    _lastAttachedGame = gameType;

                    if (gameType == SoulsGames.ER)
                    {
                        Core.Memory.UpdateEldenRingAobs(); // UpdateEldenRingAobs handles its own logging
                    }
                    return true;
                }
                else
                {
                    Log($"Failed to attach to process for {gameType}.");
                    MessageBox.Show($"Could not find or access the game process for {gameType}.\n\nMake sure the game is running and this application has the necessary permissions (try running as administrator).", "Attach Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR during attachment: {ex.Message}\n{ex.StackTrace}");
                ShowInjectionFailed(gameType, $"Attach exception: {ex.Message}");
                return false;
            }
        }

        public static void DetachFromGame()
        {
            if (_isAttached)
            {
                Log($"Detaching from {_lastAttachedGame}...");
                Core.Memory.CloseHandle();
                _isAttached = false;
                _lastAttachedGame = null;
                Log("Detached.");
            }
        }

        public static bool RequestReloadChr(SoulsGames gameType, string chrName)
        {
            if (!_isAttached || _lastAttachedGame != gameType || Core.Memory.ProcessHandle == IntPtr.Zero || Core.Memory.AttachedProcess == null || Core.Memory.AttachedProcess.HasExited)
            {
                Log($"Not attached or process unavailable for {gameType}. Attempting to attach...");
                if (!AttachToGame(gameType))
                {
                    Log("Attach failed. Cannot reload NPC.");
                    return false;
                }
                System.Threading.Thread.Sleep(250);
            }

            Log($"Requesting reload for Chr [{chrName}] in {gameType}...");

            if (string.IsNullOrWhiteSpace(chrName))
            {
                Log("ERROR: Character name cannot be empty.");
                MessageBox.Show("Please enter a Character ID (e.g., c0000).", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (chrName.Length > 0 && char.IsDigit(chrName[0]))
            {
                chrName = "c" + chrName;
                Log($"Corrected Chr ID to: {chrName}");
            }

            byte[] chrNameBytes;
            try
            {
                Encoding encoding = Encoding.Unicode; // Assume Unicode generally works
                chrNameBytes = encoding.GetBytes(chrName + "\0"); // Null terminate
            }
            catch (Exception ex)
            {
                Log($"ERROR converting character name '{chrName}' to bytes: {ex.Message}");
                MessageBox.Show($"Error encoding character name '{chrName}': {ex.Message}", "Encoding Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            // Re-check process state
            if (Core.Memory.AttachedProcess == null || Core.Memory.AttachedProcess.HasExited)
            {
                Log("ERROR: Target process has exited or became unavailable before injection.");
                ShowInjectionFailed(gameType, "Target process closed unexpectedly.");
                _isAttached = false;
                _lastAttachedGame = null;
                return false;
            }

            bool success = false;
            try
            {
                switch (gameType)
                {
                    case SoulsGames.SDT:
                        success = ReloadSekiro(chrNameBytes);
                        break;
                    case SoulsGames.DS3:
                        success = ReloadDs3(chrNameBytes);
                        break;
                    case SoulsGames.ER:
                        success = ReloadEldenRing(chrNameBytes); // Defined only once below
                        break;
                    case SoulsGames.DS1R:
                        success = ReloadDs1r(chrNameBytes);
                        break;
                    default:
                        Log($"ERROR: Reload logic not implemented for game type {gameType}.");
                        MessageBox.Show($"Reloading not supported for {gameType}.", "Unsupported Game", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR during {gameType} reload execution: {ex.Message}\n{ex.StackTrace}");
                ShowInjectionFailed(gameType, $"Execution error: {ex.Message}");
                success = false;
            }

            Log(success ? $"Reload function executed for [{chrName}]." : $"Reload execution failed for [{chrName}].");

            return success;
        }

        // --- Game Specific Reload Logic ---

        private static bool ReloadSekiro(byte[] chrNameBytes)
        {
            const long reloadFlagOffset = 0x3D7A34F;
            const long ptrStructOffset = 0x3D7A1E0;
            const long reloadFuncOffset = 0xA4AC60;

            // Use explicit IntPtr casts for address calculation
            IntPtr flagAddr = (IntPtr)(Core.Memory.BaseAddress.ToInt64() + reloadFlagOffset);
            IntPtr ptrStructAddr = (IntPtr)(Core.Memory.BaseAddress.ToInt64() + ptrStructOffset);
            IntPtr funcAddr = (IntPtr)(Core.Memory.BaseAddress.ToInt64() + reloadFuncOffset);

            if (!Core.Memory.WriteBoolean(flagAddr, true))
            {
                Log($"ERROR: Failed to write Sekiro reload flag at 0x{flagAddr.ToInt64():X}");
                ShowInjectionFailed(SoulsGames.SDT, "Failed to write reload flag.");
                return false;
            }

            var buffer = new byte[] {
                0x48, 0xB9, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // mov rcx, [PointerValue]
                0x48, 0xBA, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // mov rdx, [ArgAddress]
                0x48, 0x83, 0xEC, 0x28,                                     // sub rsp, 28
                0x49, 0xBE, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // mov r14, [FunctionAddress]
                0x41, 0xFF, 0xD6,                                           // call r14
                0x48, 0x83, 0xC4, 0x28,                                     // add rsp, 28
                0xC3,                                                       // ret
            };

            long ptrThingVal = Core.Memory.ReadInt64(ptrStructAddr);
            if (ptrThingVal == 0)
            {
                Log($"ERROR: Sekiro pointer at 0x{ptrStructAddr.ToInt64():X} is NULL.");
                ShowInjectionFailed(SoulsGames.SDT, "Required pointer value is NULL.");
                return false;
            }
            Array.Copy(BitConverter.GetBytes(ptrThingVal), 0, buffer, 0x2, 8);
            Array.Copy(BitConverter.GetBytes(funcAddr.ToInt64()), 0, buffer, 0x1A, 8);

            return Core.Memory.ExecuteBufferFunction(buffer, chrNameBytes, argLocationInAsmArray: 0xC);
        }

        private static bool ReloadDs3(byte[] chrNameBytes)
        {
            const long reloadFlagOffset = 0x4768F7F;
            const long ptrStructOffset = 0x4768E78;
            const long reloadFuncOffset = 0x8D1E10;

            // Use explicit IntPtr casts for address calculation
            IntPtr flagAddr = (IntPtr)(Core.Memory.BaseAddress.ToInt64() + reloadFlagOffset);
            IntPtr ptrStructAddr = (IntPtr)(Core.Memory.BaseAddress.ToInt64() + ptrStructOffset); // Address *of the pointer*
            IntPtr funcAddr = (IntPtr)(Core.Memory.BaseAddress.ToInt64() + reloadFuncOffset);

            if (!Core.Memory.WriteBoolean(flagAddr, true))
            {
                Log($"ERROR: Failed to write DS3 reload flag at 0x{flagAddr.ToInt64():X}");
                ShowInjectionFailed(SoulsGames.DS3, "Failed to write reload flag.");
                return false;
            }

            var buffer = new byte[] {
                0x48, 0xBA, 0, 0, 0, 0, 0, 0, 0, 0,                         // mov rdx, [ArgAddress]
                0x48, 0xA1, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // mov rax, [Absolute Address Of Pointer]
                0x48, 0x8B, 0xC8,                                           // mov rcx, rax
                0x49, 0xBE, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // mov r14, [FunctionAddress]
                0x48, 0x83, 0xEC, 0x28,                                     // sub rsp, 28
                0x41, 0xFF, 0xD6,                                           // call r14
                0x48, 0x83, 0xC4, 0x28,                                     // add rsp, 28
                0xC3                                                        // ret
            };

            Array.Copy(BitConverter.GetBytes(ptrStructAddr.ToInt64()), 0, buffer, 0xC, 8); // Address OF the pointer goes into MOV RAX instruction
            Array.Copy(BitConverter.GetBytes(funcAddr.ToInt64()), 0, buffer, 0x1B, 8);

            return Core.Memory.ExecuteBufferFunction(buffer, chrNameBytes, argLocationInAsmArray: 0x2);
        }

        //private static bool ReloadEldenRing(byte[] chrNameBytes) // Defined ONLY ONCE
        //{
        //    if (Core.Memory.EldenRing_WorldChrManPtr == IntPtr.Zero || Core.Memory.EldenRing_CrashFixPtr == IntPtr.Zero)
        //    {
        //        Log("ERROR: Cannot reload Elden Ring. Required memory addresses not found (AOB scan likely failed). Retrying scan...");
        //        // Attempt AOB scan again just in case
        //        if (!Core.Memory.UpdateEldenRingAobs() || Core.Memory.EldenRing_WorldChrManPtr == IntPtr.Zero || Core.Memory.EldenRing_CrashFixPtr == IntPtr.Zero)
        //        {
        //            ShowInjectionFailed(SoulsGames.ER, "AOB scan failed or addresses are null.");
        //            return false;
        //        }
        //        Log("AOB scan re-run successful, proceeding with reload.");
        //    }

        //    Log($"Using Configured Offsets: Trigger1=0x{EldenRingConfig.WorldChrManStructOffset_Trigger1:X}, Trigger2=0x{EldenRingConfig.WorldChrManStructOffset_Trigger2:X}, ListHeadPtr=0x{EldenRingConfig.WorldChrManStructOffset_ListHeadPtr:X}"); // Updated log message
        //    IntPtr chrReloadAsm = IntPtr.Zero;
        //    IntPtr chrReloadData = IntPtr.Zero;
        //    bool success = false;

        //    try
        //    {
        //        chrReloadAsm = Core.Memory.AllocateMemory(256);
        //        chrReloadData = Core.Memory.AllocateMemory(256 + chrNameBytes.Length);

        //        if (chrReloadAsm == IntPtr.Zero || chrReloadData == IntPtr.Zero)
        //        {
        //            Log("ERROR: Failed to allocate memory in Elden Ring process.");
        //            ShowInjectionFailed(SoulsGames.ER, "VirtualAllocEx failed.");
        //            if (chrReloadAsm != IntPtr.Zero) Core.Memory.FreeMemory(chrReloadAsm); // Cleanup partial alloc
        //            if (chrReloadData != IntPtr.Zero) Core.Memory.FreeMemory(chrReloadData);
        //            return false;
        //        }
        //        // Log($"Allocated Memory: ASM @ 0x{chrReloadAsm.ToInt64():X}, Data @ 0x{chrReloadData.ToInt64():X}");

        //        // Use explicit IntPtr cast for address calculation
        //        IntPtr chrNameStringAddr = (IntPtr)(chrReloadData.ToInt64() + 0x100);
        //        if (!Core.Memory.WriteInt64((IntPtr)(chrReloadData.ToInt64() + 0x58), chrNameStringAddr.ToInt64()) ||
        //            !Core.Memory.WriteInt8((IntPtr)(chrReloadData.ToInt64() + 0x70), 0x1F) || // Keep 0x1F for now
        //            !Core.Memory.WriteBytes(chrNameStringAddr, chrNameBytes))
        //        {
        //            Log("ERROR: Failed to write data structure or string to allocated memory.");
        //            ShowInjectionFailed(SoulsGames.ER, "WriteProcessMemory failed for data setup.");
        //            Core.Memory.FreeMemory(chrReloadAsm); Core.Memory.FreeMemory(chrReloadData); return false;
        //        }
        //        // Log($"Wrote data structure and string to 0x{chrReloadData.ToInt64():X}");

        //        byte[] crashPatchBytes = EldenRingConfig.CrashFixWriteBytes;
        //        if (crashPatchBytes == null || crashPatchBytes.Length == 0)
        //        {
        //            Log("ERROR: CrashFixWriteBytes are invalid or missing in configuration.");
        //            ShowInjectionFailed(SoulsGames.ER, "CrashFixWriteBytes configuration missing or invalid.");
        //            Core.Memory.FreeMemory(chrReloadAsm); Core.Memory.FreeMemory(chrReloadData); return false;
        //        }
        //        if (!Core.Memory.WriteBytes(Core.Memory.EldenRing_CrashFixPtr, crashPatchBytes)) // Use direct pointer from AOB scan
        //        {
        //            Log($"ERROR: Failed to write crash fix patch bytes at 0x{Core.Memory.EldenRing_CrashFixPtr.ToInt64():X}.");
        //            ShowInjectionFailed(SoulsGames.ER, "WriteProcessMemory failed for crash patch.");
        //            Core.Memory.FreeMemory(chrReloadAsm); Core.Memory.FreeMemory(chrReloadData); return false;
        //        }
        //        // Log($"Applied crash fix patch ({crashPatchBytes.Length} bytes) at 0x{Core.Memory.EldenRing_CrashFixPtr.ToInt64():X}");

        //        var buffer = new byte[] {
        //           0x48, 0xBB, 0, 0, 0, 0, 0, 0, 0, 0, // mov rbx, [chrReloadData address]
        //            0x48, 0xB9, 0, 0, 0, 0, 0, 0, 0, 0, // mov rcx, [WorldChrManPtr address] (This is now the *actual* structure address)

        //            // *** USE ListHeadPtr Offset for list manipulation ***
        //            // mov rdx,[rcx + ListHeadPtrOffset] (Get current list head/next pointer)
        //            0x48, 0x8B, 0x91, 0x00, 0x00, 0x00, 0x00,
        //            0x48, 0x89, 0x1A,                   // mov [rdx], rbx        (head->next->prev = new_node) OR (head->prev = new_node) depending on list structure
        //            0x48, 0x89, 0x13,                   // mov [rbx], rdx        (new_node->next = head->next) OR (new_node->next = head)
        //            // mov rdx,[rcx + ListHeadPtrOffset] (Get pointer again)
        //            0x48, 0x8B, 0x91, 0x00, 0x00, 0x00, 0x00,
        //            0x48, 0x89, 0x5A, 0x08,             // mov [rdx+08], rbx     (head->next->next = new_node) OR (head->next = new_node)
        //            0x48, 0x89, 0x53, 0x08,             // mov [rbx+08], rdx     (new_node->prev = head->next) OR (new_node->prev = head)
        //            // ******************************************************

        //            // Trigger Reload using Trigger1 and Trigger2 Offsets (These should be correct)
        //            0xC7, 0x81, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, // mov dword ptr [rcx + Trigger1Offset], 1
        //            0xC7, 0x81, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x41, // mov dword ptr [rcx + Trigger2Offset], 10.0f
        //            0xC3 // ret
        //        };

        //        Array.Copy(BitConverter.GetBytes(chrReloadData.ToInt64()), 0, buffer, 0x2, 8);
        //        Array.Copy(BitConverter.GetBytes(Core.Memory.EldenRing_WorldChrManPtr.ToInt64()), 0, buffer, 0xC, 8);
        //        // *** Use CORRECTED offset for list pointers ***
        //        Array.Copy(BitConverter.GetBytes(EldenRingConfig.WorldChrManStructOffset_ListHeadPtr), 0, buffer, 0x17, 4);
        //        Array.Copy(BitConverter.GetBytes(EldenRingConfig.WorldChrManStructOffset_ListHeadPtr), 0, buffer, 0x24, 4); // Adjusted offset for second mov rdx
        //        // ********************************************
        //        Array.Copy(BitConverter.GetBytes(EldenRingConfig.WorldChrManStructOffset_Trigger1), 0, buffer, 0x31, 4); // Adjusted offset for first mov dword
        //        Array.Copy(BitConverter.GetBytes(EldenRingConfig.WorldChrManStructOffset_Trigger2), 0, buffer, 0x3F, 4); // Adjusted offset for second mov dword


        //        if (!Core.Memory.WriteBytes(chrReloadAsm, buffer))
        //        {
        //            Log("ERROR: Failed to write ASM buffer to allocated memory.");
        //            ShowInjectionFailed(SoulsGames.ER, "WriteProcessMemory failed for ASM buffer.");
        //            Core.Memory.FreeMemory(chrReloadAsm); Core.Memory.FreeMemory(chrReloadData); return false;
        //        }
        //        // Log($"Wrote ASM buffer ({buffer.Length} bytes) to 0x{chrReloadAsm.ToInt64():X}");

        //        // Log("Executing remote thread...");
        //        IntPtr threadHandle = Core.Memory.ExecuteRemoteFunction(chrReloadAsm);
        //        if (threadHandle != IntPtr.Zero)
        //        {
        //            Core.Memory.WaitForThread(threadHandle, 5000);
        //            Core.Memory.CloseThreadHandle(threadHandle);
        //            // Log("Remote thread executed.");
        //            success = true;
        //        }
        //        else
        //        {
        //            Log("ERROR: Failed to create remote thread.");
        //            ShowInjectionFailed(SoulsGames.ER, "CreateRemoteThread failed.");
        //            success = false;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Log($"ERROR during Elden Ring reload: {ex.Message}\n{ex.StackTrace}");
        //        ShowInjectionFailed(SoulsGames.ER, $"Internal error: {ex.Message}");
        //        success = false;
        //    }
        //    finally
        //    {
        //        if (chrReloadAsm != IntPtr.Zero) Core.Memory.FreeMemory(chrReloadAsm);
        //        if (chrReloadData != IntPtr.Zero) Core.Memory.FreeMemory(chrReloadData);
        //        // Log($"Cleaned up ER memory. Success: {success}");
        //    }
        //    return success;
        //}
        // In NpcReloaderLogic.cs
        // In NpcReloaderLogic.cs


        // In NpcReloaderLogic.cs
        // In NpcReloaderLogic.cs


        // In NpcReloaderLogic.cs
        //private static bool ReloadEldenRing(byte[] chrNameBytes)
        //{
        //    // --- Pointer Check ---
        //    if (Core.Memory.EldenRing_WorldChrManPtr == IntPtr.Zero || Core.Memory.EldenRing_CrashFixPtr == IntPtr.Zero) { /* AOB Retry Logic */ if (!Core.Memory.UpdateEldenRingAobs() || Core.Memory.EldenRing_WorldChrManPtr == IntPtr.Zero || Core.Memory.EldenRing_CrashFixPtr == IntPtr.Zero) { ShowInjectionFailed(SoulsGames.ER, "AOB scan failed."); return false; } Log("AOB scan re-run successful."); }

        //    // --- Log Config ---
        //    Log($"Base WorldChrMan Ptr: 0x{Core.Memory.EldenRing_WorldChrManPtr.ToInt64():X}");
        //    Log($"Using Trigger Flags/List Offsets: Trigger1=0x{EldenRingConfig.WorldChrManStructOffset_Trigger1:X}, Trigger2=0x{EldenRingConfig.WorldChrManStructOffset_Trigger2:X}, ListHeadPtr=0x{EldenRingConfig.WorldChrManStructOffset_ListHeadPtr:X}");
        //    Log("Attempting injection using ALTERNATIVE pointer calculation based on CE script (0x1E268).");


        //    IntPtr chrReloadAsm = IntPtr.Zero;
        //    IntPtr chrReloadData = IntPtr.Zero;
        //    bool success = false;
        //    const int allocSize = 512;

        //    try
        //    {
        //        // *** Calculate Alternative Pointer for RCX ***
        //        IntPtr alternativeRcxPtr = IntPtr.Zero;
        //        try
        //        {
        //            IntPtr intermediatePtr1_Addr = (IntPtr)(Core.Memory.EldenRing_WorldChrManPtr.ToInt64() + 0x1E268); // WorldChrMan + 0x1E268
        //            long intermediatePtr1_Val = Core.Memory.ReadInt64(intermediatePtr1_Addr); // Dereference #1
        //            if (intermediatePtr1_Val != 0)
        //            {
        //                // According to CE script, the final pointer used for array access involves more steps.
        //                // Let's TEST using *this* pointer (after one deref) first for the trigger flags/list.
        //                alternativeRcxPtr = (IntPtr)intermediatePtr1_Val;
        //                Log($"Calculated Alternative RCX Pointer: 0x{alternativeRcxPtr.ToInt64():X}");
        //            }
        //            else
        //            {
        //                Log($"ERROR: Intermediate pointer at offset 0x1E268 is NULL.");
        //                ShowInjectionFailed(SoulsGames.ER, "Pointer at offset 0x1E268 is null.");
        //                return false; // Cannot proceed without this pointer
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Log($"ERROR calculating alternative RCX pointer: {ex.Message}");
        //            ShowInjectionFailed(SoulsGames.ER, "Exception during pointer calculation.");
        //            return false;
        //        }
        //        // *** End Alternative Pointer Calculation ***


        //        // --- Allocate Memory ---
        //        chrReloadAsm = Core.Memory.AllocateMemory(allocSize);
        //        chrReloadData = Core.Memory.AllocateMemory(allocSize);
        //        if (chrReloadAsm == IntPtr.Zero || chrReloadData == IntPtr.Zero) { /* Error Handling */ }

        //        // --- Prepare Data Structure (ORIGINAL METHOD - Link Pointer + String + 0x1F Length) ---
        //        // We still base the *ListHeadPointerValue* read off the main WorldChrMan pointer for data prep
        //        IntPtr listHeadPointerAddress = (IntPtr)(Core.Memory.EldenRing_WorldChrManPtr.ToInt64() + EldenRingConfig.WorldChrManStructOffset_ListHeadPtr);
        //        long listHeadPointerValue = Core.Memory.ReadInt64(listHeadPointerAddress);

        //        IntPtr chrReloadData_Offset8 = (IntPtr)(chrReloadData.ToInt64() + 0x8);
        //        IntPtr chrReloadData_Offset58 = (IntPtr)(chrReloadData.ToInt64() + 0x58);
        //        IntPtr chrReloadData_Offset70 = (IntPtr)(chrReloadData.ToInt64() + 0x70);
        //        IntPtr chrNameStringAddr = (IntPtr)(chrReloadData.ToInt64() + 0x100);

        //        if (!Core.Memory.WriteInt64(chrReloadData_Offset8, listHeadPointerValue) ||
        //            !Core.Memory.WriteInt64(chrReloadData_Offset58, chrNameStringAddr.ToInt64()) ||
        //            !Core.Memory.WriteInt8(chrReloadData_Offset70, 0x1F) || // Use 0x1F
        //            !Core.Memory.WriteBytes(chrNameStringAddr, chrNameBytes)) { /* Error Handling */ }
        //        Log($"Wrote data structure (Length: 0x1F)");


        //        // --- Skip Crash Fix Patch ---
        //        Log("Skipping crash fix patch for diagnostics.");


        //        // --- Prepare Assembly Buffer (ORIGINAL STRUCTURE, but using ALTERNATIVE RCX) ---
        //        var buffer = new byte[] {
        //             0x48, 0xBB, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,     // mov rbx, [chrReloadData address]
        //             0x48, 0xB9, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,     // mov rcx, [ALTERNATIVE Pointer Address] <-- CHANGE HERE
        //             // List manipulation and trigger flags now relative to the ALTERNATIVE RCX
        //             0x48, 0x8B, 0x91, 0x00, 0x00, 0x00, 0x00,                       // mov rdx,[rcx + ListHeadPtrOffset]
        //             0x48, 0x89, 0x1A,                                               // mov [rdx],rbx
        //             0x48, 0x89, 0x13,                                               // mov [rbx],rdx
        //             0x48, 0x8B, 0x91, 0x00, 0x00, 0x00, 0x00,                       // mov rdx,[rcx + ListHeadPtrOffset]
        //             0x48, 0x89, 0x5A, 0x08,                                         // mov [rdx+08],rbx
        //             0x48, 0x89, 0x53, 0x08,                                         // mov [rbx+08],rdx
        //             0xC7, 0x81, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,     // mov [rcx + Trigger1Offset], 1
        //             0xC7, 0x81, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x41,     // mov [rcx + Trigger2Offset], 10.0f
        //             0xC3,                                                           // ret
        //         };

        //        // --- Inject Addresses and Offsets ---
        //        Array.Copy(BitConverter.GetBytes(chrReloadData.ToInt64()), 0, buffer, 0x2, 8);
        //        // *** Use ALTERNATIVE POINTER for RCX ***
        //        Array.Copy(BitConverter.GetBytes(alternativeRcxPtr.ToInt64()), 0, buffer, 0xC, 8);
        //        // *** Offsets are now relative to this new RCX. Are they still correct? ***
        //        // *** Let's ASSUME for this test that ListHead/Trigger offsets are relative to this SUB-structure pointer ***
        //        // *** This is a GUESS - they might still be relative to the original WorldChrMan base ***
        //        Array.Copy(BitConverter.GetBytes(EldenRingConfig.WorldChrManStructOffset_ListHeadPtr), 0, buffer, 0x17, 4); // Offset for 1st mov rdx
        //        Array.Copy(BitConverter.GetBytes(EldenRingConfig.WorldChrManStructOffset_ListHeadPtr), 0, buffer, 0x22, 4); // Offset for 2nd mov rdx
        //        Array.Copy(BitConverter.GetBytes(EldenRingConfig.WorldChrManStructOffset_Trigger1), 0, buffer, 0x2E, 4); // Offset for Trigger1 mov
        //        Array.Copy(BitConverter.GetBytes(EldenRingConfig.WorldChrManStructOffset_Trigger2), 0, buffer, 0x3C, 4); // Offset for Trigger2 mov

        //        // --- Write and Execute ASM ---
        //        if (!Core.Memory.WriteBytes(chrReloadAsm, buffer)) { /* Error Handling */ }

        //        IntPtr threadHandle = Core.Memory.ExecuteRemoteFunction(chrReloadAsm);
        //        if (threadHandle != IntPtr.Zero) { /* Wait and Close */ success = true; } else { /* Error Handling */ success = false; }
        //    }
        //    catch (Exception ex) { /* Exception Handling */ }
        //    finally { /* Memory Cleanup */ }
        //    return success;
        //}



        private static bool ReloadEldenRing(byte[] chrNameBytes)
        {
            // --- Pointer Check ---
            if (Core.Memory.EldenRing_WorldChrManPtr == IntPtr.Zero || Core.Memory.EldenRing_CrashFixPtr == IntPtr.Zero)
            {
                Log("ERROR: Required pointers not found for ER. Attempting AOB scan again...");
                if (!Core.Memory.UpdateEldenRingAobs() || Core.Memory.EldenRing_WorldChrManPtr == IntPtr.Zero || Core.Memory.EldenRing_CrashFixPtr == IntPtr.Zero)
                {
                    ShowInjectionFailed(SoulsGames.ER, "AOB scan failed or pointers are null after retry.");
                    return false;
                }
                Log("AOB scan re-run successful.");
            }

            // --- Log Current Config (These offsets will need to be updated for 1.16!) ---
            Log($"Base WorldChrMan Ptr: 0x{Core.Memory.EldenRing_WorldChrManPtr.ToInt64():X}");
            Log($"Using (Likely Outdated for 1.16) Offsets: Trigger1=0x{EldenRingConfig.WorldChrManStructOffset_Trigger1:X}, Trigger2=0x{EldenRingConfig.WorldChrManStructOffset_Trigger2:X}, ListHeadPtr=0x{EldenRingConfig.WorldChrManStructOffset_ListHeadPtr:X}");

            IntPtr chrReloadAsm = IntPtr.Zero;
            IntPtr chrReloadData = IntPtr.Zero; // This is for the data structure you inject
            bool success = false;
            const int allocSize = 512; // Ample size

            try
            {
                // --- Allocate Memory ---
                chrReloadAsm = Core.Memory.AllocateMemory(allocSize);
                chrReloadData = Core.Memory.AllocateMemory(allocSize); // For the struct with name, etc.

                if (chrReloadAsm == IntPtr.Zero || chrReloadData == IntPtr.Zero)
                {
                    Log($"ERROR: Failed to allocate memory. ASM: 0x{chrReloadAsm.ToInt64():X}, Data: 0x{chrReloadData.ToInt64():X}");
                    ShowInjectionFailed(SoulsGames.ER, "VirtualAllocEx failed.");
                    if (chrReloadAsm != IntPtr.Zero) Core.Memory.FreeMemory(chrReloadAsm);
                    if (chrReloadData != IntPtr.Zero) Core.Memory.FreeMemory(chrReloadData);
                    return false;
                }
                Log($"Allocated Memory: ASM @ 0x{chrReloadAsm.ToInt64():X}, Data @ 0x{chrReloadData.ToInt64():X}");

                // --- Prepare Data Structure to be Injected ---
                // This structure needs to be compatible with what Elden Ring's reload mechanism expects.
                // The `listHeadPointerValue` is used to link your new data node.
                // IMPORTANT: EldenRingConfig.WorldChrManStructOffset_ListHeadPtr needs to be the CORRECT 1.16 offset.
                IntPtr listHeadPointerAddress = (IntPtr)(Core.Memory.EldenRing_WorldChrManPtr.ToInt64() + EldenRingConfig.WorldChrManStructOffset_ListHeadPtr);
                long listHeadPointerValue = Core.Memory.ReadInt64(listHeadPointerAddress);
                Log($"Read listHeadPointerValue 0x{listHeadPointerValue:X} from 0x{listHeadPointerAddress.ToInt64():X} (WorldChrMan + ListHeadOffset)");

                // Get the current list head's Flink value (pointer to the first actual item, or to sentinel's Flink if empty)
                IntPtr listHeadFlinkStorageAddress = (IntPtr)(Core.Memory.EldenRing_WorldChrManPtr.ToInt64() + EldenRingConfig.WorldChrManStructOffset_ListHeadPtr); // WCM_DATA_PTR + 0x1E660
                long currentListFlinkValue = Core.Memory.ReadInt64(listHeadFlinkStorageAddress);
                Log($"Current List Flink Value (RDX in ASM): 0x{currentListFlinkValue:X} read from 0x{listHeadFlinkStorageAddress.ToInt64():X}");

                // Prepare our new node (chrReloadData)
                // Our new node's Flink should point to where the sentinel's Flink was pointing



                Core.Memory.WriteInt64((IntPtr)(chrReloadData.ToInt64() + 0x0), currentListFlinkValue); // NewNode->Flink
                Core.Memory.WriteInt64((IntPtr)(chrReloadData.ToInt64() + 0x8), listHeadFlinkStorageAddress.ToInt64()); // NewNode->Blink

                IntPtr chrReloadData_Offset8 = (IntPtr)(chrReloadData.ToInt64() + 0x8);  // Next pointer for the list node
                IntPtr chrReloadData_Offset58 = (IntPtr)(chrReloadData.ToInt64() + 0x58); // Pointer to the string itself
                IntPtr chrReloadData_Offset70 = (IntPtr)(chrReloadData.ToInt64() + 0x70); // Length/flag field
                IntPtr chrNameStringAddr = (IntPtr)(chrReloadData.ToInt64() + 0x100);     // Where the string will be written

                // Write the data for the new node
                if (!Core.Memory.WriteInt64(chrReloadData_Offset8, listHeadPointerValue) ||  // Link to existing list head's 'next'
                    !Core.Memory.WriteInt64(chrReloadData_Offset58, chrNameStringAddr.ToInt64()) ||
                    !Core.Memory.WriteInt8(chrReloadData_Offset70, 0x1F) || // Length/flag (0x1F was used before)
                    !Core.Memory.WriteBytes(chrNameStringAddr, chrNameBytes))
                {
                    Log("ERROR: Failed to write data structure or string to allocated memory.");
                    ShowInjectionFailed(SoulsGames.ER, "WriteProcessMemory failed for data setup.");
                    Core.Memory.FreeMemory(chrReloadAsm); Core.Memory.FreeMemory(chrReloadData);
                    return false;
                }
                Log($"Wrote data structure (Length: 0x1F) to 0x{chrReloadData.ToInt64():X}");
                Log($"Wrote data structure. NewNode->Flink=0x{currentListFlinkValue:X}, NewNode->Blink=0x{listHeadFlinkStorageAddress.ToInt64():X}");

                // --- RE-ENABLE Crash Fix Patch ---
                if (Core.Memory.EldenRing_CrashFixPtr != IntPtr.Zero)
                {
                    byte[] crashPatchBytes = EldenRingConfig.CrashFixWriteBytes;
                    if (crashPatchBytes != null && crashPatchBytes.Length > 0)
                    {
                        if (Core.Memory.WriteBytes(Core.Memory.EldenRing_CrashFixPtr, crashPatchBytes))
                        {
                            Log($"Applied crash fix patch at 0x{Core.Memory.EldenRing_CrashFixPtr.ToInt64():X}");
                        }
                        else
                        {
                            Log($"WARNING: Failed to write crash fix patch at 0x{Core.Memory.EldenRing_CrashFixPtr.ToInt64():X}. Proceeding without it.");
                            // Optionally, you could choose to return false here if the crash fix is critical
                        }
                    }
                    else
                    {
                        Log("WARNING: CrashFixWriteBytes are null or empty in config. Skipping patch.");
                    }
                }
                else
                {
                    Log("WARNING: EldenRing_CrashFixPtr is null. Skipping crash fix patch.");
                }

                // --- Prepare Assembly Buffer ---
                // RCX will be Core.Memory.EldenRing_WorldChrManPtr
                // RBX will be chrReloadData (pointer to our prepared data structure)
                // Offsets MUST BE THE CORRECT 1.16 OFFSETS
                var buffer = new byte[] {
            0x48, 0xBB, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,     // mov rbx, [chrReloadData address] (our new node)
            0x48, 0xB9, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,     // mov rcx, [Core.Memory.EldenRing_WorldChrManPtr] (base of WorldChrMan)

            // Standard Doubly Linked List insertion (example, verify exact logic for ER)
            // Current List Head Pointer is at [rcx + ListHeadPtrOffset]
            // New Node is in RBX
            // RBX->Next = [rcx + ListHeadPtrOffset] (Value we wrote into chrReloadData_Offset8 earlier from listHeadPointerValue)
            // RBX->Prev = rcx + ListHeadPtrOffset (Address of the head pointer itself, if it's a circular list head node)
            // [rcx + ListHeadPtrOffset]->Prev = RBX (If the list head's next pointer pointed to an actual node)
            // [rcx + ListHeadPtrOffset] = RBX (Make new node the new head's next)

            // The DSAnimStudio / Your previous assembly for list manipulation:
            0x48, 0x8B, 0x91, 0x00, 0x00, 0x00, 0x00,                       // mov rdx,[rcx + ListHeadPtrOffset_1.16] (rdx = current_head_node_next_ptr)
            0x48, 0x89, 0x1A,                                               // mov [rdx],rbx             (current_head_node_next_ptr->prev = new_node (rbx))
            0x48, 0x89, 0x13,                                               // mov [rbx],rdx             (new_node (rbx)->next = current_head_node_next_ptr (rdx))
            // The above inserts RBX *after* the node pointed to by RDX's original content.
            // To insert at the head of the list pointed to by [rcx + ListHeadPtrOffset_1.16]:
            // Need to get pointer to head element: RDX = [RCX + ListHeadPtrOffset_1.16]
            // NewNode(RBX)->Next = RDX
            // NewNode(RBX)->Prev = RCX + ListHeadPtrOffset_1.16  (Assuming ListHeadPtrOffset is a pointer to the list head node itself, making it circular)
            // RDX->Prev = RBX
            // [RCX + ListHeadPtrOffset_1.16] = RBX (Update actual list head pointer)
            // The original assembly seemed to handle a circular list where [ListHeadPtrOffset] points to itself if empty,
            // or to the last element, and [ListHeadPtrOffset+8] points to the first.

            // Sticking to the original assembly structure, assuming WorldChrManStructOffset_ListHeadPtr is the offset to the "Next" pointer field
            // of a list head sentinel node.
            // RDX = sentinel->Next
            // sentinel->Next->Prev = RBX (our new node)
            // RBX->Next = RDX
            // RBX->Prev = sentinel (RCX + ListHeadPtrOffset - 8, if ListHeadPtrOffset is for 'Next' and 'Prev' is at ListHeadPtrOffset-8)
            // sentinel->Next = RBX
            // THIS PART IS CRITICAL AND DEPENDS ON ER'S LIST STRUCTURE AND THE MEANING OF ListHeadPtrOffset
            // The original assembly was:
            // mov rdx,[rcx + ListHeadPtrOffset_1.16] (rdx = head->next)
            // mov [rdx],rbx                          (head->next->prev = rbx)
            // mov [rbx],rdx                          (rbx->next = head->next)
            // mov rdx,[rcx + ListHeadPtrOffset_1.16] (rdx = head->next again, though now it should be rbx) <--- This seems redundant or for a different link
            // mov [rdx+08],rbx                       (head->next->next = rbx ??)
            // mov [rbx+08],rdx                       (rbx->prev = head->next ??)

            // Let's use the known working structure from DSAnimStudio, assuming ListHeadPtr points to a field which itself contains the pointer to the first element's "Next" field,
            // and that this list is circular.
            // For ER 1.07+, the offsets were: ListHeadPtr=0x1E660, Trigger1=0x1E668, Trigger2=0x1E670
            // The assembly was:
            // mov rdx,[rcx+0x1E660] // rdx = address_stored_at_ListHeadPtr_offset (e.g. sentinel_node.next_ptr_field_itself if list is empty, or actual_node.next_ptr_field)
            // mov [rdx],rbx         // *(sentinel_node.next_ptr_field_itself).prev = rbx
            // mov [rbx],rdx         // rbx.next = sentinel_node.next_ptr_field_itself
            // mov rdx,[rcx+0x1E660] // (get it again)
            // mov [rdx+08],rbx      // *(sentinel_node.next_ptr_field_itself).next = rbx (if list was empty, this makes sentinel.next point to rbx)
            // mov [rbx+08],rdx      // rbx.prev = sentinel_node.next_ptr_field_itself

            // This is a standard circular doubly linked list insertion relative to a sentinel node:
            // RBX is the new node.
            // RCX + ListHeadPtrOffset is the address of the sentinel's 'Flink' (Forward Link / Next pointer).
            // RCX + ListHeadPtrOffset + 8 could be sentinel's 'Blink' (Backward Link / Prev pointer).
            // Let ListHeadPtr be the offset to the Flink field of the sentinel.
            // 1. RBX->Flink = [RCX + ListHeadPtrOffset]  (New node's next = sentinel's current next)
            // 2. RBX->Blink = RCX + ListHeadPtrOffset    (New node's prev points to sentinel's Flink field address - this is how head is identified)
            // 3. [RCX + ListHeadPtrOffset]->Blink = RBX  (Sentinel's current next's prev = New node)
            // 4. [RCX + ListHeadPtrOffset] = RBX         (Sentinel's next = New node)

            // The assembly provided by DSAnimStudio (and your code) seems to do:
            //   Let P = RCX + ListHeadPtrOffset_1.16
            //   RDX = *P (content of sentinel's Flink, i.e., points to first element's Flink field or sentinel's Flink if empty)
            //   *(RDX) = RBX (Equivalent to RDX->Blink = RBX, if RDX is base of node and 0 is Blink)
            //                 More likely: RDX is sentinel.Flink. Let's say sentinel.Flink points to NodeA.Flink.
            //                 Then this is NodeA.Flink.Blink = RBX
            //   RBX->Flink = RDX
            //   RDX = *P (again)
            //   *(RDX+8) = RBX (Equivalent to RDX->Flink = RBX, if RDX is base of node and 8 is Flink)
            //                  More likely: RDX is sentinel.Flink. Then this is NodeA.Flink.Flink = RBX
            //   RBX->Blink = RDX (but offset +8)

            // Sticking to the byte-for-byte structure of the previous assembly, just with placeholders for new offsets
            0x48, 0x8B, 0x91, 0x00, 0x00, 0x00, 0x00,                       // mov rdx,[rcx + NEW_1_16_ListHeadPtrOffset]
            0x48, 0x89, 0x1A,                                               // mov [rdx],rbx
            0x48, 0x89, 0x13,                                               // mov [rbx],rdx
            0x48, 0x8B, 0x91, 0x00, 0x00, 0x00, 0x00,                       // mov rdx,[rcx + NEW_1_16_ListHeadPtrOffset]
            0x48, 0x89, 0x5A, 0x08,                                         // mov [rdx+08],rbx
            0x48, 0x89, 0x53, 0x08,                                         // mov [rbx+08],rdx

            // Trigger Reload
            0xC7, 0x81, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00,     // mov dword ptr [rcx + NEW_1_16_Trigger1Offset], 1
            0xC7, 0x81, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x41,     // mov dword ptr [rcx + NEW_1_16_Trigger2Offset], 10.0f
            0xC3                                                            // ret
        };

                // --- Inject Addresses and Offsets ---
                Array.Copy(BitConverter.GetBytes(chrReloadData.ToInt64()), 0, buffer, 0x2, 8); // Patch chrReloadData address into MOV RBX
                Array.Copy(BitConverter.GetBytes(Core.Memory.EldenRing_WorldChrManPtr.ToInt64()), 0, buffer, 0xC, 8); // Patch WorldChrMan DATA Ptr into MOV RCX

                // YOU MUST FIND THESE NEW OFFSETS FOR ER 1.16 using Cheat Engine relative to your WorldChrMan DATA Ptr
                // The old 0x1E66x values are placeholders here and WILL cause crashes if not updated.
                int listHeadOffset_1_16 = EldenRingConfig.WorldChrManStructOffset_ListHeadPtr; // REPLACE WITH NEW 1.16 OFFSET
                int trigger1Offset_1_16 = EldenRingConfig.WorldChrManStructOffset_Trigger1; // REPLACE WITH NEW 1.16 OFFSET
                int trigger2Offset_1_16 = EldenRingConfig.WorldChrManStructOffset_Trigger2; // REPLACE WITH NEW 1.16 OFFSET

                Array.Copy(BitConverter.GetBytes(listHeadOffset_1_16), 0, buffer, 0x17, 4); // Offset for 1st mov rdx,[rcx+ListHead]
                Array.Copy(BitConverter.GetBytes(listHeadOffset_1_16), 0, buffer, 0x22, 4); // Offset for 2nd mov rdx,[rcx+ListHead] (was 0x24 in prev. code, check ASM length)
                                                                                            // 0x14 (start of list ops) + 3 (mov rdx) + 4 (offset) + 3 (mov [rdx]) + 3 (mov [rbx]) = 0x14+D = 0x21. Next mov rdx is at 0x21. So placeholder at 0x21+3=0x24.
                                                                                            // Corrected:
                                                                                            // 0x48,0x8B,0x91, [off1] -> idx 0x14, placeholder at 0x17
                                                                                            // 0x48,0x89,0x1A -> idx 0x1B
                                                                                            // 0x48,0x89,0x13 -> idx 0x1E
                                                                                            // 0x48,0x8B,0x91, [off2] -> idx 0x21, placeholder at 0x24
                Array.Copy(BitConverter.GetBytes(listHeadOffset_1_16), 0, buffer, 0x24, 4); // Patch for the second ListHeadPtrOffset

                // Trigger Ops start after list ops:
                // 0x21 + 3 (mov rdx) + 4 (offset) + 3 (mov [rdx+08]) + 3 (mov [rbx+08]) = 0x21+D = 0x2E. First trigger starts at 0x2E.
                Array.Copy(BitConverter.GetBytes(trigger1Offset_1_16), 0, buffer, 0x31, 4); // Placeholder at 0x2E+3=0x31.
                                                                                            // First trigger: C7,81,[off],01000000 (10 bytes). Starts at 0x2E. Ends at 0x2E+A-1 = 0x37.
                                                                                            // Second trigger starts at 0x38.
                Array.Copy(BitConverter.GetBytes(trigger2Offset_1_16), 0, buffer, 0x3B, 4); // Placeholder at 0x38+3=0x3B.

                Log("Prepared ASM buffer for injection.");

                // --- Write and Execute ASM ---
                if (!Core.Memory.WriteBytes(chrReloadAsm, buffer))
                {
                    Log("ERROR: Failed to write ASM buffer to allocated memory.");
                    ShowInjectionFailed(SoulsGames.ER, "WriteProcessMemory failed for ASM buffer.");
                    Core.Memory.FreeMemory(chrReloadAsm); Core.Memory.FreeMemory(chrReloadData);
                    return false;
                }
                Log($"Wrote ASM buffer to 0x{chrReloadAsm.ToInt64():X}");

                IntPtr threadHandle = Core.Memory.ExecuteRemoteFunction(chrReloadAsm);
                if (threadHandle != IntPtr.Zero)
                {
                    Log("Remote thread created. Waiting for completion (max 5s)...");
                    bool threadCompleted = Core.Memory.WaitForThread(threadHandle, 5000); // Wait up to 5 seconds
                    Core.Memory.CloseThreadHandle(threadHandle);
                    Log(threadCompleted ? "Remote thread signaled completion." : "Remote thread timed out or failed to signal.");
                    success = threadCompleted; // Consider timeout a failure for success status
                }
                else
                {
                    Log("ERROR: Failed to create remote thread.");
                    ShowInjectionFailed(SoulsGames.ER, "CreateRemoteThread failed.");
                    success = false;
                }
            }
            catch (Exception ex)
            {
                Log($"ERROR during Elden Ring reload: {ex.Message}\n{ex.StackTrace}");
                ShowInjectionFailed(SoulsGames.ER, $"Internal error: {ex.Message}");
                success = false;
            }
            finally
            {
                Log($"Cleaning up allocated memory. ASM: {chrReloadAsm != IntPtr.Zero}, Data: {chrReloadData != IntPtr.Zero}");
                if (chrReloadAsm != IntPtr.Zero) Core.Memory.FreeMemory(chrReloadAsm);
                if (chrReloadData != IntPtr.Zero) Core.Memory.FreeMemory(chrReloadData);
                Log($"ReloadEldenRing returning: {success}");
            }
            return success;
        }

        private static bool ReloadDs1r(byte[] chrNameBytes)
        {
            const long reloadFlagOffset = 0x1D151DB;
            const long ptrStructOffset = 0x1D151B0;
            const long reloadFuncOffset = 0x3712A0;

            Log("WARNING: DS1R addresses require verification for the current game version.");

            // Use explicit IntPtr casts for address calculation
            IntPtr flagAddr = (IntPtr)(Core.Memory.BaseAddress.ToInt64() + reloadFlagOffset);
            IntPtr ptrStructAddr = (IntPtr)(Core.Memory.BaseAddress.ToInt64() + ptrStructOffset); // Address *of the pointer*
            IntPtr funcAddr = (IntPtr)(Core.Memory.BaseAddress.ToInt64() + reloadFuncOffset);

            if (!Core.Memory.WriteBoolean(flagAddr, true))
            {
                Log($"ERROR: Failed to write DS1R reload flag at 0x{flagAddr.ToInt64():X}");
                ShowInjectionFailed(SoulsGames.DS1R, "Failed to write reload flag.");
                return false;
            }

            var buffer = new byte[] {
                0x48, 0xBA, 0, 0, 0, 0, 0, 0, 0, 0,                         // mov rdx, [ArgAddress]
                0x48, 0xA1, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // mov rax, [Absolute Address Of Pointer]
                0x48, 0x8B, 0xC8,                                           // mov rcx, rax
                0x49, 0xBE, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // mov r14, [FunctionAddress]
                0x48, 0x83, 0xEC, 0x28,                                     // sub rsp, 28
                0x41, 0xFF, 0xD6,                                           // call r14
                0x48, 0x83, 0xC4, 0x28,                                     // add rsp, 28
                0xC3                                                        // Ret
            };

            Array.Copy(BitConverter.GetBytes(ptrStructAddr.ToInt64()), 0, buffer, 0xC, 8);
            Array.Copy(BitConverter.GetBytes(funcAddr.ToInt64()), 0, buffer, 0x1B, 8);

            return Core.Memory.ExecuteBufferFunction(buffer, chrNameBytes, argLocationInAsmArray: 0x2);
        }

    } // End of NpcReloaderLogic class
} // End of namespace
  // ----- End of NpcReloaderLogic.cs -----