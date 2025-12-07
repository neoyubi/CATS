using System;
using System.Diagnostics;
using System.Windows.Forms;
using MultiCarrierManager.Controls;

namespace MultiCarrierManager.CATS
{
    public class CatSitter
    {
        private TextBox output;
        private CATSForm form;
        private Process process;
        private Label countdownLabel;
        private Label etaLabel;
        private bool preJumpAlertShown;
        private NotifyIcon notifyIcon;
        private FocusWatchdog focusWatchdog;
        private string currentState;
        private bool isNavigating;

        public bool isRunning { get; set; }
        public bool IsPaused => focusWatchdog?.IsPaused ?? false;

        public CatSitter(TextBox output, CATSForm form, Label countdownLabel, Label etaLabel)
        {
            this.output = output;
            this.countdownLabel = countdownLabel;
            this.form = form;
            this.etaLabel = etaLabel;
            InitializeNotifyIcon();
            InitializeFocusWatchdog();
        }

        private void InitializeNotifyIcon()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = form.Icon;
            notifyIcon.Visible = false;
            notifyIcon.BalloonTipTitle = "CATS Alert";
            notifyIcon.BalloonTipIcon = ToolTipIcon.Warning;
        }

        private void InitializeFocusWatchdog()
        {
            focusWatchdog = new FocusWatchdog(form);
            focusWatchdog.FocusLost += FocusWatchdog_FocusLost;
            focusWatchdog.FocusRestored += FocusWatchdog_FocusRestored;
        }

        private void FocusWatchdog_FocusLost(object sender, FocusLostEventArgs e)
        {
            if (form.InvokeRequired)
            {
                form.Invoke(new Action(() => FocusWatchdog_FocusLost(sender, e)));
                return;
            }

            form.UpdateStatusIndicator(StatusIndicator.StatusState.Idle, "PAUSED - Focus lost");
            output.AppendText($"Focus lost during: {e.LastState}. Waiting for Elite Dangerous..." + Environment.NewLine);
            Program.logger.Log("FocusLost:" + e.LastState);
        }

        private void FocusWatchdog_FocusRestored(object sender, EventArgs e)
        {
            if (form.InvokeRequired)
            {
                form.Invoke(new Action(() => FocusWatchdog_FocusRestored(sender, e)));
                return;
            }

            output.AppendText("Focus restored. Resuming..." + Environment.NewLine);
            Program.logger.Log("FocusRestored");

            if (!string.IsNullOrEmpty(currentState))
            {
                form.UpdateStatusIndicator(StatusIndicator.StatusState.Plotting, $"Resumed: {currentState}");
            }
        }

        private void SetNavigationState(string state, bool navigating)
        {
            currentState = state;
            isNavigating = navigating;
            if (navigating)
            {
                focusWatchdog.UpdateState(state);
            }
        }

        public string finalSystem { get; set; }
        private string nextSystem;


        public void run_cmd()
        {
            output.Text = "";
            process = new Process();
            process.StartInfo.FileName = "CATS\\pyinstaller\\TraversalSystem\\TraversalSystem.exe";
            process.StartInfo.WorkingDirectory = "CATS";
            // The old way of executing the Python script, using a Python installation to execute instead of compiling
            // process.StartInfo.FileName = "CATS\\Python39\\python.exe";
            // process.StartInfo.WorkingDirectory = "CATS";
            // process.StartInfo.Arguments = "-u main.py";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            // Monitor the process for exit
            process.EnableRaisingEvents = true;
            process.Exited += new EventHandler((s, e) =>
            {
                if (form.InvokeRequired)
                {
                    form.Invoke(new Action(() => form.stopButton_Click(s, e)));
                }
                else
                {
                    form.stopButton_Click(s, e);
                }
                Program.logger.Log("TraversalExitCode=" + process.ExitCode);
            });
            process.OutputDataReceived += new DataReceivedEventHandler((s2, e2) =>
            {
                if (e2.Data == null) return;

                // Use Invoke to update UI from background thread
                if (form.InvokeRequired)
                {
                    form.Invoke(new Action(() => ProcessOutputLine(e2.Data)));
                }
                else
                {
                    ProcessOutputLine(e2.Data);
                }
            });
            process.ErrorDataReceived += new DataReceivedEventHandler((s2, e2) =>
            {
                if (e2.Data == null) return;

                // Also capture stderr
                if (form.InvokeRequired)
                {
                    form.Invoke(new Action(() => ProcessOutputLine("[ERR] " + e2.Data)));
                }
                else
                {
                    ProcessOutputLine("[ERR] " + e2.Data);
                }
            });
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            Program.logger.LogOutput("Traversal System script started");
        }

        public void StartFocusMonitoring()
        {
            focusWatchdog.StartMonitoring(currentState);
        }

        public void StopFocusMonitoring()
        {
            focusWatchdog.StopMonitoring();
        }

