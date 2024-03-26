namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    // This is a special button class that creates 6 buttons which are then
    // placed in the central area of the button field on the Loupedeck.
    // The functionality of the buttons changes dynamically.
    internal class LoupedeckModes : PluginDynamicCommand
    {
        private enum buttonLayer
        {
            channelProperties,
            faderModesAll,
            faderModesSend
        }

        private IDictionary<string, string> buttonData = new Dictionary<string, string>();

        public LoupedeckModes()
        {
            this.Description = "Special button for controlling Loupedeck fader modes";

            for (int i = 0; i < 6; i++)
            {
                AddParameter($"{i}", "Mode Group Button " + i, "Modes");
                buttonData[$"{i}"] = "Button " + i;
            }
        }

        protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
            if (actionParameter == null)
                return null;

            var bb = new BitmapBuilder(imageSize);

            bb.DrawText(buttonData[actionParameter], 0, 0, bb.Width, bb.Height);

            return bb.ToImage();
        }

        protected override void RunCommand(string actionParameter)
        {
            if (actionParameter == "4")
            {
                buttonData["0"] = "Wumm";
                ActionImageChanged();
            }
        }
    }
}

