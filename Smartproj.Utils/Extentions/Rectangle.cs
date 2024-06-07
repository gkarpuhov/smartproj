using System;
using System.Drawing;

namespace Smartproj.Utils
{
    public static class Rect
    {
        public static Rectangle ToRectangle(this RectangleF _rect)
        {
            return new Rectangle(_rect.X.ToInt(), _rect.Y.ToInt(), _rect.Width.ToInt(), _rect.Height.ToInt());
        }
        public static ValueTuple<Rectangle, float> FitToFrame(this Rectangle _frame, Size _image, int _shift = 0)
        {
            return FitToFrame(_frame, _image.Width, _image.Height, _shift);
        }
        public static ValueTuple<Rectangle, float> FitToFrame(this Rectangle _frame, int _imageW, int _imageH, int _shift = 0)
        {
            // Возврат:
            // 1-е значение: реальное расположение изображения в миллиметрах
            // 2-е значение: эффективное разрешение в точках на миллиметр

            //float kHor = _imageW / _imageH;
            int X;
            int Y;

            if ((float)_imageW / (float)_imageH > (float)_frame.Width / (float)_frame.Height)
            {
                X = _frame.X - _shift - ((_frame.Height * _imageW) / _imageH - _frame.Width) / 2;
                Y = _frame.Y;
                return new ValueTuple<Rectangle, float>(new Rectangle(X, Y, (_frame.Height * _imageW) / _imageH, _frame.Height), (float)_imageH / (float)_frame.Height);
            }
            else
            {
                X = _frame.X - _shift;
                Y = _frame.Y - ((_frame.Width * _imageH) / _imageW - _frame.Height) / 2;
                return new ValueTuple<Rectangle, float>(new Rectangle(X, Y, _frame.Width, (_frame.Width * _imageH) / _imageW), (float)_imageW / (float)_frame.Width);
            }
        }
        public static ValueTuple<RectangleF, float> FitToFrameF(this RectangleF _frame, float _imageW, float _imageH)
        {
            // Возврат:
            // 1-е значение: реальное расположение изображения в миллиметрах
            // 2-е значение: эффективное разрешение в точках на миллиметр

            float X;
            float Y;

            if (_imageW / _imageH > _frame.Width / _frame.Height)
            {
                X = _frame.X - ((_frame.Height * _imageW) / _imageH - _frame.Width) / 2;
                Y = _frame.Y;
                return new ValueTuple<RectangleF, float>(new RectangleF(X, Y, (_frame.Height * _imageW) / _imageH, _frame.Height), _imageH / _frame.Height);
            }
            else
            {
                X = _frame.X;
                Y = _frame.Y - ((_frame.Width * _imageH) / _imageW - _frame.Height) / 2;
                return new ValueTuple<RectangleF, float>(new RectangleF(X, Y, _frame.Width, (_frame.Width * _imageH) / _imageW), _imageW / _frame.Width);
            }
        }
    }
}
