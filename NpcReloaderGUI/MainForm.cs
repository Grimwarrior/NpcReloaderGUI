using NpcReloaderGUI.Core;
using System;
using System.IO;
using System.Linq;
using System.Threading; // For Timer
using System.Windows.Forms;
using SoulsAssetPipeline; // For SoulsGames enum

namespace NpcReloaderGUI
{
    public partial class MainForm : Form
    {
        private FileSystemWatcher _fileWatcher;
        private System.Threading.Timer _debounceTimer; // Use System.Threading.Timer for background debounce
        private string _fileToWatch = null;
        private DateTime _lastFileWriteTime = DateTime.MinValue;
        private const int DebounceMilliseconds = 500; // Wait 500ms after file change before reloading

        public MainForm()
        {
            InitializeComponent();
            InitializeFileWatcher();

            // Set the LogAction in NpcReloaderLogic to update our TextBox
            NpcReloaderLogic.LogAction = LogMessage;
            // Call the new setup method
            RefreshCharacterComboBox();
        }

        private void InitializeFileWatcher()
        {
            _fileWatcher = new FileSystemWatcher
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName // Watch for writes and renames
            };
            _fileWatcher.Changed += Watcher_Changed;
            _fileWatcher.Renamed += Watcher_Renamed; // Handle rename events too

            // Initialize debounce timer (initially disabled)
            _debounceTimer = new System.Threading.Timer(DebounceTimerCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        // +++ ADD THIS NEW METHOD +++
        private void RefreshCharacterComboBox(CharacterInfo charToSelect = null)
        {
            // 1. Get the latest, sorted list of characters.
            // The CharacterData class already loads built-in and user characters on startup.
            var characterList = CharacterData.AllCharacters;

            // 2. Detach the data source completely to reset the ComboBox state.
            cmbChrSelection.DataSource = null;
            cmbChrSelection.Items.Clear();

            // 3. Set up the auto-complete with a CUSTOM source. This is the key fix.
            // We will build our own list of strings for the auto-complete to use.
            var autoCompleteSource = new AutoCompleteStringCollection();
            autoCompleteSource.AddRange(characterList.Select(c => c.ToString()).ToArray());

            cmbChrSelection.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            cmbChrSelection.AutoCompleteSource = AutoCompleteSource.CustomSource; // <-- Use CustomSource!
            cmbChrSelection.AutoCompleteCustomSource = autoCompleteSource;

            // 4. Re-bind the updated list to the ComboBox.
            cmbChrSelection.DataSource = characterList;

            // 5. Optionally select an item.
            if (charToSelect != null)
            {
                cmbChrSelection.SelectedItem = charToSelect;
            }
            else if (cmbChrSelection.Items.Count > 0)
            {
                // Fallback to a default if nothing is specified.
                var radagon = characterList.FirstOrDefault(c => c.Id == 2190);
                cmbChrSelection.SelectedItem = radagon ?? characterList.First();
            }
        }

        // --- UI Event Handlers ---

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Populate Game ComboBox
            cmbGameSelection.DataSource = Enum.GetValues(typeof(SoulsGames))
                                              .Cast<SoulsGames>()
                                              // Add games supported by the logic
                                              .Where(g => g == SoulsGames.DS1R ||
                                                          g == SoulsGames.DS3 ||
                                                          g == SoulsGames.SDT ||
                                                          g == SoulsGames.ER)
                                              .ToList();
            cmbGameSelection.SelectedItem = SoulsGames.ER; // Default selection

            LogMessage("Application started. Select game and enter Character ID.");
            UpdateAutoReloadState(); // Set initial enabled state of controls
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Clean up resources
            NpcReloaderLogic.DetachFromGame(); // Ensure process handle is closed
            _fileWatcher?.Dispose();
            _debounceTimer?.Dispose();
        }


        //private async void btnReload_Click_1(object sender, EventArgs e)
        //{
        //    if (cmbGameSelection.SelectedItem == null)
        //    {
        //        MessageBox.Show("Please select a game.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //        return;
        //    }

        //    SoulsGames selectedGame = (SoulsGames)cmbGameSelection.SelectedItem;
        //    string chrId = txtChrId.Text.Trim();

        //    if (string.IsNullOrWhiteSpace(chrId))
        //    {
        //        MessageBox.Show("Please enter a Character ID.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //        txtChrId.Focus();
        //        return;
        //    }


        //    // Disable button during operation
        //    btnReload.Enabled = false;
        //    this.UseWaitCursor = true;

        //    // Run the reload logic asynchronously to avoid blocking UI thread entirely
        //    // Although the core injection might still block briefly
        //    bool success = await System.Threading.Tasks.Task.Run(() =>
        //        NpcReloaderLogic.RequestReloadChr(selectedGame, chrId)
        //    );

        //    // Re-enable button
        //    this.UseWaitCursor = false;
        //    btnReload.Enabled = true;

        //    if (success)
        //    {
        //        LogMessage($"Manual reload requested for {chrId} in {selectedGame}.");
        //        // Optionally flash the background or give some visual cue
        //    }
        //    // Error messages are handled by NpcReloaderLogic via MessageBox and LogMessage
        //}

