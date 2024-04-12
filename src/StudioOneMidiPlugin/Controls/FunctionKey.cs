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
        public FunctionKey()
        {
            for (int i = 0; i < 12; i++)
            {
                this.AddButton(new CommandButtonData(0x60 + i, "F" + (i + 1), new BitmapColor(200, 200, 200), BitmapColor.Black));
            }
        }

        protected override bool OnLoad()
        {
            base.OnLoad();

            this.plugin.MackieNoteReceived += (object sender, NoteOnEvent e) =>
            {
                string param = e.NoteNumber.ToString();
                if (!this.buttonData.ContainsKey(param))  return;

                var bd = this.buttonData[param];
                bd.Activated = e.Velocity > 0;
                Debug.WriteLine("FunctionKeys.MackieNoteReceived - ActionImageChanged");
                this.ActionImageChanged(param);
            };

            this.plugin.FunctionKeyChanged += (object sender, EventArgs e) =>
            {
                while (this.plugin.FunctionKeyEvents.Count > 0)
                {
                    var fke = this.plugin.FunctionKeyEvents.Dequeue();
                    string param = (fke.KeyID + 0x60).ToString();
                    var bd = this.buttonData[param];
                    bd.Name = fke.FunctionName;
                    Debug.WriteLine("FunctionKeyChanged: " + fke.KeyID + ", " + bd.Name);
                }
                Debug.WriteLine("FunctionKeyChanged - ActionImageChanged");
                this.ActionImageChanged();
            };

            return true;
        }

        private void AddButton(CommandButtonData bd)
        {
            this.buttonData[bd.Code.ToString()] = bd;
            AddParameter(bd.Code.ToString(), bd.Name, "Function Keys");
        }
    }
}
