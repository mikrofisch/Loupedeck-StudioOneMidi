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
        SelectButtonData.Mode selectMode = SelectButtonData.Mode.Select;

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
                                                                                    "arm"));
            this.addButton(ButtonLayer.channelProperties, 3, new PropertyButtonData(StudioOneMidiPlugin.MackieChannelCount,
                                                                                    ChannelProperty.BoolType.Monitor, 
                                                                                    PropertyButtonData.TrackNameMode.None,
                                                                                    "monitor"));
            this.addButton(ButtonLayer.channelProperties, 4, new ModeButtonData("VIEWS"));
            this.addButton(ButtonLayer.channelProperties, 5, new FlipPanVolCommandButtonData(0x32), true);

            this.addButton(ButtonLayer.faderModesAll, 0, new CommandButtonData(0x40, "AUDIO", new BitmapColor(0, 60, 80)));
            this.addButton(ButtonLayer.faderModesAll, 1, new CommandButtonData(0x42, "FX", new BitmapColor(0, 60, 80)));
            this.addButton(ButtonLayer.faderModesAll, 2, new CommandButtonData(0x43, "BUS", new BitmapColor(0, 60, 80)));
            this.addButton(ButtonLayer.faderModesAll, 3, new CommandButtonData(0x44, "OUT", new BitmapColor(0, 60, 80)));
            this.addButton(ButtonLayer.faderModesAll, 4, new CommandButtonData(0x33, "ALL", new BitmapColor(60, 60, 20), true), true);
            this.addButton(ButtonLayer.faderModesAll, 5, new CommandButtonData(0x29, "USER\rSENDS"));

            this.addButton(ButtonLayer.faderModesSend, 0, new ModeTopCommandButtonData(0x51, "Previous\rPlugin", ModeTopCommandButtonData.Location.Left, "plugin_prev"));
            this.addButton(ButtonLayer.faderModesSend, 1, new ModeTopCommandButtonData(0x52, "Next\rPlugin", ModeTopCommandButtonData.Location.Right, "plugin_next"));
            this.addButton(ButtonLayer.faderModesSend, 2, new CommandButtonData(0x50, "Channel Editor"));
            this.addButton(ButtonLayer.faderModesSend, 3, new UserModeButtonData());
            this.addButton(ButtonLayer.faderModesSend, 4, new CommandButtonData(0x2A, "VIEWS"));
            this.addButton(ButtonLayer.faderModesSend, 5, new SendsCommandButtonData(0x29), true);
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

            plugin.FocusDeviceChanged += (object sender, string e) =>
            {
                for (int i = 0; i < 2; i++)
                {
                    var mtcbd = this.buttonData[$"{(int)ButtonLayer.faderModesSend}:{i}"] as ModeTopCommandButtonData;
                    mtcbd.setTopDisplay(e);
                }
                ActionImageChanged();
            };
            
            return true;
        }

        private void Plugin_FocusDeviceChanged(Object sender, String e) => throw new NotImplementedException();

        protected void OnNoteReceived(object sender, NoteOnEvent e)
        {
            if (e.NoteNumber >= 0x2B && e.NoteNumber <= 0x2D)
            {
                // User mode changed
                var umbd = this.buttonData[$"{(int)ButtonLayer.faderModesSend}:3"] as UserModeButtonData;
                umbd.setUserMode(e.NoteNumber, e.Velocity > 0);
            }
            else if (this.noteReceivers.ContainsKey(e.NoteNumber))
            {
                var cbd = this.buttonData[this.noteReceivers[e.NoteNumber]] as CommandButtonData;
                cbd.Activated = e.Velocity > 0;
            }
            this.ActionImageChanged();
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
                        case 4: // VIEWS
                            this.currentLayer = ButtonLayer.faderModesAll;
                            ActionImageChanged();
                            break;
                    }
                    break;
                case ButtonLayer.faderModesAll:
                    switch (Int32.Parse(actionParameter))
                    {
                        case 5: // SENDS
                            this.currentLayer = ButtonLayer.faderModesSend;
                            this.selectMode = SelectButtonData.Mode.Send;
                            this.plugin.EmitSelectModeChanged(this.selectMode);
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
                        case 3: // USER 1 2 3
                            this.selectMode = SelectButtonData.Mode.User;
                            plugin.EmitSelectModeChanged(this.selectMode);
                            break;
                        case 4: // VIEWS (BACK)
                            this.currentLayer = ButtonLayer.faderModesAll;
                            this.selectMode = SelectButtonData.Mode.Select;
                            this.plugin.EmitSelectModeChanged(this.selectMode);
                            ActionImageChanged();
                            break;
                        case 5: // SENDS
                            this.selectMode = SelectButtonData.Mode.Send;
                            plugin.EmitSelectModeChanged(this.selectMode);
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


