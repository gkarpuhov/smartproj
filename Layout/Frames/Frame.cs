using Smartproj.Utils;
using System;
using System.Drawing;

namespace Smartproj
{
    public class PageFrameCollection : FrameCollection
    {
        private int mFramesCount;
        private int mCurrentFrame;
        public PageFrameCollection(int _w, int _h, Template _owner) : base(_w, _h, _owner)
        {
            mFramesCount = 0;
            mCurrentFrame = -1;
        }
        protected PageFrameCollection(Template _owner) : base(_owner)
        {
            mFramesCount = 0;
            mCurrentFrame = -1;
        }
        public override int FramesCount => mFramesCount;
        public bool Add(float _x, float _y, float _w, float _h)
        {
            return Add(new RectangleF(_x, _y, _w, _h));
        }
        public override bool Add(RectangleF _rect)
        {
            var sideFrames = this[0];

            mCurrentFrame++;

            if (mCurrentFrame >= sideFrames.Length)
            {
                return false;
            }

            int i = mCurrentFrame / sideFrames.GetLength(1);
            int j = mCurrentFrame % sideFrames.GetLength(1);

            sideFrames[i, j] = _rect;
            if (_rect != default) mFramesCount++;

            mImageFrameForPinnedTextHelperRef = Owner.onFrameAdded(_rect, PageSide.Single, new Point(i, j), mPinnedTextHelperRef);

            return true;
        }
        public override void Clear()
        {
            base.Clear();
            mFramesCount = 0;
            mCurrentFrame = -1;
        }
        public override void SetSide(PageSide _side)
        {
            throw new NotImplementedException();
        }
    }
    public class SpreadFrameCollection : FrameCollection
    {
        private int mFramesCount;
        private int mCurrentFrame;
        private PageSide mCurrentSide;
        public PageSide CurrentSide => mCurrentSide;
        public SpreadFrameCollection(int _leftW, int _leftH, int _rightW, int _rightH, Template _owner) : base(_leftW, _leftH, _rightW, _rightH, _owner)
        {
            mFramesCount = 0;
            mCurrentSide = PageSide.Left;
            mCurrentFrame = -1;
        }
        protected SpreadFrameCollection(Template _owner) : base(_owner)
        {
            mFramesCount = 0;
            mCurrentSide = PageSide.Left;
            mCurrentFrame = -1;
        }
        public override int FramesCount => mFramesCount;

        public bool Add(float _x, float _y, float _w, float _h)
        {
            return Add(new RectangleF(_x, _y, _w, _h));
        }
        public override bool Add(RectangleF _rect)
        {
            var sideFrames = this[mCurrentSide];

            if (mCurrentSide == PageSide.Left && mCurrentFrame >= sideFrames.Length - 1)
            {
                mCurrentSide = PageSide.Right;
                sideFrames = this[mCurrentSide];
                mCurrentFrame = -1;
            }

            mCurrentFrame++;

            if (mCurrentSide == PageSide.Right && mCurrentFrame >= sideFrames.Length)
            {
                return false;
            }

            int i = mCurrentFrame / sideFrames.GetLength(1);
            int j = mCurrentFrame % sideFrames.GetLength(1);
            sideFrames[i, j] = _rect;

            if (_rect != default) mFramesCount++;

            mImageFrameForPinnedTextHelperRef = Owner.onFrameAdded(_rect, mCurrentSide, new Point(i, j), mPinnedTextHelperRef);

            return true;
        }
        public override void Clear()
        {
            base.Clear();
            mFramesCount = 0;
            mCurrentSide = PageSide.Left;
            mCurrentFrame = -1;
        }
        public override void SetSide(PageSide _side)
        {
            mCurrentSide = _side;
            mCurrentFrame = -1;
        }
    }
}
