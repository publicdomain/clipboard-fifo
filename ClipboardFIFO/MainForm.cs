﻿// <copyright file="MainForm.cs" company="PublicDomain.com">
//     CC0 1.0 Universal (CC0 1.0) - Public Domain Dedication
//     https://creativecommons.org/publicdomain/zero/1.0/legalcode
// </copyright>

namespace ClipboardFIFO
{
    // Directives
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using System.Xml.Serialization;
    using PublicDomain;

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
        /// The current process identifier.
        /// </summary>
        private int currentProcessId;

        /// <summary>
        /// The clipboard owner process identifier.
        /// </summary>
        private uint clipboardOwnerProcessId;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:ClipboardFIFO.MainForm"/> class.
        /// </summary>
        public MainForm()
        {
            // The InitializeComponent() call is required for Windows Forms designer support.
            this.InitializeComponent();

            // Set current process id
            this.currentProcessId = Process.GetCurrentProcess().Id;

            /* Set icons */

            // Set associated icon from exe file
            this.associatedIcon = Icon.ExtractAssociatedIcon(typeof(MainForm).GetTypeInfo().Assembly.Location);

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

                    // TODO Supress exception [Can be improved]
                    try
                    {
                        // Check for text
                        if (Clipboard.ContainsText())
                        {
                            // Set clipboard text
                            var clipboardText = Clipboard.GetText();

                            // Set clipboard owner process id
                            GetWindowThreadProcessId(GetClipboardOwner(), out this.clipboardOwnerProcessId);

                            // Check for length and prevent self-adding
                            if (clipboardText.Length > 0 && this.clipboardOwnerProcessId != this.currentProcessId)
                            {
                                // Add to list
                                this.fifoListBox.Items.Add(Clipboard.GetText());

                                // Rise count
                                this.copyCount++;

                                // Update status
                                this.countToolStripStatusLabel.Text = this.copyCount.ToString();
                            }
                        }
                    }
                    catch
                    {
                        // TODO Let it fall through [Can be logged]
                    }

                    // Halt flow
                    break;

                // CTRL+V && (int)m.WParam == 1 --not compared since it's only one ID
                case WMHOTKEY:

                    /* TODO This block was modified by report [May need revision] */

                    // TODO Supress exception [Can be improved]
                    try
                    {
                        // Check for list items
                        if (this.fifoListBox.Items.Count > 0)
                        {
                            // Prevent list box drawing
                            this.fifoListBox.BeginUpdate();

                            // TODO Separated to variable to prevent the null parameter report via check [Can be handled differently]
                            string textToCopy = this.fifoListBox.Items[0].ToString();

                            // Check there's something to work with
                            if (textToCopy.Length > 0)
                            {
                                // Set clipboard to next item in FIFO order
                                Clipboard.SetText(textToCopy);

                                // Remove last item
                                this.fifoListBox.Items.RemoveAt(0);

                                // Send ^V
                                SendKeys.SendWait("^v");
                            }

                            // Resume list box drawing
                            this.fifoListBox.EndUpdate();
                        }
                    }
                    catch
                    {
                        // TODO Let it fall through [Can be logged]
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
        /// Gets the clipboard owner.
        /// </summary>
        /// <returns>The clipboard owner.</returns>
        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardOwner();

        /// <summary>
        /// Gets the window thread process identifier.
        /// </summary>
        /// <returns>The window thread process identifier.</returns>
        /// <param name="hWnd">H window.</param>
        /// <param name="lpdwProcessId">Lpdw process identifier.</param>
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

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
            // Prevent drawing
            this.fifoListBox.BeginUpdate();

            // Iterate in reverse
            for (int i = this.fifoListBox.Items.Count - 1; i >= 0; i--)
            {
                // Check if selected
                if (this.fifoListBox.GetSelected(i))
                {
                    // Remove
                    this.fifoListBox.Items.RemoveAt(i);
                }
            }

            // Resume drawing
            this.fifoListBox.EndUpdate();
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
            // Reset count
            this.copyCount = 0;

            // Update status 
            this.countToolStripStatusLabel.Text = this.copyCount.ToString();

            // Clear list
            this.fifoListBox.Items.Clear();
        }

        /// <summary>
        /// Handles the exit tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnExitToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Close application
            this.Close();
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
        /// Handles the daily releases at PublicDomainDaily.com tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnDailyReleasesPublicDomainDailycomToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Open current website
            Process.Start("https://publicdomaindaily.com");
        }

