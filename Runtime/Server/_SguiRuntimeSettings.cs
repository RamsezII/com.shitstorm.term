#if HAS_SGUI && HAS_TMPro
using _SGUI_;
using TMPro;

namespace _TERM_
{
    partial class TermServer
    {
        void InitSguiRuntimeSettings()
        {
            OSView.onRuntimeSettingsPrompt.Add(this, window =>
            {
                window.SetDialogButtons(SguiCancelTypes.Off, SguiConfirmTypes.Ok);

                var field_cmd = window.AddButton<SguiCustom_InputField>();
                field_cmd.trad_label.SetText($"{nameof(port_cmd)}:");
                field_cmd.input_field.text = port_cmd.ToString();
                var field_log = window.AddButton<SguiCustom_InputField>();
                field_log.trad_label.SetText($"{nameof(port_log)}:");
                field_log.input_field.text = port_log.ToString();

                field_cmd.input_field.contentType = field_log.input_field.contentType = TMP_InputField.ContentType.IntegerNumber;
                field_cmd.input_field.readOnly = field_log.input_field.readOnly = true;
            });
        }
    }
}
#endif