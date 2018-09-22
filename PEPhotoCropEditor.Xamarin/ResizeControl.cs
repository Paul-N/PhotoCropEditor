using System;
using CoreGraphics;
using Foundation;
using UIKit;

namespace PEPhotoCropEditor
{
    public interface IResizeControlDelegate
    {
        void ResizeControlDidBeginResizing(ResizeControl control);
        void ResizeControlDidResize(ResizeControl control);
        void ResizeControlDidEndResizing(ResizeControl control);
    }

    public class ResizeControl : UIView
    {
        //weak var delegate: ResizeControlDelegate?
        public IResizeControlDelegate ResizeControlDelegate { get; set; }
        internal CGPoint Translation { get; set; } = CGPoint.Empty;
        public bool Enabled { get; set; } = true;
        private CGPoint _startPoint = CGPoint.Empty;

        public ResizeControl() : base(new CGRect(x: 0, y: 0, width: 44.0, height: 44.0))
        {

            Initialize();
        }

        public ResizeControl(CGRect frame) : base(new CGRect(x: frame.X, y: frame.Y, width: 44.0, height: 44.0))
        {

            Initialize();
        }

        public ResizeControl(NSCoder coder) : base(new CGRect(x: 0, y: 0, width: 44.0, height: 44.0))
        {
            Initialize();
        }

        private void Initialize()
        {
            BackgroundColor = UIColor.Clear;
            ExclusiveTouch = true;


            var gestureRecognizer = new UIPanGestureRecognizer((gr) => HandlePan(gr));
            AddGestureRecognizer(gestureRecognizer);
        }

        void HandlePan(UIPanGestureRecognizer gestureRecognizer)
        {
            if (!Enabled)
            {
                return;
            }

            var translation = gestureRecognizer.TranslationInView(Superview);

            switch (gestureRecognizer.State)
            {
                case UIGestureRecognizerState.Began:
                    //var translation = gestureRecognizer.TranslationInView(Superview);
                    _startPoint = new CGPoint(x: NMath.Round(translation.X), y: NMath.Round(translation.Y));
                    ResizeControlDelegate?.ResizeControlDidBeginResizing(this);
                    break;
                case UIGestureRecognizerState.Changed:
                    //var translation = gestureRecognizer.TranslationInView(Superview);
                    this.Translation = new CGPoint(x: NMath.Round(_startPoint.X + translation.X), y: NMath.Round(_startPoint.Y + translation.Y));
                    ResizeControlDelegate?.ResizeControlDidResize(this);
                    break;
                case UIGestureRecognizerState.Ended:
                case UIGestureRecognizerState.Cancelled:
                    ResizeControlDelegate?.ResizeControlDidEndResizing(this);
                    break;
            }

        }
    }
}
