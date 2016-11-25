using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace TTSSynth
{
	public class T2WParam
	{
		private string _pathToText;
		public string PathToText
		{
			get
			{
				return _pathToText;
			}
			set
			{
				if (!File.Exists(value))
				{
					throw new IOException(string.Format("Specified file {0} doesn't exist.", value));
				}
				_pathToText = value;
			}
		}

		public int WordsPerSub { get; set; }

		public string VoiceName { get; set; }

		public string AskParams { get; set; }

		/// <summary>
		/// Voice speeed, -10 to 10
		/// </summary>
		public int VoiceRate { get; set; }

		public string FactorOrReplace { get; set; }

		public decimal ResizeFactor { get; set; }

		public decimal ReplaceFinalTime { get; set; }

		public string OutputFormat { get; set; }

		/// <summary>
		/// Samples per second, i.e. 48000
		/// </summary>
		public int SampleRate { get; set; }

		/// <summary>
		/// Bits per sample, i.e. 16
		/// </summary>
		public int BitsPerSample { get; set; }

		/// <summary>
		/// Mono=1, Stereo=2
		/// </summary>
		public int ChannelCount { get; set; }

		/// <summary>
		/// Average bytes per second, i.e. 16000
		/// </summary>
		public int AvgBytesPerSecond { get; set; }

		/// <summary>
		/// Block Align, i.e. 2
		/// </summary>
		public int BlockAlign { get; set; }

	}
}
