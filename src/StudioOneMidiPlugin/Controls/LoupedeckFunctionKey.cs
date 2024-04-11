namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Melanchall.DryWetMidi.Core;
    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;

    internal class LoupedeckFunctionKey : LoupedeckButton<CommandButtonData>
    {
        public LoupedeckFunctionKey()
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
                this.ActionImageChanged(param);
            };

            this.plugin.FunctionKeyChanged += (object sender, FunctionKeyArgs e) =>
            {
                string param = (e.KeyID + 0x5F).ToString();
                if (!this.buttonData.ContainsKey(param)) return;

                var bd = this.buttonData[param];
                bd.Name = e.FunctionName;
                this.ActionImageChanged(param);
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
