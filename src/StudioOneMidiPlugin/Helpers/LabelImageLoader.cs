namespace Loupedeck.StudioOneMidiPlugin.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    // Class to return an image for a button label. The image will contain either the label text
    // or an icon that must be present as an embedded resource. To load an icon the label text
    // that is passed in must start with a "!" followed by the name of the image resource.
    internal class LabelImageLoader
    {
        private readonly String Label;

        public LabelImageLoader(String label)
        {
            Label = label;
        }

        public BitmapImage GetImage(Int32 w, Int32 h) => this.GetImage(w, h, BitmapColor.White);
        public BitmapImage GetImage(Int32 w, Int32 h, BitmapColor tc)
        {
            var bb = new BitmapBuilder(w, h);

            if (this.Label != null)
            {

                try
                {
                    if (this.Label.Length > 0 && this.Label[0] == '!')
                    {

                        var img = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"lbl_{this.Label.Substring(1)}.png"));
                        bb.DrawImage(img, (w - img.Width) / 2, (h - img.Height) / 2);
                    }
                    else
                    {
                        bb.DrawText(this.Label, tc);
                    }
                }
                catch (ArgumentException e)
                {
                    bb.DrawText(this.Label.Substring(1), tc);
                }
            }
            return bb.ToImage();
        }
    }
}
