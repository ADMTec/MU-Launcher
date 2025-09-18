using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Launcher.Layout;

namespace Launcher.LayoutEditor
{
    internal interface IPropertyModel
    {
        object Underlying { get; }

        void OnPropertyChanged();
    }

    public class FormProperties : IPropertyModel
    {
        private readonly LayoutDefinition layout;
        private readonly string assetBaseDirectory;

        public FormProperties(LayoutDefinition layout, string assetBaseDirectory)
        {
            this.layout = layout;
            this.assetBaseDirectory = assetBaseDirectory;
            if (this.layout.Form == null)
            {
                this.layout.Form = new FormLayout();
            }
        }

        [Category("Layout"), DisplayName("Client Size"), Description("Dimensions of the launcher window.")]
        public Size ClientSize
        {
            get
            {
                return this.layout.Form.ClientSize.IsEmpty ? new Size(960, 540) : this.layout.Form.ClientSize;
            }
            set
            {
                this.layout.Form.ClientSize = value;
            }
        }

        [Category("Appearance"), DisplayName("Caption"), Description("Window title caption.")]
        public string Caption
        {
            get { return this.layout.Form.Caption; }
            set { this.layout.Form.Caption = value; }
        }

        [Category("Appearance"), DisplayName("Background Image"), Description("Form background image."), Editor(typeof(RelativePathEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string BackgroundImage
        {
            get { return this.layout.Form.BackgroundImage; }
            set { this.layout.Form.BackgroundImage = NormalizePath(value); }
        }

        [Category("Appearance"), DisplayName("Icon"), Description("Application icon."), Editor(typeof(RelativePathEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string Icon
        {
            get { return this.layout.Form.Icon; }
            set { this.layout.Form.Icon = NormalizePath(value); }
        }

        [Category("Appearance"), DisplayName("Transparency Key"), Description("Color treated as transparent. Use Empty to disable."), DefaultValue(typeof(Color), "")]
        public Color TransparencyKey
        {
            get
            {
                return this.layout.Form.TransparencyKey.HasValue ? this.layout.Form.TransparencyKey.Value : Color.Empty;
            }
            set
            {
                if (value.IsEmpty)
                {
                    this.layout.Form.TransparencyKey = null;
                }
                else
                {
                    this.layout.Form.TransparencyKey = value;
                }
            }
        }

        [Category("Appearance"), DisplayName("Show Icon"), Description("Determines if the window shows an icon."), DefaultValue(true)]
        public bool ShowIcon
        {
            get
            {
                if (!this.layout.Form.ShowIcon.HasValue)
                {
                    return true;
                }
                return this.layout.Form.ShowIcon.Value;
            }
            set { this.layout.Form.ShowIcon = value; }
        }

        [Category("Assets"), DisplayName("Asset Folder"), Description("Base folder used for asset lookups."), ReadOnly(true)]
        public string AssetFolder
        {
            get
            {
                return this.layout.AssetDirectoryName;
            }
        }

        public object Underlying
        {
            get { return this.layout.Form; }
        }

        public void OnPropertyChanged()
        {
        }

        private string NormalizePath(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return RelativePathEditor.NormalizeRelative(value, this.assetBaseDirectory);
        }
    }

    public class LayoutControlProperties : IPropertyModel
    {
        private readonly LayoutControl control;
        private readonly string assetBaseDirectory;

        public LayoutControlProperties(LayoutControl control, string assetBaseDirectory)
        {
            this.control = control;
            this.assetBaseDirectory = assetBaseDirectory;
        }

        [Category("General"), DisplayName("Name")]
        public string Name
        {
            get { return this.control.Name; }
            set { this.control.Name = value; }
        }

        [Category("General"), DisplayName("Type"), ReadOnly(true)]
        public LayoutControlType Type
        {
            get { return this.control.Type; }
        }

        [Category("Layout"), DisplayName("Location")]
        public Point Location
        {
            get { return this.control.Location; }
            set { this.control.Location = value; }
        }

        [Category("Layout"), DisplayName("Size")]
        public Size Size
        {
            get { return this.control.Size; }
            set { this.control.Size = value; }
        }

        [Category("Behavior"), DisplayName("Visible"), DefaultValue(true)]
        public bool Visible
        {
            get
            {
                if (!this.control.Visible.HasValue)
                {
                    return true;
                }
                return this.control.Visible.Value;
            }
            set { this.control.Visible = value; }
        }

        [Category("Behavior"), DisplayName("Enabled"), DefaultValue(true)]
        public bool Enabled
        {
            get
            {
                if (!this.control.Enabled.HasValue)
                {
                    return true;
                }
                return this.control.Enabled.Value;
            }
            set { this.control.Enabled = value; }
        }

        [Category("Behavior"), DisplayName("Auto Size"), DefaultValue(false)]
        public bool AutoSize
        {
            get
            {
                if (!this.control.AutoSize.HasValue)
                {
                    return false;
                }
                return this.control.AutoSize.Value;
            }
            set { this.control.AutoSize = value; }
        }

        [Category("Appearance"), DisplayName("Text")]
        public string Text
        {
            get { return this.control.Text; }
            set { this.control.Text = value; }
        }

        [Category("Appearance"), DisplayName("Fore Color"), DefaultValue(typeof(Color), "")]
        public Color ForeColor
        {
            get { return this.control.ForeColor.HasValue ? this.control.ForeColor.Value : Color.Empty; }
            set { this.control.ForeColor = value.IsEmpty ? (Color?)null : value; }
        }

        [Category("Appearance"), DisplayName("Back Color"), DefaultValue(typeof(Color), "")]
        public Color BackColor
        {
            get { return this.control.BackColor.HasValue ? this.control.BackColor.Value : Color.Empty; }
            set { this.control.BackColor = value.IsEmpty ? (Color?)null : value; }
        }

        [Category("Appearance"), DisplayName("Font")]
        public Font Font
        {
            get
            {
                if (this.control.Font == null)
                {
                    return SystemFonts.DefaultFont;
                }
                try
                {
                    return this.control.Font.ToFont();
                }
                catch
                {
                    return SystemFonts.DefaultFont;
                }
            }
            set
            {
                if (value == null)
                {
                    this.control.Font = null;
                }
                else
                {
                    this.control.Font = FontLayout.FromFont(value);
                }
            }
        }

        [Category("Appearance"), DisplayName("Background Image"), Editor(typeof(RelativePathEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string BackgroundImage
        {
            get { return this.control.BackgroundImage; }
            set { this.control.BackgroundImage = NormalizePath(value); }
        }

        [Category("Appearance"), DisplayName("Image"), Editor(typeof(RelativePathEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string Image
        {
            get { return this.control.Image; }
            set { this.control.Image = NormalizePath(value); }
        }

        [Category("Appearance"), DisplayName("Text Align"), DefaultValue(typeof(ContentAlignment), "TopLeft")]
        public ContentAlignment TextAlign
        {
            get { return this.control.TextAlign.HasValue ? this.control.TextAlign.Value : ContentAlignment.TopLeft; }
            set { this.control.TextAlign = value; }
        }

        [Category("Appearance"), DisplayName("Size Mode"), DefaultValue(typeof(PictureBoxSizeMode), "Normal")]
        public PictureBoxSizeMode SizeMode
        {
            get { return this.control.SizeMode.HasValue ? this.control.SizeMode.Value : PictureBoxSizeMode.Normal; }
            set { this.control.SizeMode = value; }
        }

        [Category("Behavior"), DisplayName("URL")]
        public string Url
        {
            get { return this.control.Url; }
            set { this.control.Url = value; }
        }

        [Category("Behavior"), DisplayName("Cursor")]
        public string Cursor
        {
            get { return this.control.Cursor; }
            set { this.control.Cursor = value; }
        }

        [Category("Appearance"), DisplayName("Background Layout"), DefaultValue(typeof(ImageLayout), "None")]
        public ImageLayout BackgroundLayout
        {
            get { return MainForm.ParseImageLayout(this.control.BackgroundLayout); }
            set { this.control.BackgroundLayout = value.ToString(); }
        }

        public object Underlying
        {
            get { return this.control; }
        }

        public void OnPropertyChanged()
        {
        }

        private string NormalizePath(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return RelativePathEditor.NormalizeRelative(value, this.assetBaseDirectory);
        }
    }

    public class DynamicButtonProperties : IPropertyModel
    {
        private readonly DynamicButtonDefinition button;
        private readonly string assetBaseDirectory;

        public DynamicButtonProperties(DynamicButtonDefinition button, string assetBaseDirectory)
        {
            this.button = button;
            this.assetBaseDirectory = assetBaseDirectory;
        }

        [Category("General"), DisplayName("Name")]
        public string Name
        {
            get { return this.button.Name; }
            set
            {
                this.button.Name = value;
                this.button.Target = value;
            }
        }

        [Category("Layout"), DisplayName("Location")]
        public Point Location
        {
            get { return this.button.Location; }
            set { this.button.Location = value; }
        }

        [Category("Layout"), DisplayName("Size")]
        public Size Size
        {
            get { return this.button.Size; }
            set { this.button.Size = value; }
        }

        [Category("Behavior"), DisplayName("Action")]
        public string Action
        {
            get { return this.button.Action; }
            set { this.button.Action = value; }
        }

        [Category("Behavior"), DisplayName("Argument")]
        public string Argument
        {
            get { return this.button.Argument; }
            set { this.button.Argument = value; }
        }

        [Category("Behavior"), DisplayName("Visible"), DefaultValue(true)]
        public bool Visible
        {
            get { return this.button.Visible; }
            set { this.button.Visible = value; }
        }

        [Category("Behavior"), DisplayName("Enabled"), DefaultValue(true)]
        public bool Enabled
        {
            get { return this.button.Enabled; }
            set { this.button.Enabled = value; }
        }

        [Category("Appearance"), DisplayName("Size Mode"), DefaultValue(typeof(PictureBoxSizeMode), "StretchImage")]
        public PictureBoxSizeMode SizeMode
        {
            get { return this.button.SizeMode.HasValue ? this.button.SizeMode.Value : PictureBoxSizeMode.StretchImage; }
            set { this.button.SizeMode = value; }
        }

        [Category("Appearance"), DisplayName("Background Layout"), DefaultValue(typeof(ImageLayout), "None")]
        public ImageLayout BackgroundLayout
        {
            get { return MainForm.ParseImageLayout(this.button.BackgroundLayout); }
            set { this.button.BackgroundLayout = value.ToString(); }
        }

        [Category("Visuals"), DisplayName("Normal"), Editor(typeof(RelativePathEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string Normal
        {
            get { return this.button.Visuals.Normal; }
            set { this.button.Visuals.Normal = NormalizePath(value); }
        }

        [Category("Visuals"), DisplayName("Hover"), Editor(typeof(RelativePathEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string Hover
        {
            get { return this.button.Visuals.Hover; }
            set { this.button.Visuals.Hover = NormalizePath(value); }
        }

        [Category("Visuals"), DisplayName("Pressed"), Editor(typeof(RelativePathEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string Pressed
        {
            get { return this.button.Visuals.Pressed; }
            set { this.button.Visuals.Pressed = NormalizePath(value); }
        }

        [Category("Visuals"), DisplayName("Disabled"), Editor(typeof(RelativePathEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string Disabled
        {
            get { return this.button.Visuals.Disabled; }
            set { this.button.Visuals.Disabled = NormalizePath(value); }
        }

        [Category("Visuals"), DisplayName("Checked"), Editor(typeof(RelativePathEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string Checked
        {
            get { return this.button.Visuals.Checked; }
            set { this.button.Visuals.Checked = NormalizePath(value); }
        }

        [Category("Visuals"), DisplayName("Unchecked"), Editor(typeof(RelativePathEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string Unchecked
        {
            get { return this.button.Visuals.Unchecked; }
            set { this.button.Visuals.Unchecked = NormalizePath(value); }
        }

        [Category("Visuals"), DisplayName("Blink"), Editor(typeof(RelativePathEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public string Blink
        {
            get { return this.button.Visuals.Blink; }
            set { this.button.Visuals.Blink = NormalizePath(value); }
        }

        public object Underlying
        {
            get { return this.button; }
        }

        public void OnPropertyChanged()
        {
        }

        private string NormalizePath(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return RelativePathEditor.NormalizeRelative(value, this.assetBaseDirectory);
        }
    }
}
