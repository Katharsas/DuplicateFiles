using PropertyChanged;
using SD.Tools.Algorithmia.GeneralDataStructures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPFFolderBrowser;
using WpfUtils;

namespace DuplicateFiles.ViewModel
{
    public class SearchVM : NotifyPropertyChanged
    {
        private int Id { get; }
        private Action<SearchInfo> OnResultStart { get; }
        private Action<SearchResultPart> OnResultPart { get; }
        private CancellationTokenSource CancelSource { get; set; }
        private Task<MultiValueDictionary<string, FileInfo>> SearchTask { get; set; }

        public DelegateCommand OnSearchDirDialog { get; set; }

        public string SearchRootDir { get; set; }

        public string SearchFilePattern { get; set; }

        public bool SearchOnlyTopDir { get; set; }

        private MatcherType _matcherType;
        public MatcherType MatcherType {
            get { return _matcherType; }
            set
            {
                _matcherType = value;
                isSimilarityThresholdEnabled = value == MatcherType.Similarity;
            }
        }

        public bool isSimilarityThresholdEnabled { get; set; }

        public string SimilarityThreshold { get; set; }

        public bool IsConstraintSameNameEnabled { get; set; }
        public bool IsConstraintSameCreationDateEnabled { get; set; }
        public bool IsConstraintSameModifiedDateEnabled { get; set; }

        public string SearchActionButtonLabel { get; set; }
        public DelegateCommand OnSearchAction { get; set; }

        private SearchState _searchState;
        public SearchState CurrentSearchState {
            get { return _searchState; }
            set
            {
                // TODO: investigate and bugreport Fody PropertyChanged
                // For whatever reason, if CheckForEquality is true (=default) in FodyWeavers settings,
                // the first call to this setter from constructor (which initializes CurrentSearchState)
                // will not get executed beacuse of weaved in EqualityCheck, although valus are not equal
                // at that point in time. Workaround is to disable CheckForEquality weaving.
                _searchState = value;
                switch (value)
                {
                    case SearchState.StoppedNoResults:
                        SearchActionButtonLabel = "Start Search";
                        SearchStatus = "Start a new search.";
                        break;
                    case SearchState.RunningNoResults:
                        SearchActionButtonLabel = "Cancel Search";
                        SearchStatus = "Search in progress.";
                        break;
                    case SearchState.StoppedShowingResults:
                        SearchActionButtonLabel = "Repeat Search";
                        break;
                }
            }
        }

        public string SearchStatus { get; set; }


        public SearchVM(int id, Action<SearchInfo> onResultStart, Action<SearchResultPart> onResultPart)
        {
            Id = id;
            OnResultStart = onResultStart;
            OnResultPart = onResultPart;

            OnSearchDirDialog = new DelegateCommand(_ =>
            {
                WPFFolderBrowserDialog dlg = new WPFFolderBrowserDialog();
                if (SearchRootDir != null)
                {
                    dlg.InitialDirectory = Path.GetFullPath(SearchRootDir);
                }
                bool? pathWasSelected = dlg.ShowDialog();
                if (pathWasSelected == true)
                    SearchRootDir = dlg.FileName;
            });

            SearchFilePattern = "*";
            SearchOnlyTopDir = false;
            MatcherType = MatcherType.LengthHash;
            SimilarityThreshold = string.Format("{0:N2}", 0.8f);

            IsConstraintSameNameEnabled = false;
            IsConstraintSameCreationDateEnabled = false;
            IsConstraintSameModifiedDateEnabled = false;

            SearchActionButtonLabel = "";
            this.CurrentSearchState = SearchState.StoppedNoResults;
            OnSearchAction = new DelegateCommand(_ =>
            {
                switch (CurrentSearchState)
                {
                    case SearchState.StoppedNoResults:
                        if (!validateSearchOptions()) return;
                        CurrentSearchState = SearchState.RunningNoResults;
                        executeSearch();
                        break;
                    case SearchState.RunningNoResults:
                        SearchStatus = "Canceling current search!";
                        CancelSource.Cancel();
                        break;
                    case SearchState.StoppedShowingResults:
                        // TODO clear results
                        if (!validateSearchOptions()) return;
                        CurrentSearchState = SearchState.RunningNoResults;
                        executeSearch();
                        break;
                }
            }, _ => {
                return !(CurrentSearchState == SearchState.RunningNoResults && CancelSource == null);
            });


            //IsRepeatOnStartEnabled = false;

            //OnTest = new DelegateCommand(_ => { Console.WriteLine(SearchActionButtonLabel); });
        }

