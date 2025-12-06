using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieScrap2000
{
	public static class Utils
	{

		public static string GetTempPath()
		{
			return Path.Combine(Path.GetTempPath(), "MovieScrap2000", "scrape_media");
		}

		public static string GetOptionsPath()
		{
			var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var dir = Path.Combine(appData, "MovieScrap2000");
			Directory.CreateDirectory(dir); // sicherstellen, dass Ordner existiert
			return dir;
		}

		public static string GetNameFromFile(string? filePath)
		{
			// Liefert den Dateinamen OHNE Erweiterung zurück.
			// Path.GetFileNameWithoutExtension() kann das auch, aber bei
			// Namen wie "Dr. Mario.zip" liefert es nur "Dr." zurück.
			// Daher hier eine eigene Implementierung.

			if (string.IsNullOrWhiteSpace(filePath))
				return string.Empty;

			string filename = Path.GetFileName(filePath);
			int index = filename.LastIndexOf('.');
			if (index < 0)
				return filename.Trim(); // keine Erweiterung
			if (index == 0)
				return string.Empty; // nur Erweiterung, kein Name

			return filename.Substring(0, index).Trim(); // Name ohne Erweiterung
		}

		public static string GetExcMsg(Exception ex)
		{
			string msg = ex.Message;
			if (ex.InnerException != null && ex.InnerException.Message != msg)
				msg += "\r\n" + GetExcMsg(ex.InnerException);
			return msg;
		}

		public static string CreateTempFileFromData(byte[]? data)
		{
			if (data == null || data.Length == 0)
				return string.Empty;

			string tempDir = GetTempPath();
			Directory.CreateDirectory(tempDir);
			string tempFilePath = Path.Combine(
				tempDir,
				Path.GetRandomFileName() + ".jpg");
			try
			{
				File.WriteAllBytes(tempFilePath, data);
				return tempFilePath;
			}
			catch (Exception ex)
			{
				MessageBox.Show(Utils.GetExcMsg(ex), "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}

			return string.Empty;
		}

		public static void OpenFileWithDefaultApp(string absPathFile)
		{
			if (!string.IsNullOrEmpty(absPathFile) && File.Exists(absPathFile))
			{
				try
				{
					var psi = new System.Diagnostics.ProcessStartInfo
					{
						FileName = absPathFile,
						UseShellExecute = true // wichtig: Standard-App verwenden
					};
					System.Diagnostics.Process.Start(psi);
				}
				catch (Exception ex)
				{
					MessageBox.Show(Utils.GetExcMsg(ex), "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		public static Image? ConvertByteArrayToImage(byte[] imageBytes)
		{
			if (imageBytes == null || imageBytes.Length == 0)
			{
				return null;
			}

			try
			{
				// 1. MemoryStream erstellen: Lädt die Byte-Daten in den Speicher
				using (MemoryStream ms = new MemoryStream(imageBytes))
				{
					// 2. Image.FromStream: Erstellt das Image-Objekt aus dem Stream
					Image image = Image.FromStream(ms);

					// Wichtig: Rückgabe einer Kopie des Bildes. 
					// Der MemoryStream (ms) wird außerhalb dieses Blocks geschlossen, 
					// und das ursprüngliche Bildobjekt könnte ungültig werden.
					return (Image)image.Clone();
				}
			}
			catch (Exception ex)
			{
				// Fehlerbehandlung, falls die Bytes keine gültige Bilddatei darstellen
				Trace.WriteLine($"Fehler beim Konvertieren der Bytes: {Utils.GetExcMsg(ex)}");
				return null;
			}
		}
	}
}
