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
    static buildFocusDeviceTextSysex(sysexBuffer, text) {
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

class LoupedeckHostCommandMessage {
    static isMessage(data, length) {
        if (length < LoupedeckHostCommandMessage.kMinLength)
            return false;
        return data[0] == 0xF0 &&
            data[1] == LoupedeckProtocol.kSysexHeader[0] &&
            data[2] == LoupedeckProtocol.kSysexHeader[1] &&
            data[3] == LoupedeckProtocol.kSysexHeader[2] &&
            data[4] == LoupedeckHostCommandMessage.kMessageType &&
            data[5] == LoupedeckHostCommandMessage.kMessageAction &&
            data[length - 1] == 0xF7;
    }
    static getPayload(data, length) {
        if (!LoupedeckHostCommandMessage.isMessage(data, length))
            return null;
        let payload = "";
        for (let i = LoupedeckHostCommandMessage.kPayloadStartIndex; i < length - 1; i++)
            payload += String.fromCharCode(data[i]);
        return payload;
    }
    static getCommandGroup(data, length) {
        let payload = LoupedeckHostCommandMessage.getPayload(data, length);
        if (payload == null)
            return "";
        let separatorIndex = payload.indexOf(LoupedeckHostCommandMessage.kSeparator);
        if (separatorIndex < 0)
            return payload;
        return payload.substring(0, separatorIndex);
    }
    static getCommandName(data, length) {
        let payload = LoupedeckHostCommandMessage.getPayload(data, length);
        if (payload == null)
            return "";
        let separatorIndex = payload.indexOf(LoupedeckHostCommandMessage.kSeparator);
        if (separatorIndex < 0)
            return "";
        return payload.substring(separatorIndex + 1);
    }
}
LoupedeckHostCommandMessage.kMessageType = 0x15;
LoupedeckHostCommandMessage.kMessageAction = 0x01;
LoupedeckHostCommandMessage.kPayloadStartIndex = 6;
LoupedeckHostCommandMessage.kSeparator = String.fromCharCode(0x1F);
LoupedeckHostCommandMessage.kMinLength = 8;
