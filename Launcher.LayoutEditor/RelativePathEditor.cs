using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Windows.Forms;

namespace Launcher.LayoutEditor
{
    public class RelativePathEditor : UITypeEditor
    {
        public static string BaseDirectory;

        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            string currentValue = value as string;
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "All files (*.*)|*.*";
                dialog.Title = "Select file";
                string baseDirectory = BaseDirectory;
                if (!string.IsNullOrEmpty(baseDirectory) && Directory.Exists(baseDirectory))
                {
                    dialog.InitialDirectory = baseDirectory;
                }

                string absoluteCurrent = ResolveAbsolutePath(currentValue, baseDirectory);
                if (!string.IsNullOrEmpty(absoluteCurrent) && File.Exists(absoluteCurrent))
                {
                    dialog.FileName = absoluteCurrent;
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    return MakeRelative(dialog.FileName, baseDirectory);
                }
            }

            return value;
        }

        public static string NormalizeRelative(string path, string baseDirectory)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            if (Path.IsPathRooted(path))
            {
                if (!string.IsNullOrEmpty(baseDirectory))
                {
                    return MakeRelative(path, baseDirectory);
                }
                return path;
            }

            return path.Replace('\', '/');
        }

        private static string ResolveAbsolutePath(string value, string baseDirectory)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            string path = value;
            if (!Path.IsPathRooted(path))
            {
                if (!string.IsNullOrEmpty(baseDirectory))
                {
                    path = Path.Combine(baseDirectory, value.Replace('/', Path.DirectorySeparatorChar));
                }
                else
                {
                    path = Path.Combine(Environment.CurrentDirectory, value.Replace('/', Path.DirectorySeparatorChar));
                }
            }

            return path;
        }

        private static string MakeRelative(string absolutePath, string baseDirectory)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                return absolutePath;
            }

            if (string.IsNullOrEmpty(baseDirectory))
            {
                return absolutePath;
            }

            try
            {
                string baseDir = baseDirectory;
                if (!baseDir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    baseDir += Path.DirectorySeparatorChar;
                }

                Uri baseUri = new Uri(baseDir);
                Uri fileUri = new Uri(absolutePath);
                string relative = baseUri.MakeRelativeUri(fileUri).ToString();
                relative = Uri.UnescapeDataString(relative);
                return relative.Replace('\', '/');
            }
            catch
            {
                return absolutePath;
            }
        }
    }
}
