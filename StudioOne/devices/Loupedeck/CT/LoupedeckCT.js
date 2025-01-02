include_file("resource://com.presonus.musicdevices/sdk/controlsurfacecomponent.js");
include_file("LoupedeckShared.js");
const kLoupedeckCTMixerBanks = [
    PreSonus.MixerConsoleBankID.kRemoteAll,
    PreSonus.MixerConsoleBankID.kAudioInput,
    PreSonus.MixerConsoleBankID.kAudioTrack,
    PreSonus.MixerConsoleBankID.kAudioSynth,
    PreSonus.MixerConsoleBankID.kAudioFX,
    PreSonus.MixerConsoleBankID.kAudioBus,
    PreSonus.MixerConsoleBankID.kAudioOutput,
    PreSonus.MixerConsoleBankID.kAudioVCA,
    PreSonus.MixerConsoleBankID.kUser
];

function getAllPropertyNames(vObject) {
    var vPropertyNames = [];
    do {
        vPropertyNames = vPropertyNames.concat(Object.getOwnPropertyNames(vObject))
    } while (vObject = Object.getPrototypeOf(vObject));;
    var vPrintPropertyNames = vPropertyNames.join("\r\n");
    Host.GUI.alert(String(vPrintPropertyNames))
}

class LoupedeckCTComponent extends LoupedeckSharedComponent {
    onInit(hostComponent) {
        // Host.Console.writeLine("Connecting Loupedeck CT...");
        super.onInit(hostComponent);
        this.activeUserPagesCounter = 0;
        let paramList = hostComponent.paramList;
        this.commandControlValue = paramList.addInteger(0, 33, "commandControlValue");
        this.commandControlValue.setValue(1);
        this.assignMode = paramList.addInteger(0, ChannelAssignmentMode.kLastMode, "assignMode");
        this.assignString = paramList.addString("assignString");
        this.sendMode = paramList.addParam("sendMode");
        this.flipMode = paramList.addParam("flipMode");
        this.panModeLED = paramList.addParam("panModeLED");
        this.panSelectedValue = paramList.addAlias("panSelectedValue");
        this.activeUserPagesParam = paramList.addInteger(0, 127, "activeUserPages");
        this.updateModeParams();
        
        this.bankList = paramList.addList("bankList");
        for (let i in kLoupedeckCTMixerBanks)
            this.bankList.appendString(kLoupedeckCTMixerBanks[i]);
        this.bankList.value = kLoupedeckCTMixerBanks.indexOf(PreSonus.MixerConsoleBankID.kRemoteAll);
    }
    onTrackFxButtonPressed(value) {
        if (!value) return;
        // getAllPropertyNames(Host.Objects.getObjectByUrl("://hostapp/DocumentManager/ActiveDocument/Environment/MixerConsole"));
    }

    // This switches between mixer pan mode and pan focus mode.
    // Pan focus mode does not seem particularly useful so it's disabled.
    //onPanButtonPressed(value) {
    //    if (!value) return;
    //    let currentMode = this.assignment.mode;
    //    if (currentMode == ChannelAssignmentMode.kPanMode) {
    //        this.assignMode.setValue(ChannelAssignmentMode.kPanFocusMode, true);
    //          return;
    //    }
    //    else
    //    this.assignMode.setValue(ChannelAssignmentMode.kPanMode, true);
    // }

    // These functions implement cursor control via stepping in snap increments which
    // automatically adapt to zoom level.
    onSnapStepFwd(value) {
        if (!value) return;
        Host.GUI.Commands.deferCommand("Edit", "Create Range from Cursor");               
        Host.GUI.Commands.deferCommand("Transport", "Locate Selection End");
        Host.GUI.Commands.deferCommand("Edit", "Deselect All");
    }
    onSnapStepRev(value) {
        if (!value) return;
        Host.GUI.Commands.deferCommand("Edit", "Create Range from Cursor");               
        Host.GUI.Commands.deferCommand("Edit", "Move Range Back");
        Host.GUI.Commands.deferCommand("Transport", "Locate Selection");
        Host.GUI.Commands.deferCommand("Edit", "Deselect All");
    }
    // Split across all tracks at cursor and select all events from split point to end of song
    onSplitAllAndSelectToEnd(value) {
        Host.GUI.Commands.deferCommand("Edit", "Select All");
        Host.GUI.Commands.deferCommand("Edit", "Split at Cursor");
        Host.GUI.Commands.deferCommand("Edit", "Select from Event to End");
    }
    updateModeParams() {
        this.assignMode.setValue(this.assignment.mode, true);
        this.assignString.string = this.assignment.getModeString();
        this.flipMode.value = this.assignment.flipActive;
        this.panModeLED.value = this.assignment.isPanMode();
    }

    getMaxSendSlotCount() {
        if (this.mixerMapping.component)
            return this.mixerMapping.invokeChildMethod("audioMixer", "getMaxSlotCount", PreSonus.FolderID.kSendsFolder);
        return kNumChannels;
    }
    onSyncDevice(otherData) {
        super.onSyncDevice(otherData);
        this.updateModeParams();
    }
    paramChanged(param) {
        // Host.Console.writeLine("LoupedeckCTComponent.paramChanged commandControlValue: " + this.commandControlValue.value);
        // Host.Console.writeLine("LoupedeckCTComponent.paramChanged");
        if (param == this.sendMode) {
            this.assignment.navigateSends(this.getMaxSendSlotCount());
            this.updateModeParams();
            this.updateAll();
            this.signalSyncDevice();
        }
        else if (param == this.assignMode) {
            this.assignment.mode = this.assignMode.value;

            this.updateModeParams();
            // Host.Console.writeLine("LoupedeckCTComponent.paramChanged " + this.assignMode.value);

            let mode = this.assignment.mode;
            let userBank = -1;
    
            if (mode == ChannelAssignmentMode.kUser1Mode) userBank = 0;
            else if (mode == ChannelAssignmentMode.kUser2Mode) userBank = 1;
            else if (mode == ChannelAssignmentMode.kUser3Mode) userBank = 2;
            else if (mode == ChannelAssignmentMode.kUser4Mode) userBank = 3;
            else if (mode == ChannelAssignmentMode.kUser5Mode) userBank = 4;
            else if (mode == ChannelAssignmentMode.kUser6Mode) userBank = 5;
    
            if (userBank >= 0) {
                // Host.Console.writeLine("paramChanged vpot[" + userBank + "]");
                let genericMappingElement = this.root.getGenericMapping();
                for (let i = 0; i < kNumChannels; i++) {
                    this.channels[i].plugControlElement = genericMappingElement.getElement(0).find("vpot[" + userBank + "][" + i + "]");
                    this.channels[i].plugButtonElement  = genericMappingElement.getElement(0).find("vbut[" + userBank + "][" + i + "]");
                    // getAllPropertyNames(this.channels[i].plugControlElement);
                }
            }

            this.updateAll();
            this.signalSyncDevice();
        }
        else if (param == this.flipMode) {
            this.assignment.flipActive = this.flipMode.value;
            this.updateAll();
            this.signalSyncDevice();
        }
        else if (param == this.bankList) {
            this.channelBankElement.selectBank(this.bankList.string);
        }
    }
}
function createLoupedeckCTInstance() {
    return new LoupedeckCTComponent();
}
