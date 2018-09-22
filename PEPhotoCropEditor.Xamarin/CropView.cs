using System;
using AVFoundation;
using CoreGraphics;
using Foundation;
using UIKit;

namespace PEPhotoCropEditor
{
    public class CropView : UIView, IUIScrollViewDelegate, IUIGestureRecognizerDelegate, ICropRectViewDelegate
    {
        private UIImage _image;
        public virtual UIImage Image
        {
            get => _image;
            set
            {
                _image = value;

                if (_image != null)
                {
                    _imageSize = _image.Size;
                }
                ImageView?.RemoveFromSuperview();
                ImageView = null;
                _zoomingView?.RemoveFromSuperview();
                _zoomingView = null;
                SetNeedsLayout();
            }
        }

        private UIView _imageView;

        public virtual UIView ImageView
        {
            get => _imageView;
            set
            {
                _imageView = value;

                //if let view = imageView , image == nil {
                //    imageSize = view.frame.size
                //}

                if (ImageView != null && Image == null)
                {
                    _imageSize = ImageView.Frame.Size;
                }
                _usingCustomImageView = true;
                SetNeedsLayout();
            }
        }

        public virtual UIImage CroppedImage => Image?.RotatedImageWithTransform(Rotation, ZoomedCropRect());

        bool _keepAspectRatio;

        public bool KeepAspectRatio
        {
            get => _keepAspectRatio;
            set
            {
                _keepAspectRatio = value;
                _cropRectView.KeepAspectRatio = _keepAspectRatio;
            }
        }

        //nfloat _cropAspectRatio;
        public virtual nfloat CropAspectRatio
        {
            get
            {
                var rect = _scrollView.Frame;
                var width = rect.Width;
                var height = rect.Height;
                return width / height;
            }

            set
            {
                SetCropAspectRatio(value, shouldCenter: true);
            }
        }

        public virtual CGAffineTransform Rotation
        {
            get
            {
                if (ImageView != null)
                    return ImageView.Transform;
                else
                {
                    return CGAffineTransform.MakeIdentity();
                }

            }
        }

        public virtual nfloat RotationAngle
        {
            get => NMath.Atan2(Rotation.yx, Rotation.xx);
            set
            {
                if (ImageView != null)
                    ImageView.Transform = CGAffineTransform.MakeRotation(value);
            }
        }

        public virtual CGRect CropRect
        {
            get => _scrollView.Frame;
            set => ZoomToCropRect(value);
        }

        private CGRect _imageCropRect = CGRect.Empty;

        public virtual CGRect ImageCropRect
        {
            get => _imageCropRect;
            set
            {
                _imageCropRect = value;

                ResetCropRect();


                var scale = NMath.Min(_scrollView.Frame.Width / _imageSize.Width, _scrollView.Frame.Height / _imageSize.Height);
                var x = ImageCropRect.GetMinX() * scale + _scrollView.Frame.GetMinX();
                var y = ImageCropRect.GetMinY() * scale + _scrollView.Frame.GetMinY();
                var width = ImageCropRect.Width * scale;
                var height = ImageCropRect.Height * scale;


                var rect = new CGRect(x: x, y: y, width: width, height: height);
                var intersection = CGRect.Intersect(rect, _scrollView.Frame);


                if (intersection != default(CGRect))
                {
                    CropRect = intersection;
                }
            }
        }

        bool _resizeEnabled = true;
        public virtual bool ResizeEnabled
        {
            get => _resizeEnabled;
            set
            {
                _resizeEnabled = value;
                _cropRectView.EnableResizing(_resizeEnabled);
            }
        }

        bool _showCroppedArea = true;
        public virtual bool ShowCroppedArea
        {
            get => _showCroppedArea;
            set
            {
                _showCroppedArea = value;
                LayoutIfNeeded();
                _scrollView.ClipsToBounds = !_showCroppedArea;
                ShowOverlayView(_showCroppedArea);
            }
        }

        public virtual UIRotationGestureRecognizer RotationGestureRecognizer { get; set; }

