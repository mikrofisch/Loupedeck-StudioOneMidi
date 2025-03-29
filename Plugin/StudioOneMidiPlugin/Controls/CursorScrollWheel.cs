namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using Melanchall.DryWetMidi.Common;

    using Melanchall.DryWetMidi.Core;

    // This class implements control of the play cursor in Studio One based via the
    // 'freecursortime' function which moves the cursor in 2s increments independent
    // of the zoom level. Basically not very useful.

    public class CursorScrollWheel : PluginDynamicAdjustment
    {
        // Initializes the adjustment class.
        // When `hasReset` is set to true, a reset command is automatically created for this adjustment.
        public CursorScrollWheel()
            : base(displayName: "Cursor Scroll Wheel", description: "Moves the cursor position", groupName: "Transport", hasReset: false)
        {
        }



        // This method is called when the adjustment is executed.
        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            if (diff < 0)
            {
                diff = 128 + diff;
                if (diff < 64) diff = 64;
            }
            if (diff > 127) diff = 127;
            var e = new ControlChangeEvent();
            e.ControlValue = (SevenBitNumber)diff;
            e.ControlNumber = (SevenBitNumber)0x3C;
            (this.Plugin as StudioOneMidiPlugin).loupedeckMidiOut.SendEvent(e);

            this.AdjustmentValueChanged();
        }

        // This method is called when the reset command related to the adjustment is executed.
        protected override void RunCommand(String actionParameter)
        {
        }

        // Returns the adjustment value that is shown next to the dial.
        // protected override String GetAdjustmentValue(String actionParameter) => this.Counter.ToString();
    }
    

}
