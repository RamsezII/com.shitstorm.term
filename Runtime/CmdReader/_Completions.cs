using System;
using System.Collections.Generic;

namespace _TERM_
{
    partial class CmdReader
    {
        internal int compl_start;
        internal readonly List<string> compl_candidates;
        bool has_completion_scope;

        //----------------------------------------------------------------------------------------------------------

        public void AddCompletions<T>(in string prefixe) where T : Enum => AddCompletions(prefixe, Enum.GetNames(typeof(T)));
        public void AddCompletions(in string prefixe, params string[] candidates) => AddCompletions(prefixe, (IEnumerable<string>)candidates);
        public void AddCompletions(in string argument, IEnumerable<string> candidates)
        {
            if (candidates == null || !IsOnCompletion() || !TryUseCompletionScope())
                return;

            foreach (string candidate in candidates)
                if (!string.IsNullOrEmpty(candidate))
                    if (!compl_candidates.Contains(candidate))
                        if (string.IsNullOrWhiteSpace(argument) || Util.IsMatchChars(argument, candidate))
                            compl_candidates.Add(candidate);
        }

        internal void ClearCompletions()
        {
            compl_candidates.Clear();
            has_completion_scope = false;
            compl_start = 0;
        }

        bool TryUseCompletionScope()
        {
            // Several handlers (for example an override followed by base.OnTryCommand)
            // may safely contribute when they target the same part of the command line.
            if (has_completion_scope && start_i == compl_start)
                return true;

            // The client receives only one replacement start. A later start is closer
            // to the cursor, so this narrower/more specific completion scope wins.
            if (!has_completion_scope || start_i > compl_start)
            {
                compl_candidates.Clear();
                compl_start = start_i;
                has_completion_scope = true;
                return true;
            }

            // A less specific parent may still execute, but its candidates cannot be
            // mixed with candidates already registered for a narrower replacement.
            return false;
        }

        // This is deliberately a pure query: checking the cursor must not overwrite
        // a replacement start previously chosen by another completion producer.
        public bool IsOnCompletion() => type == CmdTypes.Complete && start_i <= cursor && cursor <= read_i;
    }
}
