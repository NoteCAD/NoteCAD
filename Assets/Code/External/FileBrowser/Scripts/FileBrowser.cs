using UnityEngine;

namespace Crosstales.FB
{
    /// <summary>Native file browser various actions like open file, open folder and save file.</summary>
    public class FileBrowser
    {
        #region Variables

        private static Wrapper.IFileBrowser platformWrapper;

        #endregion


        #region Constructor

        static FileBrowser()
        {
#if UNITY_EDITOR
                platformWrapper = new Wrapper.FileBrowserEditor();
#elif UNITY_STANDALONE_OSX
                platformWrapper = new Wrapper.FileBrowserMac();
#elif UNITY_STANDALONE_WIN
                platformWrapper = new Wrapper.FileBrowserWindows();
#elif UNITY_STANDALONE_LINUX
                platformWrapper = new Wrapper.FileBrowserLinux();
#else
                platformWrapper = new Wrapper.FileBrowserGeneric();
#endif
        }

#endregion


#region Public methods

        /// <summary>Open native file browser for a single file.</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="extension">Allowed extension, e.g. "png"</param>
        /// <returns>Returns a string of the chosen file. Empty string when cancelled</returns>
        public static string OpenSingleFile(string title, string directory, string extension)
        {
            return OpenSingleFile(title, directory, getFilter(extension));
        }

        /// <summary>Open native file browser for a single file.</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
        /// <returns>Returns a string of the chosen file. Empty string when cancelled</returns>
        public static string OpenSingleFile(string title, string directory, ExtensionFilter[] extensions)
        {
            return platformWrapper.OpenSingleFile(title, directory, extensions);
        }

        /// <summary>Open native file browser for multiple files.</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="extension">Allowed extension, e.g. "png"</param>
        /// <param name="multiselect">Allow multiple file selection</param>
        /// <returns>Returns array of chosen files. Zero length array when cancelled</returns>
        public static string[] OpenFiles(string title, string directory, string extension, bool multiselect)
        {
            return OpenFiles(title, directory, getFilter(extension), multiselect);
        }

        /// <summary>Open native file browser for multiple files.</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
        /// <param name="multiselect">Allow multiple file selection</param>
        /// <returns>Returns array of chosen files. Zero length array when cancelled</returns>
        public static string[] OpenFiles(string title, string directory, ExtensionFilter[] extensions, bool multiselect)
        {
            return platformWrapper.OpenFiles(title, directory, extensions, multiselect);
        }

        /// <summary>Open native folder browser for a single folder.</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory (default: current, optional)</param>
        /// <returns>Returns a string of the chosen folder. Empty string when cancelled</returns>
        public static string OpenSingleFolder(string title, string directory = "")
        {
            return platformWrapper.OpenSingleFolder(title, directory);
        }

        /// <summary>
        /// Open native folder browser for multiple folders.
        /// NOTE: Multiple folder selection isnt't supported on Windows!
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory (default: current, optional)</param>
        /// <param name="multiselect">Allow multiple folder selection (default: true, optional)</param>
        /// <returns>Returns array of chosen folders. Zero length array when cancelled</returns>
        public static string[] OpenFolders(string title, string directory = "", bool multiselect = true)
        {
            return platformWrapper.OpenFolders(title, directory, multiselect);
        }

        /// <summary>Open native save file browser</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="defaultName">Default file name</param>
        /// <param name="extension">File extension, e.g. "png"</param>
        /// <returns>Returns chosen file. Empty string when cancelled</returns>
        public static string SaveFile(string title, string directory, string defaultName, string extension)
        {
            return SaveFile(title, directory, defaultName, getFilter(extension));
        }

        /// <summary>Open native save file browser</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="defaultName">Default file name</param>
        /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
        /// <returns>Returns chosen file. Empty string when cancelled</returns>
        public static string SaveFile(string title, string directory, string defaultName, ExtensionFilter[] extensions)
        {
            return platformWrapper.SaveFile(title, directory, defaultName, extensions);
        }

