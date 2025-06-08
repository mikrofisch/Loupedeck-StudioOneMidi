namespace Loupedeck.StudioOneMidiPlugin.Helpers
{
    using System;
    using System.Collections.Concurrent;
    using Loupedeck.StudioOneMidiPlugin.Controls;

    using PluginSettings;

    // Class to return an image for a button label. The image will contain either the label text
    // or an icon that must be present as an embedded resource. To load an icon the label text
    // that is passed in must start with a "!" followed by the name of the image resource.
    internal class LabelImageLoader
    {
        public static BitmapImage GetImage(String label, Int32 w, Int32 h) => GetImage(label, w, h, BitmapColor.White);

        public static BitmapImage GetImage(String label, Int32 w, Int32 h, BitmapColor tc)
        {
            if (_imageCache.TryGetValue(label, out var cachedImage)) return cachedImage;

            BitmapImage result;
            var bb = new BitmapBuilder(w, h);

            try
            {
                if (label.Length > 0 && label[0] == '!')
                {
                    BitmapImage? img = null;
                    var iconName = $"{label.Substring(1)}.png";
                    var iconPath = System.IO.Path.Combine(
                        PlugSettingsFinder.XmlConfig.ConfigFolderIconsPath ?? String.Empty,
                        iconName
                    );

                    if (!String.IsNullOrEmpty(PlugSettingsFinder.XmlConfig.ConfigFolderIconsPath) &&
                        System.IO.File.Exists(iconPath))
                    {
                        img = BitmapImage.FromFile(iconPath);
                    }
                    else
                    {
                        img = EmbeddedResources.ReadImage(EmbeddedResources.FindFile(iconName));
                    }

                    DrawImage(bb, img);
                }
                else
                {
                    bb.DrawText(label, tc, ButtonData.LabelFontSize);

                }
                result = bb.ToImage();
                _imageCache[label] = result;
            }
            catch (ArgumentException)
            {
                DrawImage(bb, EmbeddedResources.ReadImage(EmbeddedResources.FindFile("icon_404.png")));
                // bb.DrawText(label.Substring(1), tc, ButtonData.LabelFontSize);
                result = bb.ToImage();      // not chached
            }

            return result;
        }

        static void DrawImage(BitmapBuilder bb, BitmapImage img)
        {
            bb.DrawImage(img, (bb.Width - img.Width) / 2, (bb.Height - img.Height) / 2);
        }

        // Thread-safe cache for images, keyed only by label
        private static readonly ConcurrentDictionary<string, BitmapImage> _imageCache = new();

    }
}