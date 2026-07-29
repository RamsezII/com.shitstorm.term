using System;
using System.Collections.Generic;
using System.Linq;

namespace _TERM_
{
    partial class CmdClient
    {
        internal enum RespTypes : byte
        {
            error,
            exception,
            completion,
            end,
        }

        internal abstract class CmdResponse : TermResponse
        {
            public RespTypes type;

            //----------------------------------------------------------------------------------------------------------

            protected CmdResponse(in RespTypes type)
            {
                this.type = type;
            }
        }

        internal sealed class CmdResponse_error : CmdResponse
        {
            public string message;

            //----------------------------------------------------------------------------------------------------------

            internal CmdResponse_error(in string message) : base(RespTypes.error)
            {
                this.message = message;
            }
        }

        internal sealed class CmdResponse_exception : CmdResponse
        {
            public string stacktrace, message;

            //----------------------------------------------------------------------------------------------------------

            internal CmdResponse_exception(in Exception e) : base(RespTypes.exception)
            {
                stacktrace = e.StackTrace.Trim('\n', '\r');
                message = e.Message.Trim('\n', '\r');
            }
        }

        internal sealed class CmdResponse_completion : CmdResponse
        {
            public string[] candidates;

            //----------------------------------------------------------------------------------------------------------

            internal CmdResponse_completion(in IList<string> candidates) : base(RespTypes.completion)
            {
                this.candidates = candidates.ToArray();
            }
        }

        internal sealed class CmdResponse_status : CmdResponse
        {
            public float progress;
            public string prompt;

            //----------------------------------------------------------------------------------------------------------

            internal CmdResponse_status(in CmdCommand.RoutineStatus status) : base(RespTypes.end)
            {
                progress = status.progress;
                prompt = status.prompt;
            }
        }

        internal sealed class CmdResponse_end : CmdResponse
        {
            public string result;

            //----------------------------------------------------------------------------------------------------------

            internal CmdResponse_end(in string result) : base(RespTypes.end)
            {
                this.result = result;
            }
        }
    }
}