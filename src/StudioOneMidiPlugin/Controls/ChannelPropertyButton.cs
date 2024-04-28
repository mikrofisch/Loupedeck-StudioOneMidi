namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Diagnostics;

    using Melanchall.DryWetMidi.Core;

     // This defines 
    // Based on the source code for the official Loupedeck OBS Studio plugin
    // (https://github.com/Loupedeck-open-source/Loupedeck-ObsStudio-OpenPlugin)
    //
    internal class ChannelPropertyButton : ActionEditorCommand
    {
        protected StudioOneMidiPlugin plugin;

        private const String PropertySelector = "propertySelector";
        private const String ChannelSelector = "channelSelector";
        private const String ButtonTitleSelector = "buttonTitleSelector";

        private BitmapImage IconArm, IconMonitor;
        private ChannelProperty.PropertyType MultiPropertyType = ChannelProperty.PropertyType.Mute;

        public ChannelPropertyButton()
        {
            this.DisplayName = "Channel Property Button";
            this.Description = "Property controls for currently selected channel";
            this.GroupName = "";

            this.ActionEditor.AddControlEx(parameterControl:
                new ActionEditorListbox(name: PropertySelector, labelText: "Property:"/*,"Select the property to control"*/)
                    .SetRequired()
                );
            this.ActionEditor.AddControlEx(parameterControl:
                new ActionEditorListbox(name: ChannelSelector, labelText: "Channel:"/*,"Select the fader bank channel"*/)
                    .SetRequired()
                );
            this.ActionEditor.AddControlEx(parameterControl:
                new ActionEditorListbox(name: ButtonTitleSelector, labelText: "Button Title:"/*,"Select button title mode"*/)
                    .SetRequired()
                );

            this.IconArm     = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("record_52px.png"));
            this.IconMonitor = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("monitor_52px.png"));

            this.ActionEditor.ListboxItemsRequested += this.OnActionEditorListboxItemsRequested;
            this.ActionEditor.ControlValueChanged += this.OnActionEditorControlValueChanged;
        }

        protected override Boolean OnLoad()
        {
            this.plugin = base.Plugin as StudioOneMidiPlugin;

            this.plugin.ChannelDataChanged += (object sender, EventArgs e) =>
            {
                this.ActionImageChanged();
            };

            this.plugin.PropertySelectionChanged += (object sender, ChannelProperty.PropertyType e) =>
            {
                this.MultiPropertyType = e;
                this.ActionImageChanged();
            };
            return true;
        }

        private void OnActionEditorControlValueChanged(Object sender, ActionEditorControlValueChangedEventArgs e)
        {
        }

        private void OnActionEditorListboxItemsRequested(Object sender, ActionEditorListboxItemsRequestedEventArgs e)
        {
            /*
             * This does not work (yet)
             * e.ActionEditorState.SetEnabled(ButtonTitleSelector, isEnabled);
            */

            if (e.ControlName.EqualsNoCase(PropertySelector))
            {
                e.AddItem($"{(Int32)ChannelProperty.PropertyType.Mute}", "Mute", $"Mute channel");
                e.AddItem($"{(Int32)ChannelProperty.PropertyType.Solo}", "Solo", $"Solo channel");
                e.AddItem($"{(Int32)ChannelProperty.PropertyType.Arm}", "Arm/Record", $"Arm channel track for recording");
                e.AddItem($"{(Int32)ChannelProperty.PropertyType.Monitor}", "Monitor", $"Activate monitoring");
                e.AddItem("8", "Multi Mute/Solo", $"Property to control is set by selection button (default: Mute)");
                e.AddItem("9", "Multi Arm/Monitor", $"Property to control is set by selection button (default: Arm)");
            }
            else if (e.ControlName.EqualsNoCase(ChannelSelector))
            {
                int i;
                for (i = 0; i < StudioOneMidiPlugin.ChannelCount; i++)
                {
                    e.AddItem($"{i}", $"Bank Channel {i+1}", $"Channel {i+1} of the current bank of 6 channels controlled by the Loupedeck device");
                }
                e.AddItem($"{i}", $"Selected Channel", $"The channel currently selected in Studio One");
            }
            else if (e.ControlName.EqualsNoCase(ButtonTitleSelector))
            {
                e.AddItem($"{(Int32)PropertyButtonDataBase.TrackNameMode.None}", "None", $"No title");
                e.AddItem($"{(Int32)PropertyButtonDataBase.TrackNameMode.NoneOffset}", "None With Offset", $"No title with extra space at the top of the button");
                e.AddItem($"{(Int32)PropertyButtonDataBase.TrackNameMode.ShowFull}", "Full", $"Show the entire title text");
                e.AddItem($"{(Int32)PropertyButtonDataBase.TrackNameMode.ShowLeftHalf}", "Left Half", $"Show left half of the title text");
                e.AddItem($"{(Int32)PropertyButtonDataBase.TrackNameMode.ShowRightHalf}", "Right Half", $"Show right half of the title text");
            }
            else
            {
                this.Plugin.Log.Error($"Unexpected control name '{e.ControlName}'");
            }
        }

        protected override BitmapImage GetCommandImage(ActionEditorActionParameters actionParameters, Int32 imageWidth, Int32 imageHeight)
        {
            if (!actionParameters.TryGetInt32(PropertySelector, out var controlProperty)) return null;
            if (!actionParameters.TryGetInt32(ButtonTitleSelector, out var trackNameMode)) return null;
            if (!actionParameters.TryGetInt32(ChannelSelector, out var channelIndex)) return null;

            if (channelIndex == StudioOneMidiPlugin.ChannelCount && controlProperty < 8)
            {
                BitmapImage icon = null;
                if ((ChannelProperty.PropertyType)controlProperty == ChannelProperty.PropertyType.Arm)
                    icon = this.IconArm;
                if ((ChannelProperty.PropertyType)controlProperty == ChannelProperty.PropertyType.Monitor)
                    icon = this.IconMonitor;

                MackieChannelData cd = this.plugin.mackieChannelData[channelIndex.ToString()];
                return PropertyButtonDataBase.drawImage(new BitmapBuilder(imageWidth, imageHeight),
                                                    (ChannelProperty.PropertyType)controlProperty,
                                                    cd.BoolProperty[controlProperty],
                                                    (PropertyButtonDataBase.TrackNameMode)trackNameMode,
                                                    cd.Label,
                                                    icon);
            }
            else
            {
                if (controlProperty >= 8)
                {
                    if (controlProperty == 8 &&
                        (this.MultiPropertyType == ChannelProperty.PropertyType.Arm ||
                        this.MultiPropertyType == ChannelProperty.PropertyType.Monitor))
                        this.MultiPropertyType = ChannelProperty.PropertyType.Mute;
                    else if (controlProperty == 9 &&
                        (this.MultiPropertyType == ChannelProperty.PropertyType.Mute ||
                        this.MultiPropertyType == ChannelProperty.PropertyType.Solo))
                        this.MultiPropertyType = ChannelProperty.PropertyType.Arm;
                    controlProperty = (Int32)this.MultiPropertyType;
                }

                return SelectButtonData.drawImage(new BitmapBuilder(imageWidth, imageHeight),
                                                  this.plugin.mackieChannelData[channelIndex.ToString()],
                                                  StudioOneMidiPlugin.SelectButtonMode.Select,
                                                  false,
                                                  (ChannelProperty.PropertyType) controlProperty);
            }
        }

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            if (!actionParameters.TryGetInt32(PropertySelector, out var controlProperty)) return false;
            if (!actionParameters.TryGetString(ChannelSelector, out var channelIndex)) return false;

            MackieChannelData cd = this.plugin.mackieChannelData[channelIndex];

            if (controlProperty >= 8)
            {
                controlProperty = (Int32)this.MultiPropertyType;
            }

            cd.EmitChannelPropertyPress((ChannelProperty.PropertyType)controlProperty);
            return true;
        }
    }
}