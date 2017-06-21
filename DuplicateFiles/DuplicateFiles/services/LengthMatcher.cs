using SD.Tools.Algorithmia.GeneralDataStructures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateFiles
{
    class LengthMatcher
    {
        public static MultiValueDictionary<String, FileInfo> matchByLength(List<FileInfo> candidates)
        {
            MultiValueDictionary<String, FileInfo> checkedFiles = new MultiValueDictionary<string, FileInfo>();

            foreach (FileInfo file in candidates) {
                string key = "Length: " + file.Length;
                checkedFiles.Add(key, file);
            }

            return ComparisonUtils.removeOneElementGroups(checkedFiles);
        }
    }

    class LengthComparisonResult
    {
        public LengthComparisonResult(bool result)
        {
            this.result = result;
        }

        public bool result { get; }
    }

    class LengthComparison
    {
        public LengthComparisonResult compareFilesByLength(FileInfo fileA, FileInfo fileB)
        {
            if (fileA.FullName == fileB.FullName)
            {
                throw new ArgumentException("Cannot compare file with itself!");
            }
            return new LengthComparisonResult(fileA.Length == fileB.Length);
        }
    }
}
