using System;
using System.Linq;
using CoreGraphics;
using Foundation;
using UIKit;

namespace PEPhotoCropEditor
{
    public interface ICropRectViewDelegate
    {
        void CropRectViewDidBeginEditing(CropRectView view);
        void CropRectViewDidChange(CropRectView view);
        void CropRectViewDidEndEditing(CropRectView view);
    }

    public class CropRectView : UIView, IResizeControlDelegate
    {


        //weak var delegate: CropRectViewDelegate?
        public ICropRectViewDelegate CropRectViewDelegate { get; set; }

        private bool _showsGridMajor = true;
        public bool ShowsGridMajor
        {
            get => _showsGridMajor;
            set
            {
                _showsGridMajor = value;
                SetNeedsLayout();
            }
        }

        private bool _showsGridMinor = false;

        public bool ShowsGridMinor
        {
            get => _showsGridMinor;
            set
            {
                _showsGridMinor = value;
                SetNeedsLayout();
            }
        }

        bool _keepAspectRatio = false;
        public bool KeepAspectRatio
        {
            get => _keepAspectRatio;
            set
            {
                _keepAspectRatio = value;
                if (_keepAspectRatio)
                {
                    var width = Bounds.Width;
                    var height = Bounds.Height;
                    _fixedAspectRatio = NMath.Min(width / height, height / width);
                }
            }
        }

        private UIImageView _resizeImageView;
        private readonly ResizeControl _topLeftCornerView = new ResizeControl();
        private readonly ResizeControl _topRightCornerView = new ResizeControl();
        private readonly ResizeControl _bottomLeftCornerView = new ResizeControl();
        private readonly ResizeControl _bottomRightCornerView = new ResizeControl();
        private readonly ResizeControl _topEdgeView = new ResizeControl();
        private readonly ResizeControl _leftEdgeView = new ResizeControl();
        private readonly ResizeControl _rightEdgeView = new ResizeControl();
        private readonly ResizeControl _bottomEdgeView = new ResizeControl();
        private CGRect _initialRect = CGRect.Empty;
        private nfloat _fixedAspectRatio = 0.0f;

        public CropRectView(CGRect rect) : base(rect)
        {
            Initialize();
        }

        public CropRectView(NSCoder coder) : base(coder)
        {
            Initialize();
        }

        public CropRectView() : base()
        {
            Initialize();
        }

        private void Initialize()
        {
            BackgroundColor = UIColor.Clear;
            ContentMode = UIViewContentMode.Redraw;


            _resizeImageView = new UIImageView(frame: Bounds.Inset(dx: -2.0f, dy: -2.0f))
            {
                AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight
            };
            //var bundle = new Bundle(for: type(of: self)) ;
            var image = UIImage.FromBundle("PhotoCropEditorBorder", NSBundle.FromClass(this.Class), null);
            _resizeImageView.Image = image?.CreateResizableImage(new UIEdgeInsets(top: 23.0f, left: 23.0f, bottom: 23.0f, right: 23.0f));
            AddSubview(_resizeImageView);


            _topEdgeView.ResizeControlDelegate = this;
            AddSubview(_topEdgeView);
            _leftEdgeView.ResizeControlDelegate = this;
            AddSubview(_leftEdgeView);
            _rightEdgeView.ResizeControlDelegate = this;
            AddSubview(_rightEdgeView);
            _bottomEdgeView.ResizeControlDelegate = this;
            AddSubview(_bottomEdgeView);


            _topLeftCornerView.ResizeControlDelegate = this;
            AddSubview(_topLeftCornerView);
            _topRightCornerView.ResizeControlDelegate = this;
            AddSubview(_topRightCornerView);
            _bottomLeftCornerView.ResizeControlDelegate = this;
            AddSubview(_bottomLeftCornerView);
            _bottomRightCornerView.ResizeControlDelegate = this;
            AddSubview(_bottomRightCornerView);
        }

        public override UIView HitTest(CGPoint point, UIEvent uievent)
        {
            foreach (var subview in Subviews.Where(s => s is ResizeControl))
            {
                if (subview.Frame.Contains(point))
                {
                    return subview;
                }
            }
            return null;
        }

