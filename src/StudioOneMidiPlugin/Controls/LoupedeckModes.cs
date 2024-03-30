namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Management;
    using System.Windows.Navigation;
    using System.Xml.Linq;

    using Melanchall.DryWetMidi.Core;

    // This is a special button class that creates 6 buttons which are then
    // placed in the central area of the button field on the Loupedeck.
    // The functionality of the buttons changes dynamically.
    internal class LoupedeckModes : PluginDynamicCommand
    {
        // common
        StudioOneMidiPlugin plugin;

        private enum ButtonLayer
        {
            channelProperties,
            faderModesAll,
            faderModesSend
        }

        private IDictionary<string, ButtonData> buttonData = new Dictionary<string, ButtonData>();
        private IDictionary<int, string> noteReceivers = new Dictionary<int, string>();

        ButtonLayer currentLayer = ButtonLayer.channelProperties;
        bool sendMode = false;

        public LoupedeckModes()
        {
            this.Description = "Special button for controlling Loupedeck fader modes";

            // Create UI buttons
            for (int i = 0; i < 6; i++)
            {
                AddParameter($"{i}", "Mode Group Button " + i, "Modes");
            }

            // Create button data for each layer
            this.addButton(ButtonLayer.channelProperties, 0, new PropertyButtonData(StudioOneMidiPlugin.MackieChannelCount, 
                                                                                    ChannelProperty.BoolType.Mute, 
                                                                                    PropertyButtonData.TrackNameMode.ShowLeftHalf));
            this.addButton(ButtonLayer.channelProperties, 1, new PropertyButtonData(StudioOneMidiPlugin.MackieChannelCount, 
                                                                                    ChannelProperty.BoolType.Solo,
                                                                                    PropertyButtonData.TrackNameMode.ShowRightHalf));
            this.addButton(ButtonLayer.channelProperties, 2, new PropertyButtonData(StudioOneMidiPlugin.MackieChannelCount, 
                                                                                    ChannelProperty.BoolType.Arm, 
                                                                                    PropertyButtonData.TrackNameMode.None,
                                                                                    "record"));
            this.addButton(ButtonLayer.channelProperties, 3, new PropertyButtonData(StudioOneMidiPlugin.MackieChannelCount,
                                                                                    ChannelProperty.BoolType.Monitor, 
                                                                                    PropertyButtonData.TrackNameMode.None,
                                                                                    "monitor"));
            this.addButton(ButtonLayer.channelProperties, 4, new ModeButtonData("MODES"));
            this.addButton(ButtonLayer.channelProperties, 5, new FlipPanVolCommandButtonData(), true);

            this.addButton(ButtonLayer.faderModesAll, 0, new CommandButtonData(0x40, "AUDIO", new BitmapColor(0, 60, 80)));
            this.addButton(ButtonLayer.faderModesAll, 1, new CommandButtonData(0x42, "FX", new BitmapColor(0, 60, 80)));
            this.addButton(ButtonLayer.faderModesAll, 2, new CommandButtonData(0x43, "BUS", new BitmapColor(0, 60, 80)));
            this.addButton(ButtonLayer.faderModesAll, 3, new CommandButtonData(0x44, "OUT", new BitmapColor(0, 60, 80)));
            this.addButton(ButtonLayer.faderModesAll, 4, new CommandButtonData(0x33, "ALL", new BitmapColor(60, 60, 20), true), true);
            this.addButton(ButtonLayer.faderModesAll, 5, new CommandButtonData(0x29, "SEND"));

            this.addButton(ButtonLayer.faderModesSend, 4, new CommandButtonData(0x2A, "BACK"));
            this.addButton(ButtonLayer.faderModesSend, 5, new CommandButtonData(0x29, "SEND", new BitmapColor(60, 0, 70), true));
        }

        // common
        protected override bool OnLoad()
        {
            this.plugin = base.Plugin as StudioOneMidiPlugin;

            plugin.MackieNoteReceived += this.OnNoteReceived;

            foreach (var bd in this.buttonData.Values)
            {
                bd.OnLoad(plugin);
            }

            plugin.MackieDataChanged += (object sender, EventArgs e) => {
                ActionImageChanged();
            };

            plugin.SelectButtonPressed += (object sender, EventArgs e) =>
            {
                this.currentLayer = ButtonLayer.channelProperties;
                ActionImageChanged();
            };

            return true;
        }

        protected void OnNoteReceived(object sender, NoteOnEvent e)
        {
            if (this.noteReceivers.ContainsKey(e.NoteNumber))
            {
                var cbd = this.buttonData[this.noteReceivers[e.NoteNumber]] as CommandButtonData;
                cbd.Activated = e.Velocity > 0;
                this.ActionImageChanged();
            }
        }

        protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
            if (actionParameter == null)
                return null;

            string idx = $"{(int)this.currentLayer}:" + actionParameter;

            if (this.buttonData.TryGetValue(idx, out var bd))
            {
                return bd.getImage(imageSize);
            }

            BitmapBuilder bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);
            return bb.ToImage();
        }

        protected override void RunCommand(string actionParameter)
        {
            var idx = $"{(int)this.currentLayer}:{actionParameter}";
            if (this.buttonData.TryGetValue(idx, out var bd))
            {
                bd.runCommand();
            }

            switch (this.currentLayer)
            {
                case ButtonLayer.channelProperties:
                    switch (Int32.Parse(actionParameter))
                    {
                        case 4: // MODES
                            this.currentLayer = ButtonLayer.faderModesAll;
                            ActionImageChanged();
                            break;
                    }
                    break;
                case ButtonLayer.faderModesAll:
                    switch (Int32.Parse(actionParameter))
                    {
                        case 5: // SEND
                            this.currentLayer = ButtonLayer.faderModesSend;
                            this.sendMode = true;
                            this.plugin.EmitSendModeChanged(this.sendMode);
                            ActionImageChanged();
                            break;
                        default :
                            for (int i = 0; i <= 5; i++)
                            {
                                (this.buttonData[$"{(int)ButtonLayer.faderModesAll}:{i}"] as CommandButtonData).Activated = false;
                            }
                            var cbd = this.buttonData[idx] as CommandButtonData;
                            cbd.Activated = !cbd.Activated;
                            ActionImageChanged();
                            break;
                    }
                    break;
                case ButtonLayer.faderModesSend:
                    switch (Int32.Parse(actionParameter))
                    {
                        case 4: // BACK
                            this.currentLayer = ButtonLayer.faderModesAll;
                            this.sendMode = false;
                            this.plugin.EmitSendModeChanged(this.sendMode);
                            ActionImageChanged();
                            break;
                        case 5: // SEND
                            plugin.EmitSendModeChanged(this.sendMode);
                            break;
                    }
                    break;
            }
        }

        private void addButton(ButtonLayer buttonLayer, int buttonIndex, ButtonData bd, bool isNoteReceiver = false)
        {
            var idx = $"{(int)buttonLayer}:{buttonIndex}";
            this.buttonData[idx] = bd;

            if (isNoteReceiver)
            {
                var cbd = bd as CommandButtonData;
                this.noteReceivers[cbd.Code] = idx;
            }
        }
    }

}