        private void ProcessOutputLine(string line)
        {
            if (int.TryParse(line, out int remaining))
            {
                countdownLabel.Text = "Current jump: " + TimeSpan.FromSeconds(remaining).ToString(@"hh\:mm\:ss");

                if (Program.settings.PreJumpAlert && remaining == 15 && !preJumpAlertShown)
                {
                    preJumpAlertShown = true;
                    ShowPreJumpAlert();
                }
                else if (remaining > 15)
                {
                    preJumpAlertShown = false;
                }

                return;
            }

            try
            {
                output.AppendText(line + Environment.NewLine);
                if (!line.StartsWith("journal_directory=")) Program.logger.LogCats(line);

                switch (line)
                {
                    case "Beginning in 5...":
                        form.Text = $"CATS | En route to {finalSystem} | Next stop: {nextSystem} | Starting up...";
                        form.UpdateStatusIndicator(StatusIndicator.StatusState.Starting);
                        SetNavigationState("Starting up", true);
                        StartFocusMonitoring();
                        break;
                    case string s when s.StartsWith("Next stop"):
                        nextSystem = line.Split(':')[1].Remove(0, 1);
                        form.Text = $"CATS | En route to {finalSystem} | Next stop: {nextSystem} | Navigating menus...";
                        form.UpdateStatusIndicator(StatusIndicator.StatusState.Plotting, $"Next: {nextSystem}");
                        SetNavigationState($"Plotting to {nextSystem}", true);
                        break;
                    case string s when s.StartsWith("Navigation complete"):
                        form.Text = $"CATS | En route to {finalSystem} | Next stop: {nextSystem} | Counting down...";
                        form.UpdateStatusIndicator(StatusIndicator.StatusState.Waiting, $"Waiting for jump to {nextSystem}");
                        SetNavigationState($"Waiting for jump to {nextSystem}", false);
                        StopFocusMonitoring();
                        break;
                    case "Jumping!":
                        form.Text = $"CATS | En route to {finalSystem} | Next stop: {nextSystem} | In hyperspace...";
                        form.UpdateStatusIndicator(StatusIndicator.StatusState.Jumping, "In hyperspace!");
                        SetNavigationState("In hyperspace", false);
                        break;
                    case "Jump complete!":
                        form.Text = $"CATS | En route to {finalSystem} | Next stop: {nextSystem} | Cooling down...";
                        form.UpdateStatusIndicator(StatusIndicator.StatusState.CoolingDown);
                        SetNavigationState("Cooling down", false);
                        break;
                    case "Restocking tritium...":
                        form.Text = $"CATS | En route to {finalSystem} | Next stop: {nextSystem} | Restocking tritium...";
                        form.UpdateStatusIndicator(StatusIndicator.StatusState.Refueling);
                        SetNavigationState("Restocking tritium", true);
                        StartFocusMonitoring();
                        break;
                    case "Tritium successfully refuelled":
                        form.Text = $"CATS | En route to {finalSystem} | Next stop: {nextSystem} | Cooling down...";
                        form.UpdateStatusIndicator(StatusIndicator.StatusState.CoolingDown);
                        SetNavigationState("Cooling down", false);
                        StopFocusMonitoring();
                        break;
                    case "Route complete!":
                        form.Text = "Carrier Administration and Traversal System (CATS)";
                        form.UpdateStatusIndicator(StatusIndicator.StatusState.Complete, "Route complete!");
                        SetNavigationState(null, false);
                        StopFocusMonitoring();
                        break;
                    case string s when s.StartsWith("ETA:"):
                        etaLabel.Text = line;
                        break;
                    case string s when s.StartsWith("alert:"):
                        string alert = line.Split(':')[1];
                        MessageBox.Show(alert, "Alert", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        break;
                }
            }
            catch (Exception)
            {
                Program.logger.LogOutput("Exception while writing to console, possible force CATS process kill");
            }
        }

        private void ShowPreJumpAlert()
        {
            FlashWindow.Flash(form);
            System.Media.SystemSounds.Exclamation.Play();

            form.Invoke(new Action(() =>
            {
                AlertOverlay overlay = new AlertOverlay(
                    "NAVIGATION IMMINENT",
                    $"Carrier jump to {nextSystem} will be plotted in 15 seconds.\nPlease do not navigate away during processing.",
                    5
                );
                overlay.ShowAlert();
            }));
        }

        public void close()
        {
            try
            {
                StopFocusMonitoring();
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                focusWatchdog?.Dispose();
                process.Kill();
                process.Close();
            }
            catch (Exception) { }
        }

        public void TestPreJumpAlert()
        {
            nextSystem = "Sagittarius A*";
            Timer delayTimer = new Timer();
            delayTimer.Interval = 3000;
            delayTimer.Tick += (s, e) =>
            {
                delayTimer.Stop();
                delayTimer.Dispose();
                ShowPreJumpAlert();
            };
            delayTimer.Start();
        }
    }
}
