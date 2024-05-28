class TextCell {
}
class LoupedeckControls {
}

class LoupedeckProtocol {
    static buildNativeModeSysex(sysexBuffer) {
        sysexBuffer.begin(LoupedeckProtocol.kSysexHeader);
        sysexBuffer.push(0x10);
        sysexBuffer.push(0x01);
        sysexBuffer.push(0x00);
        sysexBuffer.end();
        return sysexBuffer;
    }
    
    // Mackie MCU style sysex message for sending a string to the display. 
    // Unlike the real MCU where text is sent directly to a position in the display,
    // channelID and offset are used to indicate the channel and value type.
    // offset 0 - Label text
    //        1 - Value text
    //        2 - Description
    //        3 - User button text
    //
    static buildChannelTextSysex(sysexBuffer, channelID, offset, text) {
        sysexBuffer.begin(LoupedeckProtocol.kSysexHeader);
        sysexBuffer.push(0x14);
        sysexBuffer.push(0x12);
        sysexBuffer.push(channelID * 4 + offset);
        sysexBuffer.appendAscii(text);
        sysexBuffer.end();
        return sysexBuffer;
    }
    static buildPlainTextSysex(sysexBuffer, text) {
        sysexBuffer.begin(LoupedeckProtocol.kSysexHeader);
        sysexBuffer.push(0x14);
        sysexBuffer.push(0x13);
        sysexBuffer.appendAscii(text);
        sysexBuffer.end();
        return sysexBuffer;
    }
    static buildFunctionTextSysex(sysexBuffer, index, text) {
        sysexBuffer.begin(LoupedeckProtocol.kSysexHeader);
        sysexBuffer.push(0x14);
        sysexBuffer.push(0x14);
        sysexBuffer.push(index);
        sysexBuffer.appendAscii(text);
        sysexBuffer.end();
        return sysexBuffer;
    }
}
LoupedeckProtocol.kSysexHeader = [0x00, 0x00, 0x66];
