using System;

namespace ProcessMon {

    using System.IO;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text;

    public static class Extensions {

        /// <summary>
        /// Attempts to get a given file from the current DirectoryInfo object.
        /// </summary>
        /// <param name="dirInfo">The current DirectoryInfo object.</param>
        /// <param name="filename">The name of the file to retrieve. Leave empty (null) to get the first file in the directory.</param>
        /// <returns>An instance of FileInfo representing the file requested or null if no file was found.</returns>
        public static FileInfo GetFile(this DirectoryInfo dirInfo, string filename = null) => dirInfo.EnumerateFiles().FirstOrDefault(x => filename == null ? true : x.Name.Equals(filename, StringComparison.InvariantCulture));

        public static DirectoryInfo GetSubdirectory(this DirectoryInfo dirInfo, string dirName = null) => dirInfo.EnumerateDirectories().FirstOrDefault(x => dirName == null ? true : x.Name.Equals(dirName, StringComparison.InvariantCulture));

        public static FileInfo CreateFile(this DirectoryInfo dirInfo, string fileInfo) {
            if (string.IsNullOrEmpty(fileInfo)) throw new ArgumentException("fileInfo must not be empty!", nameof(fileInfo));

            if (GetFile(dirInfo, fileInfo) != default) throw new IOException($"File { fileInfo } already exists!");

            var fInfo = new FileInfo(Path.Combine(dirInfo.FullName, fileInfo));

            using (fInfo.Create())
                return fInfo;
        }

    }
}
