namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using static Loupedeck.StudioOneMidiPlugin.Controls.PropertyButtonData;

    // This defines 
    // Based on the source code for the official Loupedeck OBS Studio plugin
    // (https://github.com/Loupedeck-open-source/Loupedeck-ObsStudio-OpenPlugin)
    //
    internal class ConfigurableButton : ActionEditorCommand
    {
        private const String PropertySelector = "propertySelector";
        private const String ChannelSelector = "channelSelector";
        private const String ButtonTitleSelector = "buttonTitleSelector";
        private const int TrackNameH = 24;

        private enum TitleMode
        {
            None,
            Full,
            LeftHalf,
            RightHalf
        }
        private BitmapImage Icon;

        public ConfigurableButton()
        {
            this.DisplayName = "Configurable Button";
            this.Description = "Property controls for currently selected channel";
            this.GroupName = "";

            this.ActionEditor.AddControlEx(parameterControl:
                new ActionEditorListbox(name: PropertySelector, labelText: "Property:"/*,"Select the property to control"*/)
                    .SetRequired()
                );
            this.ActionEditor.AddControlEx(parameterControl:
                new ActionEditorListbox(name: ChannelSelector, labelText: "Channel:"/*,"Select the property to control"*/)
                    .SetRequired()
                );
            this.ActionEditor.AddControlEx(parameterControl:
                new ActionEditorListbox(name: ButtonTitleSelector, labelText: "Button Title:"/*,"Select button title mode"*/)
                    .SetRequired()
                );

            this.ActionEditor.ListboxItemsRequested += this.OnActionEditorListboxItemsRequested;
            this.ActionEditor.ControlValueChanged += this.OnActionEditorControlValueChanged;
        }

        protected override Boolean OnLoad()
        {
            return true;
        }

        protected override Boolean OnUnload()
        {
            return true;
        }

        private void OnActionEditorControlValueChanged(Object sender, ActionEditorControlValueChangedEventArgs e)
        {
            if (e.ControlName.EqualsNoCase(PropertySelector))
            {
                var controlProperty = (ChannelProperty.PropertyType)e.ActionEditorState.GetControlValue(PropertySelector).ParseInt32();

                if (controlProperty == ChannelProperty.PropertyType.Arm)
                {
                    this.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("record_52px.png"));
                }
                else if (controlProperty == ChannelProperty.PropertyType.Monitor)
                {
                    this.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("monitor_52px.png"));
                }
                else
                {
                    this.Icon = null;
                }
                if (controlProperty == ChannelProperty.PropertyType.Select)
                {
                    if ((int)e.ActionEditorState.GetControlValue(ChannelSelector).ParseInt32() 
                        == StudioOneMidiPlugin.ChannelCount)
                    {
                        e.ActionEditorState.SetValue(ChannelSelector, "0");
                    }
                }
                this.ActionEditor.ListboxItemsChanged(ChannelSelector);
            }

            // e.ActionEditorState.SetDisplayName("");
        }

        private void OnActionEditorListboxItemsRequested(Object sender, ActionEditorListboxItemsRequestedEventArgs e)
        {
            /*
             * This does not work (yet)
             * e.ActionEditorState.SetEnabled(ButtonTitleSelector, isEnabled);
            */

            if (e.ControlName.EqualsNoCase(PropertySelector))
            {
                e.AddItem($"{(Int32)ChannelProperty.PropertyType.Select}", "Select", $"Select bank channel");
                e.AddItem($"{(Int32)ChannelProperty.PropertyType.Mute}", "Mute", $"Mute channel");
                e.AddItem($"{(Int32)ChannelProperty.PropertyType.Solo}", "Solo", $"Solo channel");
                e.AddItem($"{(Int32)ChannelProperty.PropertyType.Arm}", "Arm/Record", $"Arm channel track for recording");
                e.AddItem($"{(Int32)ChannelProperty.PropertyType.Monitor}", "Monitor", $"Activate monitoring");
            }
            else if (e.ControlName.EqualsNoCase(ChannelSelector))
            {
                int i;
                for (i = 0; i < StudioOneMidiPlugin.ChannelCount; i++)
                {
                    e.AddItem($"{i}", $"Bank Channel {i+1}", $"Channel {i+1} of the current bank of 6 channels controlled by the Loupedeck device");
                }
                if ((ChannelProperty.PropertyType)e.ActionEditorState.GetControlValue(PropertySelector).ParseInt32() 
                     != ChannelProperty.PropertyType.Select)
                {
                    e.AddItem($"{i}", $"Selected Channel", $"The channel currently selected in Studio One");
                }
            }
            else if (e.ControlName.EqualsNoCase(ButtonTitleSelector))
            {
                e.AddItem($"{(Int32)TitleMode.None}", "None", $"No title");
                e.AddItem($"{(Int32)TitleMode.Full}", "Full", $"Show the entire title text");
                e.AddItem($"{(Int32)TitleMode.LeftHalf}", "Left Half", $"Show left half of the title text");
                e.AddItem($"{(Int32)TitleMode.RightHalf}", "Right Half", $"Show right half of the title text");
            }
            else
            {
                this.Plugin.Log.Error($"Unexpected control name '{e.ControlName}'");
            }
        }

        protected override BitmapImage GetCommandImage(ActionEditorActionParameters actionParameters, Int32 imageWidth, Int32 imageHeight)
        {
            if (!actionParameters.TryGetInt32(PropertySelector, out var controlProperty)) return null;
            if (!actionParameters.TryGetInt32(ButtonTitleSelector, out var value)) return null;
            TitleMode titleMode = (TitleMode)value;

            BitmapBuilder bb = new BitmapBuilder(imageWidth, imageHeight);

            switch (titleMode)
            {
                case TitleMode.None:
                    break;
                case TitleMode.Full:
                    break;
                case TitleMode.LeftHalf:
                    break;
                case TitleMode.RightHalf:
                    break;
            }
            switch ((ChannelProperty.PropertyType)controlProperty)
            {
                case ChannelProperty.PropertyType.Select:
                    break;
                case ChannelProperty.PropertyType.Mute:
                    break;
                case ChannelProperty.PropertyType.Solo:
                    break;
                case ChannelProperty.PropertyType.Arm:
                    break;
                case ChannelProperty.PropertyType.Monitor:
                    break;
            }

            int yOff = titleMode == TitleMode.None ? 0 : TrackNameH;

            if (this.Icon != null)
            {
                bb.DrawImage(this.Icon, bb.Width / 2 - this.Icon.Width / 2, yOff + (bb.Height - yOff) / 2 - this.Icon.Height / 2);
            }
            else
            {
                bb.DrawText(ChannelProperty.PropertyLetter[controlProperty], 0, yOff, bb.Width, bb.Height - yOff, null, 32);
            }

            return bb.ToImage();
        }

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            return true;
        }
    }
}