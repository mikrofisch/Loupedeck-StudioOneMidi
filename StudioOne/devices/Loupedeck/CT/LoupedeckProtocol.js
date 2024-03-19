class TextCell {
    constructor() {
    }
}
class LoupedeckControls {
}
LoupedeckControls.kLabelText = new TextCell();
class LoupedeckProtocol {
    static buildNativeModeSysex(sysexBuffer) {
        sysexBuffer.begin(LoupedeckProtocol.kSysexHeader);
        sysexBuffer.push(0x10);
        sysexBuffer.push(0x01);
        sysexBuffer.push(0x00);
        sysexBuffer.end();
        return sysexBuffer;
    }
    static buildTextSysex(sysexBuffer, channelID, text) {
        sysexBuffer.begin(LoupedeckProtocol.kSysexHeader);
        sysexBuffer.push(0x14);
        sysexBuffer.push(0x12);
        sysexBuffer.push(channelID);
        sysexBuffer.appendAscii(text);
        sysexBuffer.end();
        return sysexBuffer;
    }
}
LoupedeckProtocol.kSysexHeader = [0x00, 0x00, 0x66];
