using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MovieScrap2000
{
	public class MovieFileInfo
	{
		[XmlElement("streamdetails")]
		public StreamDetails StreamDetails { get; set; } = new StreamDetails();
	}

	public class StreamDetails
	{
		[XmlElement("audio")]
		public List<AudioStream> AudioStreams { get; set; } = new List<AudioStream>();

		[XmlElement("video")]
		public VideoStream VideoStream { get; set; } = new VideoStream();
	}

	public class AudioStream
	{
		[XmlElement("bitrate")]
		public decimal Bitrate { get; set; }

		[XmlElement("channels")]
		public int Channels { get; set; }

		[XmlElement("codec")]
		public string? Codec { get; set; }

		[XmlElement("language")]
		public string? Language { get; set; }

		[XmlElement("longlanguage")]
		public string? LongLanguage { get; set; }
	}

	public class VideoStream
	{
		[XmlElement("aspect")]
		public decimal Aspect { get; set; }

		[XmlElement("bitrate")]
		public decimal Bitrate { get; set; }

		[XmlElement("codec")]
		public string? Codec { get; set; }

		[XmlElement("durationinseconds")]
		public int DurationInSeconds { get; set; }

		[XmlElement("height")]
		public int Height { get; set; }

		[XmlElement("scantype")]
		public string? ScanType { get; set; }

		[XmlElement("width")]
		public int Width { get; set; }

		[XmlElement("filesize")]
		public long FileSize { get; set; }
	}
}
