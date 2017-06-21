using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using SD.Tools.Algorithmia.GeneralDataStructures;
using System.Threading;

namespace DuplicateFiles
{
    class SimilarityMatcher
    {
        public const long MAX_FILE_SIZE = 20000; //20 KB;

        public static MultiValueDictionary<String, FileInfo> matchBySimilarity(List<FileInfo> candidates, float similarityThreshold, CancellationToken cancel)
        {
            MultiValueDictionary<String, FileInfo> result = new MultiValueDictionary<string, FileInfo>();

            List<FileInfo> checkedFiles = new List<FileInfo>();

            int matchCount = 0;

            foreach (FileInfo fileA in candidates)
            {
                if (fileA.Length > MAX_FILE_SIZE) break;

                foreach (FileInfo fileB in checkedFiles)
                {
                    FileMatcherResult comparisonResult = SimilarityScore.calculateSimilarity(fileA, fileB, cancel);
                    if (cancel.IsCancellationRequested)
                    {
                        return null;
                    }
                    if (comparisonResult.matchFailed)
                    {
                        throw new InvalidOperationException(comparisonResult.matchFailedMessage);
                    }
                    if (comparisonResult.result >= similarityThreshold)
                    {
                        String key = "Id: " + matchCount++ + ", Similarity: " + comparisonResult.result;
                        result.Add(key, fileA);
                        result.Add(key, fileB);
                    }
                }

                checkedFiles.Add(fileA);
            }

            return result;
        }
    }

    class FileMatcherResult
    {
        public FileMatcherResult(float result)
        {
            this.matchFailed = false;
            this.result = result;
        }

        public FileMatcherResult(string matchFailedMessage)
        {
            this.matchFailed = true;
            this.matchFailedMessage = matchFailedMessage;
        }

        public bool matchFailed { get; }
        public string matchFailedMessage { get; }

        public float result { get; }
    }

    class SimilarityScore
    {
        private const long MAX_FILE_SIZE = 2000000000L; // should be much smaller than MAX_INT, technical limit 

        public static FileMatcherResult calculateSimilarity(FileInfo fileA, FileInfo fileB, CancellationToken cancel)
        {
            if (fileA.FullName == fileB.FullName)
            {
                throw new ArgumentException("Cannot compare file with itself!");
            }
            long lengthA = fileA.Length;
            long lengthB = fileB.Length;


            if (lengthA > MAX_FILE_SIZE)
            {
                return new FileMatcherResult("File '" + fileA.Name + "' too big!");
            }
            else if (lengthB > MAX_FILE_SIZE)
            {
                return new FileMatcherResult("File '" + fileB.Name + "' too big!");
            } else
            {
                float length = Math.Max(lengthA, lengthB);
                byte[] dataA = File.ReadAllBytes(fileA.FullName);
                byte[] dataB = File.ReadAllBytes(fileB.FullName);
                int distance = calculateDistance(dataA, dataB, cancel);
                if (cancel.IsCancellationRequested)
                {
                    return null;
                }

                if (distance == 0)
                {
                    return new FileMatcherResult(1);
                }
                else
                {
                    float similarity = 1 - (distance / length);
                    return new FileMatcherResult(similarity);
                }
                
            }
        }


        /// <summary>
        /// Implementation of the Levenshtein distance according to
        /// https://en.wikipedia.org/wiki/Levenshtein_distance#Iterative_with_two_matrix_rows
        /// </summary>
        /// <param name="dataA"></param>
        /// <param name="dataB"></param>
        /// <returns></returns>
        public static int calculateDistance(byte[] dataA, byte[] dataB, CancellationToken cancel)
        {
            // We calculate the matrix with (dataA.Length + 1) columns and (dataB.Length + 1) rows.
            // To calculate the next row, only the results from the previous are needed, thus 2 rows.
            // The end result is the last cell in the last row.
            int[] scoreRow0 = new int[dataB.Length + 1];
            int[] scoreRow1 = scoreRow0; // will be reinitialized if more than 1 row is needed

            // init case (0, i)
            // which means dataA is of length 0, dataB is of length i
            // => result is number of needed changes = i (= number of needed insertions to transform a to b)
            for (int i = 0; i < dataB.Length + 1; i++)
            {
                scoreRow0[i] = i;
            }

            // we calculate all rows from the first row until we have the last row
            for (int i = 1; i < dataA.Length + 1; i++)
            {
                if (i % 1000 == 0)
                {
                    Console.WriteLine("Row: " + i);
                    if (cancel.IsCancellationRequested)
                    {
                        return 0;
                    }
                }
                
                // reinitialize
                scoreRow1 = new int[dataB.Length + 1];

                // init case (i, 0), similar to other init, just other way around
                scoreRow1[0] = i;

                // we calculate the yet unknown cells of row 1 from row 0
                for (int j = 1; j < dataB.Length + 1; j++)
                {
                    int cost = (dataA[i - 1] == dataB[j - 1]) ? 0 : 1; // substitution or match

                    scoreRow1[j] = Math.Min(Math.Min(
                        scoreRow1[j - 1] + 1, // one cell left => removed current byte from b / added to a
                        scoreRow0[j] + 1 // one cell up =>  removed current byte from a / added to b
                        ), scoreRow0[j-1] + cost // cell up + left => substituion or match
                    );

                }

                // we swap rows
                scoreRow0 = scoreRow1;
            }

            return scoreRow1[dataB.Length];
        }
    }
}
