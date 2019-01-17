#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

namespace Crosstales.FB.Wrapper
{
    public class FileBrowserEditor : FileBrowserBase
    {
        #region Implemented methods

        public override string[] OpenFiles(string title, string directory, ExtensionFilter[] extensions, bool multiselect)
        {
            if (multiselect)
                Debug.LogWarning("'multiselect' for files is not supported in the Editor.");
            
            string path = string.Empty;

            if (extensions == null)
            {
                path = EditorUtility.OpenFilePanel(title, directory, string.Empty);
            }
            else
            {
                path = EditorUtility.OpenFilePanelWithFilters(title, directory, getFilterFromFileExtensionList(extensions));
            }

            return string.IsNullOrEmpty(path) ? new string[0] : new[] { path };
        }

        public override string[] OpenFolders(string title, string directory, bool multiselect)
        {
            if (multiselect)
                Debug.LogWarning("'multiselect' for folders is not supported in the Editor.");

            string path = EditorUtility.OpenFolderPanel(title, directory, string.Empty);

            return string.IsNullOrEmpty(path) ? new string[0] : new[] { path };
        }

        public override string SaveFile(string title, string directory, string defaultName, ExtensionFilter[] extensions)
        {
            if (extensions.Length > 1)
                Debug.LogWarning("Multiple 'extensions' are not supported in the Editor.");
            
            string ext = extensions != null ? extensions[0].Extensions[0] : string.Empty;
            string name = string.IsNullOrEmpty(ext) ? defaultName : defaultName + "." + ext;

            return EditorUtility.SaveFilePanel(title, directory, name, ext);
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

        private static string[] getFilterFromFileExtensionList(ExtensionFilter[] extensions)
        {
            string[] filters = new string[extensions.Length * 2];

            for (int i = 0; i < extensions.Length; i++)
            {
                filters[(i * 2)] = extensions[i].Name;
                filters[(i * 2) + 1] = string.Join(",", extensions[i].Extensions);
            }

            return filters;
        }

        #endregion
    }
}
#endif
// © 2017-2019 crosstales LLC (https://www.crosstales.com)