using SD.Tools.Algorithmia.GeneralDataStructures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DuplicateFiles
{
    class HashContentMatcher
    {
        public static MultiValueDictionary<string, FileInfo> matchByHashOrContent(List<FileInfo> candidates, MatcherType type, CancellationToken cancel)
        {
            if (!(type == MatcherType.LengthHash || type == MatcherType.LengthHashContent))
            {
                throw new ArgumentException("Can only match by lengthHash or lengthHashContent!");
            }

            // group by length
            var lengthToFiles = new MultiValueDictionary<long, FileInfo>();

            foreach (FileInfo file in candidates)
            {
                lengthToFiles.Add(file.Length, file);
            }

            // filter out one element length groups, regroup remaining by hash
            var hashToFiles = new MultiValueDictionary<string, FileInfo>();

            using (var md5 = MD5.Create())
            {
                foreach (KeyValuePair<long, HashSet<FileInfo>> entry in lengthToFiles)
                {
                    if (entry.Value.Count > 1)
                    {
                        foreach (FileInfo file in entry.Value)
                        {
                            using (var stream = File.OpenRead(file.FullName))
                            {
                                byte[] hashBytes = md5.ComputeHash(stream);
                                string hash = md5ToString(hashBytes);

                                if (type == MatcherType.LengthHash)
                                {
                                    // in case of LengthHash, we make hash key nice for display
                                    hashToFiles.Add("Hash: " + hash, file);
                                }
                                else
                                {
                                    // in case of LengthHashContent, we don't need to becaue they are intermediate
                                    hashToFiles.Add(hash, file);
                                }
                            }
                            if (cancel.IsCancellationRequested)
                            {
                                return null;
                            }
                        }
                    }
                }
            }
            if (type == MatcherType.LengthHash)
            {
                return ComparisonUtils.removeOneElementGroups(hashToFiles);
            }

            // filter out one element hash groups, regroup remaining by content
            var idToFiles = new MultiValueDictionary<string, FileInfo>();

            // we use this map to be able to only compare with content groups from the same hash groups
            // (we only need to compare all files with each other inside a hash group, so we store the hierarchy)
            var hashToIds = new MultiValueDictionary<string, string>();

            int matchCount = 0;
            foreach (KeyValuePair<string, HashSet<FileInfo>> entry in hashToFiles)
            {
                if (entry.Value.Count > 1)
                {
                    foreach (FileInfo fileA in entry.Value)
                    {
                        bool foundAGroup = false;

                        if (hashToIds.ContainsKey(entry.Key))
                        {
                            foreach (string id in hashToIds[entry.Key])
                            {
                                foreach (FileInfo fileB in idToFiles[id])
                                {
                                    ContentComparisonResult comparisonResult = HashContentComparison.compareFilesByContent(fileA, fileB, cancel);
                                    if (cancel.IsCancellationRequested)
                                    {
                                        return null;
                                    }
                                    if (comparisonResult.result)
                                    {
                                        // even though we are iterating result, we know that we are only adding an item to the hashset inside value, not a key or value directly
                                        // thats why we should be able to modify result regardles
                                        idToFiles.Add(id, fileA);
                                        foundAGroup = true;
                                    }
                                    break;
                                }
                                if (foundAGroup)
                                {
                                    break;
                                }
                            }
                        }

                        // only if we didnt find an existing group, we happily create our own group, so that later files from the same hash can join our group
                        if (!foundAGroup)
                        {
                            string key = "Id: " + matchCount++;
                            idToFiles.Add(key, fileA);
                            hashToIds.Add(entry.Key, key);
                        }
                    }
                    if (cancel.IsCancellationRequested)
                    {
                        return null;
                    }
                }
            }

            // let's just hope there are no bugs in here, not time for testing colliding hashes and dictionary juggling
            return ComparisonUtils.removeOneElementGroups(idToFiles);
        }
        

        private static string md5ToString(byte[] md5)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < md5.Length; i++)

            {
                sb.Append(md5[i].ToString("x2"));
            }

            return sb.ToString();
        }
    }

    class ContentComparisonResult
    {
        public ContentComparisonResult(bool result)
        {
            this.result = result;
        }

        public bool result { get; }
    }

    class HashContentComparison
    {
        private const int SIZE_OF_INT64 = sizeof(Int64);

        public static ContentComparisonResult compareFilesByContent(FileInfo fileA, FileInfo fileB, CancellationToken cancel)
        {
            if (fileA.Length != fileB.Length)
            {
                return new ContentComparisonResult(false);
            }
            else if (fileA.FullName == fileB.FullName)
            {
                throw new ArgumentException("Cannot compare file with itself!");
            }
            else
            {
                // inspired by https://stackoverflow.com/a/1359947
                // TODO: could be much improved for HDDs by loading even bigger file pieces than 8 byte, for example 4KB pieces
                // TODO: rare case: could be even more improved to allow more than 2 files to be compared at the same time,
                // reducing the need to compare every file against each other (in that case hash grouping can be removed entirly)

                int iterations = (int) Math.Ceiling((double)fileA.Length / SIZE_OF_INT64);

                using (FileStream fsA = fileA.OpenRead())
                using (FileStream fsB = fileB.OpenRead())
                {
                    byte[] currentA = new byte[SIZE_OF_INT64];
                    byte[] currentB = new byte[SIZE_OF_INT64];

                    for (int i = 0; i < iterations; i++)
                    {
                        fsA.Read(currentA, 0, SIZE_OF_INT64);
                        fsB.Read(currentB, 0, SIZE_OF_INT64);

                        if (BitConverter.ToInt64(currentA, 0) != BitConverter.ToInt64(currentB, 0))
                        {
                            return new ContentComparisonResult(false);
                        }
                        if (cancel.IsCancellationRequested)
                        {
                            return null;
                        }
                    }
                }

                return new ContentComparisonResult(true);
            }
        }
    }
}
