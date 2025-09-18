using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Launcher.Layout;

namespace Launcher.LayoutEditor
{
    public partial class MainForm : Form
    {
        private LayoutDefinition currentLayout;
        private Panel layoutSurface;
        private Panel selectionOverlay;
        private Dictionary<Control, LayoutControl> staticControls;
        private Dictionary<Control, DynamicButtonDefinition> dynamicControls;
        private List<Image> loadedImages;
        private Control selectedControl;
        private object selectedDefinition;
        private bool isDirty;
        private bool isDragging;
        private Point dragOffset;

        public MainForm()
        {
            this.InitializeComponent();
            this.currentLayout = new LayoutDefinition();
            this.loadedImages = new List<Image>();
            this.staticControls = new Dictionary<Control, LayoutControl>();
            this.dynamicControls = new Dictionary<Control, DynamicButtonDefinition>();
            this.designPanel.AutoScroll = true;
            this.propertyGrid1.PropertySort = PropertySort.Categorized;
            this.UpdateWindowTitle();
            this.UpdateStatusLabel();
            this.RefreshLayoutSurface(null);
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!this.ConfirmDiscardChanges())
            {
                return;
            }

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Layout files (*.layout.xml)|*.layout.xml|XML files (*.xml)|*.xml|All files (*.*)|*.*";
                dialog.Title = "Open Layout";
                dialog.InitialDirectory = this.GetInitialLayoutDirectory();

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    this.LoadLayout(dialog.FileName);
                }
            }
        }

        private void LoadLayout(string path)
        {
            try
            {
                LayoutDefinition definition = LayoutFile.Load(path);
                if (definition.Form == null)
                {
                    definition.Form = new FormLayout();
                }
                this.currentLayout = definition;
                this.currentLayout.SourcePath = path;
                this.isDirty = false;
                this.UpdateWindowTitle();
                this.RefreshLayoutSurface(null);
                this.UpdateStatusLabel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to load layout: " + ex.Message, "Layout Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.SaveLayout(false);
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.SaveLayout(true);
        }

        private void SetAssetFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select asset folder";
                string initial = this.GetAssetBaseDirectory();
                if (!string.IsNullOrEmpty(initial) && Directory.Exists(initial))
                {
                    dialog.SelectedPath = initial;
                }

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    string relative = MakeRelativePath(this.GetLayoutDirectory(), dialog.SelectedPath);
                    if (relative.Length == 0)
                    {
                        this.currentLayout.AssetDirectoryName = string.Empty;
                    }
                    else
                    {
                        this.currentLayout.AssetDirectoryName = NormalizeAssetPath(relative);
                    }
                    this.MarkDirty();
                    this.RefreshLayoutSurface(this.selectedDefinition);
                    this.UpdateStatusLabel();
                }
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ConfirmDiscardChanges())
            {
                this.Close();
            }
        }

        private void AddDynamicButtonToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DynamicButtonDefinition definition = new DynamicButtonDefinition();
            definition.Name = this.GenerateDynamicButtonName();
            definition.Target = definition.Name;
            definition.Location = new Point(32, 32);
            definition.Size = new Size(120, 32);
            definition.Action = "OpenUrl";
            definition.Argument = "https://example.com";
            this.currentLayout.DynamicButtons.Add(definition);
            this.MarkDirty();
            this.RefreshLayoutSurface(definition);
        }

        private void DeleteSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.selectedDefinition is DynamicButtonDefinition)
            {
                DynamicButtonDefinition definition = (DynamicButtonDefinition)this.selectedDefinition;
                DialogResult result = MessageBox.Show(this, "Remove dynamic button '" + definition.Name + "'?", "Layout Editor", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    this.currentLayout.DynamicButtons.Remove(definition);
                    this.MarkDirty();
                    this.RefreshLayoutSurface(null);
                }
            }
            else
            {
                MessageBox.Show(this, "Select a dynamic button to delete.", "Layout Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void PropertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            this.MarkDirty();
            object target = null;
            if (this.propertyGrid1.SelectedObject is IPropertyModel)
            {
                IPropertyModel model = (IPropertyModel)this.propertyGrid1.SelectedObject;
                model.OnPropertyChanged();
                target = model.Underlying;
            }
            this.RefreshLayoutSurface(target);
        }

        private void DesignPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.UpdateSelection(null, null);
            }
        }

        private void LayoutSurface_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.UpdateSelection(null, null);
            }
        }

        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            Control control = sender as Control;
            if (control == null)
            {
                return;
            }

            Control designControl = this.GetDesignControl(control);
            object definition = this.GetDefinitionForControl(control);
            this.UpdateSelection(designControl, definition);

            if (designControl != null && e.Button == MouseButtons.Left && definition is DynamicButtonDefinition)
            {
                this.isDragging = true;
                this.dragOffset = this.TranslateToDesignControl(designControl, control, e.Location);
                designControl.Capture = true;
            }
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            if (!this.isDragging)
            {
                return;
            }

            Control source = sender as Control;
            if (source == null)
            {
                return;
            }

            Control control = this.GetDesignControl(source);
            if (control == null)
            {
                return;
            }

            DynamicButtonDefinition definition;
            if (!this.dynamicControls.TryGetValue(control, out definition))
            {
                return;
            }

            Point localPoint = this.TranslateToDesignControl(control, source, e.Location);

            int newX = control.Left + localPoint.X - this.dragOffset.X;
            int newY = control.Top + localPoint.Y - this.dragOffset.Y;
            if (newX < 0)
            {
                newX = 0;
            }
            if (newY < 0)
            {
                newY = 0;
            }
            if (this.layoutSurface != null)
            {
                if (newX + control.Width > this.layoutSurface.Width)
                {
                    newX = this.layoutSurface.Width - control.Width;
                }
                if (newY + control.Height > this.layoutSurface.Height)
                {
                    newY = this.layoutSurface.Height - control.Height;
                }
            }

            control.Location = new Point(newX, newY);
            this.UpdateSelectionOverlay();
            this.statusLabelPath.Text = "Position: " + newX + ", " + newY;
        }

        private void Control_MouseUp(object sender, MouseEventArgs e)
        {
            if (!this.isDragging)
            {
                return;
            }

            this.isDragging = false;
            Control source = sender as Control;
            if (source != null)
            {
                Control control = this.GetDesignControl(source);
                if (control != null)
                {
                    control.Capture = false;
                    DynamicButtonDefinition definition;
                    if (this.dynamicControls.TryGetValue(control, out definition))
                    {
                        Point location = control.Location;
                        definition.Location = location;
                        this.MarkDirty();
                        this.propertyGrid1.Refresh();
                    }
                }
            }
            this.UpdateStatusLabel();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this.ConfirmDiscardChanges())
            {
                e.Cancel = true;
            }
        }

        private bool SaveLayout(bool saveAs)
        {
            if (this.currentLayout == null)
            {
                return false;
            }

            string targetPath = this.currentLayout.SourcePath;
            if (saveAs || string.IsNullOrEmpty(targetPath))
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "Layout files (*.layout.xml)|*.layout.xml|XML files (*.xml)|*.xml|All files (*.*)|*.*";
                    dialog.Title = "Save Layout";
                    dialog.InitialDirectory = this.GetInitialLayoutDirectory();
                    if (!string.IsNullOrEmpty(targetPath))
                    {
                        dialog.FileName = targetPath;
                    }

                    if (dialog.ShowDialog(this) != DialogResult.OK)
                    {
                        return false;
                    }

                    targetPath = dialog.FileName;
                }
            }

            try
            {
                LayoutFile.Save(this.currentLayout, targetPath);
                this.currentLayout.SourcePath = targetPath;
                this.isDirty = false;
                this.UpdateWindowTitle();
                this.UpdateStatusLabel();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to save layout: " + ex.Message, "Layout Editor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool ConfirmDiscardChanges()
        {
            if (!this.isDirty)
            {
                return true;
            }

            DialogResult result = MessageBox.Show(this, "Save changes to the current layout?", "Layout Editor", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Cancel)
            {
                return false;
            }

            if (result == DialogResult.Yes)
            {
                return this.SaveLayout(false);
            }

            return true;
        }

        private void RefreshLayoutSurface(object definitionToSelect)
        {
            this.DisposeLoadedImages();
            this.designPanel.SuspendLayout();
            try
            {
                this.designPanel.Controls.Clear();
                this.staticControls.Clear();
                this.dynamicControls.Clear();
                this.selectedControl = null;

                Size clientSize = new Size(960, 540);
                if (this.currentLayout != null && this.currentLayout.Form != null && !this.currentLayout.Form.ClientSize.IsEmpty)
                {
                    clientSize = this.currentLayout.Form.ClientSize;
                }

                this.designPanel.AutoScrollMinSize = new Size(clientSize.Width + 40, clientSize.Height + 40);

                this.layoutSurface = new Panel();
                this.layoutSurface.BackColor = Color.Black;
                this.layoutSurface.BorderStyle = BorderStyle.FixedSingle;
                this.layoutSurface.Location = new Point(20, 20);
                this.layoutSurface.Size = clientSize;
                this.layoutSurface.BackgroundImageLayout = ImageLayout.Stretch;
                this.layoutSurface.MouseDown += new MouseEventHandler(this.LayoutSurface_MouseDown);

                string background = this.ResolveAssetPath(this.currentLayout != null && this.currentLayout.Form != null ? this.currentLayout.Form.BackgroundImage : null);
                if (!string.IsNullOrEmpty(background) && File.Exists(background))
                {
                    Image image = this.LoadImageCopy(background);
                    if (image != null)
                    {
                        this.layoutSurface.BackgroundImage = image;
                    }
                }

                this.designPanel.Controls.Add(this.layoutSurface);

                foreach (LayoutControl layoutControl in this.currentLayout.Controls)
                {
                    Control visual = this.CreateControlVisual(layoutControl);
                    if (visual != null)
                    {
                        this.layoutSurface.Controls.Add(visual);
                        this.staticControls.Add(visual, layoutControl);
                        this.AttachControlHandlers(visual);
                    }
                }

                foreach (DynamicButtonDefinition dynamicButton in this.currentLayout.DynamicButtons)
                {
                    Control visual = this.CreateDynamicButtonVisual(dynamicButton);
                    if (visual != null)
                    {
                        this.layoutSurface.Controls.Add(visual);
                        this.dynamicControls.Add(visual, dynamicButton);
                        this.AttachControlHandlers(visual);
                    }
                }

                this.EnsureSelectionOverlay();

                if (definitionToSelect == null)
                {
                    this.UpdateSelection(null, null);
                }
                else
                {
                    Control match = this.FindControlByDefinition(definitionToSelect);
                    this.UpdateSelection(match, definitionToSelect);
                }
            }
            finally
            {
                this.designPanel.ResumeLayout();
            }
        }

        private void AttachControlHandlers(Control control)
        {
            this.AttachHandlersRecursive(control);
            control.ControlAdded += new ControlEventHandler(this.Control_ControlAdded);
        }

        private void AttachHandlersRecursive(Control control)
        {
            control.MouseDown += new MouseEventHandler(this.Control_MouseDown);
            control.MouseMove += new MouseEventHandler(this.Control_MouseMove);
            control.MouseUp += new MouseEventHandler(this.Control_MouseUp);

            foreach (Control child in control.Controls)
            {
                this.AttachHandlersRecursive(child);
            }
        }

        private void Control_ControlAdded(object sender, ControlEventArgs e)
        {
            if (e != null && e.Control != null)
            {
                this.AttachHandlersRecursive(e.Control);
            }
        }

        private Control FindControlByDefinition(object definition)
        {
            foreach (KeyValuePair<Control, LayoutControl> pair in this.staticControls)
            {
                if (object.ReferenceEquals(pair.Value, definition))
                {
                    return pair.Key;
                }
            }

            foreach (KeyValuePair<Control, DynamicButtonDefinition> pair2 in this.dynamicControls)
            {
                if (object.ReferenceEquals(pair2.Value, definition))
                {
                    return pair2.Key;
                }
            }

            return null;
        }

        private object GetDefinitionForControl(Control control)
        {
            Control current = control;
            while (current != null && current != this.layoutSurface)
            {
                LayoutControl staticDefinition;
                if (this.staticControls.TryGetValue(current, out staticDefinition))
                {
                    return staticDefinition;
                }

                DynamicButtonDefinition dynamicDefinition;
                if (this.dynamicControls.TryGetValue(current, out dynamicDefinition))
                {
                    return dynamicDefinition;
                }

                current = current.Parent;
            }

            return null;
        }

        private Control GetDesignControl(Control control)
        {
            Control current = control;
            while (current != null && current.Parent != this.layoutSurface && current != this.layoutSurface)
            {
                current = current.Parent;
            }

            if (current == this.layoutSurface)
            {
                return null;
            }

            return current;
        }

        private Point TranslateToDesignControl(Control designControl, Control sourceControl, Point sourcePoint)
        {
            if (designControl == null || sourceControl == null)
            {
                return sourcePoint;
            }

            if (designControl == sourceControl)
            {
                return sourcePoint;
            }

            Point screenPoint = sourceControl.PointToScreen(sourcePoint);
            Point translated = designControl.PointToClient(screenPoint);
            return translated;
        }

        private Control CreateControlVisual(LayoutControl definition)
        {
            Control control = null;
            switch (definition.Type)
            {
                case LayoutControlType.Label:
                    Label label = new Label();
                    label.Text = definition.Text;
                    if (definition.AutoSize.HasValue)
                    {
                        label.AutoSize = definition.AutoSize.Value;
                    }
                    label.TextAlign = definition.TextAlign.HasValue ? definition.TextAlign.Value : ContentAlignment.TopLeft;
                    control = label;
                    break;
                case LayoutControlType.PictureBox:
                    PictureBox pictureBox = new PictureBox();
                    pictureBox.BackgroundImageLayout = ParseImageLayout(definition.BackgroundLayout);
                    pictureBox.SizeMode = definition.SizeMode.HasValue ? definition.SizeMode.Value : PictureBoxSizeMode.Normal;
                    Image pictureBackground = this.LoadImageCopy(this.ResolveAssetPath(definition.BackgroundImage));
                    if (pictureBackground != null)
                    {
                        pictureBox.BackgroundImage = pictureBackground;
                    }
                    Image mainImage = this.LoadImageCopy(this.ResolveAssetPath(definition.Image));
                    if (mainImage != null)
                    {
                        pictureBox.Image = mainImage;
                    }
                    control = pictureBox;
                    break;
                case LayoutControlType.WebBrowser:
                    Panel browserPlaceholder = new Panel();
                    browserPlaceholder.BackColor = Color.WhiteSmoke;
                    browserPlaceholder.BorderStyle = BorderStyle.FixedSingle;
                    Label placeholderLabel = new Label();
                    placeholderLabel.Text = "WebBrowser";
                    placeholderLabel.AutoSize = true;
                    placeholderLabel.BackColor = Color.Transparent;
                    placeholderLabel.Location = new Point(6, 6);
                    browserPlaceholder.Controls.Add(placeholderLabel);
                    control = browserPlaceholder;
                    break;
                case LayoutControlType.Panel:
                    Panel panel = new Panel();
                    panel.BackColor = Color.FromArgb(64, Color.Black);
                    control = panel;
                    break;
                default:
                    Panel placeholder = new Panel();
                    placeholder.BackColor = Color.Gray;
                    control = placeholder;
                    break;
            }

            if (control == null)
            {
                return null;
            }

            control.Name = definition.Name;
            control.Location = definition.Location;
            if (!definition.AutoSize.HasValue || !definition.AutoSize.Value)
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

            if (definition.ForeColor.HasValue)
            {
                control.ForeColor = definition.ForeColor.Value;
            }

            if (definition.BackColor.HasValue)
            {
                control.BackColor = definition.BackColor.Value;
            }
            else if (definition.BackColor == null && definition.Type == LayoutControlType.Label)
            {
                control.BackColor = Color.Transparent;
            }

            if (definition.Font != null)
            {
                try
                {
                    control.Font = definition.Font.ToFont();
                }
                catch
                {
                }
            }

            return control;
        }

        private Control CreateDynamicButtonVisual(DynamicButtonDefinition definition)
        {
            PictureBox pictureBox = new PictureBox();
            pictureBox.Name = definition.Name;
            pictureBox.Location = definition.Location;
            pictureBox.Size = definition.Size;
            pictureBox.SizeMode = definition.SizeMode.HasValue ? definition.SizeMode.Value : PictureBoxSizeMode.StretchImage;
            pictureBox.BackgroundImageLayout = ParseImageLayout(definition.BackgroundLayout);
            pictureBox.Visible = definition.Visible;
            pictureBox.Enabled = definition.Enabled;

            SkinVisualState initialState = SkinVisualState.Normal;
            Image image = this.GetSkinImage(definition.Visuals, initialState);
            if (image == null)
            {
                image = this.GetSkinImage(definition.Visuals, SkinVisualState.Hover);
            }
            if (image == null)
            {
                image = this.GetSkinImage(definition.Visuals, SkinVisualState.Pressed);
            }
            if (image != null)
            {
                pictureBox.BackgroundImage = image;
            }

            return pictureBox;
        }

        private Image GetSkinImage(ButtonVisuals visuals, SkinVisualState state)
        {
            string path = null;
            switch (state)
            {
                case SkinVisualState.Normal:
                    path = visuals.Normal;
                    break;
                case SkinVisualState.Hover:
                    path = visuals.Hover;
                    break;
                case SkinVisualState.Pressed:
                    path = visuals.Pressed;
                    break;
                case SkinVisualState.Disabled:
                    path = visuals.Disabled;
                    break;
                case SkinVisualState.Checked:
                    path = visuals.Checked;
                    break;
                case SkinVisualState.Unchecked:
                    path = visuals.Unchecked;
                    break;
                case SkinVisualState.Blink:
                    path = visuals.Blink;
                    break;
            }

            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string resolved = this.ResolveAssetPath(path);
            return this.LoadImageCopy(resolved);
        }

        private void EnsureSelectionOverlay()
        {
            if (this.selectionOverlay == null)
            {
                this.selectionOverlay = new Panel();
                this.selectionOverlay.BorderStyle = BorderStyle.FixedSingle;
                this.selectionOverlay.BackColor = Color.Transparent;
                this.selectionOverlay.Enabled = false;
            }

            if (this.layoutSurface != null && this.selectionOverlay.Parent != this.layoutSurface)
            {
                this.layoutSurface.Controls.Add(this.selectionOverlay);
                this.selectionOverlay.Visible = false;
            }
        }

        private void UpdateSelection(Control control, object definition)
        {
            if (definition is FormLayout)
            {
                definition = null;
            }

            this.selectedControl = control;
            this.selectedDefinition = definition;
            this.UpdateSelectionOverlay();

            string assetBase = this.GetAssetBaseDirectory();
            RelativePathEditor.BaseDirectory = assetBase;

            if (definition == null)
            {
                this.propertyGrid1.SelectedObject = new FormProperties(this.currentLayout, assetBase);
            }
            else if (definition is DynamicButtonDefinition)
            {
                DynamicButtonDefinition dynamicDefinition = (DynamicButtonDefinition)definition;
                this.propertyGrid1.SelectedObject = new DynamicButtonProperties(dynamicDefinition, assetBase);
            }
            else if (definition is LayoutControl)
            {
                LayoutControl layoutControl = (LayoutControl)definition;
                this.propertyGrid1.SelectedObject = new LayoutControlProperties(layoutControl, assetBase);
            }

            this.UpdateStatusLabel();
        }

        private void UpdateSelectionOverlay()
        {
            if (this.selectionOverlay == null || this.layoutSurface == null)
            {
                return;
            }

            if (this.selectedControl == null)
            {
                this.selectionOverlay.Visible = false;
                return;
            }

            this.selectionOverlay.Bounds = this.selectedControl.Bounds;
            this.selectionOverlay.Visible = true;
            this.selectionOverlay.BringToFront();
        }

        private void MarkDirty()
        {
            this.isDirty = true;
            this.UpdateWindowTitle();
            this.UpdateStatusLabel();
        }

        private void UpdateWindowTitle()
        {
            string name = this.currentLayout != null ? this.currentLayout.SourcePath : null;
            if (string.IsNullOrEmpty(name))
            {
                name = "New Layout";
            }
            else
            {
                name = Path.GetFileName(name);
            }
            if (this.isDirty)
            {
                name += "*";
            }
            this.Text = "Layout Editor - " + name;
        }

        private void UpdateStatusLabel()
        {
            string path = this.currentLayout != null ? this.currentLayout.SourcePath : null;
            if (string.IsNullOrEmpty(path))
            {
                path = "(unsaved layout)";
            }

            string assets = this.GetAssetBaseDirectory();
            if (string.IsNullOrEmpty(assets))
            {
                assets = "(assets not set)";
            }

            string suffix = this.isDirty ? "*" : string.Empty;
            this.statusLabelPath.Text = "Layout: " + path + suffix + " | Assets: " + assets;
        }

        private string GetInitialLayoutDirectory()
        {
            string path = this.currentLayout != null ? this.currentLayout.SourcePath : null;
            if (!string.IsNullOrEmpty(path) && Directory.Exists(Path.GetDirectoryName(path)))
            {
                return Path.GetDirectoryName(path);
            }

            string folder = this.GetLayoutDirectory();
            if (!string.IsNullOrEmpty(folder))
            {
                return folder;
            }

            return Environment.CurrentDirectory;
        }

        private string GetLayoutDirectory()
        {
            if (this.currentLayout == null || string.IsNullOrEmpty(this.currentLayout.SourcePath))
            {
                return Environment.CurrentDirectory;
            }
            return Path.GetDirectoryName(this.currentLayout.SourcePath);
        }

        private string GetAssetBaseDirectory()
        {
            if (this.currentLayout == null)
            {
                return null;
            }

            string baseName = this.currentLayout.AssetDirectoryName;
            if (string.IsNullOrEmpty(baseName))
            {
                return this.GetLayoutDirectory();
            }

            string layoutDir = this.GetLayoutDirectory();
            if (string.IsNullOrEmpty(baseName))
            {
                return layoutDir;
            }

            if (Path.IsPathRooted(baseName))
            {
                return baseName;
            }

            if (string.IsNullOrEmpty(layoutDir))
            {
                return baseName;
            }

            string combined = Path.Combine(layoutDir, baseName);
            return Path.GetFullPath(combined);
        }

        private void DisposeLoadedImages()
        {
            foreach (Image image in this.loadedImages)
            {
                if (image != null)
                {
                    image.Dispose();
                }
            }
            this.loadedImages.Clear();
        }

        private Image LoadImageCopy(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            try
            {
                using (Image original = Image.FromFile(path))
                {
                    Bitmap copy = new Bitmap(original);
                    this.loadedImages.Add(copy);
                    return copy;
                }
            }
            catch
            {
                return null;
            }
        }

        internal static ImageLayout ParseImageLayout(string value)
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

        private string ResolveAssetPath(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            string path = value.Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(path))
            {
                return path;
            }

            string baseDir = this.GetAssetBaseDirectory();
            if (string.IsNullOrEmpty(baseDir))
            {
                return path;
            }

            return Path.Combine(baseDir, path);
        }

        private static string NormalizeAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            return path.Replace('\', '/');
        }

        private static string MakeRelativePath(string baseDirectory, string targetPath)
        {
            if (string.IsNullOrEmpty(baseDirectory))
            {
                return targetPath ?? string.Empty;
            }

            try
            {
                string baseDir = baseDirectory;
                if (!baseDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    baseDir += Path.DirectorySeparatorChar;
                }
                Uri baseUri = new Uri(baseDir);
                Uri targetUri = new Uri(targetPath);
                Uri relativeUri = baseUri.MakeRelativeUri(targetUri);
                string relative = Uri.UnescapeDataString(relativeUri.ToString());
                return NormalizeAssetPath(relative);
            }
            catch
            {
                return targetPath ?? string.Empty;
            }
        }

        private string GenerateDynamicButtonName()
        {
            int counter = 1;
            string name;
            while (true)
            {
                name = "DynamicButton" + counter;
                bool exists = false;
                foreach (DynamicButtonDefinition existing in this.currentLayout.DynamicButtons)
                {
                    if (string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    return name;
                }

                counter++;
            }
        }
    }
}
