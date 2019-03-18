using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using CopyPaste.utils;
using Application = System.Windows.Application;

namespace CopyPaste.features.mainWindow {
	public class MainWindowController {
		//		private static readonly string DEFAULT_EOBD_PATH = $@"c:\Program Files";
		//		private static readonly string DEFAULT_VEHICLE_PATH = $@"c:\Program Files";
		public static readonly string DEFAULT_TEMP_PATH = $@"C:\_Data\Test\_laboratory\Temp";
		public static readonly string DEFAULT_EOBD_PATH = $@"C:\_Data\Test\_laboratory\EOBD2\V22.53";
		public static readonly string DEFAULT_VEHICLE_PATH = $@"C:\_Data\Test\_laboratory\EUROPE";
		public static readonly string ADAP_VER = "ADAP.VER";
		public static readonly string LICENSE_DAT = "LICENSE.DAT";
		public static readonly string LIB_CFG = "lib.cfg";
		public static readonly string ANDROID_DEV = "Android.dev";
		public static readonly string BASE_FOLDER = AppDomain.CurrentDomain.BaseDirectory;

		private readonly IMainWindow window;
		private readonly Dispatcher dispatcher;

		private string sEOBDPath;
		private string sVehiclePath;

		public MainWindowController(IMainWindow window, Dispatcher dispatcher) {
			this.window = window;
			this.dispatcher = dispatcher;
		}

		public void findEOBDPath() { findEOBDPath(DEFAULT_EOBD_PATH); }

		public void findEOBDPath(string startPath) { openFolderDialog(startPath, window.setEOBDPath); }

		public void findVehiclePath() { findVehiclePath(DEFAULT_VEHICLE_PATH); }

		public void findVehiclePath(string startPath) { openFolderDialog(startPath, window.setVehiclePath); }

		private void openFolderDialog(string startPath, Action<string> action) {
			try {
				using (var dialog = new FolderBrowserDialog()) {
					dialog.SelectedPath = startPath;
					var result = dialog.ShowDialog();

					switch (result) {
						case DialogResult.OK:
							dispatcher.Invoke(() => action(dialog.SelectedPath));
							break;
						case DialogResult.Cancel:
							break;
						default:
							throw new Exception(result.ToString());
					}
				}
			} catch (Exception e) { dispatcher.Invoke(() => window.showMessage(e.Message)); }
		}

		public void startProcedure(string sEOBDPath, string sVehiclePath) {
			this.sEOBDPath = sEOBDPath;
			this.sVehiclePath = sVehiclePath;

			Task.Run(() => {
								try {
									if (!new DirectoryInfo(sEOBDPath).Exists) { window.showMessage("Неверно указан путь к EOBD"); }
									if (!new DirectoryInfo(sVehiclePath).Exists) { window.showMessage("Неверно указан путь к Vehile"); }
									var libCfgPath = Path.Combine(sEOBDPath, LIB_CFG);
									if (!new FileInfo(libCfgPath).Exists) { window.showMessage($"Не найден файл {LIB_CFG}"); }
									createAndroidDev();
									createLastRow();
									dispatcher.Invoke(() => window.setTextInStatus("Склеивание"));
									glueResultingFiles(getFileList(libCfgPath), window.setValueInProgressBar).Wait();
									dispatcher.Invoke(() => window.setTextInStatus("Копирование"));
									copyFiles(getDictionary(getFilesForCopied(), sVehiclePath), window.setValueInProgressBar).Wait();
									dispatcher.Invoke(() => window.copiedCompleted());
								} catch (Exception e) {
									dispatcher.Invoke(() => window.showMessage($"{MethodBase.GetCurrentMethod().Name}: {e.Message}"));
								}
							});
		}

		private void createAndroidDev() {
			var LicenseDat = Path.Combine(sEOBDPath, LICENSE_DAT);
			var androidDev = Path.Combine(sEOBDPath, ANDROID_DEV);
			if (new FileInfo(androidDev).Exists) { File.Delete(androidDev); }
			File.Copy(LicenseDat, androidDev);
		}

		private void createLastRow() {
			try {
				var lastRow = Path.Combine(sEOBDPath, "last_row.so");
				var sVersion = new FileInfo(lastRow).Directory?.Name;

				var iVersion = 1000 * Convert.ToInt32(sVersion?[1].ToString()) + 100 * Convert.ToInt32(sVersion?[2].ToString()) +
											10 * Convert.ToInt32(sVersion?[4].ToString()) + 1 * Convert.ToInt32(sVersion?[5].ToString());
				if (iVersion > 2237) { File.WriteAllText(lastRow, $"EOBD2{sVersion}", Encoding.Default); }
			} catch (Exception e) {
				dispatcher.Invoke(() => window.showMessage($"{MethodBase.GetCurrentMethod().Name}: {e.Message}"));
			}
		}

