using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using CopyPaste.features.mainWindow;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;

namespace CopyPaste.utils {
	public static class UiHelper {

		public static void UpdateComboBox<T>(ItemsControl comboBox, List<T> list) {
			comboBox.ItemsSource = new ObservableCollection<T>(list);
		}

		public static void SetState(MainWindow mainWindow, Border border, Button bChangeState, WindowState windowState,
																int margin, string resourse) {
			mainWindow.WindowState = windowState;
			border.Margin = new Thickness(margin);
			bChangeState.Style = Application.Current.FindResource(resourse) as Style;
		}
	}
}