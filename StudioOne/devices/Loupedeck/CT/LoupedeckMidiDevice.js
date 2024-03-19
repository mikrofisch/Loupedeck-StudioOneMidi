include_file("resource://com.presonus.musicdevices/sdk/midiprotocol.js");
include_file("resource://com.presonus.musicdevices/sdk/controlsurfacedevice.js");
include_file("LoupedeckProtocol.js");
class TextHandler extends PreSonus.ControlHandler {
    constructor(name, control) {
        super();
        this.name = name;
        this.control = control;
    }
    sendValue(value, flags) {
        this.device.sendText(value, this.control);
    }
}
class LoupedeckMidiDevice extends PreSonus.ControlSurfaceDevice {
    constructor() {
        super();
        this.debugLog = false;
    }
    onInit(hostDevice) {
        super.onInit(hostDevice);
        this.debugLog = false;
        this.addHandler(new TextHandler("selectedLabelText", LoupedeckControls.kLabelText));
    }
    onMidiOutConnected(state) {
        super.onMidiOutConnected(state);
        if (state) {
//            this.sendSysex(LoupedeckProtocol.buildNativeModeSysex(this.sysexSendBuffer));
//            this.sendText("Studio One", LoupedeckControls.kUpperRightText);
//            this.sendText("Control Link", LoupedeckControls.kLowerText);
            this.hostDevice.invalidateAll();
        }
    }
    sendText(text, control) {
//        let trimmedText = this.hostDevice.trimText(text, control.length, true);

        let trimmedText = text.substring(0, 1) + text.substring(1).replace(/[aeiou ]/gi, '');
        this.sendSysex(LoupedeckProtocol.buildTextSysex(this.sysexSendBuffer, control.offset, trimmedText));
    } 
}

function createLoupedeckDeviceInstance() {
    return new LoupedeckMidiDevice();
}
