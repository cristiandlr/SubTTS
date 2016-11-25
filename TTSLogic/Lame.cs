using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

//reference Nuget Package NAudio.Lame
using NAudio.Wave;
using NAudio.Lame;


namespace TTSSynth
{
	public static class Lame
	{
		//public static void WavToMp3File(ref MemoryStream ms, string savetofilename)
		public static void WavToMp3File(string inputWavFile)
		{
			var outputFile = Path.Combine(
				Path.GetDirectoryName(inputWavFile),
				Path.GetFileNameWithoutExtension(inputWavFile) + ".mp3");

			using (var rdr = new WaveFileReader(inputWavFile))
			using (var wtr = new LameMP3FileWriter(outputFile, rdr.WaveFormat, LAMEPreset.STANDARD))
			{
				//rdr.CopyTo(wtr);
				int size = 128;

				while (rdr.Position < rdr.Length) {
					if (rdr.Length - rdr.Position < size)
						size = (int)rdr.Length - (int)rdr.Position;

					byte[] buffer = new byte[size];
					var r = rdr.Read(buffer, 0, size);
					wtr.Write(buffer, 0, size);
				}

				wtr.Flush();
			}
		}

	}
}
