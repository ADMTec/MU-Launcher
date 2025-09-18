using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Management;
using System.Threading;
using System.Windows.Forms;
using IniParser;
using IniParser.Model;
using Launcher.Exile;
using Launcher.Layout;
using Launcher.Properties;
using Microsoft.Win32;

namespace Launcher
{
	public partial class pForm : Form
	{
        private LayoutRuntimeContext layoutContext;
        private readonly List<PictureBox> layoutDynamicButtons = new List<PictureBox>();

        public pForm()
        {
            this.InitializeComponent();
            IniData iniData = new FileIniDataParser().ReadFile("mu.ini");
            string str1 = iniData["LAUNCHER"]["S12"];
            string str2 = iniData["LAUNCHER"]["updateurl"];
            Globals.ServerURL = str2;
            this.webBrowser1.Url = new Uri(str2 + "index.php");
            if (str1 == "1")
                Globals.UseSeason12 = true;
            Globals.pForm = this;
            this.LoadExternalLayout();
            Globals.Caption = this.Text;
        }

        private void LoadExternalLayout()
        {
            this.RemoveLayoutDynamicButtons();
            this.layoutContext = null;
            Globals.LayoutContext = null;
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string layoutDirectory = Path.Combine(baseDirectory, "layout");
                string layoutFile = Path.Combine(layoutDirectory, "launcher.layout.xml");
                if (!File.Exists(layoutFile))
                {
                    return;
                }

                LayoutDefinition definition = LayoutFile.Load(layoutFile);
                LayoutRuntimeContext context = LayoutRuntimeApplier.ApplyLayout(this, definition, layoutFile);
                this.layoutContext = context;
                Globals.LayoutContext = context;
                if (context != null)
                {
                    Globals.Caption = this.Text;
                }
            }
            catch
            {
            }
        }

        private void RemoveLayoutDynamicButtons()
        {
            for (int i = 0; i < this.layoutDynamicButtons.Count; i++)
            {
                PictureBox control = this.layoutDynamicButtons[i];
                if (control == null)
                {
                    continue;
                }
                control.Click -= new EventHandler(this.LayoutDynamicButton_Click);
                control.MouseEnter -= new EventHandler(this.LayoutDynamicButton_MouseEnter);
                control.MouseLeave -= new EventHandler(this.LayoutDynamicButton_MouseLeave);
                control.MouseDown -= new MouseEventHandler(this.LayoutDynamicButton_MouseDown);
                control.MouseUp -= new MouseEventHandler(this.LayoutDynamicButton_MouseUp);
                if (control.Parent == this)
                {
                    this.Controls.Remove(control);
                }
                control.Dispose();
            }
            this.layoutDynamicButtons.Clear();
        }

        internal void RegisterDynamicButtonControl(PictureBox control)
        {
            if (control == null)
            {
                return;
            }

            this.layoutDynamicButtons.Add(control);
        }

        private void pForm_Shown(object sender, EventArgs e)
        {
            Common.ChangeStatus("CONNECTING");
            DateTime now = DateTime.Now;
            do
            {
                Application.DoEvents();
            }
            while (now.AddSeconds(1.0) > DateTime.Now);
            this.BeginInvoke((Delegate)new pForm.DoWorkDelegate(this.DoWorkMethod));
            this.timer1.Interval = 600;
            this.timer1.Start();
        }

        public void DoWorkMethod()
        {
            Networking.CheckNetwork();
        }

