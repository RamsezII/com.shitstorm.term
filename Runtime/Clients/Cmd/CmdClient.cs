using System.Collections;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace _TERM_
{
    sealed partial class CmdClient : TermClient
    {
        public CmdClient(in TcpClient tcpClient) : base(tcpClient)
        {
        }

        //----------------------------------------------------------------------------------------------------------

        internal IEnumerator ESend(CmdResponse response) => base.ESend(response);
        internal Task ASend(CmdResponse response) => base.ASend(response);
    }
}