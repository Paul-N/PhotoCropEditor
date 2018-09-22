using System;
using Foundation;
using PEPhotoCropEditor;
using UIKit;

namespace PEPhotoCropControllerExample
{
    public partial class ViewController : UIViewController, IUINavigationControllerDelegate, IUIImagePickerControllerDelegate//, CropViewControllerDelegate
    {
        protected ViewController(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            // Perform any additional setup after loading the view, typically from a nib.
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
            // Release any cached data, images, etc that aren't in use.
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            UpdateEditButtonEnabled();
        }

        partial void OnCameraButtonClick(Foundation.NSObject sender)
        {
            var actionSheet = UIAlertController.Create(null, null, UIAlertControllerStyle.ActionSheet);
            var cameraAction = UIAlertAction.Create("Camera", UIAlertActionStyle.Default, (action) =>
            {
                this.ShowCamera();
            });
            actionSheet.AddAction(cameraAction);
            var albumAction = UIAlertAction.Create(title: "Photo Library", style: UIAlertActionStyle.Default, handler: (action) =>
            {
                this.OpenPhotoAlbum();
            });
            actionSheet.AddAction(albumAction);
            var cancelAction = UIAlertAction.Create(title: "Cancel", style: UIAlertActionStyle.Cancel, handler: (action) => { });

            actionSheet.AddAction(cancelAction);


            PresentViewController(actionSheet, true, null);
        }

        partial void OpenEditor(Foundation.NSObject sender)
        {
            var image = ImageView.Image;
            if(image == null)  {
                return;
        }
            // Uncomment to use crop view directly
            var imgView = new UIImageView(image: image);
            imgView.ClipsToBounds = true;
            imgView.ContentMode = UIViewContentMode.ScaleAspectFit;


            var cropView = new CropView(ImageView.Frame);


            cropView.Opaque = false;
            cropView.ClipsToBounds = true;
            cropView.BackgroundColor = UIColor.Clear;
            cropView.ImageView = imgView;
            cropView.ShowCroppedArea = true;
            cropView.CropAspectRatio = 1.098901098901099f;
            cropView.KeepAspectRatio = true;
            cropView.RotationGestureRecognizer.Enabled = false;



            View.InsertSubviewAbove(cropView, ImageView);

        // Use view controller
        //    let controller = CropViewController()

            //        controller.delegate = self
            //        controller.image = image
            //
            //        let navController = UINavigationController(rootViewController: controller)
            //        present(navController, animated: true, completion: nil)
        }

        private void ShowCamera()
        {
            var controller = new UIImagePickerController();
            controller.WeakDelegate = this;
            controller.SourceType = UIImagePickerControllerSourceType.Camera;
            PresentViewController(controller, true, null);
    }

        private void OpenPhotoAlbum()
        {
            var controller = new UIImagePickerController();
            controller.WeakDelegate = this;
            controller.SourceType = UIImagePickerControllerSourceType.PhotoLibrary;
            PresentViewController(controller, true, null);
        }

        private void UpdateEditButtonEnabled()
        {
            EditButton.Enabled = this.ImageView.Image != null;
        }

        #region UIImagePickerController delegate methods

        [Export("imagePickerController:didFinishPickingMediaWithInfo:")]
        public void FinishedPickingMedia(UIImagePickerController imagePickerController, NSDictionary info)
        {
            if (!(info[UIImagePickerController.OriginalImage] is UIImage image))
            {
                DismissViewController(true, null);
                return;
            }
            ImageView.Image = image;


            DismissViewController(true, () => this.OpenEditor(null));
        }

        #endregion
    }
}
