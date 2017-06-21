using SD.Tools.Algorithmia.GeneralDataStructures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfUtils;
using Microsoft.VisualBasic.FileIO;
using System.Threading;

namespace DuplicateFiles.ViewModel
{
    public class SearchResultVM : NotifyPropertyChanged
    {
        public SearchResultFileVM SelectedFile { get; set; }

        public DelegateCommand OnDeleteSelected { get; set; }

        public MyBindingList<SearchResultGroupVM> ResultGroups { get; set; }

        private string RootDir { get; }
        //private Map<string,  SearchResult { get; }

        public SearchResultVM(string rootDir)
        {
            RootDir = rootDir;
            ResultGroups = new MyBindingList<SearchResultGroupVM>();

            OnDeleteSelected = new DelegateCommand(_ =>
            {
                string groupKey = SelectedFile.GroupKey;
                //searchResult.Groups.GetValues(groupKey, false).Remove(SelectedFile.FileInfo);

                FileSystem.DeleteFile(SelectedFile.FileInfo.FullName, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);

                foreach (SearchResultGroupVM groupVM in ResultGroups)
                {
                    if (groupVM.GroupHeader == groupKey)
                    {
                        groupVM.Files.Remove(SelectedFile);
                        break;
                    }
                }

            }, _ => {
                return SelectedFile != null;
            });

            //SearchResult = new SearchResult;


            //SearchResult result = new SearchResult()
            //{
            //    RootDir = null,
            //    Groups = new MultiValueDictionary<string, FileInfo>()
            //};

        }

        public void Add(SearchResultPart part)
        {
            ResultGroups.Add(new SearchResultGroupVM(part.GroupKey, RootDir, part.Group.ToList()));
        }

        //public SearchResultVM(SearchResult searchResult)
        //{
        //    //SearchResult = searchResult;
        //    //var tempResultGroups = new MyBindingList<SearchResultGroupVM>();

        //    foreach (KeyValuePair<string, HashSet<FileInfo>> entry in searchResult.Groups)
        //    {
        //        //Thread.Sleep(100);
        //        //Console.WriteLine("\nKey: " + entry.Key);
        //        //Console.WriteLine("Count: " + entry.Value.Count);
        //        List<FileInfo> files = entry.Value.ToList();
        //        // TODO sort and always resort when sorting options change
        //        tempResultGroups.Add(new SearchResultGroupVM(entry.Key, searchResult.RootDir, files));
        //    }
        //    ResultGroups = tempResultGroups;

            
        //}
    }
}
