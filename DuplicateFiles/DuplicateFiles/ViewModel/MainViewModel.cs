using GalaSoft.MvvmLight;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using WPFFolderBrowser;
using WpfUtils;

namespace DuplicateFiles.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private int SearchCounter { get; set; }

        public DelegateCommand OnExit { get; set; }

        public DelegateCommand OnNewSearch { get; set; }

        public MyBindingList<SearchVM> Searches { get; private set; }

        public SearchVM SelectedSearch { get; set; }

        public SearchResultVM SearchResult { get; set; }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            SearchCounter = 0;

            OnExit = new DelegateCommand(_ =>
            {
                // TODO interrupt running search threads?
                Application.Current.Shutdown();
            });

            OnNewSearch = new DelegateCommand(_ =>
            {
                var Search = new SearchVM(SearchCounter, searchResultStart =>
                {
                    if (searchResultStart == null)
                    {
                        SearchResult = null;
                    }
                    else
                    {
                        SearchResult = new SearchResultVM(searchResultStart.RootDir);
                    }
                }, searchResultPart =>
                {
                    SearchResult.Add(searchResultPart);
                });
                Searches.Add(Search);
                SearchCounter++;
                SelectedSearch = Search;
            });

            Searches = new MyBindingList<SearchVM>();

            // TODO remove
            OnNewSearch.Execute(null);
        }
    }
}