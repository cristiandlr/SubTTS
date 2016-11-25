using System;
//using System.Speech.Synthesis;
//using System.Speech.AudioFormat;
//using System.Speech.AudioFormat;
using TTSSynth;

namespace subtts
{
	/// <summary>
	/// This app takes a text file as input and generates TTS audio file (wav or mp3) and subrip subtitles (srt)
	/// </summary>
	class Program
	{
		/// <summary>
		/// Displays a list of installed voices and ask the user to choose one.
		/// </summary>
		/// <returns></returns>
		private static string ChooseVoice()
		{
			int voiceIdx = 0;
			int numVoices = 0;

			var voices = TTSLogic.GetInstalledVoices();

			int i = 1;
			numVoices = voices.Count;

			foreach (var voice in voices)
			{
				Console.WriteLine("  {0} {1}", i++, voice);
			}

			bool isInt = false;
			do
			{
				Console.Write("> Enter the voice number: ");
				isInt = Int32.TryParse(Console.ReadLine().Trim(), out voiceIdx);
			} while (!isInt || (voiceIdx < 1 || voiceIdx > numVoices));

			return voices[voiceIdx - 1];
		}

		static void Main(string[] args)
		{
			bool showVoices = false;
			string inputFile = string.Empty;

			int i = 0;
			foreach (var arg in args)
			{
				switch (arg)
				{ 
					case "--showVoices":
						showVoices = true;
					break;
					case "--inputFile":
					inputFile = args[i + 1];
					break;
					case "--help":
						Console.WriteLine("\nTTSub.exe [params]\n\n--showVoices: Show a list of installed voices.\n--inputFile <pathTofile>: Convert specific file to Audio + Subtitles.\n--help: Show this message.");
						System.Environment.Exit(0);
					break;
				}

				i++;
			}

			Console.WriteLine("\n* Welcome to TTSub, Text to Speech + Subtitles *\ninput=(text file) -> output=(spoken wav or mp3 + srt subtitles)\n\n");

			if (showVoices) {
				Console.WriteLine("Installed voices:\n" + TTSLogic.GetInstalledVoicesStr() + "\n\nPress Ctrl + C to quit or Enter to continue...");
				Console.ReadLine();
			}

			TTSLogic tts = null;

			if (string.IsNullOrEmpty(inputFile)) 
			{
				tts = new TTSLogic();
			}
			else
			{
				tts = new TTSLogic(inputFile);
			}

			tts.ProcessFile(true);

			////var voices = synthesizer.GetInstalledVoices();
			//foreach (InstalledVoice voice in synthesizer.GetInstalledVoices())
			//{
			//    synthesizer.SelectVoice(voice.VoiceInfo.Name);
			//    synthesizer.Speak(builder);
			//} //loop
		}
		
		//static void synthesizer_SpeakCompleted(object sender, SpeakCompletedEventArgs e) 
		//{
		//    _speakCompleted = true;
		//}
	}
}
