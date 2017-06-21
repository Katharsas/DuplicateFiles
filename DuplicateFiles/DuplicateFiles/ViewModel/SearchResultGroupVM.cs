using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfUtils;

namespace DuplicateFiles.ViewModel
{
    public class SearchResultGroupVM : NotifyPropertyChanged
    {
        public string GroupHeader { get; set; }

        public MyBindingList<SearchResultFileVM> Files { get; set; }

        public SearchResultGroupVM(string groupKey, string rootDir, List<FileInfo> files)
        {
            GroupHeader = groupKey;
            var filesTemp = new MyBindingList<SearchResultFileVM>();
            foreach (FileInfo file in files)
            {
                string filePath = file.FullName;
                filePath = filePath.Substring(rootDir.Length);
                filePath = filePath.Substring(0, filePath.Length - file.Name.Length);
                filesTemp.Add(new SearchResultFileVM(filePath, file.Name, groupKey, file));
            }

            Files = filesTemp;
        }
    }
}
