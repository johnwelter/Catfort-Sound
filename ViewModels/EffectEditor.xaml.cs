using Avalonia.Controls.Platform;
using Avalonia.Input;
using CatfortSound.SoundEngine.Banks;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CatfortSound.ViewModels
{
    /// <summary>
    /// Interaction logic for EffectEditor.xaml
    /// </summary>
    public partial class EffectEditor : UserControl, INotifyPropertyChanged
    {

        public static readonly DependencyProperty EffectObjectProperty = DependencyProperty.Register("EffectObject", typeof(EffectData),  typeof(EffectEditor), new PropertyMetadata(null, OnDataChangedCallback));

        public EffectData EffectObject
        {
            get { return (EffectData)GetValue(EffectObjectProperty); }
            set { SetValue(EffectObjectProperty, value); }
        }
        
        const int width = 256;
        const int height = 16;
        const int centHeight = 33;
        private WriteableBitmap writable = new WriteableBitmap(width, height, 96, 96, PixelFormats.Rgb24, BitmapPalettes.WebPalette);
        private WriteableBitmap centeredWritable = new WriteableBitmap(width, centHeight, 96, 96, PixelFormats.Rgb24, BitmapPalettes.WebPalette);
        Int32Rect colChunk = new(0, 0, 1, 16);
        Int32Rect cenColChunk = new(0, 0, 1, 33);
        bool mouseHeld = false;

        bool wasCentered = false;

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnDataChanged()
        {
            OnPropertyChanged(nameof(EffectData));
        }
        private static void OnDataChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            EffectEditor editor = sender as EffectEditor;
            if (editor != null)
            {
                editor.OnDataChanged();
                bool swapImage = editor.wasCentered != editor.EffectObject.Centered; 
                editor.RedrawImage(swapImage);
                editor.wasCentered = editor.EffectObject.Centered;
            }
        }
        public EffectEditor()
        {
            InitializeComponent();
            EffectImage.Source = writable;
        }

        private void RedrawImage(bool swapImage)
        {
            
            if(swapImage)
            {
                EffectImage.Source = EffectObject.Centered? centeredWritable : writable; 
            }
            //data has changed, redraw the image
            for(int i = 0; i < width; i++)
            {
                //column by column, paint the new image default cols not within currents bounds to 0
                int newVal = i < EffectObject.Bytes.Count ? EffectObject.Bytes[i] : 0;

                if(EffectObject.Centered)
                {
                    newVal = (sbyte)newVal; 
                }
                //image is technically upside down, so flip the value
                PaintColumn(i, ConvertY(newVal, false));
                PaintLoop(EffectObject.LoopPoint);
            }
        }

        public int ConvertY(int y, bool toModel)
        {
            if(EffectObject.Centered)
            {
                if(toModel)
                {
                    return (y - 16);
                }
                else
                {
                    return (16 + y);
                }
            }
            else
            {
                return 15 - y;
            }
        }

        public static bool IsValidValue(object value)
        {
            int val = (int)value;
            return (val > 0 && val <= 256);
        }

        private void Click(object sender, MouseButtonEventArgs e)
        {
            (int x, int y) = ConvertPoint(e.GetPosition(EffectImage));
            UpdateViewModel(x, y);
            mouseHeld = true;
        }
        private void Release(object sender, MouseButtonEventArgs e)
        {
            mouseHeld = false;
        }

        private void Hold(object sender, MouseEventArgs e)
        {
            if(!mouseHeld)
            {
                return;
            }

            (int x, int y) = ConvertPoint(e.GetPosition(EffectImage));
            
            if((EffectObject.Centered && y >= centHeight) || (!EffectObject.Centered && y >= height))
            {
                return;
            }

            UpdateViewModel(x, y);
        }

        private void UpdateViewModel(int X, int Y)
        {
            UpdateModel(X, ConvertY(Y, true));
            PaintColumn(X, Y);
        }

        private void UpdateViewModelLoop(int X)
        {
            UpdateModelLoop(X);
            PaintLoop(X);
        }

        private void UpdateModel(int X, int Y)
        {
            EffectObject.Bytes[X] = (byte)Y;
        }

        private void UpdateModelLoop(int X)
        {
            EffectObject.LoopPoint = X;
        }

        private (int, int) ConvertPoint(Point point)
        {
            point.X = point.X / EffectImage.ActualWidth;
            point.Y = point.Y / EffectImage.ActualHeight;
            int X = (int)(point.X * 256);

            int Y = (int)(point.Y * (EffectObject.Centered? centHeight : height));
            return (X, Y);
        }

        private void PaintColumn(int X, int Y)
        {
            var targetHeight = EffectObject.Centered ? centHeight : height;

            byte[] pixels = new byte[targetHeight * 3];
            bool fillDirection = Y < 16? true : false;

            for (int i = 0; i < targetHeight; i++)
            {
                bool fill = false;
                if(EffectObject.Centered)
                {
                    fill = fillDirection ? (i <= 16 && i > Y) : (i >= 16 && i < Y); 
                }
                pixels[i * 3] = (i == Y) || fill ? (byte)0xFF : (byte)0x00;
            }
            if(EffectObject.Centered)
            {
                cenColChunk.X = X;
                centeredWritable.WritePixels(cenColChunk, pixels, 3, 0);
            }
            else
            {
                colChunk.X = X;
                writable.WritePixels(colChunk, pixels, 3, 0);
            }
        }

        private void PaintLoop(int X)
        {
            var scale = 256.0 / EffectObject.Length;
            double x = (double)X;
            double position = (x / EffectObject.Length) * (EffectBorder.ActualWidth / scale);
            LoopLine.X1 = position;
            LoopLine.X2 = position;
            LoopLine.Y1 = 0;
            LoopLine.Y2 = EffectBorder.ActualHeight;
        }

        private void Leave(object sender, MouseEventArgs e)
        {
            mouseHeld = false;
        }

        private void EffectLength_Validate(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void RightClick(object sender, MouseButtonEventArgs e)
        {
            (int x, int y) = ConvertPoint(e.GetPosition(EffectImage));
            if(x == EffectObject.LoopPoint) { return; }
            UpdateViewModelLoop(x);
        }
    }
}

namespace ValueConverters
{
    public class EffectImageScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int length = (int)value;
            return (double)(256.0 / length);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {

            throw new NotImplementedException();
        }
    }

    public class EffectImageScaleConverterInv : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int length = (int)value;
            return (double)(length/250.0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {

            throw new NotImplementedException();
        }
    }
}
