namespace _TERM_
{
    partial class CmdReader
    {
        public void SkipEmpties(in bool move_start)
        {
            SkipEmpties(ref read_i);
            if (move_start)
                start_i = read_i;
        }

        public void SkipEmpties(ref int read_i)
        {
            while (read_i < line.Length && line[read_i] == ' ')
                ++read_i;
        }

        public void SkipNoneEmpties() => SkipNoneEmpties(ref read_i);
        public void SkipNoneEmpties(ref int read_i)
        {
            while (read_i < line.Length && line[read_i] != ' ')
                ++read_i;
        }

        public bool TryRead(out string output)
        {
            SkipEmpties(true);
            return TryRead(ref read_i, out output);
        }

        public bool TryRead(ref int read_i, out string output)
        {
            SkipEmpties(ref read_i);

            if (read_i < cursor)
            {
                SkipNoneEmpties(ref read_i);
                if (read_i > start_i)
                {
                    read_last = output = line[start_i..read_i];
                    return true;
                }
            }

            output = null;
            return false;
        }
    }
}