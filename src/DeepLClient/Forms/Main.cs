using DeepLClient.Controls;
using DeepLClient.Functions;
using DeepLClient.Managers;
using Serilog;
using Syncfusion.Windows.Forms;

namespace DeepLClient.Forms
{
    public partial class Main : MetroForm
    {
        private TextPage _text;
        private DocumentsPage _documents;

        private bool _isClosing = false;

        public Main()
        {
            InitializeComponent();
        }

        private async void Main_Load(object sender, EventArgs e)
        {
            try
            {
                // catch all key presses
                KeyPreview = true;
                
                // check for updates
                _ = Task.Run(UpdateManager.Initialise);

                // initialise
                Initialise();

#if DEBUG
                // always show when debugging
                await Task.Delay(250);
                ShowMain(false);
#endif
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "[MAIN] Error on Main_Load: {err}", ex.Message);
                MessageBoxAdv.Show(this, "Something went wrong while launching the app.\r\n\r\nPlease make sure everything's configured correctly, and your internet connection is working.\r\n\r\nIf the problem persists, check your logs and contact the developer.", Variables.MessageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);

                // we're done for
                Close();
            }
        }

        private void BtnAccountInfo_Click(object sender, EventArgs e)
        {
            // show account info dialog
            using var accountInfo = new AccountInfo();
            accountInfo.ShowDialog();
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_isClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            try
            {
                Invoke(new MethodInvoker(delegate
                {
                    Hide();
                    NotifyIcon.Visible = false;
                }));

                Variables.Translator?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "[MAIN] Error on FormClosing: {err}", ex.Message);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private void BtnConfig_Click(object sender, EventArgs e)
        {
            try
            {
                // show configuration dialog
                using var config = new Configuration();
                var result = config.ShowDialog();
                if (result != DialogResult.OK) return;

                // reload
                Initialise();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "[MAIN] Error on BtnConfig_Click: {err}", ex.Message);
                MessageBoxAdv.Show(this, "Something went wrong while processing your configuration.\r\n\r\nPlease make sure everything's configured correctly, and your internet connection is working.\r\n\r\nIf the problem persists, check your logs and contact the developer.", Variables.MessageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void Initialise()
        {
            // check if we have an API key
            if (string.IsNullOrEmpty(Variables.AppSettings.DeepLAPIKey))
            {
                MessageBoxAdv.Show(this, "No API key has been set.\r\n\r\nIn the main window, click the configuration button at the bottom left to set your personal key.", Variables.MessageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                BtnAccount.Enabled = false;
                return;
            }

            // initialise DeepL
            var deepL = await DeepLManager.Initialise();
            if (!deepL.result)
            {
                MessageBoxAdv.Show(this, $"Something went wrong when initialising DeepL.\r\n\r\nPlease make sure everything's configured correctly, and your internet connection is working.\r\n\r\nIf the problem persists, check your logs and contact the developer.", Variables.MessageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                BtnAccount.Enabled = false;
                return;
            }

            // optionally remove the text page
            if (_text != null)
            {
                TabText.Controls.Remove(_text);
                _text.Dispose();
            }

            // optionally remove the documents page
            if (_documents != null)
            {
                TabDocuments.Controls.Remove(_documents);
                _documents.Dispose();
            }

            // load pages
            _text = new TextPage();
            _documents = new DocumentsPage();

            // set controls
            TabText.Controls.Add(_text);
            TabDocuments.Controls.Add(_documents);
        }

        /// <summary>
        /// Show/hide ourself
        /// </summary>
        private async void ShowMain(bool hideIfVisible = true)
        {
            try
            {
                if (!IsHandleCreated) return;
                if (IsDisposed) return;

                if (Visible && hideIfVisible)
                {
                    Hide();
                    return;
                }

                // show interface
                Show();
                await Task.Delay(250);
                BringToFront();
                Activate();

                // set to text (if possible)
                if (_text == null) return;
                if (TranslationTabs.SelectedTab != TabText) TranslationTabs.SelectedTab = TabText;
                ActiveControl = _text.TbSource;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "[MAIN] Error showing UI: {err}", ex.Message);
            }
        }

        /// <summary>
        /// Closes the application after the user agrees
        /// </summary>
        private void Shutdown()
        {
            var confirm = MessageBoxAdv.Show(this, "Are you sure you want to completely exit?", Variables.MessageBoxTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            _isClosing = true;
            Close();
        }
        
        private void NotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) return;
            ShowMain(false);
        }

        /// <summary>
        /// Hides the tray icon
        /// </summary>
        internal void HideTrayIcon()
        {
            try
            {
                if (!IsHandleCreated) return;
                if (IsDisposed) return;

                Invoke(new MethodInvoker(delegate
                {
                    try
                    {
                        NotifyIcon.Visible = true;
                    }
                    catch
                    {
                        // best effort
                    }
                }));
            }
            catch
            {
                // best effort
            }
        }

        /// <summary>
        /// Hides or closes the application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnHide_Click(object sender, EventArgs e)
        {
            // shutdown if ctrl is pressed
            if (ModifierKeys.HasFlag(Keys.Control))
            {
                Shutdown();
                return;
            }

            // just hide
            Hide();
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e) => ShowMain(false);

        private void TsExit_Click(object sender, EventArgs e) => Shutdown();

        private void TsShow_Click(object sender, EventArgs e) => ShowMain(false);
    }
}