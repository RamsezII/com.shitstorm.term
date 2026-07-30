using System;
using System.Collections.Generic;
using System.IO;

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
            prompt,
            status,
            cancelled,
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
                stacktrace = e.StackTrace?.Trim('\n', '\r') ?? string.Empty;
                message = e.Message?.Trim('\n', '\r') ?? e.GetType().FullName;
            }
        }

        internal sealed class CmdResponse_completions : CmdResponse
        {
            public int start;
            public IList<string> candidates;

            //----------------------------------------------------------------------------------------------------------

            internal CmdResponse_completions(in CmdReader reader) : base(RespTypes.completion)
            {
                start = reader.compl_start;
                candidates = reader.compl_candidates;
            }
        }

        internal sealed class CmdResponse_prompt : CmdResponse
        {
            public string prompt;

            //----------------------------------------------------------------------------------------------------------

            internal CmdResponse_prompt(in string prompt) : base(RespTypes.prompt)
            {
                this.prompt = prompt;
            }
        }

        internal sealed class CmdResponse_status : CmdResponse
        {
            public string message;
            public float progress;

            //----------------------------------------------------------------------------------------------------------

            internal CmdResponse_status(in string message, in float progress) : base(RespTypes.status)
            {
                this.message = message;
                this.progress = progress;
            }
        }

        internal sealed class CmdResponse_cancelled : CmdResponse
        {
            internal CmdResponse_cancelled() : base(RespTypes.cancelled)
            {
            }
        }

        internal sealed class CmdResponse_result : CmdResponse
        {
            public string result;

            //----------------------------------------------------------------------------------------------------------

            internal CmdResponse_result(in string result) : base(RespTypes.result)
            {
                this.result = result;
            }
        }
    }
}
