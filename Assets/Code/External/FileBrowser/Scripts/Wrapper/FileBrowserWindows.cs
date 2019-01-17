#if (UNITY_STANDALONE_WIN && !UNITY_EDITOR) || CT_ENABLED
using UnityEngine;
using System;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Ookii.Dialogs;

namespace Crosstales.FB.Wrapper
{
    // For fullscreen support:
    // - WindowWrapper class and GetActiveWindow() are required for modal file dialog.
    // - "PlayerSettings/Visible In Background" should be enabled, otherwise when file dialog opened, app window minimizes automatically.

    /// <summary>File browser implementation for Windows.</summary>
    public class FileBrowserWindows : FileBrowserBase
    {
        #region Variables

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        #endregion


        #region Implemented methods

        public override string[] OpenFiles(string title, string directory, ExtensionFilter[] extensions, bool multiselect)
        {
            string[] filenames = null;

            using (VistaOpenFileDialog fd = new VistaOpenFileDialog())
            {
                fd.Title = title;

                if (extensions != null)
                {
                    fd.Filter = getFilterFromFileExtensionList(extensions);
                    fd.FilterIndex = 1;
                }
                else
                {
                    fd.Filter = string.Empty;
                }

                fd.Multiselect = multiselect;

                //Debug.Log("multi");

                if (!string.IsNullOrEmpty(directory))
                {
                    fd.FileName = getPath(directory);
                }

                DialogResult res = fd.ShowDialog(new WindowWrapper(GetActiveWindow()));
                filenames = res == DialogResult.OK ? fd.FileNames : new string[0];
            }

            return filenames;
        }

        public override string[] OpenFolders(string title, string directory, bool multiselect)
        {
            if (multiselect)
                Debug.LogWarning("'multiselect' for folders is not supported under Windows.");

            string[] foldernames = null;

            using (VistaFolderBrowserDialog fd = new VistaFolderBrowserDialog())
            {
                fd.Description = title;

                if (!string.IsNullOrEmpty(directory))
                {
                    fd.SelectedPath = getPath(directory);
                }

                DialogResult res = fd.ShowDialog(new WindowWrapper(GetActiveWindow()));
                foldernames = res == DialogResult.OK ? new[] { fd.SelectedPath } : new string[0];
            }

            return foldernames;
        }

        public override string SaveFile(string title, string directory, string defaultName, ExtensionFilter[] extensions)
        {
            string filename = null;

            using (VistaSaveFileDialog fd = new VistaSaveFileDialog())
            {
                fd.Title = title;

                string finalFilename = string.Empty;

                if (!string.IsNullOrEmpty(directory))
                {
                    finalFilename = getPath(directory);
                }

                if (!string.IsNullOrEmpty(defaultName))
                {
                    finalFilename += defaultName;
                }

                fd.FileName = finalFilename;

                if (extensions != null)
                {
                    fd.Filter = getFilterFromFileExtensionList(extensions);
                    fd.FilterIndex = 1;
                    fd.DefaultExt = extensions[0].Extensions[0];
                    fd.AddExtension = true;
                }
                else
                {
                    fd.DefaultExt = string.Empty;
                    fd.Filter = string.Empty;
                    fd.AddExtension = false;
                }

                DialogResult res = fd.ShowDialog(new WindowWrapper(GetActiveWindow()));
                filename = res == DialogResult.OK ? fd.FileName : string.Empty;
            }

            return filename;

        }

        public override void OpenFilesAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, Action<string[]> cb)
        {
            cb.Invoke(OpenFiles(title, directory, extensions, multiselect));
        }

        public override void OpenFoldersAsync(string title, string directory, bool multiselect, Action<string[]> cb)
        {
            cb.Invoke(OpenFolders(title, directory, multiselect));
        }

        public override void SaveFileAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<string> cb)
        {
            cb.Invoke(SaveFile(title, directory, defaultName, extensions));
        }

        #endregion


        #region Private methods

        private static string getFilterFromFileExtensionList(ExtensionFilter[] extensions)
        {
            string filterString = string.Empty;

            foreach (ExtensionFilter filter in extensions)
            {
                filterString += filter.Name + "(";

                foreach (string ext in filter.Extensions)
                {
                    filterString += "*." + ext + ",";
                }

                filterString = filterString.Remove(filterString.Length - 1);
                filterString += ") |";

                foreach (string ext in filter.Extensions)
                {
                    filterString += "*." + ext + "; ";
                }

                filterString += "|";
            }

            filterString = filterString.Remove(filterString.Length - 1);
            return filterString;
        }

        private static string getPath(string path)
        {
            string directoryPath = Path.GetFullPath(path);

            if (!directoryPath.EndsWith("\\"))
            {
                directoryPath += "\\";
            }

            if (Path.GetPathRoot(directoryPath) == directoryPath)
                return path;

            return Path.GetDirectoryName(directoryPath) + Path.DirectorySeparatorChar;
        }

        #endregion
    }

    public class WindowWrapper : IWin32Window
    {
        private IntPtr _hwnd;

        public WindowWrapper(IntPtr handle) { _hwnd = handle; }

        public IntPtr Handle { get { return _hwnd; } }
    }
}
#endif
// © 2017-2019 crosstales LLC (https://www.crosstales.com)