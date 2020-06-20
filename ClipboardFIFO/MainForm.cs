// <copyright file="MainForm.cs" company="PublicDomain.com">
//     CC0 1.0 Universal (CC0 1.0) - Public Domain Dedication
//     https://creativecommons.org/publicdomain/zero/1.0/legalcode
// </copyright>

namespace ClipboardFIFO
{
    // Directives
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using System.Xml.Serialization;

    /// <summary>
    /// Description of MainForm.
    /// </summary>
    public partial class MainForm : Form
    {
        /// <summary>
        /// The clipboard update windows message.
        /// </summary>
        private const int WmClipboardUpdate = 0x031D;

        /// <summary>
        /// The mod control.
        /// </summary>
        private const int MODCONTROL = 0x0002; // Changed from MOD_CONTROL for StyleCop

        /// <summary>
        /// The wm hotkey.
        /// </summary>
        private const int WMHOTKEY = 0x0312; // Changed from  for StyleCop

        /// <summary>
        /// The copy count.
        /// </summary>
        private int copyCount = 0;

        /// <summary>
        /// The associated icon.
        /// </summary>
        private Icon associatedIcon = null;

        /// <summary>
        /// The settings data.
        /// </summary>
        private SettingsData settingsData = new SettingsData();

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ClipboardFIFO.MainForm"/> class.
        /// </summary>
        public MainForm()
        {
            // The InitializeComponent() call is required for Windows Forms designer support.
            this.InitializeComponent();

            /* Set icons */

            // Set associated icon from exe file
            this.associatedIcon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            // Set daily releases icon
            this.dailyReleasesPublicDomainDailycomToolStripMenuItem.Image = this.associatedIcon.ToBitmap();

            /* Process settings */

            // Check for settings data file
            if (!File.Exists("SettingsData.txt"))
            {
                // Not present, assume first run and create it
                this.SaveSettingsData();
            }

            // Populate settings data
            this.settingsData = this.LoadSettingsData();

            /* Clipboard */

            // Add clipboard listener
            AddClipboardFormatListener(this.Handle);

            /* Initial option processing */

            // Always on top
            if (this.settingsData.AlwaysOnTop)
            {
                // Click it
                this.alwaysOnTopToolStripMenuItem.PerformClick();
            }

            /*// Hide close button
            if (this.settingsData.HideCloseButton)
            {
                // Click it
                this.hideCloseButtonToolStripMenuItem.PerformClick();
            }*/
        }

        /// <summary>
        /// The Window procedure.
        /// </summary>
        /// <param name="m">The message.</param>
        protected override void WndProc(ref Message m)
        {
            // Test incoming message
            switch (m.Msg)
            {
                // Check for clipboard update
                case WmClipboardUpdate:

                    // Check for copied text
                    if (Clipboard.ContainsText())
                    {
                        // Add to list
                        this.fifoListBox.Items.Add(Clipboard.GetText());

                        // Rise count
                        this.copyCount++;

                        // Update status
                        this.countToolStripStatusLabel.Text = this.copyCount.ToString();
                    }

                    // Halt flow
                    break;

                // CTRL+V && (int)m.WParam == 1 --not compared since it's only one ID
                case WMHOTKEY:

                    // Check for list items
                    if (this.fifoListBox.Items.Count > 0)
                    {
                        // Unregister hotkey
                        UnregisterHotKey(this.Handle, 1);

                        // Remove cipboard listener
                        RemoveClipboardFormatListener(this.Handle);

                        // Set clipboard to next item in FIFO order
                        Clipboard.SetText(this.fifoListBox.Items[0].ToString());

                        // Send ^V
                        SendKeys.SendWait("^v");

                        // Remove last item
                        this.fifoListBox.Items.RemoveAt(0);

                        // Add clipboard listener
                        AddClipboardFormatListener(this.Handle);

                        // Register hotkey again
                        RegisterHotKey(this.Handle, 1, MODCONTROL, (int)Keys.V);
                    }

                    // Halt flow
                    break;

                // Continue processing
                default:

                    // Pass message
                    base.WndProc(ref m);

                    // Halt flow
                    break;
            }
        }

        /// <summary>
        /// Adds the clipboard format listener.
        /// </summary>
        /// <returns><c>true</c>, if clipboard format listener was added, <c>false</c> otherwise.</returns>
        /// <param name="hwnd">The handle.</param>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        /// <summary>
        /// Removes the clipboard format listener.
        /// </summary>
        /// <returns><c>true</c>, if clipboard format listener was removed, <c>false</c> otherwise.</returns>
        /// <param name="hwnd">The handle.</param>c
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        /// <summary>
        /// Registers the hot key.
        /// </summary>
        /// <returns><c>true</c>, if hot key was registered, <c>false</c> otherwise.</returns>
        /// <param name="handle">The window handle.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="modifiers">The modifiers.</param>
        /// <param name="vk">The virtual key.</param>
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr handle, int id, int modifiers, int vk);

