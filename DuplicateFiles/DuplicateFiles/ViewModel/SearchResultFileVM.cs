using System.IO;
using WpfUtils;

namespace DuplicateFiles.ViewModel
{
    public class SearchResultFileVM : NotifyPropertyChanged
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }

        public string GroupKey { get; }
        public FileInfo FileInfo { get; }

        public SearchResultFileVM(string filePath, string filename, string groupKey, FileInfo info)
        {
            FilePath = filePath;
            FileName = filename;

            GroupKey = groupKey;
            FileInfo = info;
        }
    }
}
