using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieScrap2000
{
	public class ImageCacheItem
	{
		// Die DB-Id, falls das Bild bereits gecached ist
		public int Id { get; set; }
		// Der Typ des Bildes (Poster, Fanart, ClearLogo, etc.)
		public string Type { get; set; } = string.Empty;
		// Die tatsächlichen Bilddaten (BLOB)
		public byte[] Data { get; set; } = Array.Empty<byte>();
	}

	// Dies ist die Liste der Bildtypen, die wir unterstützen
	public static class ImageTypes
	{
		public const string Poster = "Poster";
		public const string Fanart = "Fanart";
		public const string ClearLogo = "ClearLogo";
		public const string ClearArt = "ClearArt";
		public const string Banner = "Banner";
		public const string DiskArt = "DiskArt";
		public const string KeyArt = "KeyArt";
		public const string Landscape = "Landscape";
        public const string CharacterArt = "CharacterArt";

		public static List<string> AllTypes = new List<string>
		{
			Poster,
			Fanart,
			ClearLogo,
			ClearArt,
			Banner,
			DiskArt,
			KeyArt,
			Landscape
		};
  }
}
