namespace _TERM_
{
    public enum CmdTypes : byte
    {
        Complete,
        Check,
        Execute,
    }

    public struct CmdReader
    {
        public readonly string line;
        public readonly CmdTypes type;
        public readonly int cursor;
        public int start_i, read_i;
        public string error;

        //----------------------------------------------------------------------------------------------------------

        public CmdReader(in string line, in CmdTypes type, in int cursor = 0)
        {
            this.line = line;
            this.type = type;
            this.cursor = cursor;
            start_i = 0;
            read_i = 0;
            error = null;
        }

        //----------------------------------------------------------------------------------------------------------

        public void SkipEmpties()
        {
            while (read_i < line.Length && line[read_i] switch
            {
                ' ' or '\n' => true,
                _ => false,
            })
                ++read_i;
        }

        public void SkipNoneEmpties()
        {
            while (read_i < line.Length && line[read_i] switch
            {
                ' ' or '\n' => false,
                _ => true,
            })
                ++read_i;
        }

        public bool TryRead(out string output)
        {
            SkipEmpties();

            if (read_i < line.Length)
            {
                int old_read_i = read_i;
                SkipNoneEmpties();

                if (read_i > old_read_i)
                {
                    output = line[old_read_i..read_i];
                    SkipEmpties();
                    return true;
                }
            }

            output = null;
            return false;
        }
    }
}