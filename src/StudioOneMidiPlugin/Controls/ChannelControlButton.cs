namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    using Melanchall.DryWetMidi.Core;

    internal class ChannelControlButton : StudioOneButton<CommandButtonData>
    {
        List<CommandButtonData> ShowList = new List<CommandButtonData>();

        public ChannelControlButton()
        {
            this.AddButton(new CommandButtonData(0x31, "Fader Bank Left", "faderBankLeft"));
            this.AddButton(new CommandButtonData(0x32, "Fader Bank Right", "faderBankRight"));
            this.AddButton(new CommandButtonData(0x33, "Fader Channel Left", "faderChannelLeft"));
            this.AddButton(new CommandButtonData(0x34, "Fader Channel Right", "faderChannelRight"));
            this.AddButton(new CommandButtonData(0x20, "TRACK"));
            this.AddButton(new CommandButtonData(0x29, "SEND"));
            this.AddButton(new CommandButtonData(0x2A, "VOL/PAN"));
            this.AddButton(new CommandButtonData(0x36, "GLOBAL", new BitmapColor(60, 60, 20), BitmapColor.White), addToShowList: true);
            this.AddButton(new CommandButtonData(0x40, "AUDIO", new BitmapColor(0, 60, 80), BitmapColor.White), addToShowList: true);
            this.AddButton(new CommandButtonData(0x42, "FX", new BitmapColor(0, 60, 80), BitmapColor.White), addToShowList: true);
            this.AddButton(new CommandButtonData(0x43, "BUS", new BitmapColor(0, 60, 80), BitmapColor.White), addToShowList: true);
            this.AddButton(new CommandButtonData(0x44, "OUT", new BitmapColor(0, 60, 80), BitmapColor.White), addToShowList: true);
            this.AddButton(new FlipPanVolCommandButtonData(0x35));
            this.AddButton(new CommandButtonData(0x2B, "PLUGIN"));
        }
        protected override bool OnLoad()
        {
            base.OnLoad();

            this.plugin.Ch0NoteReceived += (object sender, NoteOnEvent e) =>
            {
                var idx = $"{e.NoteNumber}";

                if (!this.buttonData.ContainsKey(idx))
                    return;

                var bd = this.buttonData[idx];
                bd.Activated = e.Velocity > 0;
                this.ActionImageChanged(idx);
            };

            return true;
        }

        protected override void RunCommand(string actionParameter)
        {
            foreach (CommandButtonData bd in this.ShowList) 
            {
                if (bd.Code == actionParameter.ParseInt32()) bd.Activated = true;
                else                                         bd.Activated = false;
            }
            base.RunCommand(actionParameter);

            this.EmitActionImageChanged();
        }

        private void AddButton(CommandButtonData bd, Boolean addToShowList = false)
        {
            var idx = $"{bd.Code}";

            this.buttonData[idx] = bd;
            this.AddParameter(idx, bd.Name, "Channel Controls");

            if (addToShowList)
            {
                this.ShowList.Add(bd);
            }
        }
    }
}
