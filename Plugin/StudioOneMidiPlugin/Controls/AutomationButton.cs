namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;

    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;

    internal class AutomationButton : StudioOneButton<ButtonData>
    {
        public AutomationButton()
        {
            this.DisplayName = "Automation Mode Controls";
            this.Description = "Buttons to display and change the automation mode setting for the current channel";

            this.AddButton(new AutomationModeCommandButtonData(AutomationMode.Off), "off", "Automation Off");
            this.AddButton(new AutomationModeCommandButtonData(AutomationMode.Read), "read", "Automation: Read");
            this.AddButton(new AutomationModeCommandButtonData(AutomationMode.Touch), "touch", "Automation: Touch");
            this.AddButton(new AutomationModeCommandButtonData(AutomationMode.Latch), "latch", "Automation: Latch");
            this.AddButton(new AutomationModeCommandButtonData(AutomationMode.Write), "write", "Automation: Write");
        }
        protected override bool OnLoad()
        {
            base.OnLoad();

            ((StudioOneMidiPlugin)Plugin).AutomationModeChanged += (Object? sender, AutomationMode e) =>
            {
                this.UpdateAllActionImages();
            };

            return true;
        }
    private void AddButton(ButtonData bd, String idx, String name)
        {
            this._buttonData[idx] = bd;
            this.AddParameter(idx, name, "Automation");
        }
    }
}
