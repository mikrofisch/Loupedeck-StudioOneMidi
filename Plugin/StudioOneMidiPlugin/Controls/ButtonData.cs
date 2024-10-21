namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Security.Permissions;

    using Loupedeck.StudioOneMidiPlugin.Helpers;

    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;

    public abstract class ButtonData
    {
        public static readonly BitmapColor DefaultSelectionBgColor = new BitmapColor(60, 60, 60);

        protected StudioOneMidiPlugin Plugin;
        protected const int TrackNameH = 24;

        public virtual void OnLoad(StudioOneMidiPlugin plugin) => this.Plugin = plugin;
        public abstract BitmapImage getImage(PluginImageSize imageSize);
        public abstract void runCommand();
    }

    public class PropertyButtonData : ButtonData
    {
        public const Int32 SelectedChannel = StudioOneMidiPlugin.ChannelCount;
        public enum TrackNameMode
        {
            None,
            NoneOffset,
            ShowFull,
            ShowLeftHalf,
            ShowRightHalf
        }

        private int ChannelIndex;
        private ChannelProperty.PropertyType Type;
        private TrackNameMode ShowTrackName;
        private BitmapImage Icon;

        public PropertyButtonData(int channelIndex,
                                      ChannelProperty.PropertyType bt,
                                      TrackNameMode tm = TrackNameMode.ShowFull,
                                      string iconName = null)
        {
            this.ChannelIndex = channelIndex;
            this.Type = bt;
            this.ShowTrackName = tm;

            if (iconName != null)
            {
                this.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{iconName}_52px.png"));
            }
        }

        public void setPropertyType(ChannelProperty.PropertyType bt) => this.Type = bt;

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            ChannelData cd = this.Plugin.channelData[this.ChannelIndex.ToString()];
            //if (!this.Plugin.mackieChannelData.TryGetValue(this.ChannelIndex.ToString(), out MackieChannelData cd))
            //    return;

            // bb.FillRectangle(0, 0, bb.Width, bb.Height, new BitmapColor(20, 20, 20));
            return PropertyButtonData.drawImage(new BitmapBuilder(imageSize),
                                                    this.Type,
                                                    cd.BoolProperty[(Int32)this.Type],
                                                    this.ShowTrackName,
                                                    cd.Label,
                                                    this.Icon);
        }

        public static BitmapImage drawImage(BitmapBuilder bb,
                                            ChannelProperty.PropertyType type,
                                            Boolean isSelected,
                                            TrackNameMode showTrackName,
                                            String trackName,
                                            BitmapImage icon)
        {
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);

            if (isSelected)
            {
                bb.FillRectangle(0, 0, bb.Width, bb.Height, ChannelProperty.PropertyColor[(Int32)type]);
            }

            var yOff = showTrackName == TrackNameMode.None ? 0 : icon == null ? TrackNameH : TrackNameH - 8;

            if (icon != null)
            {
                bb.DrawImage(icon, bb.Width / 2 - icon.Width / 2, yOff + (bb.Height - yOff) / 2 - icon.Height / 2);
            }
            else
            {
                bb.DrawText(ChannelProperty.PropertyLetter[(Int32)type], 0, yOff, bb.Width, bb.Height - yOff, null, 32);
            }

            if (showTrackName != TrackNameMode.None && showTrackName != TrackNameMode.NoneOffset)
            {
                var hPos = 0;
                var width = bb.Width;

                if (showTrackName == TrackNameMode.ShowLeftHalf)
                {
                    hPos = 1;
                    width = bb.Width * 2;
                }
                else if (showTrackName == TrackNameMode.ShowRightHalf)
                {
                    hPos = -bb.Width - 1;
                    width = bb.Width * 2;
                }
                bb.DrawText(trackName, hPos, 0, width, TrackNameH);
            }

            return bb.ToImage();
        }

        public override void runCommand()
        {
            ChannelData cd = this.Plugin.channelData[this.ChannelIndex.ToString()];

            cd.EmitChannelPropertyPress(this.Type);
        }
    }

    public class PropertySelectionButtonData : ButtonData
    {
        public ChannelProperty.PropertyType TypeA, TypeB, CurrentType;
        public Boolean Activated;
        private BitmapImage IconA, IconB, IconOff;

        public PropertySelectionButtonData(ChannelProperty.PropertyType typeA,
                                           ChannelProperty.PropertyType typeB,
                                           String iconNameA,
                                           String iconNameB,
                                           String iconNameOff,
                                           Boolean activated = false)
        {
            this.TypeA = typeA;
            this.TypeB = typeB;
            this.CurrentType = typeA;

            this.IconA = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{iconNameA}_80px.png"));
            this.IconB = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{iconNameB}_80px.png"));
            this.IconOff = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{iconNameOff}_80px.png"));
            this.Activated = activated;
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);

            if (!this.Activated)
            {
                bb.DrawImage(this.IconOff, 0, 0);
            }
            else if (this.CurrentType == this.TypeA)
            {
                bb.DrawImage(this.IconA, 0, 0);
            }
            else
            {
                bb.DrawImage(this.IconB, 0, 0);
            }
            return bb.ToImage();
        }

        public override void runCommand()
        {
            if (!this.Activated)
            {
                this.Activated = true;
                this.Plugin.EmitSelectModeChanged(SelectButtonMode.Property);
            }
            else
            {
                this.CurrentType = this.CurrentType == this.TypeA ? this.TypeB : this.TypeA;
            }
            this.Plugin.EmitPropertySelectionChanged(this.CurrentType);
        }
    }

    public class SelectButtonData : ButtonData
    {
        public SelectButtonMode CurrentMode = SelectButtonMode.Property;
        public Boolean Enabled = true; 
        public Boolean UserButtonActive = false;
        public Boolean UserButtonEnabled = true;
        public Boolean UserButtonMenuActive = false;
        public static ChannelProperty.PropertyType SelectionPropertyType = ChannelProperty.PropertyType.Mute;

        public String UserLabel { get; set; }
        public String Label { get; set; }
        public static String FocusDeviceName;

        private static readonly BitmapColor CommandPropertyColor = new BitmapColor(40, 40, 40);
        public static readonly BitmapColor TextDescColor = new BitmapColor(175, 175, 175);
        public static readonly FinderColor BgColorAssigned =   new FinderColor(80, 80, 80);
        public static readonly FinderColor BgColorUnassigned = new FinderColor(40, 40, 40);
        public static readonly BitmapColor BgColorUserCircle = new BitmapColor(60, 60, 60);

        private readonly Int32 ChannelIndex = -1;
        private const Int32 TitleHeight = 24;
        private static BitmapImage IconSelMon, IconSelRec;

        public class CustomParams
        {
            public Int32 MidiChannel = 0;
            public Int32 MidiCode = 0;
            public String Label;
            public BitmapImage Icon, IconOn;
            public BitmapColor BgColor = BitmapColor.Black;
        }
        public CustomParams CurrentCustomParams { get; private set; } = new CustomParams();
        public Boolean CustomIsActivated = false;

        public static String PluginName { get; set; }

        public static readonly ColorFinder UserColorFinder = new ColorFinder(new ColorFinder.ColorSettings
        {
            OnColor = BgColorAssigned,
            OffColor = BgColorAssigned,
            TextOnColor = FinderColor.White,
            TextOffColor = FinderColor.Black
        });

        public SelectButtonData(Int32 channelIndex)
        {
            this.ChannelIndex = channelIndex;
        }

//        public void setPluginName(String text) => this.PluginName = text;

        public override void OnLoad(StudioOneMidiPlugin plugin)
        {
            base.OnLoad(plugin);
            UserColorFinder.Init(plugin);
        }

        public void SetCustomMode(SelectButtonCustomParams cp)
        {
            this.CurrentCustomParams.MidiChannel = cp.MidiChannel;
            this.CurrentCustomParams.MidiCode = cp.MidiCode;
            this.CurrentCustomParams.Label = cp.Label;
            this.CurrentCustomParams.BgColor = cp.BgColor;
            CommandButtonData.LoadIcons(cp.IconName, ref this.CurrentCustomParams.Icon, ref this.CurrentCustomParams.IconOn);

            this.CurrentMode = SelectButtonMode.Custom;
        }


        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            ChannelData cd = this.Plugin.channelData[this.ChannelIndex.ToString()];
            //if (!this.Plugin.mackieChannelData.TryGetValue(this.ChannelIndex.ToString(), out MackieChannelData cd))
            //    return;

            this.Label = cd.Label;
            this.UserLabel = cd.UserLabel;

            var bb = new BitmapBuilder(imageSize);

            return SelectButtonData.drawImage(bb, 
                                              cd,
                                              this.CurrentMode,
                                              this.CurrentMode == SelectButtonMode.Custom ? this.CustomIsActivated : this.UserButtonActive,
                                              this.UserButtonEnabled,
                                              this.UserButtonMenuActive,
                                              SelectionPropertyType,
                                              PluginName,
                                              customParams: this.CurrentCustomParams,
                                              this.Enabled);
        }

        public static BitmapImage drawImage(BitmapBuilder bb,
                                            ChannelData cd,
                                            SelectButtonMode buttonMode,
                                            Boolean userButtonActive,
                                            Boolean userButtonEnabled = true,
                                            Boolean userButtonMenuActive = false,
                                            ChannelProperty.PropertyType commandProperty = ChannelProperty.PropertyType.Select,
                                            String pluginName = "",
                                            CustomParams customParams = null,
                                            Boolean buttonEnabled = true)
        {
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);

            if (cd.Label.IsNullOrEmpty() && cd.ValueStr.IsNullOrEmpty() && cd.UserLabel.IsNullOrEmpty())
            {
                return bb.ToImage();
            }

            if (SelectButtonData.IconSelMon == null)
            {
                SelectButtonData.IconSelMon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("monitor_24px.png"));
                SelectButtonData.IconSelRec = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("record_24px.png"));
            }

            if (buttonMode == SelectButtonMode.Custom && customParams != null)
            {
                bb.FillRectangle(0, 0, bb.Width, bb.Height, customParams.BgColor);

                if (userButtonActive && customParams.IconOn != null)
                {
                    bb.DrawImage(customParams.IconOn);
                }
                else if (customParams.Icon != null)
                {
                    bb.DrawImage(customParams.Icon);
                }
                else
                {
                    bb.DrawText(customParams.Label, TextDescColor);
                }
            }
            else if (buttonMode == SelectButtonMode.Send || buttonMode == SelectButtonMode.FX)
            {
                var barHeight = 0;
                if (FocusDeviceName != null && FocusDeviceName.Contains(" - ") && cd.Label == FocusDeviceName.Substring(FocusDeviceName.IndexOf(" - ") + 3))
                {
                    barHeight = 4;
                    var barColor = new BitmapColor(80, 120, 160);
                    bb.FillRectangle(0, 0, bb.Width, bb.Height, new BitmapColor(barColor, 80));
                    bb.FillRectangle(0, 0, bb.Width, barHeight, barColor);
                    bb.FillRectangle(0, bb.Height - barHeight, bb.Width, barHeight, barColor);
                }
                // bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);
                var titleHeight = cd.Description.Length > 0 ? TitleHeight : 0;
                bb.DrawText(cd.Description, 0, barHeight + 2, bb.Width, titleHeight, TextDescColor);

                // Remove clutter from plugin name
                var typePos = -1;
                var len = cd.Label.Length;
                if (typePos < 0 && len > 16)
                {
                    typePos = cd.Label.LastIndexOf("Mono", len, 16);
                }
                if (typePos < 0 && len > 11)
                {
                    typePos = cd.Label.LastIndexOf("Stereo", len, 11);
                }
                if (typePos < 0)
                {
                    typePos = cd.Label.LastIndexOf("x64");
                }
                bb.DrawText(typePos > 0 ? cd.Label.Substring(0, typePos) : cd.Label, 0, titleHeight, bb.Width, bb.Height - titleHeight);
            }
            else if (buttonMode == SelectButtonMode.User)
            {
                UserColorFinder.CurrentChannel = cd.ChannelID + 1;

                var ubh = bb.Height / 3;
                var uby = bb.Height - ubh;

                // User Pot
                //
                if (UserColorFinder.getLabel(pluginName, cd.Label).Length > 0)
                {
                    if (UserColorFinder.getPaintLabelBg(pluginName, cd.Label))
                    {
                        bb.FillRectangle(0, 0, bb.Width, uby, buttonEnabled ? UserColorFinder.getOnColor(pluginName, cd.Label) 
                                                                            : UserColorFinder.getOffColor(pluginName, cd.Label));
                    }
                }
                bb.DrawText(cd.Description, 0, 0, bb.Width, TitleHeight, TextDescColor);
                bb.DrawText(UserColorFinder.getLabelShort(pluginName, cd.Label), 0, bb.Height / 2 - TitleHeight / 2, bb.Width, TitleHeight, 
                            UserColorFinder.getTextOnColor(pluginName, cd.Label));

                // User Button
                //
                var drawCircle = cd.UserLabel.Length > 0 && UserColorFinder.showUserButtonCircle(pluginName, cd.UserLabel);
                var tx = 0;
                var tw = bb.Width;
                if (UserColorFinder.hasMenu(pluginName, cd.UserLabel))
                {
                    userButtonActive = true;    // Use 'on' colour variants for all menu items
                }
                var tc = userButtonEnabled ? userButtonActive ? drawCircle ? UserColorFinder.getOnColor(pluginName, cd.UserLabel, isUser: true)
                                                                           : UserColorFinder.getTextOnColor(pluginName, cd.UserLabel, isUser: true)
                                                              : UserColorFinder.getTextOffColor(pluginName, cd.UserLabel, isUser: true)
                                           : BitmapColor.Black;
                if (userButtonMenuActive) tc = BitmapColor.White;
                var bc = (cd.UserLabel.Length > 0 || UserColorFinder.getLabel(pluginName, cd.UserLabel, isUser: true).Length > 0)
                                                  && userButtonEnabled ? userButtonActive ? UserColorFinder.getOnColor(pluginName, cd.UserLabel, isUser: true)
                                                                       : UserColorFinder.getOffColor(pluginName, cd.UserLabel, isUser: true)
                                                 : BgColorUnassigned;
                if (userButtonMenuActive)
                {
                    var stroke = 2;
                    bb.FillRectangle(0, uby, bb.Width, ubh, new BitmapColor(120, 120, 120));
                    bb.FillRectangle(0, uby + stroke, bb.Width, ubh - 2 * stroke - 2, new BitmapColor(40, 40, 40));
                }
                else
                {
                    bb.FillRectangle(0, uby, bb.Width, ubh, drawCircle ? BgColorUserCircle : bc);
                }
                if (drawCircle)
                {
                    var cx = ubh/2;
                    if (cd.ChannelID >= 3) cx = bb.Width - ubh / 2;
                    var cy = uby + ubh/2;
                    var cr = ubh/2 - 5;
                    if (userButtonActive) bb.FillCircle(cx, cy, cr, tc);
                    else                  bb.DrawArc(cx, cy, cr, 0, 360, tc, 2);
                    tx = ubh;
                    tw = bb.Width - ubh * 2;
                }
                var labelText = userButtonActive ? UserColorFinder.getLabelOnShort(pluginName, cd.UserLabel, isUser: true)
                                                 : UserColorFinder.getLabelShort(pluginName, cd.UserLabel, isUser: true);
                var menuItems = UserColorFinder.getMenuItems(pluginName, cd.UserLabel, isUser: true);
                if (menuItems != null)
                {
                    if (labelText.Length > 0) labelText += ": "; 
                    labelText += menuItems[cd.UserValue / (127 / (menuItems.Length - 1))];
                }
                bb.DrawImage(LabelImageLoader.GetImage(labelText, tw, TitleHeight, tc), tx, uby);
            }
            else
            {
                if (buttonMode == SelectButtonMode.Select)
                {
                    commandProperty = ChannelProperty.PropertyType.Select;
                }

                if (cd.Selected)
                {
                    bb.FillRectangle(0, 0, bb.Width, bb.Height, ChannelProperty.PropertyColor[(Int32)ChannelProperty.PropertyType.Select]);
                }
                else
                {
                    bb.FillRectangle(0, 0, bb.Width, bb.Height, new BitmapColor(20, 20, 20));
                }

                var rX = 8;
                var rY = 4;
                var rS = 8;
                var rW = (bb.Width - rS) / 2 - rX;
                var rH = (bb.Height - rY - TitleHeight) / 2 - rS;
                var rX2 = rX + rW + rS;
                var rY2 = rY + rH + rS + TitleHeight;

                bb.FillRectangle(rX2 - 5, rY, 2, rH, new BitmapColor(40, 40, 40));
                bb.FillRectangle(rX2 - 5, rY2, 2, rH, new BitmapColor(40, 40, 40));

                if (cd.Muted || commandProperty == ChannelProperty.PropertyType.Mute)
                {
                    bb.FillRectangle(rX - 2, rY - 2, rW + 4, rH + 4,
                        cd.Muted ? ChannelProperty.PropertyColor[(Int32)ChannelProperty.PropertyType.Mute]
                                 : SelectButtonData.CommandPropertyColor);
                }
                if (cd.Solo || commandProperty == ChannelProperty.PropertyType.Solo)
                {
                    bb.FillRectangle(rX2 - 2, rY - 2, rW + 4, rH + 4,
                        cd.Solo ? ChannelProperty.PropertyColor[(Int32)ChannelProperty.PropertyType.Solo]
                                : SelectButtonData.CommandPropertyColor);
                }
                if (cd.Armed || commandProperty == ChannelProperty.PropertyType.Arm)
                {
                    bb.FillRectangle(rX - 2, rY2 - 2, rW + 4, rH + 4,
                        cd.Armed ? ChannelProperty.PropertyColor[(Int32)ChannelProperty.PropertyType.Arm]
                                : SelectButtonData.CommandPropertyColor);
                }
                if (cd.Monitor || commandProperty == ChannelProperty.PropertyType.Monitor)
                {
                    bb.FillRectangle(rX2 - 2, rY2 - 2, rW + 4, rH + 4,
                        cd.Monitor ? ChannelProperty.PropertyColor[(Int32)ChannelProperty.PropertyType.Monitor]
                                : SelectButtonData.CommandPropertyColor);
                }
                bb.DrawText(ChannelProperty.PropertyLetter[(Int32)ChannelProperty.PropertyType.Mute], rX, rY, rW, rH, new BitmapColor(175, 175, 175), rH - 4);
                bb.DrawText(ChannelProperty.PropertyLetter[(Int32)ChannelProperty.PropertyType.Solo], rX2, rY, rW, rH, new BitmapColor(175, 175, 175), rH - 4);
                bb.DrawImage(SelectButtonData.IconSelRec, rX + rW / 2 - SelectButtonData.IconSelRec.Width / 2, rY2 + rH / 2 - SelectButtonData.IconSelRec.Height / 2);
                bb.DrawImage(SelectButtonData.IconSelMon, rX2 + rW / 2 - SelectButtonData.IconSelMon.Width / 2, rY2 + rH / 2 - SelectButtonData.IconSelRec.Height / 2);

                bb.DrawText(ColorFinder.stripLabel(cd.Label), 0, bb.Height / 2 - TitleHeight / 2, bb.Width, TitleHeight);
            }
            return bb.ToImage();
        }

        public override void runCommand()
        {
            ChannelData cd = this.Plugin.channelData[this.ChannelIndex.ToString()];
            switch (this.CurrentMode)
            {
                case SelectButtonMode.Select:
                    if (!cd.Selected)
                    {
                        cd.EmitChannelPropertyPress(ChannelProperty.PropertyType.Select);   // Sends MIDI data
                    }
                    this.Plugin.EmitSelectedButtonPressed();    // Notifies other components
                    break;
                case SelectButtonMode.Property:
                    cd.EmitChannelPropertyPress(SelectButtonData.SelectionPropertyType);    // Sends MIDI data
                    break;
                case SelectButtonMode.User:
                    var menuItems = UserColorFinder .getMenuItems(PluginName, cd.UserLabel, isUser: true);
                    if (menuItems != null)
                    {
                        // Display value selection menu.
                        var ubmp = new UserButtonMenuParams();
                        ubmp.ChannelIndex = this.ChannelIndex;
                        ubmp.MenuItems = menuItems;
                        this.Plugin.EmitUserButtonMenuActivated(ubmp);
                    }
                    else
                    {
                        // Toggle the user button value.
                        this.Plugin.SendMidiNote(0, UserButtonMidiBase + this.ChannelIndex, this.UserButtonActive ? 0 : 127);
                    }
                    break;
                case SelectButtonMode.FX:
                    cd.EmitChannelPropertyPress(ChannelProperty.PropertyType.Select);   // Sends MIDI data
                    this.Plugin.EmitSelectModeChanged(SelectButtonMode.User);
                    this.Plugin.EmitSelectedButtonPressed();    // Notifies other components
                    break;
                case SelectButtonMode.Custom:
                    if (this.CurrentCustomParams.MidiCode > 0)
                    {
                        this.Plugin.SendMidiNote(this.CurrentCustomParams.MidiChannel, this.CurrentCustomParams.MidiCode);
                    }
                    break;
            }
        }
    }

    public class UserMenuSelectButtonData : ButtonData
    {
        //        Int32 MidiChannel = -1;
        Int32 ChannelIndex = 0;
        protected Int32 Value = 0;
        protected String Label;

        public UserMenuSelectButtonData()
        {
        }

        public void init(Int32 channelIndex = 0, Int32 value = 0, String label = null)
        {
            this.ChannelIndex = channelIndex;
            this.Value = value;
            this.Label = label;
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);

            if (this.Label != null)
            {
                //            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.White);
                //            bb.FillRectangle(stroke, stroke, bb.Width - 2 * stroke, bb.Height - 2 * stroke, BitmapColor.Black);

                var height = bb.Height / 2;
                bb.FillRectangle(0, (bb.Height - height - 4) / 2, bb.Width, height + 4, new BitmapColor(120, 120, 120));
                bb.FillRectangle(0, (bb.Height - height) / 2, bb.Width, height, new BitmapColor(40, 40, 40));
                bb.DrawImage(LabelImageLoader.GetImage(this.Label, bb.Width, bb.Height));
            }
            return bb.ToImage();
        }

        public override void runCommand()
        {
            if (this.ChannelIndex >= 0 && this.Label != null)
            {
                this.Plugin.SendMidiNote(0, UserButtonMidiBase + this.ChannelIndex, this.Value);
            }
            this.Plugin.EmitUserButtonMenuActivated(new UserButtonMenuParams { ChannelIndex = this.ChannelIndex, IsActive = false });
        }
    }

    public class CommandButtonData : ButtonData
    {
        public Int32 Code;
        public Int32 CodeOn = 0;              // alternative code to send when activated
        public String Name;

        public virtual Boolean Activated { get; set; } = false;

        public BitmapColor OffColor = BitmapColor.Black;
        public BitmapColor OnColor = BitmapColor.Black;
        public BitmapColor TextColor = BitmapColor.White;
        public BitmapColor TextOnColor = BitmapColor.White;
        public BitmapImage Icon, IconOn;

        public static readonly BitmapColor cRectOn = new BitmapColor(200, 200, 200);
        public static readonly BitmapColor cTextOn = BitmapColor.Black;
        public static readonly BitmapColor cRectOff = new BitmapColor(50, 50, 50);
        public static readonly BitmapColor cTextOff = new BitmapColor(160, 160, 160);

        protected Int32 MidiChannel = 0;
        public int midiChannel
        {
            get => this.MidiChannel;
        }


        public CommandButtonData(Int32 code, String name, String iconName = null)
        {
            this.init(code, name, iconName);
        }

        public CommandButtonData(Int32 code, String name, String iconName, BitmapColor bgColor)
        {
            this.init(code, name, iconName);
            this.OnColor = bgColor;
            this.OffColor = bgColor;
        }

        public CommandButtonData(Int32 code, Int32 codeOn, String name, String iconName = null)
        {
            this.init(code, name, iconName);
            this.CodeOn = codeOn;
        }
        public CommandButtonData(Int32 code, String name, BitmapColor onColor, BitmapColor textOnColor, Boolean isActivatedByDefault = false)
        {
            this.init(code, name, null);
            this.OnColor = onColor;
            this.TextOnColor = textOnColor;
            this.Activated = isActivatedByDefault;
        }

        private void init(Int32 code, String name, String iconName)
        {
            this.Name = name;
            this.Code = code;

            LoadIcons(iconName, ref this.Icon, ref this.IconOn);
        }

        public static void LoadIcons(String iconName, ref BitmapImage icon, ref BitmapImage iconOn)
        {
            if (iconName != null)
            {
                var iconResExt = "_52px.png";
                if (EmbeddedResources.FindFile(iconName + iconResExt) == null) iconResExt = "_80px.png";
                icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile(iconName + iconResExt));
                var iconResOn = EmbeddedResources.FindFile(iconName + "_on" + iconResExt);
                if (iconResOn != null)
                {
                    iconOn = EmbeddedResources.ReadImage(iconResOn);
                }
            }
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            // Debug.WriteLine("CommandButtonData.getImage " + this.Code.ToString() + ", name: " + this.Name);

            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, this.Activated ? this.OnColor : this.OffColor);

            if (this.Activated && this.IconOn != null)
            {
                bb.DrawImage(this.IconOn);
            }
            else if (this.Icon != null)
            {
                bb.DrawImage(this.Icon);
            }
            else
            {
                bb.DrawText(this.Name, 0, 0, bb.Width, bb.Height, this.Activated ? this.TextOnColor : this.TextColor, 16);
            }

            return bb.ToImage();
        }

        public override void runCommand()
        {
            var note = this.Code;
            if (this.Activated && (this.CodeOn > 0))
            {
                note = this.CodeOn;
            }
            this.Plugin.SendMidiNote(this.MidiChannel, note);
        }
    }

    public class SnapStepCommandButtonData : CommandButtonData
    {
        public enum StepDir { StepFwd, StepRev };
        private StepDir DirMode;
        public SnapStepCommandButtonData(StepDir stepDir) : base(stepDir == StepDir.StepFwd ? 0x37 : 0x38, 
                                                                 "Snap Step " + (stepDir == StepDir.StepFwd ? "Fwd" : "Rev"))
        {
            this.DirMode = stepDir;
        }
        public override void runCommand()
        {
            if ((this.Plugin as StudioOneMidiPlugin).ShiftPressed)
            {
                this.Plugin.SendMidiNote(15, this.DirMode == StepDir.StepFwd ? 0x00 : 0x01);
            }
            else
            {
                base.runCommand();
            }
        }
    }

    public class FlipPanVolCommandButtonData : CommandButtonData
    {
        public FlipPanVolCommandButtonData(Int32 code) : base(code, "Flip Vol/Pan")
        {
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);

            int rY = 16;
            int rS = 8;
            int rW = bb.Width - 24;
            int rH = (bb.Height - 2 * rY - rS) / 2;
            int rX = (bb.Width - rW) / 2;

            bb.FillRectangle(rX, rY, rW, rH, this.Activated ? CommandButtonData.cRectOff : CommandButtonData.cRectOn);
            bb.DrawText("VOL", rX, rY, rW, rH, this.Activated ? CommandButtonData.cTextOff : CommandButtonData.cTextOn, rH - 6);

            bb.FillRectangle(rX, rY + rH + rS, rW, rH, this.Activated ? CommandButtonData.cRectOn : CommandButtonData.cRectOff);
            bb.DrawText("PAN", rX, rY + rH + rS, rW, rH, this.Activated ? CommandButtonData.cTextOn : CommandButtonData.cTextOff, rH - 6);

            return bb.ToImage();
        }

        public override Boolean Activated 
        { 
            get => base.Activated; 
            set {
                base.Activated = value;
                this.Plugin.EmitFaderModeChanged(value == true ? StudioOneMidiPlugin.FaderMode.Pan : StudioOneMidiPlugin.FaderMode.Volume);
            }
        }
    }

    public class ViewAllRemoteCommandButtonData : CommandButtonData
    {
        public ViewAllRemoteCommandButtonData() : base(0x36, "Show All/User")
        {
            this.Activated = true;
        }

        enum ViewMode { All, User };
        ViewMode Mode = ViewMode.All;

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var cTextOn = BitmapColor.Black;
            var cBgOn = this.Activated ? new BitmapColor(180, 180, 80) : new BitmapColor(40, 40, 40);
            var cTextOff = this.Activated ? cBgOn : new BitmapColor(80, 80, 80);
            var cBgOff = new BitmapColor(cBgOn, 80);

            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);

            bb.FillRectangle(0, 0, bb.Width, bb.Height / 2, this.Mode == ViewMode.User ? cBgOn : cBgOff);
            bb.DrawText("REMOTE", 0, 0, bb.Width, bb.Height / 2, this.Mode == ViewMode.User ? cTextOn : cTextOff);

            bb.FillRectangle(0, bb.Height / 2, bb.Width, bb.Height / 2, this.Mode == ViewMode.All ? cBgOn : cBgOff);
            bb.DrawText("ALL", 0, bb.Height / 2, bb.Width, bb.Height / 2, this.Mode == ViewMode.All ? cTextOn : cTextOff);

            return bb.ToImage();
        }

        public override void runCommand()
        {
            if (this.Activated)
            {
                this.Mode = this.Mode == ViewMode.All ? ViewMode.User : ViewMode.All;
            }
            this.Plugin.SendMidiNote(0, this.Mode == ViewMode.All ? 0x36 : 0x45);
        }
    }

    // Triggers a command from a list in LoupedeckCT.surface.xml on the Studio One side.
    // These are one-shot commands that do not provide feedback.
    //
    public class OneWayCommandButtonData : CommandButtonData
    {
        public OneWayCommandButtonData(Int32 channel, Int32 code, String name, String iconName = null) : base(code, name, iconName)
        {
            this.MidiChannel = channel;
        }

        public OneWayCommandButtonData(Int32 channel, Int32 code, String name, String iconName, BitmapColor bgColor) : base(code, name, iconName, bgColor)
        {
            this.MidiChannel = channel;
        }

        public OneWayCommandButtonData(Int32 channel, Int32 code, String name, BitmapColor textColor) : base(code, name, null)
        {
            this.MidiChannel = channel;
            this.TextColor = textColor;
        }

        public OneWayCommandButtonData(Int32 channel, Int32 code, String name, BitmapColor onColor, BitmapColor textOnColor, Boolean isActivatedByDefault = false)
            : base(code, name, onColor, textOnColor, isActivatedByDefault)
        {
            this.MidiChannel = channel;
        }

        // The code below is an alternative method for invoking commands by setting
        // a command parameter via a MIDI controller and then triggering it by a MIDI note.
        // This provides 127 addresses for command triggering per MIDI controller, but it requires
        // two separate MIDI messages to be sent which is not ideal.
        //
        // public override void runCommand()
        // {
        //
        //    var ccSet = new ControlChangeEvent();
        //    ccSet.ControlNumber = (SevenBitNumber)0x00;
        //    ccSet.ControlValue = (SevenBitNumber)this.CommandCode;
        //    this.Plugin.mackieMidiOut.SendEvent(ccSet);
        //
        //    var eTrigger = new NoteOnEvent();
        //    eTrigger.Velocity = (SevenBitNumber)127;
        //    eTrigger.NoteNumber = (SevenBitNumber)0x72;    // controlCommandTrigger
        //    this.Plugin.mackieMidiOut.SendEvent(eTrigger);
        // }
    }

    
    public abstract class NumberedSelectionButtonData : OneWayCommandButtonData
    {
        private readonly Int32 SelectionNo;
        private readonly Int32 OffsetX, OffsetY;

        public NumberedSelectionButtonData(Int32 midiChannel, Int32 midiBaseNote, 
                                           Int32 selectionNo, String label, String iconName, 
                                           Int32 offsetX, Int32 offsetY, Int32 bgR, Int32 bgG, Int32 bgB) 
               : base(midiChannel, midiBaseNote + selectionNo, $"{label} {selectionNo}", iconName)
        {
            this.SelectionNo = selectionNo;
            this.OffsetX = offsetX;
            this.OffsetY = offsetY;
            this.OffColor = new BitmapColor(bgR, bgG, bgB, 80);
        }
        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, this.OffColor);

            bb.DrawImage(this.Icon);
            bb.DrawText(this.SelectionNo.ToString(), this.OffsetX, this.OffsetY, bb.Width, bb.Height, this.TextColor, 14);

            return bb.ToImage();
        }
    }

    public class GroupSuspendButtonData : NumberedSelectionButtonData
    {
        public GroupSuspendButtonData(Int32 groupNumber) : base(14, 0x21, groupNumber, "Group", "group_suspend_no", 0, 7, 191, 255, 144)
        {
        }
    }
    public class MarkerGotoButtonData : NumberedSelectionButtonData
    {
        public static readonly BitmapColor BgColor = new BitmapColor(169, 146, 255, 80);
        public MarkerGotoButtonData(Int32 markerNumber) : base(14, 0x65, markerNumber, "Marker", "marker_goto_no", 7, 9, BgColor.R, BgColor.G, BgColor.B)
        {
        }
    }
    public class SceneSelectButtonData : NumberedSelectionButtonData
    {
        public SceneSelectButtonData(Int32 sceneNumber) : base(15, 0x37, sceneNumber, "Scene", "scene_select_no", 0, 7, 52, 116, 187)
        {
        }
    }

    public class MenuCommandButtonData : CommandButtonData
    {
        public Boolean MenuActivated { get; set; } = false;
        private readonly Int32 CodeInit = 0;            // MIDI note that gets sent when the menu is activated
        private readonly BitmapColor BgColor;           // local cache for thread safety

        public MenuCommandButtonData(Int32 codeInit, Int32 codeLED, String name, String iconName, BitmapColor bgColor) : base(codeLED, name, iconName, bgColor)
        {
            this.CodeInit = codeInit;
            this.BgColor = bgColor;
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            if (this.MenuActivated)
            {
                this.OnColor = this.BgColor;
                this.OffColor = this.BgColor;
            }
            else
            {
                this.OnColor = BitmapColor.Black;
                this.OffColor = BitmapColor.Black;
            }
            var bb = new BitmapBuilder(imageSize);
            bb.DrawImage(base.getImage(imageSize), 0, 0);
            if (this.MenuActivated)
            {
                var rc = new BitmapColor(BitmapColor.White, 120);
                bb.FillRectangle(0, 0, bb.Width, 4, rc);
                bb.FillRectangle(0, bb.Height - 5, bb.Width, 5, rc);
            }

            return bb.ToImage();
        }

        public override void runCommand()
        {
            if (!this.MenuActivated && this.CodeInit > 0)
            {
                this.Plugin.SendMidiNote(0, this.CodeInit);
            }
            this.MenuActivated = !this.MenuActivated;
        }
    }

    public class ModeButtonData : ButtonData
    {
        public String Name;
        public BitmapImage Icon = null;
        public Boolean Activated = false;
        private readonly Boolean IsMenu = false;
        private Int32 MidiCode = 0;
        private BitmapColor BgColor = ButtonData.DefaultSelectionBgColor;

        public ModeButtonData(String name, String iconName = null, Boolean isMenu = false, Int32 midiCode = 0)
        {
            this.init(name, iconName, BitmapColor.Black, midiCode);
            this.IsMenu = isMenu;
        }
        public ModeButtonData(String name, String iconName, BitmapColor bgColor, Boolean isMenu = false, Int32 midiCode = 0)
        {
            this.init(name, iconName, bgColor, midiCode);
            this.IsMenu = isMenu;
        }

        private void init(String name, String iconName, BitmapColor bgColor, Int32 midiCode)
        {
            this.Name = name;
            this.BgColor = bgColor;
            this.MidiCode = midiCode;

            if (iconName != null)
            {
                if (!iconName.Contains("px"))
                {
                    iconName += "_52px";
                }
                this.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{iconName}.png"));
            }
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);

            // Always fill the background with black first, otherwise background colors with transparency
            // will not work in version 6 of the SDK.
            //
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);
            if (this.Activated)
            {
                bb.FillRectangle(0, 0, bb.Width, bb.Height, this.BgColor);
            }

            if (this.IsMenu && this.Activated)
            {
                bb.FillRectangle(0, 0, bb.Width, 4, BitmapColor.White);
                bb.FillRectangle(0, bb.Height - 5, bb.Width, 5, BitmapColor.White);
            }
            
            if (this.Icon != null)
            {
                bb.DrawImage(this.Icon);
            }
            else
            {
                bb.DrawText(this.Name, 0, 0, bb.Width, bb.Height, null, 16);
            }

            return bb.ToImage();
        }

        public override void runCommand()
        {
            this.Activated = !this.Activated;
            if (this.MidiCode > 0 && this.Activated)
            {
                this.Plugin.SendMidiNote(0, this.MidiCode);
            }
        }
    }

    public class ModeTopCommandButtonData : OneWayCommandButtonData
    {
        public enum Location
        {
            Left,
            Right
        }
        Location ButtonLocation = Location.Left;
        String TopDisplayText;
        protected Boolean IsUserButton = false;
        protected String PluginName;
        protected ColorFinder UserColorFinder = new ColorFinder(new ColorFinder.ColorSettings { OnColor = FinderColor.Black,
                                                                                                OffColor = FinderColor.Black,
                                                                                                TextOnColor = FinderColor.White,
                                                                                                TextOffColor = FinderColor.Black});
        enum PluginType { Any, Mono, Stereo, MonoStereo }    // variants of Waves plugins

        public ModeTopCommandButtonData(Int32 channel, Int32 code, String name, Location bl, String iconName = null) : base(channel, code, name, iconName)
        {
            this.ButtonLocation = bl;
        }

        public ModeTopCommandButtonData(Int32 channel, Int32 code, String name, Location bl, String iconName, BitmapColor offColor) : base(channel, code, name, iconName)
        {
            this.ButtonLocation = bl;
            this.OffColor = offColor;
        }

        public override void OnLoad(StudioOneMidiPlugin plugin)
        {
            base.OnLoad(plugin);
            this.UserColorFinder.Init(plugin);
        }

        public void setTopDisplay(String text) => this.TopDisplayText = text;
        public void setPluginName(String text)
        {
            this.PluginName = text;

            if (this.IsUserButton && this.Name.Length > 0)
            {
                this.Icon = this.UserColorFinder.getIcon(this.PluginName, this.Name);
                this.IconOn = this.UserColorFinder.getIconOn(this.PluginName, this.Name);
            }
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);

            var dispTxtH = 24;
            var bgX = this.IsUserButton ? dispTxtH + 6 : 0;
            var bgH = bb.Height - bgX;

            if (this.IsUserButton)
            {
                if (this.Name.Length == 0)
                {
                    bb.FillRectangle(0, bgX, bb.Width, bgH, SelectButtonData.BgColorUnassigned);
                }
                else
                {
                    bb.FillRectangle(0, bgX, bb.Width, bgH, this.Activated ? this.UserColorFinder.getOnColor(this.PluginName, this.Name, isUser: true)
                                                                           : this.UserColorFinder.getOffColor(this.PluginName, this.Name, isUser: true));
                }
            }
            else
            {
                bb.FillRectangle(0, 0, bb.Width, bb.Height, this.OffColor);
            }

            if (this.Activated && this.IconOn != null)
            {
                bb.DrawImage(this.IconOn, (bb.Width - this.IconOn.Width) / 2, dispTxtH);
            }
            else if (this.Icon != null && !(this.IsUserButton && this.Name.Length == 0))
            {
                bb.DrawImage(this.Icon, (bb.Width - this.Icon.Width) / 2, dispTxtH + (bb.Height - dispTxtH - this.Icon.Height) / 2);
            }
            else
            {
                bb.DrawText(this.Activated ? this.UserColorFinder.getLabelOn(this.PluginName, this.Name, isUser: true) :
                                             this.UserColorFinder.getLabel(this.PluginName, this.Name, isUser: true), 
                                             0, dispTxtH, bb.Width, bb.Height - dispTxtH, 
                            this.Activated ? this.UserColorFinder.getTextOnColor(this.PluginName, this.Name, isUser: true)
                                           : this.UserColorFinder.getTextOffColor(this.PluginName, this.Name, isUser: true), 16);
            }

            if (this.TopDisplayText != null)
            {
                // Waves plugins come in Mono, Stereo, and Mono/Stereo variants. We replace the text
                // denoting the variant with an icon in the top right corner.
                String iconName = null;
                var len = this.TopDisplayText.Length;
                var typePos = -1;
                if (len > 16 && (typePos = this.TopDisplayText.LastIndexOf("Mono/Stereo", len, 16)) > 0)
                {
                    iconName = "plugtype_mono-stereo";
                }
                else if (len > 9 && (typePos = this.TopDisplayText.LastIndexOf("Mono", len, 9)) > 0)
                {
                    iconName = "plugtype_mono";
                }
                else if (len > 11 && (typePos = this.TopDisplayText.LastIndexOf("Stereo", len, 11)) > 0)
                {
                    iconName = "plugtype_stereo";
                }

                // Check for other clutter in plugin name
                if (typePos < 0)
                {
                    typePos = this.TopDisplayText.LastIndexOf("x64");
                }

                bb.DrawText(typePos > 0 ? this.TopDisplayText.Substring(0, typePos) : this.TopDisplayText,
                            this.ButtonLocation == Location.Left ? 1 : -bb.Width - 1, 0, bb.Width * 2, dispTxtH);
                if (iconName != null && this.ButtonLocation == Location.Right)
                {
                    // bb.FillRectangle(bb.Width - 20, 0, 20, 20, BitmapColor.Black);
                    bb.DrawImage(EmbeddedResources.ReadImage(EmbeddedResources.FindFile(iconName + "_20px.png")), bb.Width - 20, 0);
                }
            }
            return bb.ToImage();
        }
    }

    public class ModeTopUserButtonData : ModeTopCommandButtonData
    {
        public ModeTopUserButtonData(Int32 channel, Int32 code, String name, Location bl) : base(channel, code, name, bl)
        {
            this.IsUserButton = true;
            this.UserColorFinder.DefaultColorSettings.OnColor = SelectButtonData.BgColorAssigned;
            this.UserColorFinder.DefaultColorSettings.OffColor = SelectButtonData.BgColorAssigned;
        }
    }


    public class ModeChannelSelectButtonData : ButtonData
    {
        public BitmapImage Icon, IconOn;
        public Boolean Activated = true;

        public ModeChannelSelectButtonData(Boolean activated = true)
        {
            this.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("select-select_80px.png"));
            this.IconOn = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("select-select_on_80px.png"));
            this.Activated = activated;
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);

            if (!this.Activated)
            {
                bb.DrawImage(this.Icon, 0, 0);
            }
            else
            {
                bb.DrawImage(this.IconOn, 0, 0);
            }

            return bb.ToImage();
        }

        public override void runCommand()
        {
            if (!this.Activated)
            {
                this.Activated = true;
                this.Plugin.EmitSelectModeChanged(SelectButtonMode.Select);
            }
        }
    }

    public class AutomationModeButtonData : ButtonData
    {
        public static String[] AutomationText =
        {
             "Auto: Off", // Off
			 "Read",      // Read
			 "Touch",     // Touch
			 "Latch",     // Latch
			 "Write",     // Write
		};
        public static BitmapColor[] AutomationBgColor =
        {
             BitmapColor.Black, // Off
			 new BitmapColor(129, 171, 115), // Read
			 new BitmapColor(216, 176,  82), // Touch
			 new BitmapColor(194,  90,  95), // Latch
			 new BitmapColor(208,  64,  71), // Write
		};
        public static BitmapColor[] AutomationTextColor =
        {
             BitmapColor.White, // Off
			 BitmapColor.Black, // Read
			 BitmapColor.Black, // Touch
			 BitmapColor.White, // Latch
			 BitmapColor.White, // Write
		};

        public AutomationMode CurrentMode = AutomationMode.Off;
        public Boolean SelectionModeActivated = false;

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            const Int32 fillHeight = 24;
            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);

            if (this.SelectionModeActivated)
            {
                bb.FillRectangle(0, 0, bb.Width, bb.Height, ButtonData.DefaultSelectionBgColor);
                bb.DrawText("Auto\rMode", 0, 0, bb.Width, bb.Height, null, 16);
            }
            else
            {
                bb.FillRectangle(0, (bb.Height - fillHeight) / 2, bb.Width, fillHeight, AutomationBgColor[(Int32)this.CurrentMode]);
                bb.DrawText(AutomationText[(Int32)this.CurrentMode], 0, 0, bb.Width, bb.Height, AutomationTextColor[(Int32)this.CurrentMode], 16);
            }
            return bb.ToImage();
        }

        public override void runCommand()
        {
        }
    }

    public class AutomationModeCommandButtonData : ButtonData
    {
        private readonly BitmapColor BgColor = BitmapColor.Black;
        private readonly AutomationMode Mode;

        public AutomationModeCommandButtonData(AutomationMode am, BitmapColor bgColor)
        {
            this.Mode = am;
            this.BgColor = bgColor;
        }
        public AutomationModeCommandButtonData(AutomationMode am)
        {
            this.Mode = am;
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            const Int32 fillHeight = 24;
            var bb = new BitmapBuilder(imageSize);
            var bY = (bb.Height - fillHeight) / 2;

            bb.FillRectangle(0, 0, bb.Width, bb.Height, this.BgColor);
            bb.FillRectangle(0, bY, bb.Width, fillHeight, AutomationModeButtonData.AutomationBgColor[(Int32)this.Mode]);
            bb.DrawText(this.Mode == AutomationMode.Off ? "Off" : AutomationModeButtonData.AutomationText[(Int32)this.Mode], 
                        0, 0, bb.Width, bb.Height, AutomationModeButtonData.AutomationTextColor[(Int32)this.Mode], 16);

            if (this.Mode == this.Plugin.CurrentAutomationMode)
            {
                bb.FillRectangle(0, bY - 4, bb.Width, 4, BitmapColor.White);
                bb.FillRectangle(0, bY + fillHeight, bb.Width, 4, BitmapColor.White);
            }

            return bb.ToImage();
        }
        
        public override void runCommand()
        {
            if (this.Mode == this.Plugin.CurrentAutomationMode)
            {
                return;
            }

            if (this.Mode == AutomationMode.Off)
            {
                this.Plugin.SendMidiNote(0, 0x4A + (Int32)this.Plugin.CurrentAutomationMode - 1);
            }
            else
            {
                this.Plugin.SendMidiNote(0, 0x4A + (Int32)this.Mode - 1);
            }
        }
    }

    public class RecPreModeButtonData : ButtonData
    {
        public Boolean SelectionModeActivated = false;

        private BitmapImage[] Icon = new BitmapImage[3];
        private BitmapImage[] IconOn = new BitmapImage[3];

        public RecPreModeButtonData()
        {
            this.loadIcon(RecPreMode.Precount, "precount");
            this.loadIcon(RecPreMode.Preroll, "preroll");
            this.loadIcon(RecPreMode.Autopunch, "autopunch");
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);

            if (this.SelectionModeActivated)
            {
                bb.FillRectangle(0, 0, bb.Width, bb.Height, new BitmapColor(ButtonData.DefaultSelectionBgColor, 190));
                bb.FillRectangle(0, 0, bb.Width, 4, BitmapColor.White);
                bb.FillRectangle(0, bb.Height - 5, bb.Width, 5, BitmapColor.White);
            }

            for (int i = 0; i < this.Icon.Length; i++)
            {
                if (i == (Int32)this.Plugin.CurrentRecPreMode) bb.DrawImage(this.IconOn[i]);
                else                                           bb.DrawImage(this.Icon[i]);
            }
            return bb.ToImage();
        }

        public override void runCommand()
        {
        }

        private void loadIcon(RecPreMode mode, String baseName)
        {
            this.Icon[(Int32)mode]   = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{baseName}_sm_52px.png"));
            this.IconOn[(Int32)mode] = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{baseName}_sm_on_52px.png"));
        }
    }

    public class SendsCommandButtonData : CommandButtonData
    {
        public const Int32 Note = 0x29;
        public SendsCommandButtonData() : base(Note, "SENDS")
        {
            this.Activated = true;
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);

            int rY = 12;
            int rW = bb.Width - 2 * rY;
            int rH = bb.Height - 2 * rY;
            int rX = rY;

            bb.FillRectangle(rX, rY, rW, rH, this.Activated ? CommandButtonData.cRectOn : CommandButtonData.cRectOff);

            bb.DrawText(this.Name, rX, rY, rW, rH, this.Activated ? CommandButtonData.cTextOn : CommandButtonData.cTextOff, 16);

            return bb.ToImage();
        }
        public override void runCommand() => this.Plugin.SetChannelFaderMode(ChannelFaderMode.Send);
    }

    public class PanCommandButtonData : CommandButtonData
    {
        public const Int32 Note = 0x2A;
        public PanCommandButtonData(String name = "PAN") : base(Note, name)
        {
            this.Activated = true;
        }
        public override void runCommand() => this.Plugin.SetChannelFaderMode(ChannelFaderMode.Pan);
    }

    public class UserModeButtonData : ButtonData
    {
        public Int32 ActiveUserPages { get; set; } = 3;
        Int32 UserPage { get; set; } = 0;
        Int32 LastUserPage { get; set; } = 0;
        String[] PageNames;
        Boolean IsActive { get; set; } = false;

        public UserModeButtonData()
        {
        }

        public void clearActive() => this.IsActive = false;
        public void setPageNames(String[] pageNames) => this.PageNames = pageNames;

        public void setUserPage(Int32 userPage)
        {
            this.UserPage = userPage;
            if (userPage > 0)
            {
                this.LastUserPage = userPage;
            }
        }

        // Gets called when the focus device changes
        public void resetUserPage()
        {
            this.UserPage = 0;      // current user page in Studio One is unknown
            this.LastUserPage = 1;
            this.PageNames = null;
            if (this.IsActive && this.Plugin.CurrentChannelFaderMode == ChannelFaderMode.User)
            {
                this.UserPage = 1;
                this.Plugin.SetChannelFaderMode(ChannelFaderMode.User, this.UserPage);
            }
        }
        public void sendUserPage()
        {
            // Actively set the page if current page is unknown
            this.UserPage = this.UserPage > 0 ? this.UserPage : this.LastUserPage > 0 ? this.LastUserPage : 1;
            this.Plugin.SetChannelFaderMode(ChannelFaderMode.User, this.UserPage);
        }
        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);

            var rY = 12;
            var rW = bb.Width - 2 * rY;
            var rH = (bb.Height - 2 * rY) / 2;
            var rX = (bb.Width - rW) / 2;

            bb.FillRectangle(rX, rY, rW, rH, this.UserPage == 0 ? CommandButtonData.cRectOff : CommandButtonData.cRectOn);
            bb.DrawText("USER", rX, rY, rW, rH, this.UserPage == 0 ? CommandButtonData.cTextOff : CommandButtonData.cTextOn, 16);

            rY += rH;
            bb.FillRectangle(rX, rY, rW, rH, CommandButtonData.cRectOff);

            var rW2 = rW / 3 + 1;

            if (this.PageNames != null && this.PageNames.Length >= this.UserPage)
            {
                bb.DrawText(this.PageNames[(this.UserPage > 0 ? this.UserPage : this.LastUserPage) - 1], rX, rY, rW, rH, CommandButtonData.cTextOff);
            }
            else
            {
                bb.DrawText("1", rX, rY, rW2, rH, CommandButtonData.cTextOff);

                if (this.ActiveUserPages > 1)
                {
                    if (this.ActiveUserPages > 2)
                    {
                        bb.DrawText(this.ActiveUserPages.ToString(), rX + rW - rW2, rY, rW2, rH, CommandButtonData.cTextOff);
                    }
                    if (this.ActiveUserPages < 4)
                    {
                        bb.DrawText("2", rX + rW2, rY, rW2, rH, CommandButtonData.cTextOff);
                    }
                    else
                    {
                        var sp = (rW2 + 6) / 3;
                        for (var i = 0; i < 3; i++)
                        {
                            bb.FillCircle(rX + rW2 + sp * i, rY + rH / 2 + 2, 1, CommandButtonData.cTextOff);
                        }
                    }
                    if (this.ActiveUserPages < 3)
                    {
                        rX += rW2 * (this.UserPage - 1);
                    }
                    else
                    {
                        if (this.UserPage == this.ActiveUserPages)
                            rX = rX + rW - rW2;
                        else
                            rX += (rW - rW2) / (this.ActiveUserPages - 1) * (this.UserPage - 1);
                    }
                }

                if (this.UserPage > 0)
                {
                    bb.FillRectangle(rX, rY, rW2, rH, CommandButtonData.cRectOn);
                    bb.DrawText(this.UserPage.ToString(), rX, rY, rW2, rH, CommandButtonData.cTextOn);
                }
            }

            return bb.ToImage();

        }
        public override void runCommand()
        {
            if (this.IsActive)
            {
                if (this.PageNames != null)
                {
                    // Display value selection menu.
                    var ubmp = new UserButtonMenuParams();
                    ubmp.MenuItems = this.PageNames;
                    this.Plugin.EmitUserButtonMenuActivated(ubmp);
                    return;
                }
                else
                {
                    this.UserPage = (this.Plugin as StudioOneMidiPlugin).ShiftPressed
                        ? this.UserPage <= 1 ? this.ActiveUserPages : this.UserPage - 1
                        : this.UserPage > this.ActiveUserPages - 1 ? 1 : this.UserPage + 1;
                }
            }
            else
            {
                this.IsActive = true;   // activate on first click
            }
            this.sendUserPage();
        }
    }
    public class UserPageMenuSelectButtonData : UserMenuSelectButtonData
    {
        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);

            if (this.Label != null)
            {
                var height = bb.Height / 2 + 4;
                bb.FillRectangle(0, (bb.Height - height) / 2, bb.Width, height, CommandButtonData.cRectOff);
                bb.DrawImage(LabelImageLoader.GetImage(this.Label, bb.Width, bb.Height));
            }
            return bb.ToImage();
        }
        public override void runCommand()
        {
            this.Plugin.SetChannelFaderMode(ChannelFaderMode.User, this.Value);

            this.Plugin.EmitUserButtonMenuActivated(new UserButtonMenuParams { ChannelIndex = -1, IsActive = false });
        }
    }

}