        public override void Draw(CGRect rect)
        {
            base.Draw(rect);

            var width = Bounds.Width;
            var height = Bounds.Height;


            for (int i = 0; i < 3; i++)
            {
                nfloat borderPadding = 0.5f;


                if (ShowsGridMinor)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        new UIColor(red: 1.0f, green: 1.0f, blue: 0.0f, alpha: 0.3f).SetColor();
                        UIGraphics.RectFill(new CGRect(x: NMath.Round((width / 9.0f) * new nfloat(j) + (width / 3.0f) * new nfloat(i)), y: borderPadding, width: 1.0f, height: NMath.Round(height) - borderPadding * 2.0f));
                        UIGraphics.RectFill(new CGRect(x: borderPadding, y: NMath.Round((height / 9.0f) * new nfloat(j) + (height / 3.0f) * new nfloat(i)), width: NMath.Round(width) - borderPadding * 2.0f, height: 1.0f));
                    }
                }

                if (ShowsGridMajor)
                {
                    if (i > 0)
                    {
                        UIColor.White.SetFill();
                        UIGraphics.RectFill(new CGRect(x: NMath.Round(new nfloat(i) * width / 3.0f), y: borderPadding, width: 1.0f, height: NMath.Round(height) - borderPadding * 2.0f));
                        UIGraphics.RectFill(new CGRect(x: borderPadding, y: NMath.Round(new nfloat(i) * height / 3.0f), width: NMath.Round(width) - borderPadding * 2.0f, height: 1.0f));
                    }
                }
            }
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            _topLeftCornerView.Frame.Offset(new CGPoint(x: _topLeftCornerView.Bounds.Width / -2.0, y: _topLeftCornerView.Bounds.Height / -2.0));
            _topRightCornerView.Frame.Offset(new CGPoint(x: Bounds.Width - _topRightCornerView.Bounds.Width - 2.0, y: _topRightCornerView.Bounds.Height / -2.0));
            _bottomLeftCornerView.Frame.Offset(new CGPoint(x: _bottomLeftCornerView.Bounds.Width / -2.0, y: Bounds.Height - _bottomLeftCornerView.Bounds.Height / 2.0));
            _bottomRightCornerView.Frame.Offset(new CGPoint(x: Bounds.Width - _bottomRightCornerView.Bounds.Width / 2.0, y: Bounds.Height - _bottomRightCornerView.Bounds.Height / 2.0));


