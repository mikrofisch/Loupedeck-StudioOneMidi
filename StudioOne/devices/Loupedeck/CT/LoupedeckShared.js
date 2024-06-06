include_file("resource://com.presonus.musicdevices/sdk/controlsurfacecomponent.js");
include_file("resource://com.presonus.musicdevices/presonus/pslsurfacecomponent.js");

const kNumChannels = 6;
const kNumUserBanks = 6;
const kSendSlotAll = 0;
const kSendSlotFirst = 1;

class ParamDescriptor {
    constructor(label, name, altname = "") {
        this.label = label;
        this.name = name;
        this.altname = altname;
    }
}
function getSendMuteParamID(slotIndex) {
    return PreSonus.getChannelFolderParamID(PreSonus.FolderID.kSendsFolder, PreSonus.ParamID.kSendMute, slotIndex);
}
const kTrackModeParams = [
    new ParamDescriptor("BypAll", PreSonus.ParamID.kInsertBypass),
    new ParamDescriptor("Monitr", PreSonus.ParamID.kMonitor),
    new ParamDescriptor("Input", PreSonus.ParamID.kRecordPort, PreSonus.ParamID.kPortAssignmentIn),
    new ParamDescriptor("Output", PreSonus.ParamID.kOutputPort, PreSonus.ParamID.kPortAssignmentOut),
    new ParamDescriptor("S1Byp", getSendMuteParamID(0)),
    new ParamDescriptor("S2Byp", getSendMuteParamID(1)),
    new ParamDescriptor("S3Byp", getSendMuteParamID(2)),
    new ParamDescriptor("S4Byp", getSendMuteParamID(3))
];
function getInsertBypassParamID(slotIndex) {
    return PreSonus.getChannelFolderParamID(PreSonus.FolderID.kInsertsFolder, PreSonus.ParamID.kBypass, slotIndex);
}
const kFXModeParams = [
    new ParamDescriptor("FX1Byp", getInsertBypassParamID(0)),
    new ParamDescriptor("FX2Byp", getInsertBypassParamID(1)),
    new ParamDescriptor("FX3Byp", getInsertBypassParamID(2)),
    new ParamDescriptor("FX4Byp", getInsertBypassParamID(3)),
    new ParamDescriptor("FX5Byp", getInsertBypassParamID(4)),
    new ParamDescriptor("FX6Byp", getInsertBypassParamID(5)),
    new ParamDescriptor("FX7Byp", getInsertBypassParamID(6)),
    new ParamDescriptor("FX8Byp", getInsertBypassParamID(7))
];
var ChannelAssignmentMode;
(function (ChannelAssignmentMode) {
    ChannelAssignmentMode[ChannelAssignmentMode["kTrackMode"] = 0] = "kTrackMode";
    ChannelAssignmentMode[ChannelAssignmentMode["kSendMode"] = 1] = "kSendMode";
    ChannelAssignmentMode[ChannelAssignmentMode["kPanMode"] = 2] = "kPanMode";
    ChannelAssignmentMode[ChannelAssignmentMode["kUser1Mode"] = 3] = "kUser1Mode";
    ChannelAssignmentMode[ChannelAssignmentMode["kUser2Mode"] = 4] = "kUser2Mode";
    ChannelAssignmentMode[ChannelAssignmentMode["kUser3Mode"] = 5] = "kUser3Mode";
    ChannelAssignmentMode[ChannelAssignmentMode["kUser4Mode"] = 6] = "kUser4Mode";
    ChannelAssignmentMode[ChannelAssignmentMode["kUser5Mode"] = 7] = "kUser5Mode";
    ChannelAssignmentMode[ChannelAssignmentMode["kUser6Mode"] = 8] = "kUser6Mode";
    ChannelAssignmentMode[ChannelAssignmentMode["kPanFocusMode"] = 9] = "kPanFocusMode";
    ChannelAssignmentMode[ChannelAssignmentMode["kLastMode"] = 10] = "kLastMode";
})(ChannelAssignmentMode || (ChannelAssignmentMode = {}));
class Assignment {
    constructor() {
        this.mode = ChannelAssignmentMode.kPanMode;
        this.sendIndex = kSendSlotAll;
        this.flipActive = false;
        this.nameValueMode = 0;
    }
    sync(other) {
        this.mode = other.mode;
        this.sendIndex = other.sendIndex;
        this.flipActive = other.flipActive;
        this.nameValueMode = other.nameValueMode;
    }
    getModeString() {
        switch (this.mode) {
            case ChannelAssignmentMode.kTrackMode:
                return "TR";
            case ChannelAssignmentMode.kSendMode:
                return this.sendIndex == kSendSlotAll ? "SE" : "S" + this.sendIndex;
            case ChannelAssignmentMode.kPanMode:
                return "PN";
            case ChannelAssignmentMode.kPanFocusMode:
                return "PX";
            case ChannelAssignmentMode.kUser1Mode:
                return "U1";
            case ChannelAssignmentMode.kUser2Mode:
                return "U2";
            case ChannelAssignmentMode.kUser3Mode:
                return "U3";
            case ChannelAssignmentMode.kUser4Mode:
                return "U4";
            case ChannelAssignmentMode.kUser5Mode:
                return "U5";
            case ChannelAssignmentMode.kUser6Mode:
                return "U6";
            default:
                break;
        }
        return "";
    }
    navigateSends(maxSlotCount) {
        if (this.mode == ChannelAssignmentMode.kSendMode) {
            this.sendIndex++;
            if (this.sendIndex >= kSendSlotFirst + kNumChannels ||
                this.sendIndex >= kSendSlotFirst + maxSlotCount)
                this.sendIndex = kSendSlotAll;
        }
        else {
            this.mode = ChannelAssignmentMode.kSendMode;
            this.sendIndex = kSendSlotAll;
        }
    }
    isSendVisible(sendIndex) {
        return this.mode == ChannelAssignmentMode.kSendMode && this.sendIndex == sendIndex;
    }
    isPanMode() {
        return this.mode == ChannelAssignmentMode.kPanMode ||
            this.mode == ChannelAssignmentMode.kPanFocusMode;
    }
    isUserMode() {
        return this.mode == ChannelAssignmentMode.kUser1Mode ||
               this.mode == ChannelAssignmentMode.kUser2Mode ||
               this.mode == ChannelAssignmentMode.kUser3Mode ||
               this.mode == ChannelAssignmentMode.kUser4Mode ||
               this.mode == ChannelAssignmentMode.kUser5Mode ||
               this.mode == ChannelAssignmentMode.kUser6Mode ;
    }
}

