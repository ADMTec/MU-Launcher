using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Launcher.Layout
{
    public enum SkinVisualState
    {
        Normal,
        Hover,
        Pressed,
        Disabled,
        Checked,
        Unchecked,
        Blink
    }

    public struct LayoutAction
    {
        public string Name;
        public string Argument;

        public LayoutAction(string name, string argument)
        {
            this.Name = name;
            this.Argument = argument;
        }

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(this.Name); }
        }
    }

    internal class SkinImageSet
    {
        public Image Normal;
        public Image Hover;
        public Image Pressed;
        public Image Disabled;
        public Image Checked;
        public Image Unchecked;
        public Image Blink;

        public bool Apply(PictureBox box, SkinVisualState state)
        {
            Image image = this.GetImage(state);
            if (image == null)
            {
                return false;
            }

            box.BackgroundImage = image;
            return true;
        }

        public Image GetImage(SkinVisualState state)
        {
            switch (state)
            {
                case SkinVisualState.Normal:
                    return this.Normal ?? this.Hover ?? this.Pressed ?? this.Disabled ?? this.Checked ?? this.Unchecked ?? this.Blink;
                case SkinVisualState.Hover:
                    return this.Hover;
                case SkinVisualState.Pressed:
                    return this.Pressed;
                case SkinVisualState.Disabled:
                    return this.Disabled;
                case SkinVisualState.Checked:
                    return this.Checked;
                case SkinVisualState.Unchecked:
                    return this.Unchecked;
                case SkinVisualState.Blink:
                    return this.Blink;
            }

            return null;
        }
    }

    public class LayoutRuntimeContext
    {
        private readonly Dictionary<string, Control> controlsByName;
        private readonly Dictionary<string, SkinImageSet> skinsByName;
        private readonly Dictionary<PictureBox, SkinImageSet> skinsByControl;
        private readonly Dictionary<Control, LayoutAction> actions;
        private readonly Dictionary<PictureBox, bool> toggleStates;

        internal LayoutRuntimeContext()
        {
            this.controlsByName = new Dictionary<string, Control>(StringComparer.OrdinalIgnoreCase);
            this.skinsByName = new Dictionary<string, SkinImageSet>(StringComparer.OrdinalIgnoreCase);
            this.skinsByControl = new Dictionary<PictureBox, SkinImageSet>();
            this.actions = new Dictionary<Control, LayoutAction>();
            this.toggleStates = new Dictionary<PictureBox, bool>();
        }

        internal void RegisterControl(Control control)
        {
            if (control == null)
            {
                return;
            }

            if (!this.controlsByName.ContainsKey(control.Name))
            {
                this.controlsByName.Add(control.Name, control);
            }
        }

        internal void RegisterSkin(string name, PictureBox control, SkinImageSet skin, bool? initialToggle)
        {
            if (control == null || skin == null)
            {
                return;
            }

            if (!this.skinsByName.ContainsKey(name))
            {
                this.skinsByName.Add(name, skin);
            }
            this.skinsByControl[control] = skin;
            if (initialToggle.HasValue)
            {
                this.toggleStates[control] = initialToggle.Value;
            }
        }

        internal void RegisterAction(PictureBox control, LayoutAction action)
        {
            if (control == null)
            {
                return;
            }

            this.actions[control] = action;
        }

        public bool SetState(string name, SkinVisualState state)
        {
            Control control;
            if (!this.controlsByName.TryGetValue(name, out control))
            {
                return false;
            }

            PictureBox pictureBox = control as PictureBox;
            if (pictureBox == null)
            {
                return false;
            }

            return this.SetState(pictureBox, state);
        }

        public bool SetState(PictureBox control, SkinVisualState state)
        {
            SkinImageSet skin;
            if (!this.skinsByControl.TryGetValue(control, out skin))
            {
                return false;
            }

            bool applied = skin.Apply(control, state);
            if (applied && (state == SkinVisualState.Checked || state == SkinVisualState.Unchecked))
            {
                this.toggleStates[control] = state == SkinVisualState.Checked;
            }
            return applied;
        }

        public bool ToggleState(PictureBox control)
        {
            SkinImageSet skin;
            if (!this.skinsByControl.TryGetValue(control, out skin))
            {
                return false;
            }

            bool isChecked;
            if (!this.toggleStates.TryGetValue(control, out isChecked))
            {
                isChecked = false;
            }

            isChecked = !isChecked;
            this.toggleStates[control] = isChecked;
            SkinVisualState target = isChecked ? SkinVisualState.Checked : SkinVisualState.Unchecked;
            if (!skin.Apply(control, target))
            {
                skin.Apply(control, SkinVisualState.Normal);
            }
            return true;
        }

        public bool SetToggleState(PictureBox control, bool isChecked)
        {
            SkinImageSet skin;
            if (!this.skinsByControl.TryGetValue(control, out skin))
            {
                return false;
            }

            this.toggleStates[control] = isChecked;
            SkinVisualState state = isChecked ? SkinVisualState.Checked : SkinVisualState.Unchecked;
            if (!skin.Apply(control, state))
            {
                skin.Apply(control, SkinVisualState.Normal);
            }
            return true;
        }

        public bool TryBlink(PictureBox control, ref bool blinkFlag)
        {
            SkinImageSet skin;
            if (!this.skinsByControl.TryGetValue(control, out skin))
            {
                return false;
            }

            if (skin.Blink == null)
            {
                return false;
            }

            if (blinkFlag)
            {
                skin.Apply(control, SkinVisualState.Normal);
            }
            else
            {
                skin.Apply(control, SkinVisualState.Blink);
            }

            blinkFlag = !blinkFlag;
            return true;
        }

        public bool TryGetAction(Control control, out LayoutAction action)
        {
            return this.actions.TryGetValue(control, out action);
        }
    }

    public static class LayoutRuntimeApplier
    {
        public static LayoutRuntimeContext ApplyLayout(pForm form, LayoutDefinition layout, string layoutPath)
        {
            if (layout == null || form == null)
            {
                return null;
            }

            LayoutRuntimeContext context = new LayoutRuntimeContext();
            RegisterControls(form, context);

            string baseDirectory = Path.GetDirectoryName(layoutPath);
            string assetDirectory = baseDirectory;
            if (!string.IsNullOrEmpty(layout.AssetDirectoryName))
            {
                string candidate = Path.Combine(baseDirectory, layout.AssetDirectoryName);
                if (Directory.Exists(candidate))
                {
                    assetDirectory = candidate;
                }
            }

            ApplyFormSettings(form, layout.Form, assetDirectory);
            ApplyControls(form, layout, context, assetDirectory);
            ApplyButtons(form, layout, context, assetDirectory);
            ApplyDynamicButtons(form, layout, context, assetDirectory);
            return context;
        }

        private static void RegisterControls(Control parent, LayoutRuntimeContext context)
        {
            context.RegisterControl(parent);
            foreach (Control child in parent.Controls)
            {
                RegisterControls(child, context);
            }
        }

        private static void ApplyFormSettings(pForm form, FormLayout layout, string assetDirectory)
        {
            if (layout == null)
            {
                return;
            }

            if (!layout.ClientSize.IsEmpty)
            {
                form.ClientSize = layout.ClientSize;
            }

            Image background = LoadImage(assetDirectory, layout.BackgroundImage);
            if (background != null)
            {
                form.BackgroundImage = background;
            }

            if (!string.IsNullOrEmpty(layout.Caption))
            {
                form.Text = layout.Caption;
            }

            if (layout.TransparencyKey.HasValue)
            {
                form.TransparencyKey = layout.TransparencyKey.Value;
            }

            if (!string.IsNullOrEmpty(layout.Icon))
            {
                string iconPath = Path.Combine(assetDirectory, layout.Icon);
                if (File.Exists(iconPath))
                {
                    try
                    {
                        form.Icon = new Icon(iconPath);
                    }
                    catch
                    {
                    }
                }
            }

            if (layout.ShowIcon.HasValue)
            {
                form.ShowIcon = layout.ShowIcon.Value;
            }
        }

        private static void ApplyControls(pForm form, LayoutDefinition layout, LayoutRuntimeContext context, string assetDirectory)
        {
            for (int i = 0; i < layout.Controls.Count; i++)
            {
                LayoutControl definition = layout.Controls[i];
                if (definition.Name == null || definition.Name.Length == 0)
                {
                    continue;
                }

                Control[] matches = form.Controls.Find(definition.Name, true);
                if (matches == null || matches.Length == 0)
                {
                    continue;
                }

                Control control = matches[0];
                ApplyControlProperties(control, definition, assetDirectory);
                context.RegisterControl(control);
            }
        }

        private static void ApplyButtons(pForm form, LayoutDefinition layout, LayoutRuntimeContext context, string assetDirectory)
        {
            for (int i = 0; i < layout.ImageButtons.Count; i++)
            {
                ImageButtonDefinition button = layout.ImageButtons[i];
                if (button.Target == null || button.Target.Length == 0)
                {
                    continue;
                }

                Control[] matches = form.Controls.Find(button.Target, true);
                if (matches == null || matches.Length == 0)
                {
                    continue;
                }

                PictureBox pictureBox = matches[0] as PictureBox;
                if (pictureBox == null)
                {
                    continue;
                }

                SkinImageSet skin = LoadSkin(assetDirectory, button.Visuals);
                context.RegisterSkin(button.Target, pictureBox, skin, button.Checkable ? (bool?)false : null);
                if (!pictureBox.Enabled)
                {
                    if (!context.SetState(pictureBox, SkinVisualState.Disabled))
                    {
                        context.SetState(pictureBox, SkinVisualState.Normal);
                    }
                }
                else
                {
                    context.SetState(pictureBox, SkinVisualState.Normal);
                }
            }
        }

        private static void ApplyDynamicButtons(pForm form, LayoutDefinition layout, LayoutRuntimeContext context, string assetDirectory)
        {
            for (int i = 0; i < layout.DynamicButtons.Count; i++)
            {
                DynamicButtonDefinition definition = layout.DynamicButtons[i];
                PictureBox control = new PictureBox();
                control.Name = string.IsNullOrEmpty(definition.Name) ? ("dynamicButton" + i.ToString()) : definition.Name;
                control.BackColor = Color.Transparent;
                control.Location = definition.Location;
                control.Size = definition.Size;
                control.Visible = definition.Visible;
                control.Enabled = definition.Enabled;
                control.Cursor = Cursors.Hand;
                control.BackgroundImageLayout = ParseLayout(definition.BackgroundLayout);
                if (definition.SizeMode.HasValue)
                {
                    control.SizeMode = definition.SizeMode.Value;
                }
                else
                {
                    control.SizeMode = PictureBoxSizeMode.Normal;
                }

                SkinImageSet skin = LoadSkin(assetDirectory, definition.Visuals);
                if (!context.SetState(control, SkinVisualState.Normal))
                {
                    Image normal = skin.GetImage(SkinVisualState.Normal);
                    if (normal != null)
                    {
                        control.BackgroundImage = normal;
                    }
                }

                form.Controls.Add(control);
                control.BringToFront();
                form.RegisterDynamicButtonControl(control);

                control.Click += new EventHandler(form.LayoutDynamicButton_Click);
                control.MouseEnter += new EventHandler(form.LayoutDynamicButton_MouseEnter);
                control.MouseLeave += new EventHandler(form.LayoutDynamicButton_MouseLeave);
                control.MouseDown += new MouseEventHandler(form.LayoutDynamicButton_MouseDown);
                control.MouseUp += new MouseEventHandler(form.LayoutDynamicButton_MouseUp);

                context.RegisterControl(control);
                context.RegisterSkin(control.Name, control, skin, definition.Checkable ? (bool?)false : null);
                context.RegisterAction(control, new LayoutAction(definition.Action, definition.Argument));
                context.SetState(control, SkinVisualState.Normal);
            }
        }

        private static void ApplyControlProperties(Control control, LayoutControl definition, string assetDirectory)
        {
            control.Location = definition.Location;
            if (!definition.Size.IsEmpty)
            {
                control.Size = definition.Size;
            }

            if (definition.Visible.HasValue)
            {
                control.Visible = definition.Visible.Value;
            }

            if (definition.Enabled.HasValue)
            {
                control.Enabled = definition.Enabled.Value;
            }

            Label label = control as Label;
            if (label != null)
            {
                if (definition.AutoSize.HasValue)
                {
                    label.AutoSize = definition.AutoSize.Value;
                }
                if (!string.IsNullOrEmpty(definition.Text))
                {
                    label.Text = definition.Text;
                }
                if (definition.ForeColor.HasValue)
                {
                    label.ForeColor = definition.ForeColor.Value;
                }
                if (definition.BackColor.HasValue)
                {
                    label.BackColor = definition.BackColor.Value;
                }
                if (definition.Font != null)
                {
                    try
                    {
                        label.Font = definition.Font.ToFont();
                    }
                    catch
                    {
                    }
                }
                if (definition.TextAlign.HasValue)
                {
                    label.TextAlign = definition.TextAlign.Value;
                }
            }

            PictureBox pictureBox = control as PictureBox;
            if (pictureBox != null)
            {
                if (definition.SizeMode.HasValue)
                {
                    pictureBox.SizeMode = definition.SizeMode.Value;
                }
                pictureBox.BackgroundImageLayout = ParseLayout(definition.BackgroundLayout);
                Image background = LoadImage(assetDirectory, definition.BackgroundImage);
                if (background != null)
                {
                    pictureBox.BackgroundImage = background;
                }
                Image foreground = LoadImage(assetDirectory, definition.Image);
                if (foreground != null)
                {
                    pictureBox.Image = foreground;
                }
            }

            WebBrowser browser = control as WebBrowser;
            if (browser != null)
            {
                if (!string.IsNullOrEmpty(definition.Url))
                {
                    try
                    {
                        browser.Url = new Uri(definition.Url, UriKind.RelativeOrAbsolute);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static ImageLayout ParseLayout(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return ImageLayout.None;
            }

            try
            {
                return (ImageLayout)Enum.Parse(typeof(ImageLayout), value, true);
            }
            catch
            {
                return ImageLayout.None;
            }
        }

        private static SkinImageSet LoadSkin(string baseDirectory, ButtonVisuals visuals)
        {
            SkinImageSet skin = new SkinImageSet();
            skin.Normal = LoadImage(baseDirectory, visuals.Normal);
            skin.Hover = LoadImage(baseDirectory, visuals.Hover);
            skin.Pressed = LoadImage(baseDirectory, visuals.Pressed);
            skin.Disabled = LoadImage(baseDirectory, visuals.Disabled);
            skin.Checked = LoadImage(baseDirectory, visuals.Checked);
            skin.Unchecked = LoadImage(baseDirectory, visuals.Unchecked);
            skin.Blink = LoadImage(baseDirectory, visuals.Blink);
            return skin;
        }

        internal static Image LoadImage(string baseDirectory, string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return null;
            }

            string path = Path.Combine(baseDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        byte[] buffer = new byte[4096];
                        int read;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            memoryStream.Write(buffer, 0, read);
                        }
                        memoryStream.Position = 0L;
                        return Image.FromStream(memoryStream);
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
