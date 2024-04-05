include_file("resource://com.presonus.musicdevices/sdk/midiprotocol.js");
include_file("resource://com.presonus.musicdevices/sdk/controlsurfacedevice.js");
include_file("LoupedeckProtocol.js");

class ChannelTextHandler extends PreSonus.ControlHandler {
    constructor(name, channelID, offset) {
        super();
        this.name = name;
        this.channelID = channelID;
        this.offset = offset;
    }
    sendValue(value, flags) {
        this.device.sendChannelText(this.channelID, this.offset, value, this.control);
    }
}

class PlainTextHandler extends PreSonus.ControlHandler {
    constructor(name) {
        super();
        this.name = name;
    }
    sendValue(value, flags) {
        this.device.sendPlainText(value);
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
        this.addHandler(new ChannelTextHandler("labelText[0]", 0, 0));
        this.addHandler(new ChannelTextHandler("valueText[0]", 0, 1));
        this.addHandler(new ChannelTextHandler("descText[0]",  0, 2));
        this.addHandler(new ChannelTextHandler("userText[0]",  0, 3));
        this.addHandler(new ChannelTextHandler("labelText[1]", 1, 0));
        this.addHandler(new ChannelTextHandler("valueText[1]", 1, 1));
        this.addHandler(new ChannelTextHandler("descText[1]",  1, 2));
        this.addHandler(new ChannelTextHandler("userText[1]",  1, 3));
        this.addHandler(new ChannelTextHandler("labelText[2]", 2, 0));
        this.addHandler(new ChannelTextHandler("valueText[2]", 2, 1));
        this.addHandler(new ChannelTextHandler("descText[2]",  2, 2));
        this.addHandler(new ChannelTextHandler("userText[2]",  2, 3));
        this.addHandler(new ChannelTextHandler("labelText[3]", 3, 0));
        this.addHandler(new ChannelTextHandler("valueText[3]", 3, 1));
        this.addHandler(new ChannelTextHandler("descText[3]",  3, 2));
        this.addHandler(new ChannelTextHandler("userText[3]",  3, 3));
        this.addHandler(new ChannelTextHandler("labelText[4]", 4, 0));
        this.addHandler(new ChannelTextHandler("valueText[4]", 4, 1));
        this.addHandler(new ChannelTextHandler("descText[4]",  4, 2));
        this.addHandler(new ChannelTextHandler("userText[4]",  4, 3));
        this.addHandler(new ChannelTextHandler("labelText[5]", 5, 0));
        this.addHandler(new ChannelTextHandler("valueText[5]", 5, 1));
        this.addHandler(new ChannelTextHandler("descText[5]",  5, 2));
        this.addHandler(new ChannelTextHandler("userText[5]",  5, 3));
        this.addHandler(new ChannelTextHandler("selectedLabelText", 6, 0));
        this.addHandler(new ChannelTextHandler("selectedValueText", 6, 1));
        this.addHandler(new PlainTextHandler("focusDeviceText"));
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
    sendChannelText(channelID, offset, text) {
        // let outText = text.substring(0, 1) + text.substring(1);
        let outText = text;
        if (outText.length > 8) outText = outText.replace(/(?<!^)[aeiou]/g, '');
        this.sendSysex(LoupedeckProtocol.buildChannelTextSysex(this.sysexSendBuffer, channelID, offset, outText)); 
    } 
    sendPlainText(text) {
        this.sendSysex(LoupedeckProtocol.buildPlainTextSysex(this.sysexSendBuffer, text)); 
    } 
}

function createLoupedeckDeviceInstance() {
    return new LoupedeckMidiDevice();
}
