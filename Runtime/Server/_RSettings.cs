using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using UnityEngine;

namespace _TERM_
{
    partial class TermServer
    {
#if UNITY_EDITOR
        const string prefixe_button_rsettings = "Assets/" + nameof(_TERM_) + "/" + nameof(TermServer) + ".";
#endif

        //----------------------------------------------------------------------------------------------------------

#if UNITY_EDITOR
        static string GetRSavePath()
        {
            string fname = typeof(TermServer).FullName + ".json.txt";
            string dpath = Path.Combine(Application.dataPath, "Resources");

            if (!Directory.Exists(dpath))
                Directory.CreateDirectory(dpath);

            string fpath = Path.Combine(dpath, fname);
            return fpath;
        }

        [UnityEditor.MenuItem(prefixe_button_rsettings + nameof(OpenRSettings))]
        static void OpenRSettings() => Application.OpenURL(GetRSavePath());

        [UnityEditor.MenuItem(prefixe_button_rsettings + nameof(OpenResources))]
        static void OpenResources() => Application.OpenURL(Directory.GetParent(GetRSavePath()).FullName);

        [ContextMenu(nameof(SaveRSettings))]
        void SaveRSettings()
        {
            string fpath = GetRSavePath();

            var jobj = new JObject
            {
                [nameof(terminal_key)] = terminal_key.ToString(),
            };

            File.WriteAllText(fpath, JsonConvert.SerializeObject(jobj, Formatting.Indented));
            Debug.Log($"Saved: " + fpath, this);
        }
#endif

        [ContextMenu(nameof(LoadRSettings))]
        void LoadRSettings()
        {
            string rname = typeof(TermServer).FullName + ".json";
            var rtext = Resources.Load<TextAsset>(rname);

            if (rtext != null)
            {
                Debug.Log("Loaded text: " + rtext, this);

                var jobj = JsonConvert.DeserializeObject<JObject>(rtext.text);

                if (jobj.TryGetValue(nameof(terminal_key), out var _terminal_key) &&
                    Enum.TryParse((string)_terminal_key, true, out KeyCode parsed_terminal_key))
                    terminal_key = parsed_terminal_key;
            }
#if UNITY_EDITOR
            else
                SaveRSettings();
#endif
        }
    }
}
