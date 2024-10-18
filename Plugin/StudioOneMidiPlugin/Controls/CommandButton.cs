namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using Melanchall.DryWetMidi.Core;

    class CommandButton : StudioOneButton<CommandButtonData>
	{
		public CommandButton()
		{
            this.AddButton(new CommandButtonData(0x5E, 0x5D, "Play", "play"), "Transport");   // 1st click - play, 2nd click - stop
            this.AddButton(new CommandButtonData(0x5D, "Stop", "stop"), "Transport");
            this.AddButton(new CommandButtonData(0x5F, "Record", "record"), "Transport");
            this.AddButton(new CommandButtonData(0x5C, "Fast forward", "fast_forward"), "Transport");
            this.AddButton(new CommandButtonData(0x5B, "Rewind", "rewind"), "Transport");
            this.AddButton(new CommandButtonData(0x55, "Loop", "loop"), "Transport");
            this.AddButton(new CommandButtonData(0x59, "Click", "click"), "Settings");
            this.AddButton(new CommandButtonData(0x57, "Preroll", "preroll"), "Transport");
            this.AddButton(new CommandButtonData(0x58, "Autopunch", "autopunch"), "Transport");
            this.AddButton(new CommandButtonData(0x56, "Precount", "precount"), "Transport");
            this.AddButton(new SnapStepCommandButtonData(SnapStepCommandButtonData.StepDir.StepFwd), "Transport");
            this.AddButton(new SnapStepCommandButtonData(SnapStepCommandButtonData.StepDir.StepRev), "Transport");
            this.AddButton(new OneWayCommandButtonData(14, 0x00, "Mix"), "View");
            this.AddButton(new OneWayCommandButtonData(14, 0x01, "Browse", "browser"), "View");
            this.AddButton(new OneWayCommandButtonData(14, 0x02, "Edit"), "View");
            this.AddButton(new OneWayCommandButtonData(14, 0x03, "Fullscreen", "fullscreen"), "View");
            this.AddButton(new OneWayCommandButtonData(14, 0x04, "Inspector", "inspector"), "View");
            this.AddButton(new OneWayCommandButtonData(14, 0x05, "Record Panel", "rec_panel"), "View");
            this.AddButton(new OneWayCommandButtonData(14, 0x06, "Track List", "track_list"), "View");
            this.AddButton(new OneWayCommandButtonData(14, 0x07, "Previous Perspective"), "View");
            this.AddButton(new OneWayCommandButtonData(14, 0x08, "Next Perspective"), "View");
            this.AddButton(new OneWayCommandButtonData(14, 0x09, "Show Groups", "show_groups"), "View");
            this.AddButton(new OneWayCommandButtonData(14, 0x0A, "Floating Window"), "View");
            this.AddButton(new OneWayCommandButtonData(14, 0x0B, "Time Display", "time_display"), "View");
            this.AddButton(new OneWayCommandButtonData(14, 0x0C, "Fit Timeline"), "View");
            this.AddButton(new OneWayCommandButtonData(14, 0x0D, "Reset Window Positions", "reset_window_positions"), "View");
            this.AddButton(new OneWayCommandButtonData(14, 0x10, "Show Inputs", "show_inputs"), "Console");
            this.AddButton(new OneWayCommandButtonData(14, 0x11, "Show Track"), "Console");
            this.AddButton(new OneWayCommandButtonData(14, 0x12, "Channel Editor", "channel_editor"), "Console");
            this.AddButton(new OneWayCommandButtonData(14, 0x13, "Instrument Editor"), "Console");
            this.AddButton(new OneWayCommandButtonData(14, 0x14, "Open Channel"), "Console");
            this.AddButton(new OneWayCommandButtonData(14, 0x15, "Add Insert", "add_insert"), "Console");
            this.AddButton(new OneWayCommandButtonData(14, 0x16, "Add Send", "add_send"), "Console");
            this.AddButton(new OneWayCommandButtonData(14, 0x17, "Add Bus Channel", "add_bus"), "Console");
            this.AddButton(new OneWayCommandButtonData(14, 0x18, "Add FX Channel", "add_fx"), "Console");
            this.AddButton(new OneWayCommandButtonData(14, 0x19, "Global Mute"), "Console");
            this.AddButton(new OneWayCommandButtonData(14, 0x1A, "Global Solo"), "Console");
            this.AddButton(new OneWayCommandButtonData(14, 0x1B, "Next Channel", "channel_next"), "Console");
            this.AddButton(new OneWayCommandButtonData(14, 0x1C, "Previous Channel", "channel_prev"), "Console");
            this.AddButton(new OneWayCommandButtonData(14, 0x1D, "Toggle Height", "console_height"), "Console");
            this.AddButton(new OneWayCommandButtonData(14, 0x1E, "Toggle Width", "console_width"), "Console");
            this.AddButton(new OneWayCommandButtonData(14, 0x1F, "Show Outputs", "show_outputs"), "Console");
            this.AddButton(new OneWayCommandButtonData(14, 0x20, "Suspend Group"), "Group");
            this.AddButton(new OneWayCommandButtonData(14, 0x21, "Suspend All Groups", "groups_suspend"), "Group");
            this.AddButton(new GroupSuspendButtonData(1), "Group");
            this.AddButton(new GroupSuspendButtonData(2), "Group");
            this.AddButton(new GroupSuspendButtonData(3), "Group");
            this.AddButton(new GroupSuspendButtonData(4), "Group");
            this.AddButton(new GroupSuspendButtonData(5), "Group");
            this.AddButton(new GroupSuspendButtonData(6), "Group");
            this.AddButton(new GroupSuspendButtonData(7), "Group");
            this.AddButton(new GroupSuspendButtonData(8), "Group");
            this.AddButton(new OneWayCommandButtonData(14, 0x30, "Previous Layer", "layer_up"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x31, "Next Layer", "layer_dn"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x32, "Add Layer", "layer_add"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x33, "Remove Layer", "layer_remove"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x34, "Expand Layers", "layers_expand"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x35, "Rename Layer", "layer_rename"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x36, "Group Selected Tracks", "group_new"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x37, "Dissolve Group", "group_dissolve"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x38, "Show Automation", "show_automation"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x39, "Show in Console"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x3A, "Expand Folder Track", "folder_expand"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x3B, "Collapse All Tracks", "tracks_collapse"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x3C, "Add Track"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x3D, "Add Automation Track", "add_automation"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x3E, "Add Audio (Mono)", "add_audio_mono"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x3F, "Add Audio (Stereo)", "add_audio_stereo"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x40, "Add Audio (Surround)", "add_audio_surround"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x41, "Add Instrument", "add_instrument"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x42, "Add Folder", "add_folder"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x43, "Add Bus for Selected Channels", "add_bus"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x44, "Arm All Audio Tracks"), "Track");
            this.AddButton(new OneWayCommandButtonData(14, 0x50, "Return to Zero", "return_to_zero"), "Transport");
            this.AddButton(new OneWayCommandButtonData(14, 0x51, "Return to Start on Stop"), "Transport");
            this.AddButton(new OneWayCommandButtonData(14, 0x52, "Enable Play Start Marker"), "Transport");
            this.AddButton(new OneWayCommandButtonData(14, 0x53, "Set Play Start Marker", "playmarker"), "Transport");
            this.AddButton(new OneWayCommandButtonData(14, 0x54, "Loop Selection", "loop_selection"), "Transport");
            this.AddButton(new OneWayCommandButtonData(14, 0x55, "Loop Selection Snapped", "loop_selection_snapped"), "Transport");
            this.AddButton(new OneWayCommandButtonData(14, 0x56, "Play from Loop Start", "play_loop"), "Transport");
            this.AddButton(new OneWayCommandButtonData(14, 0x57, "Loop Follows Selection", "loop_follows_selection"), "Transport");
            this.AddButton(new OneWayCommandButtonData(14, 0x58, "Tap Tempo", "tempo_tap"), "Transport");
            this.AddButton(new OneWayCommandButtonData(14, 0x59, "Set Loop Start", "loop_set_start"), "Transport");
            this.AddButton(new OneWayCommandButtonData(14, 0x5A, "Set Loop End", "loop_set_end"), "Transport");
            this.AddButton(new OneWayCommandButtonData(14, 0x5B, "Goto Loop Start", "loop_goto_start"), "Transport");
            this.AddButton(new OneWayCommandButtonData(14, 0x5C, "Goto Loop End", "loop_goto_end"), "Transport");
            this.AddButton(new OneWayCommandButtonData(14, 0x5D, "Locate Selection", "locate_selection"), "Transport");
            this.AddButton(new OneWayCommandButtonData(14, 0x5E, "Forward Bar", "forward_bar"), "Transport");
            this.AddButton(new OneWayCommandButtonData(14, 0x5F, "Rewind Bar", "rewind_bar"), "Transport");
            this.AddButton(new OneWayCommandButtonData(14, 0x60, "Delete Marker", "marker_delete"), "Marker");
            this.AddButton(new OneWayCommandButtonData(14, 0x61, "Insert Marker", "marker_insert"), "Marker");
            this.AddButton(new OneWayCommandButtonData(14, 0x62, "Insert Named Marker", "marker_insert_named"), "Marker");
            this.AddButton(new OneWayCommandButtonData(14, 0x63, "Previous Marker", "marker_previous"), "Marker");
            this.AddButton(new OneWayCommandButtonData(14, 0x64, "Next Marker", "marker_next"), "Marker");
            this.AddButton(new OneWayCommandButtonData(14, 0x65, "Recall Marker", "marker_goto", MarkerGotoButtonData.BgColor), "Marker");
            this.AddButton(new MarkerGotoButtonData(1), "Marker");
            this.AddButton(new MarkerGotoButtonData(2), "Marker");
            this.AddButton(new MarkerGotoButtonData(3), "Marker");
            this.AddButton(new MarkerGotoButtonData(4), "Marker");
            this.AddButton(new MarkerGotoButtonData(5), "Marker");
            this.AddButton(new MarkerGotoButtonData(6), "Marker");
            this.AddButton(new MarkerGotoButtonData(7), "Marker");
            this.AddButton(new MarkerGotoButtonData(8), "Marker");
            this.AddButton(new OneWayCommandButtonData(14, 0x70, "Previous Track"), "Navigation");
            this.AddButton(new OneWayCommandButtonData(14, 0x71, "Next Track"), "Navigation");
            this.AddButton(new OneWayCommandButtonData(14, 0x72, "Previous Event"), "Navigation");
            this.AddButton(new OneWayCommandButtonData(14, 0x73, "Next Event"), "Navigation");
            this.AddButton(new OneWayCommandButtonData(14, 0x74, "Previous Plugin", "plugin_prev"), "Navigation");
            this.AddButton(new OneWayCommandButtonData(14, 0x75, "Next Plugin", "plugin_next"), "Navigation");
            this.AddButton(new OneWayCommandButtonData(14, 0x7B, "Browse Instruments", "browser_instruments"), "Browser");
            this.AddButton(new OneWayCommandButtonData(14, 0x7C, "Browse Effects", "browser_effects"), "Browser");
            this.AddButton(new OneWayCommandButtonData(14, 0x7D, "Browse Loops", "browser_loops"), "Browser");
            this.AddButton(new OneWayCommandButtonData(14, 0x7E, "Browse Files", "browser_files"), "Browser");
            this.AddButton(new OneWayCommandButtonData(14, 0x7F, "Browse Pool", "browser_pool"), "Browser");
            this.AddButton(new OneWayCommandButtonData(15, 0x00, "Nudge Fwd"), "Edit");
            this.AddButton(new OneWayCommandButtonData(15, 0x01, "Nudge Rev"), "Edit");
            this.AddButton(new OneWayCommandButtonData(15, 0x02, "Toggle Snap", "toggle_snap"), "Edit");
            this.AddButton(new OneWayCommandButtonData(15, 0x03, "Toggle Ripple", "toggle_ripple"), "Edit");
            this.AddButton(new OneWayCommandButtonData(15, 0x04, "Arrow Tool", "tool_arrow"), "Edit");
            this.AddButton(new OneWayCommandButtonData(15, 0x05, "Range Tool", "tool_range"), "Edit");
            this.AddButton(new OneWayCommandButtonData(15, 0x06, "Split Tool", "tool_split"), "Edit");
            this.AddButton(new OneWayCommandButtonData(15, 0x07, "Erase Tool", "tool_erase"), "Edit");
            this.AddButton(new OneWayCommandButtonData(15, 0x08, "Paint Tool", "tool_paint"), "Edit");
            this.AddButton(new OneWayCommandButtonData(15, 0x09, "Mute Tool", "tool_mute"), "Edit");
            this.AddButton(new OneWayCommandButtonData(15, 0x0A, "Bend Tool", "tool_bend"), "Edit");
            this.AddButton(new OneWayCommandButtonData(15, 0x0B, "Listen Tool", "tool_listen"), "Edit");
            this.AddButton(new OneWayCommandButtonData(15, 0x10, "Zoom In", "zoom_in_h"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x11, "Zoom In Fine", "zoom_in_h"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x12, "Zoom Out", "zoom_out_h"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x13, "Zoom Out Fine", "zoom_out_h"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x14, "Zoom In Vertical", "zoom_in_v"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x15, "Zoom In Vertical Fine", "zoom_in_v"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x16, "Zoom Out Vertical", "zoom_out_v"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x17, "Zoom Out Vertical Fine", "zoom_out_v"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x18, "Zoom Undo", "zoom_undo"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x19, "Zoom Redo", "zoom_redo"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x1A, "Toggle Zoom", "zoom_toogle_noauto"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x1B, "Zoom Full", "zoom_full"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x1C, "Zoom Full Horizontally", "zoom_full_h"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x1D, "Zoom Full Vertically", "zoom_full_v"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x1F, "Zoom Loop", "zoom_loop"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x20, "Zoom Selection", "zoom_selection"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x21, "Zoom Selection Horizontally", "zoom_selection_h"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x22, "Toggle Auto Zoom", "auto_zoom_toggle"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x23, "Toggle Auto Zoom Full", "auto_zoom_toggle_full"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x24, "Toggle Auto Zoom Horizontally", "auto_zoom_toggle_h"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x25, "Toggle Auto Zoom Vertically", "auto_zoom_toggle_v"), "Zoom");
            this.AddButton(new OneWayCommandButtonData(15, 0x29, "Display Lyrics", "ruler_lyrics"), "View");
            this.AddButton(new OneWayCommandButtonData(15, 0x2A, "Marker Track", "ruler_marker"), "View");
            this.AddButton(new OneWayCommandButtonData(15, 0x2B, "Signature Track", "ruler_signature"), "View");
            this.AddButton(new OneWayCommandButtonData(15, 0x2C, "Tempo Track", "ruler_tempo"), "View");
            this.AddButton(new OneWayCommandButtonData(15, 0x2D, "Arranger Track", "ruler_arranger"), "View");
            this.AddButton(new OneWayCommandButtonData(15, 0x2E, "Video Track", "ruler_video"), "View");
            this.AddButton(new OneWayCommandButtonData(15, 0x2F, "Chord Track", "ruler_chord"), "View");
            this.AddButton(new OneWayCommandButtonData(15, 0x30, "Show Scenes (Console)", "show_scenes"), "Scenes");
            this.AddButton(new OneWayCommandButtonData(15, 0x31, "Show Scenes (Track List)", "show_scenes"), "Scenes");
            this.AddButton(new OneWayCommandButtonData(15, 0x32, "Add Scene", "scene_add"), "Scenes");
            this.AddButton(new OneWayCommandButtonData(15, 0x33, "Remove Scene", "scene_remove"), "Scenes");
            this.AddButton(new OneWayCommandButtonData(15, 0x34, "Rename Scene", "scene_rename"), "Scenes");
            this.AddButton(new OneWayCommandButtonData(15, 0x35, "Update Scene", "scene_update"), "Scenes");
            this.AddButton(new OneWayCommandButtonData(15, 0x36, "Previous Scene", "scene_next"), "Scenes");
            this.AddButton(new OneWayCommandButtonData(15, 0x37, "Next Scene", "scene_previous"), "Scenes");
            this.AddButton(new SceneSelectButtonData(1), "Scenes");
            this.AddButton(new SceneSelectButtonData(2), "Scenes");
            this.AddButton(new SceneSelectButtonData(3), "Scenes");
            this.AddButton(new SceneSelectButtonData(4), "Scenes");
            this.AddButton(new SceneSelectButtonData(5), "Scenes");
            this.AddButton(new SceneSelectButtonData(6), "Scenes");
            this.AddButton(new SceneSelectButtonData(7), "Scenes");
            this.AddButton(new SceneSelectButtonData(8), "Scenes");
            this.AddButton(new OneWayCommandButtonData(15, 0x40, "Create Ranges", "ranges_create"), "Range");
            this.AddButton(new OneWayCommandButtonData(15, 0x41, "Create Ranges on Tracks", "ranges_create_on_tracks"), "Range");
            this.AddButton(new OneWayCommandButtonData(15, 0x42, "Select Events", "range_select_events"), "Range");
            this.AddButton(new OneWayCommandButtonData(15, 0x43, "Copy Event to Track", "range_copy_event_to_track"), "Range");
            this.AddButton(new OneWayCommandButtonData(15, 0x44, "Split Range", "range_split"), "Range");
            this.AddButton(new OneWayCommandButtonData(15, 0x45, "Range Start to Cursor", "range_start_to_cursor"), "Range");
            this.AddButton(new OneWayCommandButtonData(15, 0x46, "Range End to Cursor", "range_end_to_cursor"), "Range");
        }

        protected override bool OnLoad()
		{
            base.OnLoad();

            this.plugin.CommandNoteReceived += (object sender, NoteOnEvent e) =>
            {
                var idx = $"{e.Channel}:{e.NoteNumber}";

                if (!this.buttonData.ContainsKey(idx)) return;

                var bd = this.buttonData[idx];
                bd.Activated = e.Velocity > 0;
                this.ActionImageChanged(idx);
            };

            return true;
		}

        private void AddButton(CommandButtonData bd, System.String parameterGroup = "Control")
        {
            var idx = $"{bd.midiChannel}:{bd.Code}"; 

			this.buttonData[idx] = bd;
			this.AddParameter(idx, bd.Name, parameterGroup);
		}
	}
}
