using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace _TERM_
{
    partial class TermClient
    {
        internal abstract class TermResponse
        {
            static readonly StringEnumConverter enum_converter = new();

            //----------------------------------------------------------------------------------------------------------

            internal string Serialize(in Formatting formatting = Formatting.None) => JsonConvert.SerializeObject(this, formatting, enum_converter);
        }
    }
}
