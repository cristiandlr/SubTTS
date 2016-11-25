using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TTSSynth
{
	public class SpeakProgress
	{
		public TimeSpan AudioPosition { get; set; }
		public int CharacterCount { get; set; }
		public int CharacterPosition { get; set; }
		public string Text { get; set; }
	}
}