        public string GetHDDSerial()
        {
            foreach (ManagementObject managementObject in new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMedia").Get())
            {
                if (managementObject["SerialNumber"] != null)
                    return managementObject["SerialNumber"].ToString();
            }
            return string.Empty;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            pForm._m = new Mutex(true, "#32770");
            Starter.Start();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            if (!Globals.UseSeason12)
            {
                try
                {
                    RegistryKey subKey = Registry.CurrentUser.CreateSubKey("Software\\Webzen\\Mu\\Config");
                    int num = (int)subKey.GetValue("WindowMode");
                    subKey.CreateSubKey("WindowMode");
                    if (num == 1)
                    {
                        subKey.SetValue("WindowMode", (object)0, RegistryValueKind.DWord);
                        if (this.layoutContext == null || !this.layoutContext.SetToggleState(this.pictureBox4, false))
                        {
                            this.pictureBox4.BackgroundImage = (Image)Resources.windowmode_uncheck;
                        }
                    }
                    else
                    {
                        subKey.SetValue("WindowMode", (object)1, RegistryValueKind.DWord);
                        if (this.layoutContext == null || !this.layoutContext.SetToggleState(this.pictureBox4, true))
                        {
                            this.pictureBox4.BackgroundImage = (Image)Resources.windowmode;
                        }
                    }
                    subKey.Close();
                }
                catch
                {
                }
            }
            else
            {
                string str = ".\\\\LauncherOption.if";
                try
                {
                    if (System.IO.File.ReadAllLines(str)[1] == "WindowMode:1")
                    {
                        Common.lineChanger("WindowMode:0", str, 2);
                        if (this.layoutContext == null || !this.layoutContext.SetToggleState(this.pictureBox4, false))
                        {
                            this.pictureBox4.BackgroundImage = (Image)Resources.windowmode_uncheck;
                        }
                    }
                    else
                    {
                        Common.lineChanger("WindowMode:1", str, 2);
                        if (this.layoutContext == null || !this.layoutContext.SetToggleState(this.pictureBox4, true))
                        {
                            this.pictureBox4.BackgroundImage = (Image)Resources.windowmode;
                        }
                    }
                }
                catch
                {
                    if (!System.IO.File.Exists(str))
                    {
                        string[] contents = new string[3]
                        {
              "DevModeIndex:1",
              "WindowMode:1",
              "ID:"
                        };
                        System.IO.File.WriteAllLines(str, contents);
                    }
                    else
                        Common.lineChanger("WindowMode:1", str, 2);
                    if (this.layoutContext == null || !this.layoutContext.SetToggleState(this.pictureBox4, true))
                    {
                        this.pictureBox4.BackgroundImage = (Image)Resources.windowmode;
                    }
                }
            }
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            Opcoes opcoes = new Opcoes();
            int num = (int)opcoes.ShowDialog();
            opcoes.Dispose();
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        internal void LayoutDynamicButton_Click(object sender, EventArgs e)
        {
            if (this.layoutContext == null)
            {
                return;
            }

            PictureBox control = sender as PictureBox;
            if (control == null)
            {
                return;
            }

            LayoutAction action;
            if (this.layoutContext.TryGetAction(control, out action))
            {
                this.ExecuteLayoutAction(action, control);
            }
        }

        internal void LayoutDynamicButton_MouseEnter(object sender, EventArgs e)
        {
            PictureBox control = sender as PictureBox;
            if (control == null)
            {
                return;
            }

            if (this.layoutContext != null && this.layoutContext.SetState(control, SkinVisualState.Hover))
            {
                return;
            }
        }

        internal void LayoutDynamicButton_MouseLeave(object sender, EventArgs e)
        {
            PictureBox control = sender as PictureBox;
            if (control == null)
            {
                return;
            }

            if (this.layoutContext != null && this.layoutContext.SetState(control, SkinVisualState.Normal))
            {
                return;
            }
        }

        internal void LayoutDynamicButton_MouseDown(object sender, MouseEventArgs e)
        {
            PictureBox control = sender as PictureBox;
            if (control == null)
            {
                return;
            }

            if (this.layoutContext != null && this.layoutContext.SetState(control, SkinVisualState.Pressed))
            {
                return;
            }
        }

        internal void LayoutDynamicButton_MouseUp(object sender, MouseEventArgs e)
        {
            PictureBox control = sender as PictureBox;
            if (control == null)
            {
                return;
            }

            if (this.layoutContext != null && this.layoutContext.SetState(control, SkinVisualState.Hover))
            {
                return;
            }
        }

        private void ExecuteLayoutAction(LayoutAction action, PictureBox source)
        {
            if (action.IsEmpty)
            {
                return;
            }

            string name = action.Name;
            string argument = action.Argument;
            try
            {
                if (string.Equals(name, "Start", StringComparison.OrdinalIgnoreCase))
                {
                    this.pictureBox1_Click(this.pictureBox1, EventArgs.Empty);
                }
                else if (string.Equals(name, "Exit", StringComparison.OrdinalIgnoreCase))
                {
                    this.Close();
                }
                else if (string.Equals(name, "Options", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "ShowOptions", StringComparison.OrdinalIgnoreCase))
                {
                    this.pictureBox3_Click(this.pictureBox3, EventArgs.Empty);
                }
                else if (string.Equals(name, "ToggleWindowMode", StringComparison.OrdinalIgnoreCase))
                {
                    this.pictureBox4_Click(this.pictureBox4, EventArgs.Empty);
                }
                else if (string.Equals(name, "ToggleState", StringComparison.OrdinalIgnoreCase))
                {
                    if (this.layoutContext != null && source != null)
                    {
                        this.layoutContext.ToggleState(source);
                    }
                }
                else if (string.Equals(name, "Minimize", StringComparison.OrdinalIgnoreCase))
                {
                    this.WindowState = FormWindowState.Minimized;
                }
                else if (string.Equals(name, "OpenUrl", StringComparison.OrdinalIgnoreCase))
                {
                    this.OpenExternalResource(argument);
                }
                else if (string.Equals(name, "Run", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "Launch", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "OpenFile", StringComparison.OrdinalIgnoreCase))
                {
                    this.StartProcess(argument);
                }
                else if (string.Equals(name, "OpenFolder", StringComparison.OrdinalIgnoreCase))
                {
                    this.StartProcess(argument);
                }
                else if (string.Equals(name, "Message", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(string.IsNullOrEmpty(argument) ? string.Empty : argument, Globals.Caption);
                }
            }
            catch
            {
            }
        }

        private void OpenExternalResource(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }
            try
            {
                Process.Start(url);
            }
            catch
            {
                try
                {
                    string escaped = url.Replace("\"", "\"\"");
                    string arguments = string.Concat("/c start \"\" \"", escaped, "\"");
                    ProcessStartInfo info = new ProcessStartInfo("cmd", arguments);
                    info.CreateNoWindow = true;
                    info.UseShellExecute = false;
                    Process.Start(info);
                }
                catch
                {
                }
            }
        }

        private void StartProcess(string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                return;
            }

            try
            {
                Process.Start(target);
            }
            catch
            {
            }
        }

        private void pForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;
            Common.ReleaseCapture();
            Common.SendMessage(this.Handle, 161, 2, 0);
        }

