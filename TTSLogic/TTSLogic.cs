using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Speech.Synthesis;
using System.Configuration;
using System.IO;
using System.Speech.AudioFormat;
using System.Text.RegularExpressions;

namespace TTSSynth
{
	public class TTSLogic
	{
		private List<SpeakProgress> _progress = new List<SpeakProgress>();
		private string _textToRead = string.Empty;
		private int _percentage = 0;
		private int _charPos = 0;
		private T2WParam _config = null;
		private TimeSpan? _lastPositiveTime = null;

		/** (e. g.)
		 * 4
		 * 00:00:08,099 --> 00:00:12,357
		 * Se vengó de la persona equivocada.
		 * Y ahora tiene a mi hijo.
		 */
		static private string _outputSub = @"{0}
{1:D2}:{2:D2}:{3:D2},{4:D3} --> {5:D2}:{6:D2}:{7:D2},{8:D3}
{9}

";

		/// <summary>
		/// Default constructor
		/// </summary>
		public TTSLogic()
		{
			_config = TTSLogic.LoadConfig("");
		}

		/// <summary>
		/// Constructor with specific parameters
		/// </summary>
		/// <param name="conf"></param>
		public TTSLogic(T2WParam conf)
		{
			_config = conf;
		}

		/// <summary>
		/// Instantiate with default parameters but use specific input file
		/// </summary>
		/// <param name="inputFile"></param>
		public TTSLogic(string inputFile)
		{
			_config = TTSLogic.LoadConfig(inputFile);
			//_config.PathToText = inputFile;
		}

		/// <summary>
		/// Load configuration from App.config
		/// </summary>
		/// <returns></returns>
		public static T2WParam LoadConfig(string inputFile)
		{
			var param = new T2WParam();
			if (string.IsNullOrEmpty(inputFile))
			{
				param.PathToText = ConfigurationManager.AppSettings["PathToText"].ToString();
			}
			else
			{
				param.PathToText = inputFile;
			}

			param.VoiceName = ConfigurationManager.AppSettings["VoiceName"].ToString();
			param.WordsPerSub = Int32.Parse(ConfigurationManager.AppSettings["WordsPerSub"].ToString());
			param.VoiceRate = Int32.Parse(ConfigurationManager.AppSettings["VoiceRate"].ToString());
			param.AskParams = ConfigurationManager.AppSettings["AskParams"].ToString();
			param.FactorOrReplace = ConfigurationManager.AppSettings["FactorOrReplace"].ToString();
			param.ResizeFactor = decimal.Parse(ConfigurationManager.AppSettings["ResizeFactor"].ToString());
			param.ReplaceFinalTime = decimal.Parse(ConfigurationManager.AppSettings["ReplaceFinalTime"].ToString());
			param.OutputFormat = ConfigurationManager.AppSettings["OutputFormat"].ToString();
			param.SampleRate = Int32.Parse(ConfigurationManager.AppSettings["SampleRate"]);
			param.BitsPerSample = Int32.Parse(ConfigurationManager.AppSettings["BitsPerSample"]);
			param.ChannelCount = Int32.Parse(ConfigurationManager.AppSettings["ChannelCount"]);
			param.AvgBytesPerSecond = Int32.Parse(ConfigurationManager.AppSettings["AvgBytesPerSecond"]);
			param.BlockAlign = Int32.Parse(ConfigurationManager.AppSettings["BlockAlign"]);

			return param;
		}

		/// <summary>
		/// Returns a string with the list of installed voices in a String
		/// separated by new line symbol
		/// </summary>
		/// <returns></returns>
		public static string GetInstalledVoicesStr()
		{
			string voices = string.Empty;

			using (SpeechSynthesizer synthesizer = new SpeechSynthesizer())
			{
				int numVoices = synthesizer.GetInstalledVoices().Count;
				foreach (var voice in synthesizer.GetInstalledVoices())
				{
					voices += voice.VoiceInfo.Name + Environment.NewLine;
				}
			}

			return voices;
		}

		/// <summary>
		/// Returns a string with the list of installed voices
		/// </summary>
		/// <returns></returns>
		public static List<string> GetInstalledVoices()
		{
			var voices = new List<string>();

			using (SpeechSynthesizer synthesizer = new SpeechSynthesizer())
			{
				int numVoices = synthesizer.GetInstalledVoices().Count;
				foreach (var voice in synthesizer.GetInstalledVoices())
				{
					voices.Add(voice.VoiceInfo.Name);
				}
			}

			return voices;
		}

