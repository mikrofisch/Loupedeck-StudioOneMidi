namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;

    // This class implements an empty dummy command that can be used for things like menu navigation.

    public class DummyCommand : ActionEditorCommand
    {
        // Initializes the command class.
        public DummyCommand()
        {
            this.DisplayName = "Dummy Command";
            this.Description = "A command that does nothing";
            this.GroupName = "";

            this.ActionEditor.AddControlEx(parameterControl:
                new ActionEditorTextbox(name: "LabelText", labelText: "Label:"/*,"Random ID"*/)
                );
        }


        protected override BitmapImage GetCommandImage(ActionEditorActionParameters actionParameters, Int32 imageWidth, Int32 imageHeight)
        {
            if (!actionParameters.TryGetString("LabelText", out var labelText))
                return null;

            var bb = new BitmapBuilder(imageWidth, imageHeight);

            bb.DrawText(labelText);

            return bb.ToImage();
        }

        // This method is called when the user executes the command.
        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            return true;
        }
    }
}
