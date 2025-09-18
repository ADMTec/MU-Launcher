using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Xml;

namespace Launcher.Layout
{
    public enum LayoutControlType
    {
        Unknown,
        Label,
        PictureBox,
        WebBrowser,
        Panel
    }

    public class LayoutDefinition
    {
        private string assetDirectoryName;
        private string sourcePath;
        private FormLayout form;
        private readonly List<LayoutControl> controls;
        private readonly List<ImageButtonDefinition> imageButtons;
        private readonly List<DynamicButtonDefinition> dynamicButtons;

        public LayoutDefinition()
        {
            this.controls = new List<LayoutControl>();
            this.imageButtons = new List<ImageButtonDefinition>();
            this.dynamicButtons = new List<DynamicButtonDefinition>();
            this.form = new FormLayout();
        }

        public string AssetDirectoryName
        {
            get { return this.assetDirectoryName; }
            set { this.assetDirectoryName = value; }
        }

        public string SourcePath
        {
            get { return this.sourcePath; }
            set { this.sourcePath = value; }
        }

        public FormLayout Form
        {
            get { return this.form; }
            set { this.form = value; }
        }

        public List<LayoutControl> Controls
        {
            get { return this.controls; }
        }

        public List<ImageButtonDefinition> ImageButtons
        {
            get { return this.imageButtons; }
        }

        public List<DynamicButtonDefinition> DynamicButtons
        {
            get { return this.dynamicButtons; }
        }

        public ImageButtonDefinition FindButtonByTarget(string target)
        {
            foreach (ImageButtonDefinition definition in this.imageButtons)
            {
                if (string.Equals(definition.Target, target, StringComparison.OrdinalIgnoreCase))
                {
                    return definition;
                }
            }

            foreach (DynamicButtonDefinition definition2 in this.dynamicButtons)
            {
                if (string.Equals(definition2.Target, target, StringComparison.OrdinalIgnoreCase))
                {
                    return definition2;
                }
            }

            return null;
        }
    }

    public class FormLayout
    {
        public Size ClientSize = Size.Empty;
        public string BackgroundImage;
        public string Icon;
        public Color? TransparencyKey;
        public string Caption;
        public bool? ShowIcon;
    }

    public class LayoutControl
    {
        public string Name;
        public LayoutControlType Type = LayoutControlType.Unknown;
        public Point Location = Point.Empty;
        public Size Size = Size.Empty;
        public bool? Visible;
        public bool? Enabled;
        public bool? AutoSize;
        public string Text;
        public Color? ForeColor;
        public Color? BackColor;
        public FontLayout Font;
        public string BackgroundImage;
        public string Image;
        public ContentAlignment? TextAlign;
        public PictureBoxSizeMode? SizeMode;
        public string Url;
        public string Cursor;
        public string BackgroundLayout;
    }

    public class FontLayout
    {
        public string Family;
        public float Size;
        public FontStyle Style;

        public static FontLayout FromFont(Font font)
        {
            FontLayout layout = new FontLayout();
            layout.Family = font.FontFamily.Name;
            layout.Size = font.Size;
            layout.Style = font.Style;
            return layout;
        }

        public Font ToFont()
        {
            if (this.Family == null || this.Family.Length == 0)
            {
                return SystemFonts.DefaultFont;
            }

            if (this.Size <= 0f)
            {
                return new Font(this.Family, SystemFonts.DefaultFont.Size, this.Style);
            }

            return new Font(this.Family, this.Size, this.Style);
        }
    }

    public class ButtonVisuals
    {
        public string Normal;
        public string Hover;
        public string Pressed;
        public string Disabled;
        public string Checked;
        public string Unchecked;
        public string Blink;
    }

    public class ImageButtonDefinition
    {
        public string Target;
        public ButtonVisuals Visuals = new ButtonVisuals();
        public bool Blink;
        public bool Checkable;
    }

    public class DynamicButtonDefinition : ImageButtonDefinition
    {
        public string Name;
        public Point Location = Point.Empty;
        public Size Size = Size.Empty;
        public string Action;
        public string Argument;
        public bool Visible = true;
        public bool Enabled = true;
        public PictureBoxSizeMode? SizeMode;
        public string BackgroundLayout;
    }