        private async void btnReload_Click_1(object sender, EventArgs e)
        {
            if (cmbGameSelection.SelectedItem == null)
            {
                MessageBox.Show("Please select a game.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SoulsGames selectedGame = (SoulsGames)cmbGameSelection.SelectedItem;
            string chrIdToReload = null;

            // Get the selected item from the ComboBox.
            var selectedCharacter = cmbChrSelection.SelectedItem as CharacterInfo;

            if (selectedCharacter != null)
            {
                // Get the formatted character ID string (e.g., "c2190").
                chrIdToReload = selectedCharacter.CharacterIdString;
            }
            else
            {
                // Fallback for if the user typed something not in the list.
                chrIdToReload = cmbChrSelection.Text.Trim();
            }

            if (string.IsNullOrWhiteSpace(chrIdToReload))
            {
                MessageBox.Show("Please select or enter a character to reload.", "No Character Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbChrSelection.Focus();
                return;
            }

            // Disable button during operation
            btnReload.Enabled = false;
            this.UseWaitCursor = true;

            bool success = await System.Threading.Tasks.Task.Run(() =>
                NpcReloaderLogic.RequestReloadChr(selectedGame, chrIdToReload)
            );

            // Re-enable button
            this.UseWaitCursor = false;
            btnReload.Enabled = true;

            if (success)
            {
                LogMessage($"Manual reload requested for {chrIdToReload} in {selectedGame}.");
            }
        }
        private void btnBrowseScript_Click_1(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Script Files (*.lua; *.hks)|*.lua;*.hks|All Files (*.*)|*.*";
                ofd.Title = "Select Script File to Watch";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _fileToWatch = ofd.FileName;
                    txtScriptPath.Text = _fileToWatch;
                    LogMessage($"Selected script file: {_fileToWatch}");
                    UpdateFileWatcherPath();
                }
            }
        }

        private void chkAutoReload_CheckedChanged_1(object sender, EventArgs e)
        {
            UpdateAutoReloadState();
        }

        // --- File Watching Logic ---

        private void UpdateAutoReloadState()
        {
            bool enabled = chkAutoReload.Checked;
            txtScriptPath.Enabled = enabled;
            btnBrowseScript.Enabled = enabled;

            if (enabled && !string.IsNullOrEmpty(_fileToWatch) && File.Exists(_fileToWatch))
            {
                UpdateFileWatcherPath(); // Ensure watcher is configured if enabled now
                _fileWatcher.EnableRaisingEvents = true;
                LogMessage($"Auto-reload enabled. Watching: {_fileToWatch}");
            }
            else
            {
                _fileWatcher.EnableRaisingEvents = false;
                LogMessage(enabled ? "Auto-reload enabled, but no valid script file selected." : "Auto-reload disabled.");
                // Disable debounce timer if auto-reload is turned off
                _debounceTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        private void UpdateFileWatcherPath()
        {
            if (string.IsNullOrEmpty(_fileToWatch) || !File.Exists(_fileToWatch))
            {
                _fileWatcher.EnableRaisingEvents = false;
                LogMessage("Cannot watch file: Path is invalid or file doesn't exist.");
                return;
            }

            try
            {
                _fileWatcher.Path = Path.GetDirectoryName(_fileToWatch);
                _fileWatcher.Filter = Path.GetFileName(_fileToWatch);
                // Re-enable if checkbox is checked
                _fileWatcher.EnableRaisingEvents = chkAutoReload.Checked;
                if (chkAutoReload.Checked)
                    LogMessage($"Watcher configured for: {_fileToWatch}");
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR configuring file watcher: {ex.Message}");
                _fileWatcher.EnableRaisingEvents = false;
            }
        }


        // Called when the file watcher detects a change or rename
        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            // Check if it's the file we care about (sometimes changes fire for other files)
            if (string.Equals(e.FullPath, _fileToWatch, StringComparison.OrdinalIgnoreCase))
            {
                LogMessage($"File change detected: {e.ChangeType} on {e.Name}");
                TriggerDebouncedReload();
            }
        }
        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            // If the file we were watching got renamed TO, trigger reload
            if (string.Equals(e.FullPath, _fileToWatch, StringComparison.OrdinalIgnoreCase))
            {
                LogMessage($"File rename detected: {e.OldName} -> {e.Name}");
                TriggerDebouncedReload();
            }
            // Optional: If the file was renamed FROM, stop watching?
            // else if (string.Equals(e.OldFullPath, _fileToWatch, StringComparison.OrdinalIgnoreCase)) { ... }
        }

        // Starts or resets the debounce timer
        private void TriggerDebouncedReload()
        {
            // Use Invoke to marshal UI access safely from the watcher thread if needed (though timer runs on thread pool)
            // Check file write time to avoid spurious events if file content didn't actually change
            try
            {
                DateTime currentWriteTime = File.GetLastWriteTimeUtc(_fileToWatch);
                if (currentWriteTime != _lastFileWriteTime)
                {
                    _lastFileWriteTime = currentWriteTime;
                    LogMessage("Debounce timer started/reset...");
                    // Reset the timer to fire after DebounceMilliseconds
                    _debounceTimer.Change(DebounceMilliseconds, Timeout.Infinite);
                }
                else
                {
                    LogMessage("File change event ignored (write time identical).");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error checking file write time: {ex.Message}");
                // Still try to reload if we can't check time? Maybe just log error.
            }
        }

        // This callback executes on a ThreadPool thread after the debounce period
        //private void DebounceTimerCallback(object state)
        //{
        //    LogMessage("Debounce timer elapsed. Triggering auto-reload.");

        //    // We need to get UI values (game, chrId) safely. Use Invoke.
        //    this.Invoke((MethodInvoker)delegate
        //    {
        //        if (!chkAutoReload.Checked) return; // Double check if user disabled it

        //        if (cmbGameSelection.SelectedItem == null || string.IsNullOrWhiteSpace(txtChrId.Text))
        //        {
        //            LogMessage("Auto-reload skipped: Game or Character ID not set.");
        //            return;
        //        }

        //        SoulsGames selectedGame = (SoulsGames)cmbGameSelection.SelectedItem;
        //        string chrId = txtChrId.Text.Trim(); // Get fresh value

        //        // Perform the reload - consider running async again if NpcReloaderLogic takes time
        //        // But since this is already off the UI thread, direct call might be okay
        //        // Using Task.Run ensures any blocking within doesn't stall the ThreadPool thread excessively.
        //        System.Threading.Tasks.Task.Run(() =>
        //        {
        //            LogMessage($"Auto-reloading {chrId} in {selectedGame} due to script change...");
        //            NpcReloaderLogic.RequestReloadChr(selectedGame, chrId);
        //        });

        //    });
        //}
        private void DebounceTimerCallback(object state)
        {
            LogMessage("Debounce timer elapsed. Triggering auto-reload.");

            // We need to get UI values safely. Use Invoke.
            this.Invoke((MethodInvoker)delegate
            {
                if (!chkAutoReload.Checked) return;

                SoulsGames selectedGame;
                string chrIdToReload = null;

                // Safely get game and character ID from UI controls
                if (cmbGameSelection.SelectedItem == null)
                {
                    LogMessage("Auto-reload skipped: Game not set.");
                    return;
                }
                selectedGame = (SoulsGames)cmbGameSelection.SelectedItem;

                var selectedCharacter = cmbChrSelection.SelectedItem as CharacterInfo;
                if (selectedCharacter != null)
                {
                    chrIdToReload = selectedCharacter.CharacterIdString;
                }
                else
                {
                    chrIdToReload = cmbChrSelection.Text.Trim();
                }

                if (string.IsNullOrWhiteSpace(chrIdToReload))
                {
                    LogMessage("Auto-reload skipped: Character ID not set.");
                    return;
                }

                System.Threading.Tasks.Task.Run(() =>
                {
                    LogMessage($"Auto-reloading {chrIdToReload} in {selectedGame} due to script change...");
                    NpcReloaderLogic.RequestReloadChr(selectedGame, chrIdToReload);
                });

            });
        }


        // --- Logging ---

        private void LogMessage(string message)
        {
            // Ensure thread-safe update to the TextBox
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action<string>(LogMessageInternal), message);
            }
            else
            {
                LogMessageInternal(message);
            }
        }

