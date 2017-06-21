using SD.Tools.Algorithmia.GeneralDataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateFiles
{
    class ComparisonUtils
    {
        public static MultiValueDictionary<K, V> removeOneElementGroups<K, V>(MultiValueDictionary<K, V> groups)
        {
            var result = new MultiValueDictionary<K, V>();
            foreach (KeyValuePair<K, HashSet<V>> entry in groups)
            {
                if (entry.Value.Count > 1)
                {
                    result[entry.Key] = entry.Value;
                }
            }
            return result;
        }
    }
}
