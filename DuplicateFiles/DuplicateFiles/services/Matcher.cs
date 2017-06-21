using SD.Tools.Algorithmia.GeneralDataStructures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DuplicateFiles
{
    public class SearchOptions
    {
        public MatcherType matcherType { get; set; }
        public float similarityMatcherThreshold { get; set; }

        public HashSet<MatcherConstraint> matcherConstraints { get; set; }
        public string searchRootDir { get; set; }
        public string searchPattern { get; set; }
        public bool searchOnlyTopDir { get; set; }
    }

    class SearchExecutor
    {
        public static MultiValueDictionary<string, FileInfo> match(SearchOptions options, CancellationToken cancel)
        {
            Console.WriteLine("Find candidates: Started");
            List<FileInfo> candidates = findCandidates(options);
            if (cancel.IsCancellationRequested)
            {
                return null;
            }
            Console.WriteLine("Find candidates: Done");
            Console.WriteLine("Match 1st stage: Started");
            MultiValueDictionary<string, FileInfo> matched = matchIntoGroups(options, candidates, cancel);
            if (cancel.IsCancellationRequested)
            {
                return null;
            }
            Console.WriteLine("Match 1st stage: Done");
            Console.WriteLine("Match 2nd stage: Started");
            foreach (MatcherConstraint constraint in options.matcherConstraints)
            {
                matched = matchConstraints(matched, constraint);
                if (cancel.IsCancellationRequested)
                {
                    return null;
                }
            }
            Console.WriteLine("Match 2nd stage: Done");

            return matched;
        }

        private static List<FileInfo> findCandidates(SearchOptions options)
        {
            SearchOption onlyTopDir = options.searchOnlyTopDir ?
                SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;

            string rootDir = options.searchRootDir;

            try
            {
                IEnumerable<string> files = Directory.EnumerateFiles(rootDir, options.searchPattern, onlyTopDir);
                return files
                .Select(filePath => new FileInfo(filePath))
                .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                MessageBox.Show(ex.Message,
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<FileInfo>();
            }
            
        }

        private static MultiValueDictionary<string, FileInfo> matchIntoGroups(SearchOptions options, List<FileInfo> candidates, CancellationToken cancel) {
            switch (options.matcherType)
            {
                case MatcherType.Always:
                    {
                        var singleGroup = new MultiValueDictionary<string, FileInfo>();
                        foreach (FileInfo file in candidates)
                        {
                            singleGroup.Add("Any", file);
                        }
                        return ComparisonUtils.removeOneElementGroups(singleGroup);
                    }
                case MatcherType.Length:
                    {
                        return LengthMatcher.matchByLength(candidates);
                    }
                case MatcherType.LengthHash:
                    {
                        return HashContentMatcher.matchByHashOrContent(candidates, options.matcherType, cancel);
                    }
                case MatcherType.LengthHashContent:
                    {
                        return HashContentMatcher.matchByHashOrContent(candidates, options.matcherType, cancel);
                    }
                case MatcherType.Similarity:
                    {
                        return SimilarityMatcher.matchBySimilarity(candidates, options.similarityMatcherThreshold, cancel);
                    }
                default:
                    throw new ArgumentException("Invalid MatcherType!");
            }
        }

        private static MultiValueDictionary<string, FileInfo> matchConstraints(MultiValueDictionary<string, FileInfo> groups, MatcherConstraint constraint)
        {
            string constraintDescription;
            Func<FileInfo, string> getConstraintInfo;

            switch (constraint)
            {
                case MatcherConstraint.SameName:
                    constraintDescription = "Filename";
                    getConstraintInfo = info => info.Name;
                    break;
                case MatcherConstraint.SameCreationDate:
                    constraintDescription = "Creation Date";
                    getConstraintInfo = info => info.CreationTime.ToString();
                    break;
                case MatcherConstraint.SameModifiedDate:
                    constraintDescription = "Modfied Date";
                    getConstraintInfo = info => info.LastWriteTime.ToString();
                    break;
                default:
                    throw new ArgumentException("Invalid MatcherConstraint");
            }

            var regrouped = new MultiValueDictionary<string, FileInfo>(); ;

            foreach (KeyValuePair<string, HashSet<FileInfo>> entry in groups)
            {
                foreach (FileInfo info in entry.Value)
                {
                    string constraintValue = getConstraintInfo.Invoke(info);
                    string constraintKey = constraintDescription + ": " + constraintValue;
                    string newkey = entry.Key + ",  " + constraintKey;
                    regrouped.Add(newkey, info);
                }
            }

            return ComparisonUtils.removeOneElementGroups(regrouped);
        }
    }
}
