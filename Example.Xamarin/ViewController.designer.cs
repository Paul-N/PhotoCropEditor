// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace PEPhotoCropControllerExample
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        UIKit.UIBarButtonItem CameraButton { get; set; }

        [Outlet]
        UIKit.UIBarButtonItem EditButton { get; set; }

        [Outlet]
        UIKit.UIImageView ImageView { get; set; }

        [Action ("OnCameraButtonClick:")]
        partial void OnCameraButtonClick (Foundation.NSObject sender);

        [Action ("OpenEditor:")]
        partial void OpenEditor (Foundation.NSObject sender);
        
        void ReleaseDesignerOutlets ()
        {
            if (ImageView != null) {
                ImageView.Dispose ();
                ImageView = null;
            }

            if (CameraButton != null) {
                CameraButton.Dispose ();
                CameraButton = null;
            }

            if (EditButton != null) {
                EditButton.Dispose ();
                EditButton = null;
            }
        }
    }
}
