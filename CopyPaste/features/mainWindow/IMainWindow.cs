namespace CopyPaste.features.mainWindow {
	public interface IMainWindow {
		void showMessage(string message);

		void setEOBDPath(string path);

		void setVehiclePath(string path);

		void setValueInProgressBar(double value);

		void setTextInStatus(string status);

		void copiedCompleted();
	}
}