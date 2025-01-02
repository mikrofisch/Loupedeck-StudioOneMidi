namespace Loupedeck.StudioOneMidiPlugin.Helpers
{
    using System;

    using Loupedeck.StudioOneMidiPlugin.Controls;

    // Class to return an image for a button label. The image will contain either the label text
    // or an icon that must be present as an embedded resource. To load an icon the label text
    // that is passed in must start with a "!" followed by the name of the image resource.
    internal class LabelImageLoader
    {
        public static BitmapImage GetImage(String label, Int32 w, Int32 h) => GetImage(label, w, h, BitmapColor.White);
        public static BitmapImage GetImage(String label, Int32 w, Int32 h, BitmapColor tc)
        {
            var bb = new BitmapBuilder(w, h);

            if (label != null)
            {

                try
                {
                    if (label.Length > 0 && label[0] == '!')
                    {

                        var img = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"lbl_{label.Substring(1)}.png"));
                        bb.DrawImage(img, (w - img.Width) / 2, (h - img.Height) / 2);
                    }
                    else
                    {
                        bb.DrawText(label, tc, ButtonData.LabelFontSize);
                    }
                }
                catch (ArgumentException)
                {
                    bb.DrawText(label.Substring(1), tc, ButtonData.LabelFontSize);
                }
            }
            return bb.ToImage();
        }
    }
}
