using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using UnityEngine;

namespace _TERM_
{
    partial class TermServer
    {
#if UNITY_EDITOR
        const string prefixe_button_settings = "Assets/" + nameof(_TERM_) + "/" + nameof(TermServer) + ".";
#endif

        //----------------------------------------------------------------------------------------------------------

#if UNITY_EDITOR
        static string GetSavePath()
        {
            string fname = typeof(TermServer).FullName + ".json.txt";
            string dpath = Path.Combine(Application.dataPath, "Resources");

            if (!Directory.Exists(dpath))
                Directory.CreateDirectory(dpath);

            string fpath = Path.Combine(dpath, fname);
            return fpath;
        }

        [UnityEditor.MenuItem(prefixe_button_settings + nameof(OpenSettings))]
        static void OpenSettings() => Application.OpenURL(GetSavePath());

        [UnityEditor.MenuItem(prefixe_button_settings + nameof(OpenResources))]
        static void OpenResources() => Application.OpenURL(Directory.GetParent(GetSavePath()).FullName);

        [ContextMenu(nameof(SaveSettings))]
        void SaveSettings()
        {
            string fpath = GetSavePath();

            var jobj = new JObject
            {
                [nameof(port_cmd)] = port_cmd,
                [nameof(port_log)] = port_log,
            };

            File.WriteAllText(fpath, JsonConvert.SerializeObject(jobj, Formatting.Indented));
            Debug.Log($"Saved: " + fpath, this);
        }
#endif

        [ContextMenu(nameof(LoadSettings))]
        void LoadSettings()
        {
            string rname = typeof(TermServer).FullName + ".json";
            var rtext = Resources.Load<TextAsset>(rname);

            if (rtext == null)
                SaveSettings();
            else
            {
                Debug.Log("Loaded text: " + rtext, this);

                var jobj = JsonConvert.DeserializeObject<JObject>(rtext.text);

                if (jobj.TryGetValue(nameof(port_cmd), out var _port_cmd))
                    port_cmd = (ushort)_port_cmd;
                if (jobj.TryGetValue(nameof(port_log), out var _log_port))
                    port_log = (ushort)_log_port;
            }
        }
    }
}