        private CGSize _imageSize = new CGSize(width: 1.0, height: 1.0);
        private UIScrollView _scrollView;
        private UIView _zoomingView;
        private readonly CropRectView _cropRectView = new CropRectView();
        private readonly UIView _topOverlayView = new UIView();
        private readonly UIView _leftOverlayView = new UIView();
        private readonly UIView _rightOverlayView = new UIView();
        private readonly UIView _bottomOverlayView = new UIView();
        private CGRect _insetRect = CGRect.Empty;
        private CGRect _editingRect = CGRect.Empty;
        private UIInterfaceOrientation _interfaceOrientation = UIApplication.SharedApplication.StatusBarOrientation;
        private bool _resizing = false;
        private bool _usingCustomImageView = false;
        private readonly nfloat _marginTop = 37.0f;
        private readonly nfloat _marginLeft = 20.0f;

        public CropView(CGRect frame) : base(frame) => Initialize();

        public CropView(NSCoder coder) : base(coder) => Initialize();

        private void Initialize()
        {
            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            BackgroundColor = UIColor.Clear;


            _scrollView = new UIScrollView(Bounds);
            _scrollView.WeakDelegate = this;
            _scrollView.AutoresizingMask = UIViewAutoresizing.FlexibleTopMargin | UIViewAutoresizing.FlexibleLeftMargin | UIViewAutoresizing.FlexibleBottomMargin | UIViewAutoresizing.FlexibleRightMargin;
            _scrollView.BackgroundColor = UIColor.Clear;
            _scrollView.MaximumZoomScale = 20.0f;
            _scrollView.MinimumZoomScale = 1.0f;
            _scrollView.ShowsHorizontalScrollIndicator = false;
            _scrollView.ShowsVerticalScrollIndicator = false;
            _scrollView.Bounces = false;
            _scrollView.BouncesZoom = false;
            _scrollView.ClipsToBounds = false;
            AddSubview(_scrollView);


            RotationGestureRecognizer = new UIRotationGestureRecognizer((gr) => HandleRotation(gr))
            {
                WeakDelegate = this
            };
            _scrollView.AddGestureRecognizer(RotationGestureRecognizer);


            _cropRectView.CropRectViewDelegate = this;
            AddSubview(_cropRectView);


            ShowOverlayView(ShowCroppedArea);
            AddSubview(_topOverlayView);
            AddSubview(_leftOverlayView);
            AddSubview(_rightOverlayView);
            AddSubview(_bottomOverlayView);
        }

        public override UIView HitTest(CGPoint point, UIEvent uievent)
        {
            if (!UserInteractionEnabled)
            {
                return null;
            }

            var hitView = _cropRectView.HitTest(ConvertPointToView(point, _cropRectView), uievent);
            if (hitView != null)
            {
                return hitView;
            }
            var locationInImageView = ConvertPointToView(point, _zoomingView);
            var zoomedPoint = new CGPoint(x: locationInImageView.X * _scrollView.ZoomScale, y: locationInImageView.Y * _scrollView.ZoomScale);
            if (_zoomingView.Frame.Contains(zoomedPoint))
            {
                return _scrollView;
            }

            return base.HitTest(point, uievent);
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            var interfaceOrientation = UIApplication.SharedApplication.StatusBarOrientation;


            if (Image == null && ImageView == null)
            {
                return;
            }

            SetupEditingRect();

            if (ImageView == null)
            {
                if (interfaceOrientation.IsPortrait())
                {
                    _insetRect = Bounds.Inset(_marginLeft, _marginTop);
                }
                else
                {
                    _insetRect = Bounds.Inset(_marginLeft, _marginLeft);
                }
                if (!_showCroppedArea)
                {
                    _insetRect = _editingRect;
                }
                SetupZoomingView();
                SetupImageView();
            }
            else if (_usingCustomImageView)
            {
                if (interfaceOrientation.IsPortrait())
                {
                    _insetRect = Bounds.Inset(_marginLeft, _marginTop);
                }
                else
                {
                    _insetRect = Bounds.Inset(_marginLeft, _marginLeft);
                }
                if (!ShowCroppedArea)
                {
                    _insetRect = _editingRect;
                }
                SetupZoomingView();
                if (ImageView != null)
                    ImageView.Frame = _zoomingView.Bounds;
                _zoomingView?.AddSubview(ImageView);
                _usingCustomImageView = false;
            }

            if (!_resizing)
            {
                LayoutCropRectViewWithCropRect(_scrollView.Frame);
                if (this._interfaceOrientation != interfaceOrientation)
                {
                    ZoomToCropRect(_scrollView.Frame);
                }
            }


            this._interfaceOrientation = interfaceOrientation;
        }