		/// <summary>
		/// Create a wav file from text
		/// </summary>
		/// <param name="text"></param>
		/// <param name="param"></param>
		/// <param name="wavePath"></param>
		/// <returns></returns>
		private List<SpeakProgress> CreateWav(string text, T2WParam param, out string wavePath)
		{
			wavePath = Path.Combine(
				Path.GetDirectoryName(param.PathToText),
				Path.GetFileNameWithoutExtension(param.PathToText) + ".wav");

			using (var synthesizer = new SpeechSynthesizer())
			{
				var synthFormat = new SpeechAudioFormatInfo(EncodingFormat.Pcm, param.SampleRate, param.BitsPerSample, param.ChannelCount, param.AvgBytesPerSecond, param.BlockAlign, null);
				//synthesizer.SetOutputToAudioStream(streamAudio, synthFormat);

				synthesizer.SpeakProgress += new EventHandler<SpeakProgressEventArgs>(synthesizer_SpeakProgress);
				//synthesizer.SpeakCompleted += new EventHandler<SpeakCompletedEventArgs>(synthesizer_SpeakCompleted);
				//synthesizer.SetOutputToDefaultAudioDevice();
				synthesizer.SetOutputToWaveFile(wavePath, synthFormat);
				synthesizer.SelectVoice(param.VoiceName);
				synthesizer.Rate = param.VoiceRate;

				PromptBuilder builder = new PromptBuilder();
				builder.StartVoice(param.VoiceName);
				builder.StartStyle(new PromptStyle(PromptEmphasis.Moderate));
				//builder.AppendText(text, (PromptRate)param.VoiceRate);

				//if (param.VoiceName == "Ines22k_HQ")
				//{
				//	var dirtyText = text.Replace("...", ",,");
				//	dirtyText = Regex.Replace(dirtyText, @"[\s]*[\r\n]+", " ");
				//	dirtyText = dirtyText.Replace(".", ";");
				//	builder.AppendText(dirtyText, PromptVolume.Loud);
				//}
				//else
				//{
				//	builder.AppendText(text, PromptVolume.Loud);
				//}

				builder.AppendText(text, PromptVolume.Loud);

				builder.EndStyle();
				builder.EndVoice();

				synthesizer.Speak(builder);
			}

			return _progress;
		}

		private SpeakProgress EstimateEndTime(long AudioPositionTicks, long CharacterPosition, int CharacterCount)
		{
			var p = new SpeakProgress();

			//var ticks = progress[lastIdx].AudioPosition.Ticks;
			//var txtCount = progress[lastIdx].CharacterPosition;

			var ticksByChar = Convert.ToInt64(AudioPositionTicks / CharacterPosition);

			p.AudioPosition = new TimeSpan(AudioPositionTicks + ticksByChar * CharacterCount);

			return p;
		}

		private void synthesizer_SpeakProgress(object sender, SpeakProgressEventArgs e)
		{
			string t = string.Empty;
			int c = _textToRead.Length;
			string token = string.Empty;
			int charCount = e.Text.Length;

			for (int i = _charPos; i < c; i++)
			{
				t = _textToRead.Substring(i, charCount);
				if (e.Text == t)
				{
					if (i - _charPos > 0 && _progress.Count > 0)
					{
						token = _textToRead.Substring(_charPos, i - _charPos);
						_progress[_progress.Count - 1].Text += token;
					}

					//t = _textToRead.Substring(i, e.CharacterCount);
					_charPos = i + charCount;
					break;
				}

			}

			//Fix negative timespan issue
			TimeSpan audioPos = e.AudioPosition;

			if (e.AudioPosition.Ticks < 0)
			{
				if (_lastPositiveTime == null)
				{
					_lastPositiveTime = _progress[_progress.Count - 1].AudioPosition;
				}

				audioPos = new TimeSpan(2 * _lastPositiveTime.Value.Ticks + e.AudioPosition.Ticks);
			}

			var prog = new SpeakProgress()
			{
				AudioPosition = audioPos,
				CharacterCount = charCount,
				CharacterPosition = e.CharacterPosition,
				Text = t
			};

			_progress.Add(prog);


			if (100 * _charPos / c > _percentage)
			{
				_percentage = 100 * _charPos / c;
				Console.Write(string.Format("\r{0:d3}%", _percentage));
			}
			//s = s + e.Text + Environment.NewLine;
			//Console.WriteLine("SpeakProgress: AudioPosition=" + e.AudioPosition + ",\tCharacterPosition=" + e.CharacterPosition + ",\tCharacterCount=" + e.CharacterCount + ",\tText=" + e.Text);
			//_s = _s + "SpeakProgress: AudioPosition=" + e.AudioPosition + ",\tCharacterPosition=" + e.CharacterPosition + ",\tCharacterCount=" + e.CharacterCount + ",\tText=" + e.Text + Environment.NewLine;
		}

		/// <summary>
		/// Replaces some characters (tab, blankspace, newline) to a single blank space.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		private string FormatText(string text, string lineEndingStr)
		{
			string t = Regex.Replace(text, @"[ \t]{2,}", " "); //remove tabs and double spaces
			t = Regex.Replace(t, @"[\r\n]+[ ]*[\r\n]+", "\n"); //remove double endlines
			t = Regex.Replace(t, @"\n", m => string.Format("{0}{1}", lineEndingStr, m.Value));
			//t = Regex.Replace(t, @"\.\n", "####");
			//t = Regex.Replace(t, @"\n", ".\n");
			//t = Regex.Replace(t, @"####", ".\n");

			return t;
		}