		private List<FileInfo> getFileList(string libCfgPath) {
			var fileList = new List<FileInfo>();

			try {
				var folderPath = new FileInfo(libCfgPath).Directory?.FullName ??
												throw new Exception($"Не могу получить путь к директории {libCfgPath}");
				var content = File.ReadAllText(libCfgPath);
				var fileNames = content.Split(';').ToList();
				fileNames.Add("last_row.so");

				foreach (var fileName in fileNames) {
					if (string.IsNullOrEmpty(fileName)) { continue; }
					fileList.Add(new FileInfo(Path.Combine(folderPath, fileName)));
				}
			} catch (Exception e) {
				dispatcher.Invoke(() => window.showMessage($"{MethodBase.GetCurrentMethod().Name}: {e.Message}"));
			}
			return fileList;
		}

		private async Task glueResultingFiles(List<FileInfo> fileList, Action<double> progressCallback) {
			var adapVer = Path.Combine(sEOBDPath, ADAP_VER);
			if (new FileInfo(adapVer).Exists) { File.Delete(adapVer); }
			var total_size = fileList.Select(fileInfo => fileInfo.Length).Sum();
			long total_read = 0;
			const double progress_size = 10000.0;

			foreach (var fileInfo in fileList) {
				long total_read_for_file = 0;
				var from = fileInfo.FullName;
				var to = adapVer;

				using (var outStream = new FileStream(to, FileMode.Append, FileAccess.Write, FileShare.Read)) {
					using (var inStream = new FileStream(from, FileMode.Open, FileAccess.Read, FileShare.Read)) {
						await copyStream(inStream, outStream, x => {
																										total_read_for_file = x;

																										dispatcher.Invoke(() => progressCallback((total_read + total_read_for_file) /
																																														(double) total_size * progress_size));
																									});
					}
				}
			}
		}

		private List<FileInfo> getFilesForCopied() {
			var fileList = new List<FileInfo>();

			try {
				fileList.Add(new FileInfo(Path.Combine(sEOBDPath, "adap.ver")));
				fileList.Add(new FileInfo(Path.Combine(BASE_FOLDER, "Dpu.ver")));
				fileList.Add(new FileInfo(Path.Combine(sEOBDPath, "Android.dev")));
				fileList.Add(new FileInfo(Path.Combine(BASE_FOLDER, "libSTD.so")));
			} catch (Exception e) {
				dispatcher.Invoke(() => window.showMessage($"{MethodBase.GetCurrentMethod().Name}: {e.Message}"));
			}
			return fileList;
		}

		private Dictionary<string, string> getDictionary(List<FileInfo> fileList, string finalPath) {
			var dictionary = new Dictionary<string, string>();

			try {
				var directoryList = new DirectoryInfo(finalPath).GetDirectories("*", SearchOption.AllDirectories).ToList();
				directoryList.Add(new DirectoryInfo(finalPath));

				directoryList.ForEach(directoryInfo =>
																fileList.ForEach(info => dictionary.Add(Path.Combine(directoryInfo.FullName, info.Name),
																																				info.FullName)));
			} catch (Exception e) {
				dispatcher.Invoke(() => window.showMessage($"{MethodBase.GetCurrentMethod().Name}: {e.Message}"));
			}
			return dictionary;
		}

		public async Task copyFiles(Dictionary<string, string> files, Action<double> progressCallback) {
//			foreach (var key in files.Keys) { File.Delete(key); }
//			return;
			try {
				var total_size = files.Values.Select(x => new FileInfo(x).Length).Sum();
				long total_read = 0;
				const double progress_size = 10000.0;

				foreach (var item in files) {
					long total_read_for_file = 0;
					var from = item.Value;
					var to = item.Key;

					using (var outStream = new FileStream(to, FileMode.Create, FileAccess.Write, FileShare.Read)) {
						using (var inStream = new FileStream(from, FileMode.Open, FileAccess.Read, FileShare.Read)) {
							await copyStream(inStream, outStream, x => {
																											total_read_for_file = x;

																											dispatcher.Invoke(() => progressCallback((total_read + total_read_for_file) /
																																															(double) total_size * progress_size));
																										});
						}
					}
					total_read += total_read_for_file;
				}
			} catch (Exception e) {
				dispatcher.Invoke(() => window.showMessage($"{MethodBase.GetCurrentMethod().Name}: {e.Message}"));
			}
		}

		public async Task copyStream(Stream from, Stream to, Action<long> progress) {
			try {
				const int buffer_size = 1024 * 10;
				var buffer = new byte[buffer_size];
				long total_read = 0;

				while (total_read < from.Length) {
					var read = await from.ReadAsync(buffer, 0, buffer_size);
					await to.WriteAsync(buffer, 0, read);
					total_read += read;
					progress(total_read);
				}
			} catch (Exception e) {
				dispatcher.Invoke(() => window.showMessage($"{MethodBase.GetCurrentMethod().Name}: {e.Message}"));
			}
		}
	}
}