        public virtual void SetRotationAngle(nfloat rotationAngle, bool snap)
        {
            var rotation = rotationAngle;
            if (snap)
            {
                rotation = NMath.Round(rotationAngle / new nfloat(NMath.PI / 2)) * new nfloat(NMath.PI / 2); //nearbyint
            }
            this.RotationAngle = rotation;
        }

        public virtual void ResetCropRect()
        {
            ResetCropRectAnimated(false);
        }

        public virtual void ResetCropRectAnimated(bool animated)
        {
            if (animated)
            {
                UIView.BeginAnimations(null);
                UIView.SetAnimationDuration(0.25);
                UIView.SetAnimationBeginsFromCurrentState(true);
            }
            if (ImageView != null)
                ImageView.Transform = CGAffineTransform.MakeIdentity();
            var contentSize = _scrollView.ContentSize;
            var initialRect = new CGRect(x: 0, y: 0, width: contentSize.Width, height: contentSize.Height);
            _scrollView.ZoomToRect(initialRect, false);


            LayoutCropRectViewWithCropRect(_scrollView.Bounds);


            if (animated)
            {
                UIView.CommitAnimations();
            }
        }

        public virtual CGRect ZoomedCropRect()
        {
            var cropRect = ConvertRectToView(_scrollView.Frame, _zoomingView);
            nfloat ratio = 1.0f;
            var orientation = UIApplication.SharedApplication.StatusBarOrientation;
            if (UIKit.UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad || orientation.IsPortrait())
            {
                ratio = _insetRect.WithAspectRatio(_imageSize).Width / _imageSize.Width;// AVMakeRect(aspectRatio: _imageSize, insideRect: _insetRect).width / _imageSize.Width;
            }
            else
            {
                ratio = _insetRect.WithAspectRatio(_imageSize).Height / _imageSize.Height;//AVMakeRect(aspectRatio: _imageSize, insideRect: _insetRect).height / _imageSize.Height;
            }

            var zoomedCropRect = new CGRect(x: cropRect.Location.X / ratio,
                                            y: cropRect.Location.Y / ratio,
                width: cropRect.Size.Width / ratio,
                                        height: cropRect.Size.Height / ratio);//origin -> location

            return zoomedCropRect;
        }

        public virtual UIImage GetCroppedImage(UIImage image)
        {
            _imageSize = image.Size;
            return image.RotatedImageWithTransform(Rotation, ZoomedCropRect());
        }

        private void HandleRotation(UIRotationGestureRecognizer gestureRecognizer)
        {
            if (ImageView != null)
            {
                var rotation = gestureRecognizer.Rotation;
                var transform = CGAffineTransform.Rotate(ImageView.Transform, rotation);//rotated
                ImageView.Transform = transform;
                gestureRecognizer.Rotation = 0.0f;
            }

            switch (gestureRecognizer.State)
            {
                case UIGestureRecognizerState.Began:
                case UIGestureRecognizerState.Changed:
                    _cropRectView.ShowsGridMinor = true;
                    break;
                default:
                    _cropRectView.ShowsGridMinor = false;
                    break;
            }
        }

        private void ShowOverlayView(bool show)
        {
            var color = show ? UIColor.FromWhiteAlpha(white: 0.0f, alpha: 0.4f) : UIColor.Clear;

            _topOverlayView.BackgroundColor = color;
            _leftOverlayView.BackgroundColor = color;
            _rightOverlayView.BackgroundColor = color;
            _bottomOverlayView.BackgroundColor = color;
        }

        private void SetupEditingRect()
        {
            var interfaceOrientation = UIApplication.SharedApplication.StatusBarOrientation;
            if (interfaceOrientation.IsPortrait())
            {
                _editingRect = Bounds.Inset(dx: _marginLeft, dy: _marginTop);
            }
            else
            {
                _editingRect = Bounds.Inset(dx: _marginLeft, dy: _marginLeft);
            }
            if (!ShowCroppedArea)
            {
                _editingRect = new CGRect(x: 0, y: 0, width: Bounds.Width, height: Bounds.Height);
            }
        }

        private void SetupZoomingView()
        {
            var cropRect = _insetRect.WithAspectRatio(_imageSize);


            _scrollView.Frame = cropRect;
            _scrollView.ContentSize = cropRect.Size;


            _zoomingView = new UIView(frame: _scrollView.Bounds)
            {
                BackgroundColor = UIColor.Clear
            };
            _scrollView.AddSubview(_zoomingView);
        }

