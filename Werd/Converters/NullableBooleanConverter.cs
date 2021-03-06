﻿using System;
using Windows.UI.Xaml.Data;

namespace Werd.Converters
{
	public sealed class NullableBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value == null) return false;
			return (bool)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			if (value == null) return false;
			return (bool)value;
		}
	}
}
