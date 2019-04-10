﻿using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using CopyPaste.utils;

namespace CopyPaste.features.mainWindow {
	/// <inheritdoc>
	///   <cref></cref>
	/// </inheritdoc>
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : IMainWindow {
		private readonly MainWindowController _controller;

		public MainWindow() {
			InitializeComponent();
			_controller = new MainWindowController(this, Dispatcher.CurrentDispatcher);
		}

		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
			CbRevert.Visibility = Utils.isDebug() ? Visibility.Visible : Visibility.Collapsed;
		}

		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
			base.OnMouseLeftButtonDown(e);
			var y = e.GetPosition(this).Y;

			if (y > 25 ||
					y < 0) { return; }
			DragMove();
		}

		public void Close_OnClick(object sender, RoutedEventArgs e) { Application.Current.Shutdown(); }

		public void ChangeState_OnClick(object sender, RoutedEventArgs e) {
			switch (WindowState) {
				case WindowState.Maximized: {
					UiHelper.SetState(this, Border, BChangeState, WindowState.Normal, 0, "BMaximizeWindow");
					break;
				}
				case WindowState.Normal: {
					UiHelper.SetState(this, Border, BChangeState, WindowState.Maximized, 7, "BNormalizeWindow");
					break;
				}
				case WindowState.Minimized:
					break;
			}
		}

		private void MinimizedState_OnClick(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }

		private void BEobdExplorer_OnClick(object sender, RoutedEventArgs e) { _controller.findEOBDPath(); }

		private void BVehicleExplorer_OnClick(object sender, RoutedEventArgs e) { _controller.findVehiclePath(); }

		private void BStart_OnClick(object sender, RoutedEventArgs e) {
			workState();
			_controller.startProcedure(tboxEOBDPath.Text, tboxVehiclePath.Text, CbRevert.IsChecked);
		}

		private void startState() {
			setTextInStatus(string.Empty);
			bStart.Visibility = Visibility.Visible;
			pbFiles.Visibility = Visibility.Hidden;
		}

		private void workState() {
			setTextInStatus(string.Empty);
			bStart.Visibility = Visibility.Hidden;
			pbFiles.Visibility = Visibility.Visible;
		}

		public void showMessage(string message) {
			MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
			startState();
		}

		public void setEOBDPath(string path) { tboxEOBDPath.Text = path; }

		public void setVehiclePath(string path) { tboxVehiclePath.Text = path; }

		public void setValueInProgressBar(double value) { pbFiles.Value = value; }

		public void setTextInStatus(string status) { tbFilePath.Text = status; }

		public void copiedCompleted() {
			startState();
			setTextInStatus("Завершено");
		}
	}
}