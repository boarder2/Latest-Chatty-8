﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Werd.Converters
{
	/// <summary>
	/// Value converter that translates values greater than zero to <see cref="Visibility.Visible"/> and zero to
	/// <see cref="Visibility.Collapsed"/>.
	/// </summary>
	public sealed class GreaterThanZeroVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return (value is int && (int)value > 0) ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
