using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Rendering.Calendar {
    public class ScrollableElement : FrameworkElement, IScrollInfo {

        #region DependencyProperty
        public Size ItemSize {
            get { return (Size)GetValue(ItemSizeProperty); }
            set { SetValue(ItemSizeProperty, value); }
        }

        public static readonly DependencyProperty ItemSizeProperty =
            DependencyProperty.Register("ItemSize", typeof(Size), typeof(ScrollableElement), new PropertyMetadata(new Size(100, 30)));



        public double HorizontalOffset {
            get { return (double)GetValue(HorizontalOffsetProperty); }
            private set { SetValue(HorizontalOffsetProperty, value); }
        }

        public static readonly DependencyProperty HorizontalOffsetProperty =
            DependencyProperty.Register("HorizontalOffset", typeof(double), typeof(ScrollableElement), new PropertyMetadata(0.0));


        public double VerticalOffset {
            get { return (double)GetValue(VerticalOffsetProperty); }
            private set { SetValue(VerticalOffsetProperty, value); }
        }

        public static readonly DependencyProperty VerticalOffsetProperty =
            DependencyProperty.Register("VerticalOffset", typeof(double), typeof(ScrollableElement), new PropertyMetadata(0.0));


        #endregion

        #region IScrollInfo
        public bool CanVerticallyScroll { get; set; }
        public bool CanHorizontallyScroll { get; set; }

        public double ExtentWidth { get; private set; }

        public double ExtentHeight { get; private set; }

        public double ViewportWidth { get; private set; }

        public double ViewportHeight { get; private set; }

        public ScrollViewer ScrollOwner { get; set; }

        public virtual void LineDown() {
            this.SetVerticalOffset(this.VerticalOffset + this.ItemSize.Height);
        }

        public virtual void LineLeft() {
            this.SetHorizontalOffset(this.HorizontalOffset - this.ItemSize.Width);
        }

        public virtual void LineRight() {
            this.SetHorizontalOffset(this.HorizontalOffset + this.ItemSize.Width);
        }

        public virtual void LineUp() {
            this.SetVerticalOffset(this.VerticalOffset - this.ItemSize.Height);
        }

        public Rect MakeVisible(Visual visual, Rect rectangle) => rectangle;

        public void MouseWheelDown() {
            this.SetVerticalOffset(this.VerticalOffset + (this.ItemSize.Height * 3));
        }

        public void MouseWheelLeft() {
            this.SetHorizontalOffset(this.HorizontalOffset - (this.ItemSize.Width * 3));
        }

        public void MouseWheelRight() {
            this.SetHorizontalOffset(this.HorizontalOffset - (this.ItemSize.Width * 3));
        }

        public void MouseWheelUp() {
            this.SetVerticalOffset(this.VerticalOffset - (this.ItemSize.Height * 3));
        }

        public void PageDown() {
            this.SetVerticalOffset(this.VerticalOffset + this.ViewportHeight);
        }

        public void PageLeft() {
            this.SetHorizontalOffset(this.HorizontalOffset - this.ViewportWidth);
        }

        public void PageRight() {
            this.SetHorizontalOffset(this.HorizontalOffset + this.ViewportWidth);
        }

        public void PageUp() {
            this.SetVerticalOffset(this.VerticalOffset - this.ViewportHeight);
        }

        public void SetHorizontalOffset(double offset) {
            if (offset < 0 || this.ExtentWidth <= this.ViewportWidth)
                offset = 0;
            else if (offset >= this.ExtentWidth - this.ViewportWidth)
                offset = this.ExtentWidth - this.ViewportWidth;
            else if (offset % this.ItemSize.Width != 0)
                offset = (int)(offset / this.ItemSize.Width) * this.ItemSize.Width;

            var last = HorizontalOffset;
            this.HorizontalOffset = offset;

            this.UpdateScrollOwner();

            if (offset != last)
                this.OnScroll(offset, last, true);
        }

        public void SetVerticalOffset(double offset) {
            if (offset < 0 || this.ExtentHeight <= this.ViewportHeight)
                offset = 0;
            else if (offset >= this.ExtentHeight - this.ViewportHeight)
                offset = this.ExtentHeight - this.ViewportHeight;
            else if (offset % this.ItemSize.Height != 0)
                offset = (int)(offset / this.ItemSize.Height) * this.ItemSize.Height;

            var last = this.VerticalOffset;
            this.VerticalOffset = offset;

            this.UpdateScrollOwner();

            if (offset != last)
                this.OnScroll(offset, last, false);
        }
        #endregion

        #region Overrides
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
            base.OnRenderSizeChanged(sizeInfo);

            if (sizeInfo.WidthChanged)
                this.SetHorizontalOffset(this.HorizontalOffset);

            if (sizeInfo.HeightChanged)
                this.SetVerticalOffset(this.VerticalOffset);

            SetScroolSizeArea(sizeInfo.NewSize);
        }
        #endregion

        #region Helps
        protected void SetScroolSizeArea(Size contentSize) {

            this.ViewportWidth = contentSize.Width;
            this.ViewportHeight = contentSize.Height;

            this.UpdateScrollOwner();
            this.OnScroll(0, 0);
        }

        protected void SetScrollExtent(double extentWidth, double extentHeight, double actualWidth, double actualHeight) {

            this.ExtentWidth = extentWidth;
            this.ExtentHeight = extentHeight;
            this.ViewportWidth = this.ActualWidth;
            this.ViewportHeight = this.ActualHeight;

            this.UpdateScrollOwner();
            this.OnScroll(0, 0);
        }

        protected virtual void OnScroll(double newValue, double lastValue, bool? horizontal = null) { }

        private void UpdateScrollOwner() {
            this.CanHorizontallyScroll = this.ViewportWidth < this.ExtentWidth;
            this.CanVerticallyScroll = this.ViewportHeight < this.ExtentHeight;

            if (this.ScrollOwner != null)
                this.ScrollOwner.InvalidateScrollInfo();
        }
        #endregion
    }
}