class ChannelInfo {
    setLabel(element, paramName) {
        return element.connectAliasParam(this.labelString, paramName);
    }
    setDesc(element, paramName) {
        return element.connectAliasParam(this.descString, paramName);
    }
    setUser(element, paramName) {
        return element.connectAliasParam(this.userString, paramName);
    }
    setLabelDirect(param) {
        this.labelString.setOriginal(param);
    }
    setConstantLabel(text) {
        this.constantString.string = text;
        this.labelString.setOriginal(this.constantString);
        return true;
    }
    setConstantDesc(text) {
        this.constantString.string = text;
        this.descString.setOriginal(this.constantString);
        return true;
    }
    setConstantUser(text) {
        this.constantString.string = text;
        this.userString.setOriginal(this.constantString);
        return true;
    }
    setFader(element, paramName) {
        // Host.Console.writeLine(" faderValue.default: " + this.faderValue._default);
        return element.connectAliasParam(this.faderValue, paramName);
    }
    clearFader() {
        this.faderValue.setOriginal(null);
    }
    setValue(element, paramName) {
        return element.connectAliasParam(this.valueString, paramName);
    }
    setButton(element, paramName) {
        return element.connectAliasParam(this.buttonValue, paramName);
    }
    clearDisplay() {
        this.labelString.setOriginal(null);
        this.valueString.setOriginal(null);
        this.descString.setOriginal(null);
        this.userString.setOriginal(null);
    }
}
ChannelInfo.kInvalidChannelIndex = -1;

