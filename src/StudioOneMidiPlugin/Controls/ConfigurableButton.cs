namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    // This defines 
    // Based on the source code for the official Loupedeck OBS Studio plugin
    // (https://github.com/Loupedeck-open-source/Loupedeck-ObsStudio-OpenPlugin)
    //
    internal class ConfigurableButton : ActionEditorCommand
    {
        private const String PropertySelector = "propertySelector";
        private const String ChannelSelector = "channelSelector";
        private const String ButtonTitleSelector = "buttonTitleSelector";
        private enum TitleMode
        {
            None,
            Full,
            LeftHalf,
            RightHalf
        }
        private TitleMode SelectedTitleMode = TitleMode.None;

        public ConfigurableButton()
        {
            this.DisplayName = "Configurable Button";
            this.Description = "Property controls for currently selected channel";
            this.GroupName = "";

            // Configuration widgets

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

            // Buttons

            for (int i = 0; i <= StudioOneMidiPlugin.ChannelCount; i++)
            {
                if (i < StudioOneMidiPlugin.ChannelCount)
                {
                    this.AddButton(i, ChannelProperty.BoolType.Select, "Select");
                }

                this.AddButton(i, ChannelProperty.BoolType.Mute, "Mute");
                this.AddButton(i, ChannelProperty.BoolType.Solo, "Solo");
                this.AddButton(i, ChannelProperty.BoolType.Arm, "Arm/Rec", "arm");
                this.AddButton(i, ChannelProperty.BoolType.Monitor, "Monitor", "monitor");
            }
        }

        protected override Boolean OnLoad()
        {
            return true;
        }

        protected override Boolean OnUnload()
        {
            return true;
        }

        private void OnSceneListChanged(Object sender, EventArgs e) => this.ActionEditor.ListboxItemsChanged(ButtonTitleSelector);

        private void OnActionEditorControlValueChanged(Object sender, ActionEditorControlValueChangedEventArgs e)
        {

            if (e.ControlName.EqualsNoCase(ButtonTitleSelector))
            {
                this.SelectedTitleMode = (TitleMode)e.ActionEditorState.GetControlValue(ButtonTitleSelector).ParseInt32();

                this.ActionEditor.ListboxItemsChanged(ButtonTitleSelector);
                // this.Plugin.Log.Info($"Button title mode changed.");
            }

            e.ActionEditorState.SetDisplayName("Tideldum");
        }

        private void OnActionEditorListboxItemsRequested(Object sender, ActionEditorListboxItemsRequestedEventArgs e)
        {
            /*
             * This does not work (yet)
             * e.ActionEditorState.SetEnabled(ButtonTitleSelector, isEnabled);
            */

            if (e.ControlName.EqualsNoCase(ButtonTitleSelector))
            {
                e.AddItem($"{(Int32)TitleMode.None}", "None", $"No title");
                e.AddItem($"{(Int32)TitleMode.Full}", "Full", $"Show the entire title text");
                e.AddItem($"{(Int32)TitleMode.LeftHalf}", "Left Half", $"Show left half of the title text");
                e.AddItem($"{(Int32)TitleMode.RightHalf}", "Right Half", $"Show right half of the title text");
            }
            if (e.ControlName.EqualsNoCase(ButtonTitleSelector))
            {
                e.AddItem(, "Mute");
            }
            else
            {
                this.Plugin.Log.Error($"Unexpected control name '{e.ControlName}'");
            }
        }

        private void OnAppConnected(Object sender, EventArgs e)
        {
        }

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
        }

        protected override BitmapImage GetCommandImage(ActionEditorActionParameters actionParameters, Int32 imageWidth, Int32 imageHeight)
        {
            BitmapBuilder bb = new BitmapBuilder(imageWidth, imageHeight);

            String titleText = "";

            switch (this.SelectedTitleMode)
            {
                case TitleMode.None:
                    titleText = "None";
                    break;
                case TitleMode.Full:
                    titleText = "Full";
                    break;
                case TitleMode.LeftHalf:
                    titleText = "Left";
                    break;
                case TitleMode.RightHalf:
                    titleText = "Right";
                    break;
            }

            bb.DrawText(titleText, 0, 0, bb.Width, bb.Height);

            return bb.ToImage();
        }

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            return true;
        }

        private void AddButton(int i, ChannelProperty.BoolType bt, string name, string iconName = null)
        {
            string chstr = i == StudioOneMidiPlugin.ChannelCount ? " (Selected channel)" : $" (CH {i + 1})";

            PropertyButtonData bd;

            if (bt == ChannelProperty.BoolType.Select)
            {
                bd = new SelectButtonData(i, bt);
            }
            else
            {
                bd = new PropertyButtonData(i, bt, PropertyButtonData.TrackNameMode.ShowFull, iconName);
            }

            var idx = $"{i}:{(int)bd.Type}";
            this.buttonData[idx] = bd;
            AddParameter(idx, name + chstr, name);
        }
    }
}