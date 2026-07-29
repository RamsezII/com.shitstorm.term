using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace _TERM_
{
    abstract partial class TermClient : IDisposable
    {
        readonly TcpClient tcpClient;
        readonly StreamWriter writer;
        readonly SemaphoreSlim writeLock = new(1, 1);
        int dispose_state;

        public readonly StreamReader reader;

        //----------------------------------------------------------------------------------------------------------

        protected TermClient(in TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;

            NetworkStream stream = tcpClient.GetStream();

            reader = new StreamReader(stream, new UTF8Encoding(false), false, 1024, true);

            writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, true)
            {
                AutoFlush = true
            };
        }

        //----------------------------------------------------------------------------------------------------------

        protected IEnumerator ESend(TermResponse response)
        {
            var task = ASend(response);
            while (!task.IsCompleted)
                yield return null;
        }

        protected async Task ASend(TermResponse response)
        {
            string json = response.Serialize();

            await writeLock.WaitAsync();

            try
            {
                await writer.WriteLineAsync(json);
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is ObjectDisposedException ||
                exception is SocketException)
            {
            }
            finally
            {
                writeLock.Release();
            }
        }

        //----------------------------------------------------------------------------------------------------------

        public void Dispose()
        {
            if (Interlocked.Exchange(ref dispose_state, 1) != 0)
                return;

            tcpClient.Close();

            try
            {
                reader.Dispose();
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is ObjectDisposedException ||
                exception is SocketException)
            {
            }

            try
            {
                writer.Dispose();
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is ObjectDisposedException ||
                exception is SocketException)
            {
            }
        }
    }
}
