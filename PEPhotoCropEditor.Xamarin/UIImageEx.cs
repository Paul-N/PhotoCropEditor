using System;
using CoreGraphics;
using UIKit;

namespace PEPhotoCropEditor
{
    public static class UIImageEx
    {
        public static UIImage RotatedImageWithTransform(this UIImage img, CGAffineTransform rotation, CGRect rect)
        {
            var rotatedImage = img.RotatedImageWithTransform(rotation);


            var scale = rotatedImage.CurrentScale;
            var cropRect = CGAffineTransform.CGRectApplyAffineTransform(rect, CGAffineTransform.MakeScale(scale, scale)); //TODO: Is it correct



            var croppedImage = rotatedImage.CGImage?.WithImageInRect(cropRect);
            var image = new UIImage(cgImage: croppedImage, scale: img.CurrentScale, orientation: rotatedImage.Orientation);
            return image;
        }

        private static UIImage RotatedImageWithTransform(this UIImage img, CGAffineTransform transform)
        {
            UIGraphics.BeginImageContextWithOptions(img.Size, true, img.CurrentScale);
            var context = UIGraphics.GetCurrentContext();
            context?.TranslateCTM(img.Size.Width / 2.0f, img.Size.Height / 2.0f);
            context?.ConcatCTM(transform);
            context?.TranslateCTM(img.Size.Width / -2.0f, img.Size.Height / -2.0f);
            img.Draw(new CGRect(x: 0.0f, y: 0.0f, width: img.Size.Width, height: img.Size.Height));
            var rotatedImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
            return rotatedImage;
        }
    }
}