    public static class LayoutFile
    {
        public static LayoutDefinition Load(string path)
        {
            LayoutDefinition definition = new LayoutDefinition();
            if (!File.Exists(path))
            {
                return definition;
            }

            definition.SourcePath = path;

            XmlDocument document = new XmlDocument();
            document.Load(path);
            XmlElement root = document.DocumentElement;
            if (root == null || root.Name != "layout")
            {
                return definition;
            }

            definition.AssetDirectoryName = GetAttribute(root, "basePath");

            foreach (XmlNode node in root.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                switch (node.Name)
                {
                    case "form":
                        definition.Form = ParseForm(node);
                        break;
                    case "control":
                        definition.Controls.Add(ParseControl(node));
                        break;
                    case "imageButton":
                        definition.ImageButtons.Add(ParseImageButton(node, false));
                        break;
                    case "toggleButton":
                        ImageButtonDefinition toggle = ParseImageButton(node, true);
                        toggle.Checkable = true;
                        definition.ImageButtons.Add(toggle);
                        break;
                    case "dynamicButton":
                        definition.DynamicButtons.Add(ParseDynamicButton(node));
                        break;
                }
            }

            return definition;
        }

        public static void Save(LayoutDefinition definition, string path)
        {
            XmlDocument document = new XmlDocument();
            XmlElement root = document.CreateElement("layout");
            if (!string.IsNullOrEmpty(definition.AssetDirectoryName))
            {
                root.SetAttribute("basePath", definition.AssetDirectoryName);
            }
            document.AppendChild(root);

            AppendForm(document, root, definition.Form);

            foreach (LayoutControl control in definition.Controls)
            {
                AppendControl(document, root, control);
            }

            foreach (ImageButtonDefinition button in definition.ImageButtons)
            {
                AppendImageButton(document, root, button, false);
            }

            foreach (DynamicButtonDefinition dynamicButton in definition.DynamicButtons)
            {
                AppendDynamicButton(document, root, dynamicButton);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            document.Save(path);
            definition.SourcePath = path;
        }

        private static void AppendForm(XmlDocument document, XmlElement root, FormLayout form)
        {
            XmlElement element = document.CreateElement("form");
            if (!form.ClientSize.IsEmpty)
            {
                element.SetAttribute("width", form.ClientSize.Width.ToString(CultureInfo.InvariantCulture));
                element.SetAttribute("height", form.ClientSize.Height.ToString(CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrEmpty(form.BackgroundImage))
            {
                element.SetAttribute("background", form.BackgroundImage);
            }

            if (!string.IsNullOrEmpty(form.Icon))
            {
                element.SetAttribute("icon", form.Icon);
            }

            if (form.TransparencyKey.HasValue)
            {
                element.SetAttribute("transparency", ColorToString(form.TransparencyKey.Value));
            }

            if (!string.IsNullOrEmpty(form.Caption))
            {
                element.SetAttribute("caption", form.Caption);
            }

            if (form.ShowIcon.HasValue)
            {
                element.SetAttribute("showIcon", form.ShowIcon.Value ? "true" : "false");
            }

            root.AppendChild(element);
        }

        private static void AppendControl(XmlDocument document, XmlElement root, LayoutControl control)
        {
            XmlElement element = document.CreateElement("control");
            element.SetAttribute("name", control.Name);
            element.SetAttribute("type", control.Type.ToString());
            element.SetAttribute("x", control.Location.X.ToString(CultureInfo.InvariantCulture));
            element.SetAttribute("y", control.Location.Y.ToString(CultureInfo.InvariantCulture));
            element.SetAttribute("width", control.Size.Width.ToString(CultureInfo.InvariantCulture));
            element.SetAttribute("height", control.Size.Height.ToString(CultureInfo.InvariantCulture));

            if (control.Visible.HasValue)
            {
                element.SetAttribute("visible", control.Visible.Value ? "true" : "false");
            }

            if (control.Enabled.HasValue)
            {
                element.SetAttribute("enabled", control.Enabled.Value ? "true" : "false");
            }

            if (control.AutoSize.HasValue)
            {
                element.SetAttribute("autoSize", control.AutoSize.Value ? "true" : "false");
            }

            if (!string.IsNullOrEmpty(control.Text))
            {
                element.SetAttribute("text", control.Text);
            }

            if (control.ForeColor.HasValue)
            {
                element.SetAttribute("foreColor", ColorToString(control.ForeColor.Value));
            }

            if (control.BackColor.HasValue)
            {
                element.SetAttribute("backColor", ColorToString(control.BackColor.Value));
            }

            if (control.Font != null)
            {
                element.SetAttribute("font", FontToString(control.Font));
            }

            if (!string.IsNullOrEmpty(control.BackgroundImage))
            {
                element.SetAttribute("background", control.BackgroundImage);
            }

            if (!string.IsNullOrEmpty(control.Image))
            {
                element.SetAttribute("image", control.Image);
            }

            if (control.TextAlign.HasValue)
            {
                element.SetAttribute("textAlign", control.TextAlign.Value.ToString());
            }

            if (control.SizeMode.HasValue)
            {
                element.SetAttribute("sizeMode", control.SizeMode.Value.ToString());
            }

            if (!string.IsNullOrEmpty(control.Url))
            {
                element.SetAttribute("url", control.Url);
            }

            if (!string.IsNullOrEmpty(control.Cursor))
            {
                element.SetAttribute("cursor", control.Cursor);
            }

            if (!string.IsNullOrEmpty(control.BackgroundLayout))
            {
                element.SetAttribute("backgroundLayout", control.BackgroundLayout);
            }

            root.AppendChild(element);
        }

        private static void AppendImageButton(XmlDocument document, XmlElement root, ImageButtonDefinition button, bool dynamic)
        {
            string nodeName = dynamic ? "dynamicButton" : (button.Checkable ? "toggleButton" : "imageButton");
            XmlElement element = document.CreateElement(nodeName);
            if (dynamic)
            {
                DynamicButtonDefinition dyn = (DynamicButtonDefinition)button;
                element.SetAttribute("name", dyn.Name);
                element.SetAttribute("x", dyn.Location.X.ToString(CultureInfo.InvariantCulture));
                element.SetAttribute("y", dyn.Location.Y.ToString(CultureInfo.InvariantCulture));
                element.SetAttribute("width", dyn.Size.Width.ToString(CultureInfo.InvariantCulture));
                element.SetAttribute("height", dyn.Size.Height.ToString(CultureInfo.InvariantCulture));
                if (!string.IsNullOrEmpty(dyn.Action))
                {
                    element.SetAttribute("action", dyn.Action);
                }
                if (!string.IsNullOrEmpty(dyn.Argument))
                {
                    element.SetAttribute("argument", dyn.Argument);
                }
                if (!dyn.Visible)
                {
                    element.SetAttribute("visible", "false");
                }
                if (!dyn.Enabled)
                {
                    element.SetAttribute("enabled", "false");
                }
                if (dyn.SizeMode.HasValue)
                {
                    element.SetAttribute("sizeMode", dyn.SizeMode.Value.ToString());
                }
                if (!string.IsNullOrEmpty(dyn.BackgroundLayout))
                {
                    element.SetAttribute("backgroundLayout", dyn.BackgroundLayout);
                }
            }
            else
            {
                element.SetAttribute("target", button.Target);
            }

            if (!string.IsNullOrEmpty(button.Visuals.Normal))
            {
                element.SetAttribute("normal", button.Visuals.Normal);
            }
            if (!string.IsNullOrEmpty(button.Visuals.Hover))
            {
                element.SetAttribute("hover", button.Visuals.Hover);
            }
            if (!string.IsNullOrEmpty(button.Visuals.Pressed))
            {
                element.SetAttribute("pressed", button.Visuals.Pressed);
            }
            if (!string.IsNullOrEmpty(button.Visuals.Disabled))
            {
                element.SetAttribute("disabled", button.Visuals.Disabled);
            }
            if (!string.IsNullOrEmpty(button.Visuals.Checked))
            {
                element.SetAttribute("checked", button.Visuals.Checked);
            }
            if (!string.IsNullOrEmpty(button.Visuals.Unchecked))
            {
                element.SetAttribute("unchecked", button.Visuals.Unchecked);
            }
            if (!string.IsNullOrEmpty(button.Visuals.Blink))
            {
                element.SetAttribute("blink", button.Visuals.Blink);
            }
            if (button.Blink)
            {
                element.SetAttribute("useBlinkState", "true");
            }

            root.AppendChild(element);
        }

        private static void AppendDynamicButton(XmlDocument document, XmlElement root, DynamicButtonDefinition dynamicButton)
        {
            AppendImageButton(document, root, dynamicButton, true);
        }

        private static FormLayout ParseForm(XmlNode node)
        {
            FormLayout layout = new FormLayout();
            layout.ClientSize = new Size(
                ParseInt(GetAttribute(node, "width"), 0),
                ParseInt(GetAttribute(node, "height"), 0));
            layout.BackgroundImage = GetAttribute(node, "background");
            layout.Icon = GetAttribute(node, "icon");
            layout.TransparencyKey = ParseColor(GetAttribute(node, "transparency"));
            layout.Caption = GetAttribute(node, "caption");
            string showIcon = GetAttribute(node, "showIcon");
            if (showIcon != null && showIcon.Length > 0)
            {
                layout.ShowIcon = ParseBool(showIcon);
            }
            return layout;
        }

        private static LayoutControl ParseControl(XmlNode node)
        {
            LayoutControl control = new LayoutControl();
            control.Name = GetAttribute(node, "name");
            control.Type = ParseControlType(GetAttribute(node, "type"));
            control.Location = new Point(
                ParseInt(GetAttribute(node, "x"), 0),
                ParseInt(GetAttribute(node, "y"), 0));
            control.Size = new Size(
                ParseInt(GetAttribute(node, "width"), 0),
                ParseInt(GetAttribute(node, "height"), 0));

            string visible = GetAttribute(node, "visible");
            if (visible != null && visible.Length > 0)
            {
                control.Visible = ParseBool(visible);
            }

            string enabled = GetAttribute(node, "enabled");
            if (enabled != null && enabled.Length > 0)
            {
                control.Enabled = ParseBool(enabled);
            }

            string autoSize = GetAttribute(node, "autoSize");
            if (autoSize != null && autoSize.Length > 0)
            {
                control.AutoSize = ParseBool(autoSize);
            }

            control.Text = GetAttribute(node, "text");
            control.ForeColor = ParseColor(GetAttribute(node, "foreColor"));
            control.BackColor = ParseColor(GetAttribute(node, "backColor"));
            string font = GetAttribute(node, "font");
            if (!string.IsNullOrEmpty(font))
            {
                control.Font = ParseFont(font);
            }
            control.BackgroundImage = GetAttribute(node, "background");
            control.Image = GetAttribute(node, "image");
            string textAlign = GetAttribute(node, "textAlign");
            if (!string.IsNullOrEmpty(textAlign))
            {
                try
                {
                    control.TextAlign = (ContentAlignment)Enum.Parse(typeof(ContentAlignment), textAlign, true);
                }
                catch
                {
                }
            }
            string sizeMode = GetAttribute(node, "sizeMode");
            if (!string.IsNullOrEmpty(sizeMode))
            {
                try
                {
                    control.SizeMode = (PictureBoxSizeMode)Enum.Parse(typeof(PictureBoxSizeMode), sizeMode, true);
                }
                catch
                {
                }
            }
            control.Url = GetAttribute(node, "url");
            control.Cursor = GetAttribute(node, "cursor");
            control.BackgroundLayout = GetAttribute(node, "backgroundLayout");
            return control;
        }

        private static ImageButtonDefinition ParseImageButton(XmlNode node, bool toggle)
        {
            ImageButtonDefinition definition = new ImageButtonDefinition();
            definition.Target = GetAttribute(node, "target");
            definition.Blink = ParseBool(GetAttribute(node, "useBlinkState"));
            definition.Checkable = toggle;
            ButtonVisuals visuals = definition.Visuals;
            visuals.Normal = GetAttribute(node, "normal");
            visuals.Hover = GetAttribute(node, "hover");
            visuals.Pressed = GetAttribute(node, "pressed");
            visuals.Disabled = GetAttribute(node, "disabled");
            visuals.Checked = GetAttribute(node, "checked");
            visuals.Unchecked = GetAttribute(node, "unchecked");
            visuals.Blink = GetAttribute(node, "blink");
            return definition;
        }

        private static DynamicButtonDefinition ParseDynamicButton(XmlNode node)
        {
            DynamicButtonDefinition definition = new DynamicButtonDefinition();
            definition.Name = GetAttribute(node, "name");
            definition.Target = definition.Name;
            definition.Location = new Point(
                ParseInt(GetAttribute(node, "x"), 0),
                ParseInt(GetAttribute(node, "y"), 0));
            definition.Size = new Size(
                ParseInt(GetAttribute(node, "width"), 0),
                ParseInt(GetAttribute(node, "height"), 0));
            definition.Action = GetAttribute(node, "action");
            definition.Argument = GetAttribute(node, "argument");
            string visible = GetAttribute(node, "visible");
            if (visible != null && visible.Length > 0)
            {
                definition.Visible = ParseBool(visible);
            }
            string enabled = GetAttribute(node, "enabled");
            if (enabled != null && enabled.Length > 0)
            {
                definition.Enabled = ParseBool(enabled);
            }
            string sizeMode = GetAttribute(node, "sizeMode");
            if (!string.IsNullOrEmpty(sizeMode))
            {
                try
                {
                    definition.SizeMode = (PictureBoxSizeMode)Enum.Parse(typeof(PictureBoxSizeMode), sizeMode, true);
                }
                catch
                {
                }
            }
            definition.BackgroundLayout = GetAttribute(node, "backgroundLayout");

            ButtonVisuals visuals = definition.Visuals;
            visuals.Normal = GetAttribute(node, "normal");
            visuals.Hover = GetAttribute(node, "hover");
            visuals.Pressed = GetAttribute(node, "pressed");
            visuals.Disabled = GetAttribute(node, "disabled");
            visuals.Checked = GetAttribute(node, "checked");
            visuals.Unchecked = GetAttribute(node, "unchecked");
            visuals.Blink = GetAttribute(node, "blink");
            return definition;
        }

        private static LayoutControlType ParseControlType(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return LayoutControlType.Unknown;
            }

            try
            {
                return (LayoutControlType)Enum.Parse(typeof(LayoutControlType), value, true);
            }
            catch
            {
                return LayoutControlType.Unknown;
            }
        }

        private static int ParseInt(string value, int fallback)
        {
            if (string.IsNullOrEmpty(value))
            {
                return fallback;
            }

            int result;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            return fallback;
        }

        private static bool ParseBool(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }

        private static Color? ParseColor(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            try
            {
                if (value.StartsWith("#"))
                {
                    string text = value.Substring(1);
                    if (text.Length == 6)
                    {
                        int r = int.Parse(text.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        int g = int.Parse(text.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        int b = int.Parse(text.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        return Color.FromArgb(r, g, b);
                    }
                    if (text.Length == 8)
                    {
                        int a = int.Parse(text.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        int r = int.Parse(text.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        int g = int.Parse(text.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        int b = int.Parse(text.Substring(6, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                        return Color.FromArgb(a, r, g, b);
                    }
                }
                else
                {
                    return Color.FromName(value);
                }
            }
            catch
            {
            }

            return null;
        }

        private static string ColorToString(Color color)
        {
            if (color.A == 255)
            {
                return string.Concat("#", color.R.ToString("X2", CultureInfo.InvariantCulture), color.G.ToString("X2", CultureInfo.InvariantCulture), color.B.ToString("X2", CultureInfo.InvariantCulture));
            }

            return string.Concat("#", color.A.ToString("X2", CultureInfo.InvariantCulture), color.R.ToString("X2", CultureInfo.InvariantCulture), color.G.ToString("X2", CultureInfo.InvariantCulture), color.B.ToString("X2", CultureInfo.InvariantCulture));
        }

        private static FontLayout ParseFont(string value)
        {
            string[] parts = value.Split(new char[1] { ',' });
            if (parts.Length < 2)
            {
                return null;
            }

            FontLayout layout = new FontLayout();
            layout.Family = parts[0];
            float size;
            if (float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out size))
            {
                layout.Size = size;
            }
            else
            {
                layout.Size = SystemFonts.DefaultFont.Size;
            }

            FontStyle style = FontStyle.Regular;
            if (parts.Length > 2)
            {
                try
                {
                    style = (FontStyle)Enum.Parse(typeof(FontStyle), parts[2], true);
                }
                catch
                {
                }
            }
            layout.Style = style;
            return layout;
        }

        private static string FontToString(FontLayout font)
        {
            return string.Concat(font.Family, ",", font.Size.ToString(CultureInfo.InvariantCulture), ",", font.Style.ToString());
        }

        private static string GetAttribute(XmlNode node, string name)
        {
            XmlAttribute attribute = node.Attributes[name];
            return attribute == null ? null : attribute.Value;
        }
    }
}
