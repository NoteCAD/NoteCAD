namespace Crosstales.FB.Wrapper
{
    /// <summary>Base class for all file browsers.</summary>
    public abstract class FileBrowserBase : IFileBrowser
    {
        #region Implemented methods

        /// <summary>Open native file browser for a single file.</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
        /// <returns>Returns a string of the chosen file. Empty string when cancelled</returns>
        public string OpenSingleFile(string title, string directory, ExtensionFilter[] extensions)
        {
            string[] files = OpenFiles(title, directory, extensions, false);
            return files.Length > 0 ? files[0] : string.Empty;
        }

        /// <summary>Open native file browser for multiple files.</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
        /// <param name="multiselect">Allow multiple file selection</param>
        /// <returns>Returns array of chosen files. Zero length array when cancelled</returns>
        public abstract string[] OpenFiles(string title, string directory, ExtensionFilter[] extensions, bool multiselect);

        /// <summary>Open native folder browser for a single folder.</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <returns>Returns a string of the chosen folder. Empty string when cancelled</returns>
        public string OpenSingleFolder(string title, string directory)
        {
            string[] folders = OpenFolders(title, directory, false);
            return folders.Length > 0 ? folders[0] : string.Empty;
        }

        /// <summary>Open native folder browser for multiple folders.</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="multiselect">Allow multiple folder selection</param>
        /// <returns>Returns array of chosen folders. Zero length array when cancelled</returns>
        public abstract string[] OpenFolders(string title, string directory, bool multiselect);

        /// <summary>Open native save file browser.</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="defaultName">Default file name</param>
        /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
        /// <returns>Returns chosen file. Empty string when cancelled</returns>
        public abstract string SaveFile(string title, string directory, string defaultName, ExtensionFilter[] extensions);

        /// <summary>Open native file browser for multiple files (async).</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
        /// <param name="multiselect">Allow multiple file selection</param>
        /// <param name="cb">Callback for the async operation.</param>
        /// <returns>Returns array of chosen files. Zero length array when cancelled</returns>
        public abstract void OpenFilesAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, System.Action<string[]> cb);

        /// <summary>Open native folder browser for multiple folders (async).</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="multiselect">Allow multiple folder selection</param>
        /// <param name="cb">Callback for the async operation.</param>
        /// <returns>Returns array of chosen folders. Zero length array when cancelled</returns>
        public abstract void OpenFoldersAsync(string title, string directory, bool multiselect, System.Action<string[]> cb);

        /// <summary>Open native save file browser (async).</summary>
        /// <param name="title">Dialog title</param>
        /// <param name="directory">Root directory</param>
        /// <param name="defaultName">Default file name</param>
        /// <param name="extensions">List of extension filters. Filter Example: new ExtensionFilter("Image Files", "jpg", "png")</param>
        /// <param name="cb">Callback for the async operation.</param>
        /// <returns>Returns chosen file. Empty string when cancelled</returns>
        public abstract void SaveFileAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, System.Action<string> cb);

        #endregion
    }
}
// © 2017-2019 crosstales LLC (https://www.crosstales.com)