        private static bool IsSingleInstance()
        {
            try
            {
                Mutex.OpenExisting(pForm.InstanceName);
            }
            catch
            {
                pForm._m = new Mutex(true, pForm.InstanceName);
                return true;
            }
            return false;
        }

        private void pForm_Load(object sender, EventArgs e)
        {
            if (!pForm.IsSingleInstance() && MessageBox.Show("Another Autoupdate is running.\nDo you want to continue?", Globals.Caption, MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                this.Close();
            if (!Globals.UseSeason12)
            {
                try
                {
                    RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Webzen\\Mu\\Config");
                    if (registryKey != null)
                    {
                        object value = registryKey.GetValue("WindowMode");
                        if (value != null)
                        {
                            if ((int)value == 1)
                            {
                                if (this.layoutContext == null || !this.layoutContext.SetToggleState(this.pictureBox4, true))
                                {
                                    this.pictureBox4.BackgroundImage = (Image)Resources.windowmode;
                                }
                            }
                            else if (this.layoutContext == null || !this.layoutContext.SetToggleState(this.pictureBox4, false))
                            {
                                this.pictureBox4.BackgroundImage = (Image)Resources.windowmode_uncheck;
                            }
                        }
                        else
                        {
                            registryKey = Registry.CurrentUser.CreateSubKey("Software\\Webzen\\Mu\\Config");
                            registryKey.CreateSubKey("WindowMode");
                            registryKey.SetValue("WindowMode", (object)0, RegistryValueKind.DWord);
                            if (this.layoutContext == null || !this.layoutContext.SetToggleState(this.pictureBox4, false))
                            {
                                this.pictureBox4.BackgroundImage = (Image)Resources.windowmode_uncheck;
                            }
                        }
                    }
                    else
                    {
                        registryKey = Registry.CurrentUser.CreateSubKey("Software\\Webzen\\Mu\\Config");
                        registryKey.CreateSubKey("WindowMode");
                        registryKey.SetValue("WindowMode", (object)0, RegistryValueKind.DWord);
                        if (this.layoutContext == null || !this.layoutContext.SetToggleState(this.pictureBox4, false))
                        {
                            this.pictureBox4.BackgroundImage = (Image)Resources.windowmode_uncheck;
                        }
                    }
                    registryKey.Close();
                }
                catch
                {
                }
            }
            else
            {
                try
                {
                    string[] lines = System.IO.File.ReadAllLines("LauncherOption.if");
                    if (lines.Length > 1 && lines[1] == "WindowMode:1")
                    {
                        if (this.layoutContext == null || !this.layoutContext.SetToggleState(this.pictureBox4, true))
                        {
                            this.pictureBox4.BackgroundImage = (Image)Resources.windowmode;
                        }
                    }
                    else if (this.layoutContext == null || !this.layoutContext.SetToggleState(this.pictureBox4, false))
                    {
                        this.pictureBox4.BackgroundImage = (Image)Resources.windowmode_uncheck;
                    }
                }
                catch
                {
                }
            }
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.layoutContext != null && this.layoutContext.SetState(this.pictureBox1, SkinVisualState.Pressed))
            {
                return;
            }
            this.pictureBox1.BackgroundImage = (Image)Resources.start_3;
        }

        private void pictureBox1_MouseHover(object sender, EventArgs e)
        {
            if (this.layoutContext != null && this.layoutContext.SetState(this.pictureBox1, SkinVisualState.Hover))
            {
                this.Pic1_Hover = true;
                return;
            }
            this.pictureBox1.BackgroundImage = (Image)Resources.start_2;
            this.Pic1_Hover = true;
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            if (this.layoutContext != null && this.layoutContext.SetState(this.pictureBox1, SkinVisualState.Normal))
            {
                this.Pic1_Hover = false;
                return;
            }
            this.pictureBox1.BackgroundImage = (Image)Resources.start_1;
            this.Pic1_Hover = false;
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.layoutContext != null && this.layoutContext.SetState(this.pictureBox1, SkinVisualState.Normal))
            {
                return;
            }
            this.pictureBox1.BackgroundImage = (Image)Resources.start_1;
        }

