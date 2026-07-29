using Newtonsoft.Json;

namespace _TERM_
{
    partial class TermClient
    {
        internal abstract class TermResponse
        {

            //----------------------------------------------------------------------------------------------------------

            internal string Serialize(in Formatting formatting = Formatting.None) => JsonConvert.SerializeObject(this, formatting);
        }
    }
}