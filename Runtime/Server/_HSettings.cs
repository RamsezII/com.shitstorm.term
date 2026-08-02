using _ARK_;
using Newtonsoft.Json.Linq;
using System.IO;
using UnityEngine;

namespace _TERM_
{
    partial class TermServer
    {
        static string GetHSettingsPath() => Path.Combine(ArkMachine.DFHome.FullName, typeof(TermServer).GetJSonFileName());

        //----------------------------------------------------------------------------------------------------------

        [ContextMenu(nameof(SaveHSettings))]
        void SaveHSettings() => SaveHSettings(log: true);
        void SaveHSettings(in bool log)
        {
            string fpath = GetHSettingsPath();

            var jobj = new JObject
            {
                [nameof(port_cmd_override)] = port_cmd_override,
                [nameof(port_log_override)] = port_log_override,
            };

            jobj.NJSave(fpath, log);
        }

        [ContextMenu(nameof(LoadHSettings))]
        void LoadHSettings() => LoadHSettings(log: true);
        void LoadHSettings(in bool log)
        {
            string fpath = GetHSettingsPath();

            Util.TryNJRead(fpath, out JObject jobj, force: true, log_success: log, log_failure: true);

            if (jobj.TryGetValue(nameof(port_cmd_override), out var _port_cmd_override))
                port_cmd_override = (ushort)_port_cmd_override;

            if (jobj.TryGetValue(nameof(port_log_override), out var _port_log_override))
                port_log_override = (ushort)_port_log_override;
        }
    }
}