class LoupedeckSharedComponent extends FocusChannelPanComponent {
    onInit(hostComponent) {
        super.onInit(hostComponent);
        this.channelLabel = "";
        this.assignment = new Assignment;
        this.channelBankElement = this.mixerMapping.find("ChannelBankElement");
        this.focusSendsBankElement = this.focusChannelElement.find("SendsBankElement");
        let paramList = hostComponent.paramList;

        this.channels = [];
        for (let i = 0; i < kNumChannels; i++) {
            let channelInfo = new ChannelInfo;
            channelInfo.faderValue = paramList.addAlias("faderValue" + i);
            channelInfo.buttonValue = paramList.addAlias("buttonValue" + i);
            channelInfo.labelString = paramList.addAlias("labelString" + i);
            channelInfo.valueString = paramList.addAlias("valueString" + i);
            channelInfo.descString = paramList.addAlias("descString" + i);
            channelInfo.userString = paramList.addAlias("userString" + i);
            channelInfo.constantString = paramList.addString("constantString" + i);
            channelInfo.channelElement = this.channelBankElement.getElement(i);
            channelInfo.sendsBankElement = channelInfo.channelElement.find("SendsBankElement");
            // Note: channelInfo.plugControlElement is set according to the user button bank selection in paramChanged().
            channelInfo.setLabel(channelInfo.channelElement, PreSonus.ParamID.kLabel);
            this.channels.push(channelInfo);
        }
    }
    onConnectChannel(channelIndex) {
        this.updateChannel(channelIndex);
}
    onConnectChannelInsert(channelIndex, insertIndex) {
    }
    onConnectChannelSend(channelIndex, sendIndex) {
        if (this.assignment.isSendVisible(sendIndex + 1))
            this.updateChannel(channelIndex);
    }
    onConnectFocusChannel() {
        let channelIndex = this.getFocusChannelIndex();
        if (channelIndex != ChannelInfo.kInvalidChannelIndex) {
            let stripCount = this.root.getPlacementGroupSize();
           if (stripCount == 0)
                stripCount = kNumChannels;
            let position = Math.floor(channelIndex / stripCount) * stripCount;
            // Host.Console.writeLine("channelIndex: "+channelIndex+", stripCount: "+stripCount+", position: "+position);
            this.channelBankElement.scrollTo(position);
        }
        super.onConnectFocusChannel();
        if (this.assignment.mode == ChannelAssignmentMode.kTrackMode || this.assignment.mode == ChannelAssignmentMode.kFXMode) {
            this.updateAll();
        }
    }
    onConnectFocusChannelInsert(insertIndex) {
        if (this.assignment.mode == ChannelAssignmentMode.kFXMode)
            this.updateChannel(insertIndex);
    }
    onConnectFocusChannelSend(sendIndex) {
        if (this.assignment.isSendVisible(kSendSlotAll))
            this.updateChannel(sendIndex);
        else if (this.assignment.mode == ChannelAssignmentMode.kTrackMode)
            this.updateAll();
    }
    onConnectPlugControl(index) {
        if (this.assignment.isUserMode()) {
            if (index == kNumChannels - 1) {
                // Determine maximum number of used user banks when the last channel of new bank
                // is connected. There must be a better way to do this but at least it comes
                // up with the correct number eventually. It actually counts up from zero until
                // the maximum number is reached, so something dynamic is going on in the background.
                // Note that dynamics don't seem to improve much between triggering on the first and
                // the last channel.
                // However, all of this seems to happen fast enough for only one MIDI controller message
                // to be sent.
                
                // Host.Console.writeLine("onConnectPlugControl(" + index + ") assignment.mode: " + this.assignment.mode);
                let ubCount = kNumUserBanks;
                let genericMappingElement = this.root.getGenericMapping();
                for (let bank = 0; bank < kNumUserBanks; bank++) {
                    let i = 0;
                    for (i = 0; i < kNumChannels; i++) {
                        // Host.Console.writeLine("vpot["+bank+"]["+i+"].getParamCount: " + genericMappingElement.getElement(0).find("vpot[" + bank + "][" + i + "]").getParamCount());
                        if (genericMappingElement.getElement(0).find("vpot[" + bank + "][" + i + "]").isConnected()) break;
                        if (genericMappingElement.getElement(0).find("vbut[" + bank + "][" + i + "]").isConnected()) break;
                    }
                    if (i == kNumChannels)
                    {
                        ubCount = bank;
                        break;
                    }
                }
                if (this.userBanksActive != ubCount) {
                    this.userBanksActive = ubCount;
                    // Host.Console.writeLine("userBanksActive: " + this.userBanksActive);

                    // This triggers a MIDI controller message to the Loupedeck.
                    this.userPagesActive.value = this.userBanksActive;
                }
            }

            this.updateChannel(index);
        }
    }
    getFocusChannelIndex() {
        if (!this.focusChannelElement.isConnected())
            return ChannelInfo.kInvalidChannelIndex;
        return this.channelBankElement.getBankChildIndex(this.focusChannelElement);
    }
    updateAll() {
        for (let i = 0; i < kNumChannels; i++)
            this.updateChannel(i);
    }
    updateChannel(index) {
        let channelInfo = this.channels[index];
        let channelElement = channelInfo.channelElement;
        let flipped = this.assignment.flipActive;
        let mode = this.assignment.mode;

        if (this.assignment.isUserMode())  {
            // Host.Console.writeLine("updateChannel UserMode");
            let plugControlElement = channelInfo.plugControlElement;
            let plugButtonElement = channelInfo.plugButtonElement;
            
            channelInfo.setLabel(plugControlElement, PreSonus.ParamID.kTitle);
            channelInfo.setValue(plugControlElement, PreSonus.ParamID.kValue);
            channelInfo.setUser(plugButtonElement, PreSonus.ParamID.kTitle);
            if (index == 0) {
                channelInfo.setDesc(this.focusChannelElement.getElement(), PreSonus.ParamID.kLabel);
            } else {
                channelInfo.setConstantDesc("");
            }
            channelInfo.setFader(plugControlElement, PreSonus.ParamID.kValue);
            channelInfo.setButton(plugButtonElement, PreSonus.ParamID.kValue);
        }
        else if (mode == ChannelAssignmentMode.kSendMode) {
            let sendElement = null;
            if (this.assignment.sendIndex == kSendSlotAll) {
                sendElement = this.focusSendsBankElement.getElement(index);
                if (index == 0) {
                    channelInfo.setDesc(this.focusChannelElement.getElement(), PreSonus.ParamID.kLabel);
                } else {
                    channelInfo.setConstantDesc("");
                }
            } else {
                sendElement = channelInfo.sendsBankElement.getElement(this.assignment.sendIndex - 1);
                channelInfo.setDesc(channelElement, PreSonus.ParamID.kLabel);
            }
            channelInfo.setLabel(sendElement, PreSonus.ParamID.kSendPort);
            channelInfo.setValue(sendElement, PreSonus.ParamID.kSendLevel);

            channelInfo.setFader(sendElement, PreSonus.ParamID.kSendLevel);
        }
        else if (mode == ChannelAssignmentMode.kTrackMode || mode == ChannelAssignmentMode.kFXMode) {
            let descriptor = mode == ChannelAssignmentMode.kTrackMode ? kTrackModeParams[index] : kFXModeParams[index];
            channelInfo.setConstantLabel(descriptor.label);
            if (!channelInfo.setValue(this.focusChannelElement, descriptor.name) && descriptor.altname.length > 0)
                channelInfo.setValue(this.focusChannelElement, descriptor.altname);
            channelInfo.setFader(channelElement, PreSonus.ParamID.kVolume);
        }
        else if (mode == ChannelAssignmentMode.kPanMode) {
            channelInfo.setLabel(channelElement, PreSonus.ParamID.kLabel);
            channelInfo.setValue(channelElement, flipped ? PreSonus.ParamID.kPan : PreSonus.ParamID.kVolume);
            channelInfo.setFader(channelElement, flipped ? PreSonus.ParamID.kPan : PreSonus.ParamID.kVolume);
        }
        else if (mode == ChannelAssignmentMode.kPanFocusMode) {
            this.updateChannelForPanFocusMode(channelInfo, index, flipped);
        }
    }
    updateChannelForPanFocusMode(channel, channelIndex, flipped) {
        let pannerType = this.getPanActiveType();
        let lastChannelIndex = kNumChannels - 1;
        channel.clearDisplay();
        channel.clearFader();
        if (pannerType == PreSonus.AudioPannerType.kPanTypeSimple) {
            if (channelIndex == 0)
                this.updateChannelForPanFocusAssigned(channel, PreSonus.ParamID.kPanStereoBalance, this.getPanParamTitle(channelIndex), flipped);
            else if (channelIndex == lastChannelIndex)
                this.updateChannelForPanFocusInfo(channel, flipped);
            else
                this.updateChannelForPanFocusUnassigned(channel, flipped);
        }
        else if (pannerType == PreSonus.AudioPannerType.kPanTypeDual) {
            if (channelIndex == 0)
                this.updateChannelForPanFocusAssigned(channel, PreSonus.ParamID.kPanDualLeft, this.getPanParamTitle(channelIndex), flipped);
            else if (channelIndex == 1)
                this.updateChannelForPanFocusAssigned(channel, PreSonus.ParamID.kPanDualRight, this.getPanParamTitle(channelIndex), flipped);
            else if (channelIndex == lastChannelIndex)
                this.updateChannelForPanFocusInfo(channel, flipped);
            else
                this.updateChannelForPanFocusUnassigned(channel, flipped);
        }
        else if (pannerType == PreSonus.AudioPannerType.kPanTypeBinaural) {
            if (channelIndex == 0)
                this.updateChannelForPanFocusAssigned(channel, PreSonus.ParamID.kPanBinauralBalance, this.getPanParamTitle(channelIndex), flipped);
            else if (channelIndex == 1)
                this.updateChannelForPanFocusAssigned(channel, PreSonus.ParamID.kPanBinauralWidth, this.getPanParamTitle(channelIndex), flipped);
            else if (channelIndex == lastChannelIndex)
                this.updateChannelForPanFocusInfo(channel, flipped);
            else
                this.updateChannelForPanFocusUnassigned(channel, flipped);
        }
        else {
            if (channelIndex == lastChannelIndex)
                this.updateChannelForPanFocusInfo(channel, flipped);
        }
    }
    updateChannelForPanFocusAssigned(channel, valueParamID, titleParam, flipped) {
        channel.setLabelDirect(titleParam);
        channel.setValue(this.focusChannelElement, valueParamID);
        if (flipped) {
            channel.setFader(this.focusChannelElement, valueParamID);
        }
        else {
            channel.setFader(channel.channelElement, PreSonus.ParamID.kVolume);
        }
    }
    ;
    updateChannelForPanFocusUnassigned(channel, flipped) {
        channel.setFader(channel.channelElement, PreSonus.ParamID.kVolume);
    }
    updateChannelForPanFocusInfo(channel, flipped) {
        channel.setLabel(this.focusChannelElement, PreSonus.ParamID.kLabel);
        channel.setValue(this.focusChannelElement, PreSonus.ParamID.kPanType);
        if (flipped) {
            channel.setFader(this.focusChannelElement, PreSonus.ParamID.kPanStereoMode);
        }
        else {
            channel.setFader(channel.channelElement, PreSonus.ParamID.kVolume);
        }
    }
    getDeviceSyncID() {
        return "LoupedeckCT";
    }
    getSyncData() {
        return this.assignment;
    }
    onSyncDevice(otherData) {
        this.assignment.sync(otherData);
        this.updateAll();
    }
    paramChanged(param) {
        // Host.Console.writeLine("LoupedeckSharedComponent.paramChanged");
    }
    updatePanModeStatus() {
        if (this.assignment.mode == ChannelAssignmentMode.kPanFocusMode)
            this.updateChannel(kNumChannels - 1);
    }
    updatePanModeControls() {
        if (this.assignment.mode == ChannelAssignmentMode.kPanFocusMode)
            this.updateAll();
    }
}
;
