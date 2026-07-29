using System.Collections;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace _TERM_
{
    sealed partial class LogClient : TermClient
    {

        //----------------------------------------------------------------------------------------------------------

        public LogClient(in TcpClient tcpClient) : base(tcpClient)
        {
        }

        //----------------------------------------------------------------------------------------------------------

        internal IEnumerator ESend(LogResponse response) => base.ESend(response);
        internal Task ASend(LogResponse response) => base.ASend(response);
    }
}