        /// <summary>
        /// Unregisters the hot key.
        /// </summary>
        /// <returns><c>true</c>, if the hot key was unregistered, <c>false</c> otherwise.</returns>
        /// <param name="handle">The window handle.</param>
        /// <param name="id">The identifier.</param>
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr handle, int id);

        /// <summary>
        /// Handles the pause resume button click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnPauseResumeButtonClick(object sender, EventArgs e)
        {
            // Check if must pause
            if (this.pauseResumeButton.Text.StartsWith("&P", StringComparison.InvariantCulture))
            {
                // Remove clipboard listener
                RemoveClipboardFormatListener(this.Handle);

                // Unregister hotkey
                UnregisterHotKey(this.Handle, 1);

                // Update monitor status
                this.monitorGroupBox.Text = "Monitor is: INACTIVE";

                // Set button text
                this.pauseResumeButton.Text = "&Resume";
            }
            else
            {
                // Add clipboard listener
                AddClipboardFormatListener(this.Handle);

                // Register hotkey again
                RegisterHotKey(this.Handle, 1, MODCONTROL, (int)Keys.V);

                // Update monitor status
                this.monitorGroupBox.Text = "Monitor is: ACTIVE";

                // Set button text
                this.pauseResumeButton.Text = "&Pause";
            }
        }

        /// <summary>
        /// Saves the settings data.
        /// </summary>
        private void SaveSettingsData()
        {
            // Use stream writer
            using (StreamWriter streamWriter = new StreamWriter("SettingsData.txt", false))
            {
                // Set xml serialzer
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(SettingsData));

                // Serialize settings data
                xmlSerializer.Serialize(streamWriter, this.settingsData);
            }
        }

        /// <summary>
        /// Loads the settings data.
        /// </summary>
        /// <returns>The settings data.</returns>ing
        private SettingsData LoadSettingsData()
        {
            // Use file stream
            using (FileStream fileStream = File.OpenRead("SettingsData.txt"))
            {
                // Set xml serialzer
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(SettingsData));

                // Return populated settings data
                return xmlSerializer.Deserialize(fileStream) as SettingsData;
            }
        }

        /// <summary>
        /// Handles the minimize tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnMinimizeToolStripMenuItemClick(object sender, EventArgs e)
        {
            // TODO Add code
        }

        /// <summary>
        /// Handles the delete selected button click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnDeleteSelectedButtonClick(object sender, EventArgs e)
        {
            // TODO Add code
        }

        /// <summary>
        /// Handles the clear button click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnClearButtonClick(object sender, EventArgs e)
        {
            // Clear FIFO list box
            this.fifoListBox.Items.Clear();
        }

        /// <summary>
        /// Handles the new tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnNewToolStripMenuItemClick(object sender, EventArgs e)
        {
            // TODO Add code
        }

        /// <summary>
        /// Handles the exit tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnExitToolStripMenuItemClick(object sender, EventArgs e)
        {
            // TODO Add code
        }

        /// <summary>
        /// Handles the always on top tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnAlwaysOnTopToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Toggle check state
            this.alwaysOnTopToolStripMenuItem.Checked = !this.alwaysOnTopToolStripMenuItem.Checked;

            // Set topmost state
            this.TopMost = this.alwaysOnTopToolStripMenuItem.Checked;

            // Save setting
            this.settingsData.AlwaysOnTop = this.alwaysOnTopToolStripMenuItem.Checked;
        }

        /// <summary>
        /// Handles the hide close button tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnHideCloseButtonToolStripMenuItemClick(object sender, EventArgs e)
        {
            // TODO Add code
        }

        /// <summary>
        /// Handles the daily releases public domain dailycom tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnDailyReleasesPublicDomainDailycomToolStripMenuItemClick(object sender, EventArgs e)
        {
            // TODO Add code
        }

        /// <summary>
        /// Handles the original thread donation codercom tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnOriginalThreadDonationCodercomToolStripMenuItemClick(object sender, EventArgs e)
        {
            // TODO Add code
        }

        /// <summary>
        /// Handles the source code githubcom tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnSourceCodeGithubcomToolStripMenuItemClick(object sender, EventArgs e)
        {
            // TODO Add code
        }

        /// <summary>
        /// Handles the about tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnAboutToolStripMenuItemClick(object sender, EventArgs e)
        {
            // TODO Add code
        }

        /// <summary>
        /// Handles the main form load event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnMainFormLoad(object sender, EventArgs e)
        {
            // Register hotkey
            RegisterHotKey(this.Handle, 1, MODCONTROL, (int)Keys.V);
        }

        /// <summary>
        /// Handles the main form form closing event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnMainFormFormClosing(object sender, FormClosingEventArgs e)
        {
            // Check if active
            if (this.pauseResumeButton.Text.StartsWith("&P", StringComparison.InvariantCulture))
            {
                // Remove clipboard listener
                RemoveClipboardFormatListener(this.Handle);

                // Unregister hotkey
                UnregisterHotKey(this.Handle, 1);
            }
        }
    }
}