        /// <summary>
        /// Handles the original thread DonationCoder.com tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnOriginalThreadDonationCodercomToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Open original thread @ DonationCoder
            Process.Start("https://www.donationcoder.com/forum/index.php?topic=50014.0");
        }

        /// <summary>
        /// Handles the source code GitHub.com tool strip menu item click event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnSourceCodeGithubcomToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Open GitHub
            Process.Start("https://github.com/publicdomain");
        }

        /// <summary>
        /// Handles the about tool strip menu item click.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnAboutToolStripMenuItemClick(object sender, EventArgs e)
        {
            // Set license text
            var licenseText = $"CC0 1.0 Universal (CC0 1.0) - Public Domain Dedication{Environment.NewLine}" +
                $"https://creativecommons.org/publicdomain/zero/1.0/legalcode{Environment.NewLine}{Environment.NewLine}" +
                $"Libraries and icons have separate licenses.{Environment.NewLine}{Environment.NewLine}" +
                $"Arrows icon by Leovinus - Pixabay License{Environment.NewLine}" +
                $"https://pixabay.com/vectors/arrows-next-direction-green-559297/{Environment.NewLine}{Environment.NewLine}" +
                $"Minimize icon by Gregor Cresnar from www.flaticon.com{Environment.NewLine}" +
                $"https://www.flaticon.com/authors/gregor-cresnar{Environment.NewLine}{Environment.NewLine}" +
                $"Patreon icon used according to published brand guidelines{Environment.NewLine}" +
                $"https://www.patreon.com/brand{Environment.NewLine}{Environment.NewLine}" +
                $"GitHub mark icon used according to published logos and usage guidelines{Environment.NewLine}" +
                $"https://github.com/logos{Environment.NewLine}{Environment.NewLine}" +
                $"DonationCoder icon used with permission{Environment.NewLine}" +
                $"https://www.donationcoder.com/forum/index.php?topic=48718{Environment.NewLine}{Environment.NewLine}" +
                $"PublicDomain icon is based on the following source images:{Environment.NewLine}{Environment.NewLine}" +
                $"Bitcoin by GDJ - Pixabay License{Environment.NewLine}" +
                $"https://pixabay.com/vectors/bitcoin-digital-currency-4130319/{Environment.NewLine}{Environment.NewLine}" +
                $"Letter P by ArtsyBee - Pixabay License{Environment.NewLine}" +
                $"https://pixabay.com/illustrations/p-glamour-gold-lights-2790632/{Environment.NewLine}{Environment.NewLine}" +
                $"Letter D by ArtsyBee - Pixabay License{Environment.NewLine}" +
                $"https://pixabay.com/illustrations/d-glamour-gold-lights-2790573/";

            // Set title
            string programTitle = typeof(MainForm).GetTypeInfo().Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;

            // Set version for generating semantic version 
            Version version = typeof(MainForm).GetTypeInfo().Assembly.GetName().Version;

            // Set about form
            var aboutForm = new AboutForm(
                $"About {programTitle}",
                $"{programTitle} v{version.Major}.{version.Minor}.{version.Build}",
                $"Made for: dwilbank{Environment.NewLine}DonationCoder.com{Environment.NewLine}Day #175, Week #26 @ June 2020",
                licenseText,
                this.Icon.ToBitmap());

            // Set about form icon
            aboutForm.Icon = this.associatedIcon;

            // Match topmost
            aboutForm.TopMost = this.TopMost;

            // Show about form
            aboutForm.ShowDialog();
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
