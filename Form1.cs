using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RSDSwissKnife { 

    // Authored by Willj99
    // A Stoner Team tool
    //
    // Convert RSD's to WAV and OGG in batch, no more individual RSD files and export windows and endless clicking! No more audacity, no more online web converters.
    //
    //
    // Uses NVorbis and NETVorbisEncoding, all credit and license where due. Thank you.
    //
    public partial class Form1 : Form
    {
        private bool _selectAllState = false;
        private string _selectedPath;
        private ContextMenuStrip _listContextMenu;
        private RSDAudioPlayer _player;

        ToolStripMenuItem deleteMenuItem = new ToolStripMenuItem("Delete selected sound file(s)");
        ToolStripMenuItem filterByRSD = new ToolStripMenuItem("Hide all .RSD") { CheckOnClick = true };
        ToolStripMenuItem filterByWAV = new ToolStripMenuItem("Hide all .WAV") { CheckOnClick = true };
        ToolStripMenuItem filterByOGG = new ToolStripMenuItem("Hide all .OGG") { CheckOnClick = true };
        ToolStripMenuItem ScanNameCaseDialog = new ToolStripMenuItem("Scan for dialog with improper name casing.") ;
        ToolStripMenuItem refreshList = new ToolStripMenuItem("Refresh");

        public Form1()
        {
            InitializeComponent();

            list_file.View = View.Details;
            list_file.Columns.Add("File Name", list_file.Width - 90);
            list_file.Columns.Add("File Type", list_file.Width - 10);
            list_file.FullRowSelect = true;
            list_file.MultiSelect = true;

            button_opnpath.Click += button_opnpath_Click;
            list_file.SelectedIndexChanged += list_file_SelectedIndexChanged;
            InitializeContextMenu();

            _player = new RSDAudioPlayer();
            _player.ProgressUpdated += Player_ProgressUpdated;
            _player.PlaybackStopped += Player_PlaybackStopped;
        }

        public Form1(string[] args) : this()
        {
            if (args != null && args.Length > 0)
            {
                string clickedFilePath = args[0];

                if (File.Exists(clickedFilePath))
                {
                    string folderPath = Path.GetDirectoryName(clickedFilePath);

                    // Load the parent directory if an rsd is double clicked.
                    this.Load += (s, e) => LoadDirectory(folderPath, clickedFilePath);
                }
            }
        }

        private void LoadDirectory(string folderPath, string fileToSelect = null)
        {
            _selectedPath = folderPath;

  
            text_opnpath.Text = _selectedPath;

      
            RefreshFileList();

            
            if (!string.IsNullOrEmpty(fileToSelect))
            {
                string fileName = Path.GetFileName(fileToSelect);
                ListViewItem targetItem = list_file.FindItemWithText(fileName);

                if (targetItem != null)
                {
                    targetItem.Selected = true;
                    targetItem.EnsureVisible();
                    list_file.Focus();
                }
            }
        }


        private void Player_ProgressUpdated(object sender, PlaybackProgressEventArgs e)
        {
            label_CurrenrTimeVsTotal.Text = $"{e.CurrentTime:mm\\:ss} / {e.TotalTime:mm\\:ss}";
        }

        private void Player_PlaybackStopped(object sender, EventArgs e)
        {
            button_playpause.Text = "Play";
        }

        private void InitializeContextMenu()
        {
            _listContextMenu = new ContextMenuStrip();

            deleteMenuItem.Click += DeleteSelectedFiles_Click;
            ScanNameCaseDialog.Click += ScanNameCaseDialog_Click;
            refreshList.Click += refreshList_Click;
            filterByRSD.CheckedChanged += FilterMenuItem_CheckedChanged;
            filterByWAV.CheckedChanged += FilterMenuItem_CheckedChanged;
            filterByOGG.CheckedChanged += FilterMenuItem_CheckedChanged;

            _listContextMenu.Items.Add(deleteMenuItem);
            
            _listContextMenu.Items.Add(new ToolStripSeparator());
            _listContextMenu.Items.Add(filterByRSD);
            _listContextMenu.Items.Add(filterByWAV);
            _listContextMenu.Items.Add(filterByOGG);

            _listContextMenu.Items.Add(new ToolStripSeparator());
            _listContextMenu.Items.Add(ScanNameCaseDialog);
            _listContextMenu.Items.Add(refreshList);

            // Enable delete option only when items are selected
            _listContextMenu.Opening += (s, e) =>
            {
                deleteMenuItem.Enabled = list_file.SelectedItems.Count > 0;
            };

            list_file.ContextMenuStrip = _listContextMenu;
        }

        private void ScanNameCaseDialog_Click(object? sender, EventArgs e)
        {
            NameCasingFixer.FixNameCase(_selectedPath);
        }

        private void refreshList_Click(object? sender, EventArgs e) {
            RefreshFileList();
        }

        // Hi you using ILSpy! xxxx

        private void FilterMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            RefreshFileList();
        }

        private void DeleteSelectedFiles_Click(object sender, EventArgs e)
        {
            int count = list_file.SelectedItems.Count;
            if (count == 0 || string.IsNullOrEmpty(_selectedPath)) return;

            DialogResult confirm = MessageBox.Show(
                $"Are you sure you want to delete {count} selected file(s)?",
                "Confirm Deletion",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            foreach (ListViewItem item in list_file.SelectedItems)
            {
                string filePath = Path.Combine(_selectedPath, item.Text);
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not delete file '{item.Text}': {ex.Message}", "Error");
                }
            }

            RefreshFileList();
        }

        private void button_opnpath_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;

                _selectedPath = dialog.SelectedPath;
                text_opnpath.Text = _selectedPath;
                RefreshFileList();
            }
        }

        private void RefreshFileList()
        {
            list_file.Items.Clear();
            list_attributes.Items.Clear();

            if (string.IsNullOrEmpty(_selectedPath) || !Directory.Exists(_selectedPath))
            {
                return;
            }

            string[] validExtensions = { ".rsd", ".wav", ".ogg" };

            FileInfo[] files = new DirectoryInfo(_selectedPath)
                .GetFiles("*.*", SearchOption.TopDirectoryOnly)
                .Where(f => validExtensions.Contains(f.Extension.ToLower()))
                .ToArray();

            if (files.Length == 0) return;

            int GetPriority(string ext) => ext switch
            {
                ".rsd" => 1,
                ".wav" => 2,
                ".ogg" => 3,
                _ => 4
            };

            var sortedFiles = files
                .OrderBy(f => GetPriority(f.Extension.ToLower()))
                .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase);

            list_file.BeginUpdate();
            foreach (FileInfo file in sortedFiles)
            {
                string ext = file.Extension.ToLower();

                // Skip files based on checked menu items
                if (filterByRSD.Checked && ext == ".rsd") continue;
                if (filterByWAV.Checked && ext == ".wav") continue;
                if (filterByOGG.Checked && ext == ".ogg") continue;

                string typeLabel = ext switch
                {
                    ".rsd" => "RSD Audio",
                    ".wav" => "WAV Audio",
                    ".ogg" => "OGG Vorbis",
                    _ => "Unknown"
                };

                ListViewItem item = new ListViewItem(file.Name);
                item.SubItems.Add(typeLabel);

                item.ForeColor = ext switch
                {
                    ".rsd" => Color.DarkBlue,
                    ".wav" => Color.DarkOrange,
                    ".ogg" => Color.Purple,
                    _ => list_file.ForeColor
                };

                list_file.Items.Add(item);
            }
            list_file.EndUpdate();
        }

        private void list_file_SelectedIndexChanged(object sender, EventArgs e)
        {
            list_attributes.Items.Clear();

            if (list_file.SelectedItems.Count == 0 || string.IsNullOrEmpty(_selectedPath))
                return;

            if (list_file.SelectedItems.Count > 1)
            {
                button_playpause.Enabled = false;
                list_attributes.Items.Add($"Selected: {list_file.SelectedItems.Count} sound files");
                return;
            }

            button_playpause.Enabled = true;

            string fileName = list_file.SelectedItems[0].Text;
            string fullPath = Path.Combine(_selectedPath, fileName);

            if (!File.Exists(fullPath)) return;

            string extension = Path.GetExtension(fullPath).ToLower();

            try
            {
                switch (extension)
                {
                    case ".rsd": ReadRsdAttributes(fullPath); break;
                    case ".wav": ReadWavAttributes(fullPath); break;
                    case ".ogg": ReadOggAttributes(fullPath); break;
                }
            }
            catch (Exception ex)
            {
                list_attributes.Items.Add("Error reading metadata");
                list_attributes.Items.Add(ex.Message);
            }
        }

        private void SelectAllFiles()
        {
            _selectAllState = !_selectAllState;
            list_file.BeginUpdate();
            foreach (ListViewItem item in list_file.Items)
            {
                item.Selected = _selectAllState;
            }
            list_file.EndUpdate();
        }



        private void ReadRsdAttributes(string filePath)
        {
            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using BinaryReader br = new BinaryReader(fs);

            if (fs.Length < 2048) return;

            byte[] headerBytes = br.ReadBytes(8);
            string rawHeader = Encoding.ASCII.GetString(headerBytes).Replace("\0", "").Trim();
            string formatType = rawHeader.StartsWith("RADP") ? "RADP" : rawHeader.StartsWith("RADPCMB") ? "PCMB" : rawHeader.StartsWith("RADPCM") ? "PCM" : rawHeader;

            fs.Position = 8;
            int channels = br.ReadInt32();
            fs.Position = 16;
            int sampleRate = br.ReadInt32();

            long audioPayloadBytes = fs.Length - 2048;
            double totalSeconds = 0;

            if (rawHeader.Contains("PCM"))
            {
              
                int bytesPerSampleFrame = channels * 2;
                long totalSamples = audioPayloadBytes / bytesPerSampleFrame;
                totalSeconds = (double)totalSamples / sampleRate;
            }
            else
            {
            
                int frameHeaderBytes = 4 * channels;
                int frameDataBytes = 16 * channels;
                int frameSize = frameHeaderBytes + frameDataBytes;

                long totalFrames = audioPayloadBytes / frameSize;
                totalSeconds = (totalFrames * 32.0) / sampleRate;
            }

            TimeSpan duration = TimeSpan.FromSeconds(totalSeconds);

            list_attributes.Items.Add($"Format: {formatType} (.RSD)");
            list_attributes.Items.Add($"Duration: {duration:mm\\:ss}"); 
            list_attributes.Items.Add($"Channels: {channels}");
            list_attributes.Items.Add($"Sample Rate: {sampleRate} Hz");
            list_attributes.Items.Add($"File Size: {Math.Ceiling(fs.Length / 1024.0):N0} KB");
        }

        private void ReadWavAttributes(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                fs.Position = 12;

                int sampleRate = 0;
                short channels = 0;
                short bitsPerSample = 0;
                int dataSize = 0;

                while (fs.Position < fs.Length)
                {
                    string chunkId = new string(br.ReadChars(4));
                    int chunkSize = br.ReadInt32();

                    if (chunkId == "fmt ")
                    {
                        br.ReadInt16();
                        channels = br.ReadInt16();
                        sampleRate = br.ReadInt32();
                        br.ReadInt32();
                        br.ReadInt16();
                        bitsPerSample = br.ReadInt16();
                        if (chunkSize > 16) fs.Position += (chunkSize - 16);
                    }
                    else if (chunkId == "data")
                    {
                        dataSize = chunkSize;
                        break;
                    }
                    else
                    {
                        fs.Position += chunkSize;
                    }
                }

                int bytesPerSample = (bitsPerSample / 8) * channels;
                double totalSeconds = (bytesPerSample > 0 && sampleRate > 0) ? (double)dataSize / (sampleRate * bytesPerSample) : 0;
                TimeSpan duration = TimeSpan.FromSeconds(totalSeconds);

                list_attributes.Items.Add("Format: Wave (.WAV)");
                list_attributes.Items.Add($"Duration: {duration:mm\\:ss}");
                list_attributes.Items.Add($"Channels: {channels}");
                list_attributes.Items.Add($"Sample Rate: {sampleRate} Hz");
                list_attributes.Items.Add($"Bits Per Sample: {bitsPerSample}-bit");
                list_attributes.Items.Add($"File Size: {Math.Ceiling(fs.Length / 1024.0):N0} KB");
            }
        }

        private void ReadOggAttributes(string filePath)
        {
            FileInfo info = new FileInfo(filePath);
            double totalSeconds = 0;
            int sampleRate = 0;
            byte channels = 0;

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                byte[] firstPage = br.ReadBytes(Math.Min(200, (int)fs.Length));
                byte[] vorbisPattern = Encoding.ASCII.GetBytes("\x01vorbis");

                for (int i = 0; i <= firstPage.Length - 15; i++)
                {
                    if (firstPage[i] == vorbisPattern[0] && firstPage[i + 1] == vorbisPattern[1] &&
                        firstPage[i + 2] == vorbisPattern[2] && firstPage[i + 3] == vorbisPattern[3] &&
                        firstPage[i + 4] == vorbisPattern[4] && firstPage[i + 5] == vorbisPattern[5] &&
                        firstPage[i + 6] == vorbisPattern[6])
                    {
                        channels = firstPage[i + 11];
                        sampleRate = BitConverter.ToInt32(firstPage, i + 12);
                        break;
                    }
                }

                long seekPos = Math.Max(0, fs.Length - 8500);
                fs.Position = seekPos;

                byte[] searchPattern = Encoding.ASCII.GetBytes("OggS");
                byte[] buffer = br.ReadBytes((int)(fs.Length - seekPos));

                for (int i = buffer.Length - 4; i >= 0; i--)
                {
                    if (buffer[i] == searchPattern[0] && buffer[i + 1] == searchPattern[1] &&
                        buffer[i + 2] == searchPattern[2] && buffer[i + 3] == searchPattern[3])
                    {
                        long granPosOffset = i + 6;
                        long granulePosition = BitConverter.ToInt64(buffer, (int)granPosOffset);

                        if (granulePosition > 0 && sampleRate > 0)
                        {
                            totalSeconds = (double)granulePosition / sampleRate;
                        }
                        break;
                    }
                }
            }

            TimeSpan duration = TimeSpan.FromSeconds(totalSeconds);

            list_attributes.Items.Add("Format: OGG Vorbis (.OGG)");
            list_attributes.Items.Add($"Duration: {duration:mm\\:ss}");
            list_attributes.Items.Add($"Channels: {channels}");
            list_attributes.Items.Add($"Sample Rate: {sampleRate} Hz");
            list_attributes.Items.Add($"File Size: {Math.Ceiling(info.Length / 1024.0):N0} KB");
        }

        private string GetRsdHeaderType(string filePath)
        {
            try
            {
                using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                if (fs.Length < 8) return "UNKNOWN";

                byte[] headerBytes = new byte[8];
                fs.Read(headerBytes, 0, 8);
                string rawHeader = Encoding.ASCII.GetString(headerBytes).Replace("\0", "").Trim();

                if (rawHeader.StartsWith("RSD4RADP")) return "RADP";
                if (rawHeader.StartsWith("RADPCMB")) return "PCMB";
                if (rawHeader.StartsWith("RADPCM")) return "PCM";

                return rawHeader;
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedPath) || list_file.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select at least one .RSD sound file from the list.");
                return;
            }

            foreach (ListViewItem item in list_file.SelectedItems)
            {
                string inputFilePath = Path.Combine(_selectedPath, item.Text);

                if (!File.Exists(inputFilePath) || !Path.GetExtension(inputFilePath).Equals(".rsd", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string headerType = GetRsdHeaderType(inputFilePath);
                string outputWavPath = Path.Combine(_selectedPath, Path.GetFileNameWithoutExtension(item.Text) + ".wav");

                switch (headerType)
                {
                    case "RADP":
                        RSDDecoder.ConvertRadpToWav(inputFilePath, outputWavPath);
                        break;

                    case "RSD4PCM":
                        RSDDecoder.ConvertPcmToWav(inputFilePath, outputWavPath);
                        break;

                    case "RSD4PCMB":
                        RSDDecoder.ConvertPcmbToWav(inputFilePath, outputWavPath);
                        break;

                    default:
                        MessageBox.Show($"Unknown RSD header format: '{headerType}' for file: {item.Text}, only RADP, PCM and PCMB are supported.");
                        break;
                }
            }

            RefreshFileList();
        }

        private void button_playpause_Click(object sender, EventArgs e)
        {
            if (_player.IsPlaying)
            {
                _player.Stop();
                return;
            }

            if (list_file.SelectedItems.Count != 1 || string.IsNullOrEmpty(_selectedPath))
            {
                MessageBox.Show("Please select a single sound file to play.", "Playback Error");
                return;
            }

            string selectedFileName = list_file.SelectedItems[0].Text;
            string fullPath = Path.Combine(_selectedPath, selectedFileName);

            _player.IsLooping = checkbox_loop.Checked;

            try
            {
                _player.Play(fullPath);
                button_playpause.Text = "Stop";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to play sound file: {ex.Message}", "Playback Error");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedPath) || list_file.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select at least one .WAV sound file from the list.");
                return;
            }

            foreach (ListViewItem item in list_file.SelectedItems)
            {
                string inputFilePath = Path.Combine(_selectedPath, item.Text);

                // Process only WAV files
                if (!File.Exists(inputFilePath) || !Path.GetExtension(inputFilePath).Equals(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string outputOggPath = Path.Combine(_selectedPath, Path.GetFileNameWithoutExtension(item.Text) + ".ogg");

                try
                {
                    OggConverter.ConvertWavToOgg(inputFilePath, outputOggPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to convert {item.Text}: {ex.Message}", "Conversion Error");
                }
            }

            RefreshFileList();
        }

        private void button_wipeallfiles_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedPath) || !Directory.Exists(_selectedPath))
            {
                MessageBox.Show("No valid directory is currently loaded.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult result = MessageBox.Show(
                "Are you sure you want to delete ALL .RSD and .WAV files from this directory?\nThis action cannot be undone.",
                "Confirm Wipe",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {

                string[] targetExtensions = { ".rsd", ".wav" };

                var filesToDelete = Directory.GetFiles(_selectedPath)
                    .Where(file => targetExtensions.Contains(Path.GetExtension(file).ToLower()))
                    .ToList();

                if (filesToDelete.Count == 0)
                {
                    MessageBox.Show("No .RSD or .WAV files were found to delete.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }


                _player.Stop();

                int deletedCount = 0;
                foreach (string filePath in filesToDelete)
                {
                    try
                    {
                        File.Delete(filePath);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine($"Failed to delete {Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }

                MessageBox.Show($"Successfully wiped {deletedCount} file(s).", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during wipe operation: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {

                RefreshFileList();
            }
        }

        private void list_file_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            _player.Stop();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}