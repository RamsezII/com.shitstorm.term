namespace _TERM_
{
    partial class CmdReader
    {
        internal void SkipEmpties(in bool move_start)
        {
            SkipEmpties(ref read_i);
            if (move_start)
                start_i = read_i;
        }

        internal void SkipEmpties(ref int read_i)
        {
            while (read_i < line.Length && line[read_i] == ' ')
                ++read_i;
        }

        public void SkipNoneEmpties() => SkipNoneEmpties(ref read_i);
        internal void SkipNoneEmpties(ref int read_i)
        {
            while (read_i < line.Length && line[read_i] != ' ')
                ++read_i;
        }

        public bool TryRead(out string output)
        {
            SkipEmpties(true);
            return TryRead(ref read_i, out output);
        }

        internal bool TryRead(ref int read_i, out string output)
        {
            SkipEmpties(ref read_i);
            int token_start_i = read_i;

            if (read_i < cursor)
            {
                SkipNoneEmpties(ref read_i);
                if (read_i > token_start_i)
                {
                    // This overload is also used for look-ahead with a copied index.
                    // Its token bounds must therefore not depend on the reader's
                    // current global completion scope.
                    read_last = output = line[token_start_i..read_i];
                    return true;
                }
            }

            output = null;
            return false;
        }
    }
}
