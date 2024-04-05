include_file("resource://com.presonus.musicdevices/sdk/midiprotocol.js");
include_file("resource://com.presonus.musicdevices/sdk/controlsurfacedevice.js");
include_file("LoupedeckProtocol.js");

class TextHandler extends PreSonus.ControlHandler {
    constructor(name, channelID, offset, control) {
        super();
        this.name = name;
        this.channelID = channelID;
        this.offset = offset;
        this.control = control;
    }
    sendValue(value, flags) {
        this.device.sendText(this.channelID, this.offset, value, this.control);
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
        this.addHandler(new TextHandler("labelText[0]", 0, 0, LoupedeckControls.kLabelText0));
        this.addHandler(new TextHandler("valueText[0]", 0, 1, LoupedeckControls.kValueText0));
        this.addHandler(new TextHandler("descText[0]",  0, 2, LoupedeckControls.kDescText0));
        this.addHandler(new TextHandler("userText[0]",  0, 3, LoupedeckControls.kUserText0));
        this.addHandler(new TextHandler("labelText[1]", 1, 0, LoupedeckControls.kLabelText1));
        this.addHandler(new TextHandler("valueText[1]", 1, 1, LoupedeckControls.kValueText1));
        this.addHandler(new TextHandler("descText[1]",  1, 2, LoupedeckControls.kDescText1));
        this.addHandler(new TextHandler("userText[1]",  1, 3, LoupedeckControls.kUserText1));
        this.addHandler(new TextHandler("labelText[2]", 2, 0, LoupedeckControls.kLabelText2));
        this.addHandler(new TextHandler("valueText[2]", 2, 1, LoupedeckControls.kValueText2));
        this.addHandler(new TextHandler("descText[2]",  2, 2, LoupedeckControls.kDescText2));
        this.addHandler(new TextHandler("userText[2]",  2, 3, LoupedeckControls.kUserText2));
        this.addHandler(new TextHandler("labelText[3]", 3, 0, LoupedeckControls.kLabelText3));
        this.addHandler(new TextHandler("valueText[3]", 3, 1, LoupedeckControls.kValueText3));
        this.addHandler(new TextHandler("descText[3]",  3, 2, LoupedeckControls.kDescText3));
        this.addHandler(new TextHandler("userText[3]",  3, 3, LoupedeckControls.kUserText3));
        this.addHandler(new TextHandler("labelText[4]", 4, 0, LoupedeckControls.kLabelText4));
        this.addHandler(new TextHandler("valueText[4]", 4, 1, LoupedeckControls.kValueText4));
        this.addHandler(new TextHandler("descText[4]",  4, 2, LoupedeckControls.kDescText4));
        this.addHandler(new TextHandler("userText[5]",  4, 3, LoupedeckControls.kUserText4));
        this.addHandler(new TextHandler("labelText[5]", 5, 0, LoupedeckControls.kLabelText5));
        this.addHandler(new TextHandler("valueText[5]", 5, 1, LoupedeckControls.kValueText5));
        this.addHandler(new TextHandler("descText[5]",  5, 2, LoupedeckControls.kDescText5));
        this.addHandler(new TextHandler("userText[5]",  5, 3, LoupedeckControls.kUserText5));
        this.addHandler(new TextHandler("selectedLabelText", 6, 0, LoupedeckControls.kSelectedLabelText));
        this.addHandler(new TextHandler("selectedValueText", 6, 1, LoupedeckControls.kSelectedValueText));
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
    sendText(channelID, offset, text, control) {
        let outText = text.substring(0, 1) + text.substring(1);
        //this.Console.writeLine(outText.Length);
        if (outText.length > 8) outText = outText.replace(/(?<!^)[aeiou]/g, '');
        this.sendSysex(LoupedeckProtocol.buildTextSysex(this.sysexSendBuffer, channelID, offset, outText)); 
    } 
}

function createLoupedeckDeviceInstance() {
    return new LoupedeckMidiDevice();
}
