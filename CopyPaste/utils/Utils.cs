using System.Threading.Tasks;

namespace CopyPaste.utils {
	public static class Utils {
		public static void Delay(int millisec) { Task.Run(async () => await Task.Delay(millisec)).Wait(); }

		public static bool isDebug() {
#if DEBUG
			return true;
#else
			return false;
#endif
		}
	}
}