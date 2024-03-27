namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows.Navigation;
    using System.Xml.Linq;

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

        private IDictionary<string, ModeButtonData> buttonData = new Dictionary<string, ModeButtonData>();

        private abstract class ModeButtonData
        {
            public abstract BitmapImage getImage(PluginImageSize imageSize);
        }

        ButtonLayer currentLayer = ButtonLayer.channelProperties;

        private class PropertyButtonData : ModeButtonData
        {
            public ChannelProperty.BoolType Type;
            public BitmapImage Icon = null;

            public PropertyButtonData(ChannelProperty.BoolType bt, string iconName = null)
            {
                Type = bt;
                if (iconName != null)
                    Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{iconName}_52px.png"));
            }

            public override BitmapImage getImage(PluginImageSize imageSize)
            {
                BitmapBuilder bb = new BitmapBuilder(imageSize);

                if (this.Icon != null)
                {
                    bb.DrawImage(this.Icon, bb.Width / 2 - this.Icon.Width / 2, bb.Height);
                }
                else
                {
                    bb.DrawText(ChannelProperty.boolPropertyLetter[(int)this.Type], 0, 0, bb.Width, bb.Height, null, 32);
                }

                return bb.ToImage();
            }
        }


        public LoupedeckModes()
        {
            this.Description = "Special button for controlling Loupedeck fader modes";

            // Create UI buttons
            for (int i = 0; i < 6; i++)
            {
                AddParameter($"{i}", "Mode Group Button " + i, "Modes");
            }

            // Create button data for each layer
            addButton(ButtonLayer.channelProperties, 0, new PropertyButtonData(ChannelProperty.BoolType.Mute));
            addButton(ButtonLayer.channelProperties, 1, new PropertyButtonData(ChannelProperty.BoolType.Solo));
            addButton(ButtonLayer.channelProperties, 2, new PropertyButtonData(ChannelProperty.BoolType.Arm, "record"));
            addButton(ButtonLayer.channelProperties, 3, new PropertyButtonData(ChannelProperty.BoolType.Monitor, "monitor"));

        }

        // common
        protected override bool OnLoad()
        {
            plugin = base.Plugin as StudioOneMidiPlugin;

            plugin.MackieDataChanged += (object sender, EventArgs a) => {
                ActionImageChanged();
            };

            return true;
        }

        protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
            if (actionParameter == null)
                return null;

            string idx = $"{(int)currentLayer}:" + actionParameter;

            return buttonData[idx].getImage(imageSize);
        }

        protected override void RunCommand(string actionParameter)
        {
            switch (currentLayer)
            {
                case ButtonLayer.channelProperties:
                    break;
                case ButtonLayer.faderModesAll:
                    break;
                case ButtonLayer.faderModesSend:
                    break;
            }
        }

        private void addButton(ButtonLayer buttonLayer, int buttonIndex, ModeButtonData bd)
        {
            string idx = $"{(int)buttonLayer}:{buttonIndex}";
            buttonData[idx] = bd;
        }
    }

}


