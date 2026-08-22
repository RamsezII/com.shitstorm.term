using System;
using System.Collections.Generic;

namespace _TERM_
{
    partial class CmdReader
    {
        internal int compl_start;
        internal readonly List<string> compl_candidates;

        //----------------------------------------------------------------------------------------------------------

        public void AddCompletions<T>(in string prefixe) where T : Enum => AddCompletions(prefixe, Enum.GetNames(typeof(T)));
        public void AddCompletions(in string prefixe, params string[] candidates) => AddCompletions(prefixe, (IEnumerable<string>)candidates);
        public void AddCompletions(in string argument, IEnumerable<string> candidates)
        {
            if (candidates == null || !IsOnCompletion())
                return;

            foreach (string candidate in candidates)
                if (!string.IsNullOrEmpty(candidate))
                    if (!compl_candidates.Contains(candidate))
                        if (string.IsNullOrWhiteSpace(argument) || Util.IsMatchChars(argument, candidate))
                            compl_candidates.Add(candidate);
        }

        public void ReplaceCompletions(in string argument, IEnumerable<string> candidates)
        {
            if (candidates == null || !IsOnCompletion())
                return;
            compl_candidates.Clear();
            AddCompletions(argument, candidates);
        }

        public bool IsOnCompletion()
        {
            if (type == CmdTypes.Complete)
                if (start_i <= cursor && cursor <= read_i)
                {
                    compl_start = start_i;
                    return true;
                }
            return false;
        }
    }
}