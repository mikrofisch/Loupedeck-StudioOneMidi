/**
 * Defines the forward/rewind navigation mode behavior.
 * @enum {number}
 */
var ForwardMode;
(function (ForwardMode) {
    ForwardMode[ForwardMode["kForwardBeat"] = 0] = "kForwardBeat";
    ForwardMode[ForwardMode["kRewindBeat"] = 1] = "kRewindBeat";
    ForwardMode[ForwardMode["kForwardGrid"] = 2] = "kForwardGrid";
    ForwardMode[ForwardMode["kRewindGrid"] = 3] = "kRewindGrid";
    ForwardMode[ForwardMode["kRewindFrame"] = 4] = "kRewindFrame";
    ForwardMode[ForwardMode["kForwardFrame"] = 5] = "kForwardFrame";
    ForwardMode[ForwardMode["kForwardSecond"] = 6] = "kForwardSecond";
    ForwardMode[ForwardMode["kRewindSecond"] = 7] = "kRewindSecond";
})(ForwardMode || (ForwardMode = {}));

function getParamNameForForwardMode(mode)
{
    switch (mode) {
        case ForwardMode.kForwardBeat:
        case ForwardMode.kRewindBeat:
            return 'Beats';
        case ForwardMode.kForwardGrid:
        case ForwardMode.kRewindGrid:
            return 'Steps';
        case ForwardMode.kForwardFrame:
        case ForwardMode.kRewindFrame:
            return 'Frames';
        case ForwardMode.kForwardSecond:
        case ForwardMode.kRewindSecond:
            return 'Seconds';
        default:
            return '';
    }
}

var ForwardStepsTask = (function () {
    function ForwardStepsTask(forwardMode)
    {
        this.interfaces = [Host.Interfaces.IEditTask];
        this.forwardMode = forwardMode;
    }

    ForwardStepsTask.prototype.prepareEdit = function (context)
    {
        this.paramList = Host.Classes.createInstance('CCL:ParamList');
        this.paramList.controller = this;

        var parameters = context.parameters;
        this.Steps = parameters.addFloat(0.001, 1000, getParamNameForForwardMode(this.forwardMode));

        return Host.Results.kResultOk;
    };

    ForwardStepsTask.prototype.performEdit = function (context)
    {
        var hasArguments = context.getArguments();

        if (!context.editor.quantize)
            return Host.Results.kResultFailed;

        var amount = hasArguments ? Number(this.Steps.value) : 1;
        var epsilon = 1e-10;
        var cursorTime = context.editor.cursorInfo.cursorTime;

        var floorOrRound = function (value)
        {
            return Math.abs(value - Math.round(value)) < epsilon ? Math.round(value) : Math.floor(value);
        };

        var ceilOrRound = function (value)
        {
            return Math.abs(value - Math.round(value)) < epsilon ? Math.round(value) : Math.ceil(value);
        };

        var quantizeBase = context.editor.quantize.base;

        if (this.forwardMode === ForwardMode.kForwardGrid)
        {
            var remainder = cursorTime.musical % quantizeBase;
            if (remainder !== 0)
                cursorTime.musical = cursorTime.musical + (quantizeBase - remainder);

            cursorTime.musical += quantizeBase * amount;
        }
        else if (this.forwardMode === ForwardMode.kRewindGrid)
        {
            var previousGrid = Math.floor(cursorTime.musical / quantizeBase) * quantizeBase;
            cursorTime.musical = Math.abs(cursorTime.musical - previousGrid) < epsilon
                ? previousGrid - context.editor.quantize.base * amount
                : cursorTime.musical - context.editor.quantize.base * amount;
        }
        else if (this.forwardMode === ForwardMode.kForwardBeat)
        {
            cursorTime.musical = floorOrRound(cursorTime.musical) + amount;
        }
        else if (this.forwardMode === ForwardMode.kRewindBeat)
        {
            cursorTime.musical = ceilOrRound(cursorTime.musical) - amount;
        }
        else if (this.forwardMode === ForwardMode.kForwardFrame)
        {
            cursorTime.frames = cursorTime.frames + amount;
        }
        else if (this.forwardMode === ForwardMode.kRewindFrame)
        {
            cursorTime.frames = cursorTime.frames - amount;
        }
        else if (this.forwardMode === ForwardMode.kForwardSecond)
        {
            if (amount !== 1)
                cursorTime.seconds += amount;
            else
                cursorTime.seconds = floorOrRound(cursorTime.seconds) + amount;
        }
        else if (this.forwardMode === ForwardMode.kRewindSecond)
        {
            if (amount !== 1)
                cursorTime.seconds -= amount;
            else
                cursorTime.seconds = ceilOrRound(cursorTime.seconds) - amount;
        }

        context.editor.cursorInfo.setCursorTime(cursorTime);
        return Host.Results.kResultOk;
    };

    return ForwardStepsTask;
})();

function createForwardBeatTaskInstance()
{
    return new ForwardStepsTask(ForwardMode.kForwardBeat);
}

function createRewindBeatTaskInstance()
{
    return new ForwardStepsTask(ForwardMode.kRewindBeat);
}

function createForwardGridTaskInstance()
{
    return new ForwardStepsTask(ForwardMode.kForwardGrid);
}

function createRewindGridTaskInstance()
{
    return new ForwardStepsTask(ForwardMode.kRewindGrid);
}

function createForwardFrameTaskInstance()
{
    return new ForwardStepsTask(ForwardMode.kForwardFrame);
}

function createRewindFrameTaskInstance()
{
    return new ForwardStepsTask(ForwardMode.kRewindFrame);
}

function createForwardSecondTaskInstance()
{
    return new ForwardStepsTask(ForwardMode.kForwardSecond);
}

function createRewindSecondTaskInstance()
{
    return new ForwardStepsTask(ForwardMode.kRewindSecond);
}
