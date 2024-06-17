namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Melanchall.DryWetMidi.Common;

    using Melanchall.DryWetMidi.Core;

    // This class implements control of the wheel function in Studio One based on the functionality
    // of a Mackie MCU device.

    public class Wheel : PluginDynamicAdjustment
    {
        // This variable holds the current value of the counter.
        private Int32 Counter = 0;

        // Initializes the adjustment class.
        // When `hasReset` is set to true, a reset command is automatically created for this adjustment.
        public Wheel()
            : base(displayName: "Wheel", description: "Counts rotation ticks", groupName: "Control", hasReset: false)
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
            (this.Plugin as StudioOneMidiPlugin).mackieMidiOut.SendEvent(e);

            this.AdjustmentValueChanged();
        }

        // This method is called when the reset command related to the adjustment is executed.
        protected override void RunCommand(String actionParameter)
        {
        }

        // Returns the adjustment value that is shown next to the dial.
        protected override String GetAdjustmentValue(String actionParameter) => this.Counter.ToString();
    }
    

}