        /*
        /// <summary>Open native file browser for multiple files.</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="extension">Allowed extension, e.g. "png"</param>
        /// <param name="multiselect">Allow multiple file selection</param>
        /// <param name="cb">Callback for the async operation.</param>
        /// <returns>Returns array of chosen files. Zero length array when cancelled</returns>
        public static void OpenFilesAsync(string title, string directory, string extension, bool multiselect, System.Action<string[]> cb)
        {
            OpenFilesAsync(title, directory, getFilter(extension), multiselect, cb);
        }

        /// <summary>Open native file browser for multiple files (async).</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
        /// <param name="multiselect">Allow multiple file selection</param>
        /// <param name="cb">Callback for the async operation.</param>
        /// <returns>Returns array of chosen files. Zero length array when cancelled</returns>
        public static void OpenFilesAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, System.Action<string[]> cb)
        {
            //System.Threading.Thread worker = new System.Threading.Thread(() => platformWrapper.OpenFilesAsync(title, directory, extensions, multiselect, cb));
            //worker.Start();
            platformWrapper.OpenFilesAsync(title, directory, extensions, multiselect, cb);
        }

        /// <summary>Open native folder browser for multiple folders (async).</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="multiselect"></param>
        /// <param name="cb">Callback for the async operation.</param>
        /// <returns>Returns array of chosen folders. Zero length array when cancelled</returns>
        public static void OpenFoldersAsync(string title, string directory, bool multiselect, System.Action<string[]> cb)
        {
            //System.Threading.Thread worker = new System.Threading.Thread(() => platformWrapper.OpenFoldersAsync(title, directory, multiselect, cb));
            //worker.Start();
            platformWrapper.OpenFoldersAsync(title, directory, multiselect, cb);
        }

        /// <summary>Open native save file browser</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="defaultName">Default file name</param>
        /// <param name="extension">File extension, e.g. "png"</param>
        /// <param name="cb">Callback for the async operation.</param>
        /// <returns>Returns chosen file. Empty string when cancelled</returns>
        public static void SaveFileAsync(string title, string directory, string defaultName, string extension, System.Action<string> cb)
        {
            SaveFileAsync(title, directory, defaultName, getFilter(extension), cb);
        }

        /// <summary>Open native save file browser (async).</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="defaultName">Default file name</param>
        /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
        /// <param name="cb">Callback for the async operation.</param>
        /// <returns>Returns chosen file. Empty string when cancelled</returns>
        public static void SaveFileAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, System.Action<string> cb)
        {
            //System.Threading.Thread worker = new System.Threading.Thread(() => platformWrapper.SaveFileAsync(title, directory, defaultName, extensions, cb));
            //worker.Start();
            platformWrapper.SaveFileAsync(title, directory, defaultName, extensions, cb);
        }
        */

        /// <summary>
        /// Find files inside a path.
        /// </summary>
        /// <param name="path">Path to find the files</param>
        /// <param name="extension">Extension for the file search</param>
        /// <param name="isRecursive">Recursive search (default: false, optional)</param>
        /// <returns>Returns array of the found files inside the path. Zero length array when an error occured.</returns>
        public static string[] GetFiles(string path, string extension, bool isRecursive = false)
        {
            return GetFiles(path, getFilter(extension), isRecursive);
        }

        /// <summary>
        /// Find files inside a path.
        /// </summary>
        /// <param name="path">Path to find the files</param>
        /// <param name="extensions">List of extension filters for the find. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
        /// <param name="isRecursive">Recursive search (default: false, optional)</param>
        /// <returns>Returns array of the found files inside the path. Zero length array when an error occured.</returns>
        public static string[] GetFiles(string path, ExtensionFilter[] extensions, bool isRecursive = false)
        {
            try
            {
                if (extensions == null)
                {
                    return System.IO.Directory.GetFiles(path, "*", isRecursive ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly);
                }
                else
                {
                    System.Collections.Generic.List<string> files = new System.Collections.Generic.List<string>();

                    foreach (ExtensionFilter extensionFilter in extensions)
                    {
                        foreach (string ext in extensionFilter.Extensions)
                        {
                            files.AddRange(System.IO.Directory.GetFiles(path, "*." + ext, isRecursive ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly));
                        }
                    }

                    return files.ToArray();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Could not scan the path for files: " + ex);
            }

            return new string[0];
        }

        /// <summary>
        /// Find directories inside a path without recursion.
        /// </summary>
        /// <param name="path">Path to find the directories</param>
        /// <param name="isRecursive">Recursive search (default: false, optional)</param>
        /// <returns>Returns array of the found directories inside the path. Zero length array when an error occured.</returns>
        public static string[] GetDirectories(string path, bool isRecursive = false)
        {
            try
            {
                return System.IO.Directory.GetDirectories(path, "*", isRecursive ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("Could not scan the path for directories: " + ex);
            }

            return new string[0];
        }

#endregion


#region Private methods

        private static ExtensionFilter[] getFilter(string extension)
        {
            return string.IsNullOrEmpty(extension) ? null : new[] { new ExtensionFilter(string.Empty, extension) };
        }

#endregion
    }

    /// <summary>Filter for extensions.</summary>
    public struct ExtensionFilter
    {
        public string Name;
        public string[] Extensions;

        public ExtensionFilter(string filterName, params string[] filterExtensions)
        {
            Name = filterName;
            Extensions = filterExtensions;
        }
    }
}
// © 2017-2019 crosstales LLC (https://www.crosstales.com)