        private void SetupImageView()
        {
            var imageView = new UIImageView(frame: _zoomingView.Bounds)
            {
                BackgroundColor = UIColor.Clear,
                ContentMode = UIViewContentMode.ScaleAspectFit,
                Image = Image
            };
            _zoomingView.AddSubview(imageView);
            this.ImageView = imageView;
            _usingCustomImageView = false;
        }

        private void LayoutCropRectViewWithCropRect(CGRect cropRect)
        {
            _cropRectView.Frame = cropRect;
            LayoutOverlayViewsWithCropRect(cropRect);
        }

        private void LayoutOverlayViewsWithCropRect(CGRect cropRect)
        {
            _topOverlayView.Frame = new CGRect(x: 0, y: 0, width: Bounds.Width, height: cropRect.GetMinY());
            _leftOverlayView.Frame = new CGRect(x: 0, y: cropRect.GetMinY(), width: cropRect.GetMinX(), height: cropRect.Height);
            _rightOverlayView.Frame = new CGRect(x: cropRect.GetMaxX(), y: cropRect.GetMinY(), width: Bounds.Width - cropRect.GetMaxX(), height: cropRect.Height);
            _bottomOverlayView.Frame = new CGRect(x: 0, y: cropRect.GetMaxY(), width: Bounds.Width, height: Bounds.Height - cropRect.GetMaxY());
        }

        private void ZoomToCropRect(CGRect toRect) => ZoomToCropRect(toRect, false, true);

        private void ZoomToCropRect(CGRect toRect, bool shouldCenter, bool animated, Action completion = null)
        {
            if (_scrollView.Frame.Equals(toRect))
            {
                return;
            }

            var width = toRect.Width;
            var height = toRect.Height;
            var scale = NMath.Min(_editingRect.Width / width, _editingRect.Height / height);


            var scaledWidth = width * scale;
            var scaledHeight = height * scale;
            var cropRect = new CGRect(x: (Bounds.Width - scaledWidth) / 2.0f, y: (Bounds.Height - scaledHeight) / 2.0f, width: scaledWidth, height: scaledHeight);


            var zoomRect = ConvertRectToView(toRect, _zoomingView);
            //zoomRect.Size.Width = cropRect.Width / (_scrollView.ZoomScale * scale);
            //zoomRect.Size.Height = cropRect.Height / (_scrollView.ZoomScale * scale);
            zoomRect.Size = new CGSize(cropRect.Width / (_scrollView.ZoomScale * scale), cropRect.Height / (_scrollView.ZoomScale * scale));

            if (ImageView != null && shouldCenter)
            {
                var imageViewBounds = ImageView.Bounds;
                //zoomRect.Location.X = (imageViewBounds.Width / 2.0f) - (zoomRect.Width / 2.0f);
                //zoomRect.Location.Y = (imageViewBounds.Height / 2.0f) - (zoomRect.Height / 2.0f);
                zoomRect.Location = new CGPoint((imageViewBounds.Width / 2.0f) - (zoomRect.Width / 2.0f), (imageViewBounds.Height / 2.0f) - (zoomRect.Height / 2.0f));
            }

            var duration = 0.0;
            if (animated)
            {
                duration = 0.25;
            }

            UIView.Animate(duration, 0.0, UIViewAnimationOptions.BeginFromCurrentState, () =>
            {
                this._scrollView.Bounds = cropRect;
                this._scrollView.ZoomToRect(zoomRect, false);
                this.LayoutCropRectViewWithCropRect(cropRect);
            },
            () => completion?.Invoke());

        }