        private bool validateSearchOptions()
        {
            string caption = "Invalid Settings";
            MessageBoxImage image = MessageBoxImage.Exclamation;
            if (SearchRootDir == null)
            {
                MessageBox.Show("Please specify the directory, where duplicated files should be searched in!",
                    caption, MessageBoxButton.OK, image);
                return false;
            }
            string dir = Path.GetFullPath(SearchRootDir);
            if (!Directory.Exists(dir))
            {
                MessageBox.Show("The selected directory does not exist or is not accessable!",
                    caption, MessageBoxButton.OK, image);
                return false;
            }

            if (MatcherType == MatcherType.Always)
            {
                if (!IsConstraintSameNameEnabled && !IsConstraintSameCreationDateEnabled && !IsConstraintSameModifiedDateEnabled)
                {
                    MessageBox.Show("For the selected identification method, at least one constraint must be enabled!",
                        caption, MessageBoxButton.OK, image);
                    return false;
                }
            }
            if (MatcherType == MatcherType.Similarity)
            {
                float threshold = float.Parse(SimilarityThreshold);
                if (threshold == 0)
                {
                    MessageBox.Show("The selected similarity threshold is zero, please use equivalent 'Constraints Only' method instead!",
                        caption, MessageBoxButton.OK, image);
                    return false;
                }
                if (threshold == 1)
                {
                    MessageBox.Show("The selected similarity threshold is 100%, please use equivalent 'Byte Similarity' method instead!",
                        caption, MessageBoxButton.OK, image);
                    return false;
                }
            }

            return true;
        }

        private void executeSearch()
        {
            var uiContext = SynchronizationContext.Current;

            HashSet<MatcherConstraint> constraints = new HashSet<MatcherConstraint>();
            if (IsConstraintSameNameEnabled)
            {
                constraints.Add(MatcherConstraint.SameName);
            }
            if (IsConstraintSameCreationDateEnabled)
            {
                constraints.Add(MatcherConstraint.SameCreationDate);
            }
            if (IsConstraintSameModifiedDateEnabled)
            {
                constraints.Add(MatcherConstraint.SameModifiedDate);
            }

            float similarityThreshold = float.Parse(SimilarityThreshold);

            SearchOptions searchOptions = new SearchOptions()
            {
                matcherType = MatcherType,
                similarityMatcherThreshold = similarityThreshold,

                matcherConstraints = constraints,
                searchRootDir = Path.GetFullPath(SearchRootDir),
                searchPattern = SearchFilePattern,
                searchOnlyTopDir = SearchOnlyTopDir,
            };
            OnResultStart.Invoke(null);
            CancelSource = new CancellationTokenSource();
            CancellationToken cancel = CancelSource.Token;
            SearchTask =  Task<MultiValueDictionary<string, FileInfo>>.Factory.StartNew(() =>
            {
                return SearchExecutor.match(searchOptions, cancel);
            }, cancel);

            SearchTask.ContinueWith(task =>
            {
                var result = task.Result;
                CancelSource = null;
                uiContext.Send(_ => OnSearchAction.RaiseCanExecuteChanged(), null);
                if (result == null)
                {
                    // task was canceled
                    SearchStatus = "Search was canceled.";
                    CurrentSearchState = SearchState.StoppedNoResults;
                    uiContext.Send(_ => OnResultStart.Invoke(null), null);
                }
                else
                {
                    if (task.Result.Count == 0)
                    {
                        SearchStatus = "No duplicates were found!";
                    }
                    else
                    {
                        SearchStatus = "Adding found duplicates...";
                        Thread.Sleep(100);

                        Console.WriteLine("Presenting Results: Started");
                        uiContext.Send(_ => OnResultStart.Invoke(new SearchInfo()
                        {
                            RootDir = searchOptions.searchRootDir,
                        }), null);

                        foreach (KeyValuePair<string, HashSet<FileInfo>> Entry in task.Result)
                        {
                            uiContext.Send(_ => OnResultPart.Invoke(new SearchResultPart()
                            {
                                GroupKey = Entry.Key,
                                Group = Entry.Value,
                            }), null);
                            Thread.Sleep(15);
                        }
                        Console.WriteLine("Presenting Results: Done");

                        SearchStatus = "Displaying all found groups of duplicates (" + task.Result.Count + ") !";
                    }
                    CurrentSearchState = SearchState.StoppedShowingResults;
                }
            });
        }
    }

    public class SearchInfo
    {
        public string RootDir { get; set; }
    }

    public class SearchResultPart
    {
        public string GroupKey { get; set; }
        public HashSet<FileInfo> Group { get; set; }
    }
}
