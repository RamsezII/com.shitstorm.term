using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace _TERM_
{
    partial class CmdClient
    {
        internal enum RespTypes : byte
        {
            intro,
            error,
            exception,
            completion,
            result,
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

        internal sealed class CmdResponse_intro : CmdResponse
        {
            public string default_prompt = Directory.GetCurrentDirectory().Replace('\\', '/');

            //----------------------------------------------------------------------------------------------------------

            internal CmdResponse_intro() : base(RespTypes.intro)
            {
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
            public int start, end;
            public IList<string> candidates;

            //----------------------------------------------------------------------------------------------------------

            internal CmdResponse_completion(in Range range, in IList<string> candidates) : base(RespTypes.completion)
            {
                start = range.Start.Value;
                end = range.End.Value;
                this.candidates = candidates;
            }
        }

        internal sealed class CmdResponse_status : CmdResponse
        {
            public float progress;
            public string prompt;

            //----------------------------------------------------------------------------------------------------------

            internal CmdResponse_status(in CmdCommand.RoutineStatus status) : base(RespTypes.result)
            {
                progress = status.progress;
                prompt = status.prompt;
            }
        }

        internal sealed class CmdResponse_end : CmdResponse
        {
            public string result;

            //----------------------------------------------------------------------------------------------------------

            internal CmdResponse_end(in string result) : base(RespTypes.result)
            {
                this.result = result;
            }
        }
    }
}
