using System;

namespace _TERM_
{
    partial class LogClient
    {
        internal enum LogTypes : byte
        {
            error,
            assert,
            warning,
            log,
            exception,
        }

        internal class LogResponse : TermResponse
        {
            public LogTypes type;
            public string message;

            //----------------------------------------------------------------------------------------------------------

            internal LogResponse(in LogTypes type, in string message)
            {
                this.type = type;
                this.message = message;
            }
        }

        internal sealed class LogResponse_exception : LogResponse
        {
            public string stacktrace;

            //----------------------------------------------------------------------------------------------------------

            internal LogResponse_exception(in Exception e) : this(e.Message?.Trim('\n', '\r') ?? e.GetType().FullName, e.StackTrace?.Trim('\n', '\r') ?? string.Empty)
            {
            }

            internal LogResponse_exception(in string message, in string stacktrace) : base(LogTypes.exception, message?.Trim('\n', '\r') ?? string.Empty)
            {
                this.stacktrace = stacktrace?.Trim('\n', '\r') ?? string.Empty;
            }
        }
    }
}