        private CGRect CappedCropRectInImageRectWithCropRectView(CropRectView cropRectView)
        {
            var cropRect = cropRectView.Frame;


            var rect = ConvertRectToView(cropRect, _scrollView);
            if (rect.GetMinX() < _zoomingView.Frame.GetMinX())
            {
                //cropRect.Location.X = _scrollView.ConvertRectToView(_zoomingView.Frame, this).GetMinX();
                cropRect.Location = new CGPoint(_scrollView.ConvertRectToView(_zoomingView.Frame, this).GetMinX(), cropRect.Location.Y);
                var cappedWidth = rect.GetMaxX();
                var height = !KeepAspectRatio ? cropRect.Size.Height : cropRect.Size.Height * (cappedWidth / cropRect.Size.Width);
                cropRect.Size = new CGSize(width: cappedWidth, height: height);
            }

            if (rect.GetMinY() < _zoomingView.Frame.GetMinY())
            {
                //cropRect.Location.Y = _scrollView.ConvertRectToView(_zoomingView.Frame, this).GetMinY();
                cropRect.Location = new CGPoint(cropRect.X, _scrollView.ConvertRectToView(_zoomingView.Frame, this).GetMinY());
                var cappedHeight = rect.GetMaxY();
                var width = !KeepAspectRatio ? cropRect.Size.Width : cropRect.Size.Width * (cappedHeight / cropRect.Size.Height);
                cropRect.Size = new CGSize(width: width, height: cappedHeight);
            }

            if (rect.GetMaxX() > _zoomingView.Frame.GetMaxX())
            {
                var cappedWidth = _scrollView.ConvertRectToView(_zoomingView.Frame, this).GetMaxX() - cropRect.GetMinX();
                var height = !KeepAspectRatio ? cropRect.Size.Height : cropRect.Size.Height * (cappedWidth / cropRect.Size.Width);
                cropRect.Size = new CGSize(width: cappedWidth, height: height);
            }

            if (rect.GetMaxY() > _zoomingView.Frame.GetMaxY())
            {
                var cappedHeight = _scrollView.ConvertRectToView(_zoomingView.Frame, this).GetMaxY() - cropRect.GetMinY();
                var width = !KeepAspectRatio ? cropRect.Size.Width : cropRect.Size.Width * (cappedHeight / cropRect.Size.Height);
                cropRect.Size = new CGSize(width: width, height: cappedHeight);
            }

            return cropRect;
        }

        private void AutomaticZoomIfEdgeTouched(CGRect cropRect)
        {
            if (cropRect.GetMinX() < _editingRect.GetMinX() - 5.0f ||
                cropRect.GetMaxX() > _editingRect.GetMaxX() + 5.0f ||
                cropRect.GetMinY() < _editingRect.GetMinY() - 5.0f ||
                cropRect.GetMaxY() > _editingRect.GetMaxY() + 5.0f)

                UIView.Animate(1.0f, 0.0f, UIViewAnimationOptions.BeginFromCurrentState, () => this.ZoomToCropRect(this._cropRectView.Frame), null);
        }

        private void SetCropAspectRatio(nfloat ratio, bool shouldCenter)
        {
            var cropRect = _scrollView.Frame;
            var width = cropRect.Width;
            var height = cropRect.Height;
            if (ratio <= 1.0)
            {
                width = height * ratio;
                if (width > ImageView.Bounds.Width)
                {
                    width = cropRect.Width;
                    height = width / ratio;
                }
            }
            else
            {
                height = width / ratio;
                if (height > ImageView.Bounds.Height)
                {
                    height = cropRect.Height;
                    width = height * ratio;
                }
            }
            cropRect.Size = new CGSize(width: width, height: height);
            ZoomToCropRect(cropRect, shouldCenter: shouldCenter, animated: false, completion: () =>
            {
                var scale = this._scrollView.ZoomScale;
                this._scrollView.MinimumZoomScale = scale;
            });
        }

        #region CropView delegate methods

        public void CropRectViewDidBeginEditing(CropRectView view)
        {
            _resizing = true;
        }

        public void CropRectViewDidChange(CropRectView view)
        {
            var cropRect = CappedCropRectInImageRectWithCropRectView(view);
            LayoutCropRectViewWithCropRect(cropRect);
            AutomaticZoomIfEdgeTouched(cropRect);
        }

        public void CropRectViewDidEndEditing(CropRectView view)
        {
            _resizing = false;
            ZoomToCropRect(_cropRectView.Frame);
        }

        #endregion

        #region ScrollView delegate methods

        [Export("viewForZoomingInScrollView:")]
        public virtual UIView ViewForZoomingInScrollView(UIScrollView scrollView) 
        {
            return _zoomingView;
        }

        [Export("scrollViewWillEndDragging:withVelocity:targetContentOffset:")]
        public virtual void WillEndDragging(UIScrollView scrollView, CGPoint velocity, ref CGPoint targetContentOffset)
        {
            var contentOffset = scrollView.ContentOffset;
            targetContentOffset/*.pointee*/ = contentOffset; //TODO: ?
        }

        #endregion

        #region Gesture Recognizer delegate methods

        [Export("gestureRecognizer:shouldRecognizeSimultaneouslyWithGestureRecognizer:")]
        public virtual bool ShouldRecognizeSimultaneously(UIGestureRecognizer gestureRecognizer, UIGestureRecognizer otherGestureRecognizer)
        {
            return true;
        }

        #endregion
    }
}
