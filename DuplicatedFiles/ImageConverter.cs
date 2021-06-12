using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DuplicatedFiles
{
	public class ImageConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var path = value as string;

			if (path == null)
			{
				return DependencyProperty.UnsetValue;
			}
			//create new stream and create bitmap frame
			var bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			try
			{
				bitmapImage.StreamSource = new FileStream(path, FileMode.Open, FileAccess.Read);
				//load the image now so we can immediately dispose of the stream
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				//clean up the stream to avoid file access exceptions when attempting to delete images
				bitmapImage.StreamSource.Dispose();
				return bitmapImage;
			}
			catch (NotSupportedException e) when (e.HResult == -2146233067)
			{
				using (var memory = new MemoryStream())
				{
					Properties.Resources.NoValidImage_de.Save(memory, ImageFormat.Png);
					memory.Position = 0;

					bitmapImage = new BitmapImage();
					bitmapImage.BeginInit();
					bitmapImage.StreamSource = memory;
					bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
					bitmapImage.EndInit();
					bitmapImage.Freeze();
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
			return bitmapImage;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
