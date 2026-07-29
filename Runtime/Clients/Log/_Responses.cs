using System;

namespace _TERM_
{
    partial class LogClient
    {
        internal enum RespTypes : byte
        {
            Error,
            Assert,
            Warning,
            Log,
            Exception,
        }

        internal class LogResponse : TermResponse
        {
            public RespTypes type;
            public string message;

            //----------------------------------------------------------------------------------------------------------

            internal LogResponse(in RespTypes type, in string message)
            {
                this.type = type;
                this.message = message;
            }
        }

        internal sealed class LogResponse_exception : LogResponse
        {
            public string stacktrace;

            //----------------------------------------------------------------------------------------------------------

            internal LogResponse_exception(in Exception e) : base(RespTypes.exception, e.Message.Trim('\n', '\r'))
            {
                stacktrace = e.StackTrace.Trim('\n', '\r');
            }
        }
    }
}