namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Melanchall.DryWetMidi.Core;
    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;

    internal class FunctionKey : StudioOneButton<CommandButtonData>
    {
        public FunctionKey() : base()
        {
            for (int i = 0; i < 12; i++)
            {
                this.AddButton(new CommandButtonData(0x60 + i, "F" + (i + 1), new BitmapColor(200, 200, 200), BitmapColor.Black));
            }
        }

        protected override bool OnLoad()
        {
            base.OnLoad();

            this._plugin.CommandNoteReceived += (object sender, NoteOnEvent e) =>
            {
                string param = e.NoteNumber.ToString();
                if (!this._buttonData.ContainsKey(param)) return;

                var bd = this._buttonData[param];
                bd.Activated = e.Velocity > 0;
                this.ActionImageChanged(param);
                // this.EmitActionImageChanged();
            };

            this._plugin.FunctionKeyChanged += (object sender, FunctionKeyParams fke) =>
            {
                // Need to check if there is a key in the dictionary for the received
                // parameters since the global user buttons are handled as additional
                // function keys.
                //
                var code = (fke.KeyID + 0x60).ToString();
                if (this._buttonData.TryGetValue(code, out var bd))
                {
                    bd.Name = fke.FunctionName;
                    this.ActionImageChanged(code);
                }
            };

            return true;
        }

        private void AddButton(CommandButtonData bd)
        {
            this._buttonData[bd.Code.ToString()] = bd;
            this.AddParameter(bd.Code.ToString(), bd.Name, "Function Keys");
        }
    }
}