            _topEdgeView.Frame = new CGRect(x: _topLeftCornerView.Frame.GetMaxX(), y: _topEdgeView.Frame.Height / -2.0f, width: _topRightCornerView.Frame.GetMinX() - _topLeftCornerView.Frame.GetMaxX(), height: _topEdgeView.Bounds.Height);
            _leftEdgeView.Frame = new CGRect(x: _leftEdgeView.Frame.Width / -2.0f, y: _topLeftCornerView.Frame.GetMaxY(), width: _leftEdgeView.Frame.Width, height: _bottomLeftCornerView.Frame.GetMinY() - _topLeftCornerView.Frame.GetMaxY());
            _bottomEdgeView.Frame = new CGRect(x: _bottomLeftCornerView.Frame.GetMaxX(), y: _bottomLeftCornerView.Frame.GetMinY(), width: _bottomRightCornerView.Frame.GetMinX() - _bottomLeftCornerView.Frame.GetMaxX(), height: _bottomEdgeView.Frame.Height);
            _rightEdgeView.Frame = new CGRect(x: Bounds.Width - _rightEdgeView.Frame.Width / 2.0f, y: _topRightCornerView.Frame.GetMaxY(), width: _rightEdgeView.Frame.Width, height: _bottomRightCornerView.Frame.GetMinY() - _topRightCornerView.Frame.GetMaxY());
        }

        internal void EnableResizing(bool enabled)
        {
            _resizeImageView.Hidden = !enabled;


            _topLeftCornerView.Enabled = enabled;
            _topRightCornerView.Enabled = enabled;
            _bottomLeftCornerView.Enabled = enabled;
            _bottomRightCornerView.Enabled = enabled;


            _topEdgeView.Enabled = enabled;
            _leftEdgeView.Enabled = enabled;
            _bottomEdgeView.Enabled = enabled;
            _rightEdgeView.Enabled = enabled;
        }

        public void ResizeControlDidBeginResizing(ResizeControl control)
        {
            _initialRect = Frame;
            CropRectViewDelegate?.CropRectViewDidBeginEditing(this);
        }

        public void ResizeControlDidResize(ResizeControl control)
        {
            Frame = cropRectWithResizeControlView(control);
            CropRectViewDelegate?.CropRectViewDidChange(this);
        }

        public void ResizeControlDidEndResizing(ResizeControl control)
        {
            CropRectViewDelegate?.CropRectViewDidEndEditing(this);
        }

        private CGRect cropRectWithResizeControlView(ResizeControl resizeControl)
        {
            var rect = Frame;

            if (resizeControl == _topEdgeView)
            {
                rect = new CGRect(x: _initialRect.GetMinX(),
                                  y: _initialRect.GetMinY() + resizeControl.Translation.Y,
                          width: _initialRect.Width,
                                  height: _initialRect.Height - resizeControl.Translation.Y);

                if (KeepAspectRatio)
                {
                    rect = ConstrainedRectWithRectBasisOfHeight(rect);
                }
            }
            else if (resizeControl == _leftEdgeView)
            {
                rect = new CGRect(x: _initialRect.GetMinX() + resizeControl.Translation.X,
                                  y: _initialRect.GetMinY(),
                              width: _initialRect.Width - resizeControl.Translation.X,
                                  height: _initialRect.Height);

                if (KeepAspectRatio)
                {
                    rect = ConstrainedRectWithRectBasisOfWidth(rect);
                }
            }
            else if (resizeControl == _bottomEdgeView)
            {
                rect = new CGRect(x: _initialRect.GetMinX(),
                                  y: _initialRect.GetMinY(),
                          width: _initialRect.Width,
                                  height: _initialRect.Height + resizeControl.Translation.Y);

                if (KeepAspectRatio)
                {
                    rect = ConstrainedRectWithRectBasisOfHeight(rect);
                }
            }
            else if (resizeControl == _rightEdgeView)
            {
                rect = new CGRect(x: _initialRect.GetMinX(),
                                  y: _initialRect.GetMinY(),
                          width: _initialRect.Width + resizeControl.Translation.X,
                                  height: _initialRect.Height);

                if (KeepAspectRatio)
                {
                    rect = ConstrainedRectWithRectBasisOfWidth(rect);
                }
            }
            else if (resizeControl == _topLeftCornerView)
            {
                rect = new CGRect(x: _initialRect.GetMinX() + resizeControl.Translation.X,
                                  y: _initialRect.GetMinY() + resizeControl.Translation.Y,
                          width: _initialRect.Width - resizeControl.Translation.X,
                                  height: _initialRect.Height - resizeControl.Translation.Y);

                if (KeepAspectRatio)
                {
                    CGRect constrainedFrame;
                    if (NMath.Abs(resizeControl.Translation.X) < NMath.Abs(resizeControl.Translation.Y))
                    {
                        constrainedFrame = ConstrainedRectWithRectBasisOfHeight(rect);
                    }
                    else
                    {
                        constrainedFrame = ConstrainedRectWithRectBasisOfWidth(rect);
                    }
                    //constrainedFrame.Location.X -= constrainedFrame.Width - rect.Width;
                    //constrainedFrame.Location.Y -= constrainedFrame.Height - rect.Height;
                    constrainedFrame.Location = new CGPoint(constrainedFrame.Location.X - (constrainedFrame.Width - rect.Width),
                                                            constrainedFrame.Location.Y - (constrainedFrame.Height - rect.Height));
                    rect = constrainedFrame;
                }
            }
            else if (resizeControl == _topRightCornerView)
            {
                rect = new CGRect(x: _initialRect.GetMinX(),
                                  y: _initialRect.GetMinY() + resizeControl.Translation.Y,
                          width: _initialRect.Width + resizeControl.Translation.X,
                                  height: _initialRect.Height - resizeControl.Translation.Y);

                if (KeepAspectRatio)
                {
                    if (NMath.Abs(resizeControl.Translation.X) < NMath.Abs(resizeControl.Translation.Y))
                    {
                        rect = ConstrainedRectWithRectBasisOfHeight(rect);
                    }
                    else
                    {
                        rect = ConstrainedRectWithRectBasisOfWidth(rect);
                    }
                }
            }
            else if (resizeControl == _bottomLeftCornerView)
            {
                rect = new CGRect(x: _initialRect.GetMinX() + resizeControl.Translation.X,
                              y: _initialRect.GetMinY(),
                          width: _initialRect.Width - resizeControl.Translation.X,
                                  height: _initialRect.Height + resizeControl.Translation.Y);

                if (KeepAspectRatio)
                {
                    CGRect constrainedFrame;
                    if (NMath.Abs(resizeControl.Translation.X) < NMath.Abs(resizeControl.Translation.Y))
                    {
                        constrainedFrame = ConstrainedRectWithRectBasisOfHeight(rect);
                    }
                    else
                    {
                        constrainedFrame = ConstrainedRectWithRectBasisOfWidth(rect);
                    }
                    //constrainedFrame.Location.X -= constrainedFrame.Width - rect.Width;
                    constrainedFrame.Location = new CGPoint(constrainedFrame.Location.X - (constrainedFrame.Width - rect.Width), constrainedFrame.Location.Y);
                    rect = constrainedFrame;
                }
            }
            else if (resizeControl == _bottomRightCornerView)
            {
                rect = new CGRect(x: _initialRect.GetMinX(),
                              y: _initialRect.GetMinY(),
                              width: _initialRect.Width + resizeControl.Translation.X,
                              height: _initialRect.Height + resizeControl.Translation.Y);

                if (KeepAspectRatio)
                {
                    if (NMath.Abs(resizeControl.Translation.X) < NMath.Abs(resizeControl.Translation.Y))
                    {
                        rect = ConstrainedRectWithRectBasisOfHeight(rect);
                    }
                    else
                    {
                        rect = ConstrainedRectWithRectBasisOfWidth(rect);
                    }
                }
            }

            var minWidth = _leftEdgeView.Bounds.Width + _rightEdgeView.Bounds.Width;
            if (rect.Width < minWidth)
            {
                //rect.Location.X = Frame.GetMaxX() - minWidth;
                rect.Location = new CGPoint(Frame.GetMaxX() - minWidth, rect.Location.Y);
                //rect.Size.Width = minWidth;
                rect.Size = new CGSize(minWidth, rect.Size.Height);
            }

            var minHeight = _topEdgeView.Bounds.Height + _bottomEdgeView.Bounds.Height;
            if (rect.Height < minHeight)
            {
                //rect.Location.Y = Frame.GetMaxY() - minHeight;
                rect.Location = new CGPoint(rect.Location.X, Frame.GetMaxY() - minHeight);
                //rect.Size.Height = minHeight;
                rect.Size = new CGSize(rect.Size.Width, minHeight);
            }

            if (_fixedAspectRatio > 0)
            {
                var constraintedFrame = rect;
                if (rect.Width < minWidth)
                {
                    //constraintedFrame.Size.Width = rect.Size.Height * (minWidth / rect.Size.Width);
                    constraintedFrame.Size = new CGSize(rect.Size.Height * (minWidth / rect.Size.Width), constraintedFrame.Height);
                }
                if (rect.Height < minHeight)
                {
                    //constraintedFrame.Size.Height = rect.Size.Width * (minHeight / rect.Size.Height);
                    constraintedFrame.Size = new CGSize(constraintedFrame.Size.Width, rect.Size.Width * (minHeight / rect.Size.Height));
                }
                rect = constraintedFrame;
            }

            return rect;
        }

        private CGRect ConstrainedRectWithRectBasisOfWidth(CGRect frame)
        {
            var result = frame;
            var width = frame.Width;
            var height = frame.Height;

            if (width < height)
            {
                height = width / _fixedAspectRatio;
            }
            else
            {
                height = width * _fixedAspectRatio;
            }
            result.Size = new CGSize(width: width, height: height);
            return result;
        }

        private CGRect ConstrainedRectWithRectBasisOfHeight(CGRect frame)
        {
            var result = frame;
            var width = frame.Width;
            var height = frame.Height;

            if (width < height)
            {
                width = height * _fixedAspectRatio;
            }
            else
            {
                width = height / _fixedAspectRatio;
            }
            result.Size = new CGSize(width: width, height: height);
            return result;
        }
    }
}
