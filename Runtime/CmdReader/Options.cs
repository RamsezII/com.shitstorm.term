using System;

namespace _TERM_
{
    public readonly struct CmdOption : IEquatable<CmdOption>, IEquatable<string>, IEquatable<char>
    {
        public readonly char short_name;
        public readonly string long_name;
        public readonly Func<CmdReader, object> function;

        //----------------------------------------------------------------------------------------------------------

        public CmdOption(in string long_name) : this((char)0, long_name, null) { }
        public CmdOption(in char short_name, in string long_name) : this(short_name, long_name, null) { }
        public CmdOption(in string long_name, in Func<CmdReader, object> function) : this((char)0, long_name, function) { }
        public CmdOption(in char short_name, in string long_name, in Func<CmdReader, object> function)
        {
            this.short_name = short_name;
            this.long_name = long_name;
            this.function = function;
        }

        //----------------------------------------------------------------------------------------------------------

        public static implicit operator CmdOption(in string long_name) => new(long_name);
        public static implicit operator CmdOption(in (char short_name, string long_name) option) => new(option.short_name, option.long_name);
        public static implicit operator CmdOption(in (string long_name, Func<CmdReader, object> function) option) => new(option.long_name, option.function);
        public static implicit operator CmdOption(in (char short_name, string long_name, Func<CmdReader, object> function) option) => new(option.short_name, option.long_name, option.function);

        public override readonly bool Equals(object obj) => obj is CmdOption other && long_name.Equals(other.long_name);
        public bool Equals(string other) => long_name.Equals(other, StringComparison.OrdinalIgnoreCase);
        public bool Equals(CmdOption other) => long_name.Equals(other.long_name, StringComparison.OrdinalIgnoreCase);
        public bool Equals(char other) => short_name.Equals(other);

        public override readonly int GetHashCode() => long_name.GetHashCode(StringComparison.OrdinalIgnoreCase);
        public override string ToString()
        {
            if (short_name == 0)
                return long_name;
            return $"{{{short_name},{long_name}}}";
        }
    }
}