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
                this.AddButton(new CommandButtonData(0x60 + i, "F" + (i + 1), new BitmapColor(100, 100, 100), BitmapColor.Black));
            }
        }

        protected override bool OnLoad()
        {
            base.OnLoad();

            var plugin = (StudioOneMidiPlugin)this.Plugin;

            plugin.CommandNoteReceived += (object? sender, NoteOnEvent e) =>
            {
                string param = e.NoteNumber.ToString();
                if (!this._buttonData.ContainsKey(param)) return;

                var bd = this._buttonData[param];
                if (bd != null)
                {
                    bd.Activated = e.Velocity > 0;
                    this.ActionImageChanged(param);
                }
            };

            plugin.FunctionKeyChanged += (object? sender, FunctionKeyParams fke) =>
            {
                // Need to check if there is a key in the dictionary for the received
                // parameters since the global user buttons are handled as additional
                // function keys.
                //
                var code = (fke.KeyID + 0x60).ToString();
                if (this._buttonData.TryGetValue(code, out var bd))
                {
                    if (bd != null)
                    {
                        if (!String.IsNullOrEmpty(fke.FunctionName))
                        {
                            bd.Name = fke.FunctionName;
                            bd.TextColor = new BitmapColor(200, 200, 200);
                        }
                        else
                        {
                            bd.Name = "F" + fke.KeyID;
                            bd.TextColor = new BitmapColor(80, 80, 80);
                        }
                        this.ActionImageChanged(code);
                    }
                }
            };

            return true;
        }

        private void AddButton(CommandButtonData bd)
        {
            bd.TextColor = new BitmapColor(80, 80, 80);
            this._buttonData[bd.Code.ToString()] = bd;
            this.AddParameter(bd.Code.ToString(), bd.Name, "Function Keys");
        }
    }
}