		/// <summary>
		/// Generates SRT file after the wav file has been created
		/// </summary>
		/// <param name="progress"></param>
		/// <param name="param"></param>
		/// <returns></returns>
		private List<string> GenSubtitles(List<SpeakProgress> progress, T2WParam param)
		{
			//MergeWords(ref progress, text);

			var p = progress[progress.Count - 1];
			progress.Add(EstimateEndTime(p.AudioPosition.Ticks, p.CharacterPosition, p.CharacterCount)); //added to avoid exception on final loop iteration.

			//Get original text (text with punctuation)
			//text = Regex.Replace(text, @"[\s\r\n]+", " "); //replace new line with blank space.
			//var originalWords = text.Split(' ');

			var finalSubs = new List<string>();
			TimeSpan t1 = new TimeSpan();
			TimeSpan t2 = new TimeSpan();
			int wordCounter = 0;
			int subNo = 1;
			string sub = string.Empty;

			t1 = progress[0].AudioPosition;
			for (int i = 0; i < progress.Count - 1; i++)
			{
				wordCounter++;
				t2 = progress[i + 1].AudioPosition;

				//sub = sub + " " + originalWords[j];
				sub = sub + progress[i].Text;
				if (wordCounter >= param.WordsPerSub || i >= progress.Count - 2)
				{
					finalSubs.Add(
						string.Format(_outputSub,
							(subNo).ToString("D5"),
							t1.Hours, t1.Minutes, t1.Seconds, t1.Milliseconds,
							t2.Hours, t2.Minutes, t2.Seconds, t2.Milliseconds,
							sub.Trim())
					);

					subNo++;
					sub = string.Empty;
					t1 = t2;
					wordCounter = 0;
				}
			}
			return finalSubs;
		}


		/// <summary>
		/// Writes out the SRT File
		/// </summary>
		/// <param name="param"></param>
		/// <param name="texts"></param>
		private void ListStr2SRTFile(T2WParam param, List<string> texts)
		{
			string srtPath = Path.Combine(
				Path.GetDirectoryName(param.PathToText),
				Path.GetFileNameWithoutExtension(param.PathToText) + ".srt");

			using (FileStream fs = File.Create(srtPath))
			{
				foreach (var sub in texts)
				{
					Byte[] info = new UTF8Encoding(true).GetBytes(sub);
					//Byte[] info = System.Text.Encoding.Unicode.GetBytes(sub);
					fs.Write(info, 0, info.Length);
				}
			}
		}


		/// <summary>
		/// Resizes the time of the subtitles
		/// </summary>
		/// <param name="subs"></param>
		/// <param name="FactorOrReplace">factor or replace method</param>
		/// <param name="finalTime">set the final length (in miliseconds) of the SRT</param>
		/// <param name="factor">final length (in ms) = Current length * factor</param>
		private void ResizeSubtitles(ref List<SpeakProgress> subs, string FactorOrReplace, decimal finalTime, decimal factor)
		{
			//decimal factor = 0;
			if (FactorOrReplace.ToUpper() == "REPLACE")
			{
				var p = subs[subs.Count - 1];
				var actualMs = EstimateEndTime(p.AudioPosition.Ticks, p.CharacterPosition, p.CharacterCount).AudioPosition.TotalMilliseconds;
				factor = finalTime / (decimal)actualMs;
			}

			if (factor == 1)
				return;

			for (int i = 0; i < subs.Count; i++)
			{
				subs[i].AudioPosition = new TimeSpan(Convert.ToInt64(factor * subs[i].AudioPosition.Ticks));
			}
		}


		/// <summary>
		/// Process the file based on the current parameters
		/// </summary>
		/// <param name="genSubs">if True, generates an SRT file</param>
		public void ProcessFile(bool genSubs)
		{
			Encoding e = TextFileEncodingDetector.DetectTextFileEncoding(_config.PathToText);
			if (e == null)
				e = Encoding.Default;

			_textToRead = File.ReadAllText(_config.PathToText, e);
			var textToRead = FormatText(_textToRead, ".");
			_textToRead = FormatText(_textToRead, " ");
			string pathToWav = "";

			Console.WriteLine("Creating wav file...");
			List<SpeakProgress> progress = CreateWav(textToRead, _config, out pathToWav);

			if (genSubs)
			{
				Console.WriteLine(Environment.NewLine + "Creating srt file...");
				ResizeSubtitles(ref progress, _config.FactorOrReplace, _config.ReplaceFinalTime, _config.ResizeFactor);
				List<string> fSubs = GenSubtitles(progress, _config);
				ListStr2SRTFile(_config, fSubs);
			}

			if (_config.OutputFormat.ToUpper() == "MP3")
			{
				Console.WriteLine("Creating mp3 file...");
				Lame.WavToMp3File(pathToWav);
				File.Delete(pathToWav);
			}
		}

	}
}