        private void pictureBox3_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.layoutContext != null && this.layoutContext.SetState(this.pictureBox3, SkinVisualState.Pressed))
            {
                return;
            }
            this.pictureBox3.BackgroundImage = (Image)Resources.setting_3;
        }

        private void pictureBox3_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.layoutContext != null && this.layoutContext.SetState(this.pictureBox3, SkinVisualState.Normal))
            {
                return;
            }
            this.pictureBox3.BackgroundImage = (Image)Resources.setting_1;
        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {
        }

        private void CopyRightLabel_Click(object sender, EventArgs e)
        {
        }

        private void pictureBox2_MouseDown(object sender, MouseEventArgs e)
        {
            if (this.layoutContext != null && this.layoutContext.SetState(this.pictureBox2, SkinVisualState.Pressed))
            {
                return;
            }
            this.pictureBox2.BackgroundImage = (Image)Resources.exit_3;
        }

        private void pictureBox2_MouseHover(object sender, EventArgs e)
        {
            if (this.layoutContext != null && this.layoutContext.SetState(this.pictureBox2, SkinVisualState.Hover))
            {
                return;
            }
            this.pictureBox2.BackgroundImage = (Image)Resources.exit_2;
        }

        private void pictureBox2_MouseLeave(object sender, EventArgs e)
        {
            if (this.layoutContext != null && this.layoutContext.SetState(this.pictureBox2, SkinVisualState.Normal))
            {
                return;
            }
            this.pictureBox2.BackgroundImage = (Image)Resources.exit_1;
        }

        private void pictureBox2_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.layoutContext != null && this.layoutContext.SetState(this.pictureBox2, SkinVisualState.Normal))
            {
                return;
            }
            this.pictureBox2.BackgroundImage = (Image)Resources.exit_1;
        }

        private void pictureBox3_MouseHover(object sender, EventArgs e)
        {
            if (this.layoutContext != null && this.layoutContext.SetState(this.pictureBox3, SkinVisualState.Hover))
            {
                return;
            }
            this.pictureBox3.BackgroundImage = (Image)Resources.setting_2;
        }

        private void pictureBox3_MouseLeave(object sender, EventArgs e)
        {
            if (this.layoutContext != null && this.layoutContext.SetState(this.pictureBox3, SkinVisualState.Normal))
            {
                return;
            }
            this.pictureBox3.BackgroundImage = (Image)Resources.setting_1;
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            this.webBrowser1.Visible = true;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.Pic1_Hover || !Globals.EnableStartBTN)
                return;
            if (this.layoutContext != null && this.layoutContext.TryBlink(this.pictureBox1, ref this.blink))
            {
                return;
            }
            if (this.blink)
                this.pictureBox1.BackgroundImage = (Image)Resources.start_2;
            else
                this.pictureBox1.BackgroundImage = (Image)Resources.start_1;
            this.blink = !this.blink;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
        }

        private static Mutex _m;

		private static string InstanceName = "#launcheritself";

                private bool Pic1_Hover = false;

                private bool blink = false;

		public delegate void DoWorkDelegate();
	}
}