        private void LogMessageInternal(string message)
        {
            if (txtLog.TextLength > 40000) // Prevent log becoming too large
            {
                txtLog.Text = txtLog.Text.Substring(20000);
            }
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
            txtLog.ScrollToCaret(); // Keep latest message visible
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void txtLog_TextChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void labelCharacterName_Click(object sender, EventArgs e)
        {

        }

        // This is the new event handler for the "Add Entry" button.
        private void btnAddChar_Click(object sender, EventArgs e)
        {
            // 1. Get and Validate Input (This part is unchanged)
            string newIdText = txtNewCharId.Text.Trim();
            string newName = txtNewCharName.Text.Trim();

            if (!int.TryParse(newIdText, out int newId))
            {
                MessageBox.Show("The ID must be a valid number.", "Invalid ID", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtNewCharId.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("The Name cannot be empty.", "Invalid Name", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtNewCharName.Focus();
                return;
            }
            if (CharacterData.AllCharacters.Any(c => c.Id == newId))
            {
                MessageBox.Show($"The Character ID '{newId}' already exists.", "Duplicate ID", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtNewCharId.Focus();
                return;
            }

            // 2. Create the new character object
            var newCharacter = new CharacterInfo { Id = newId, Name = newName };

            // 3. Add the character to our data source and save it to the file
            CharacterData.AddUserCharacter(newCharacter);

            // 4. Call our new robust refresh method, telling it which character to select
            RefreshCharacterComboBox(newCharacter); // <-- THIS IS THE NEW PART

            // 5. Give feedback and clear the input boxes
            MessageBox.Show($"Successfully added '{newCharacter.Name}'.", "Entry Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
            txtNewCharId.Clear();
            txtNewCharName.Clear();
        }

        private void txtNewCharId_TextChanged(object sender, EventArgs e)
        {

        }
    }
}