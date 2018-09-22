using System;
using CoreGraphics;
using UIKit;

namespace PEPhotoCropEditor
{
    public interface CropViewControllerDelegate 
    {
        void CropViewController(CropViewController controller, UIImage image);
        void CropViewController(CropViewController controller, UIImage image, CGAffineTransform transform, CGRect cropRect);
        void CropViewControllerDidCancel(CropViewController controller);
    }

    public class CropViewController : UIViewController
    {

    }
}
