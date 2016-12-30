using System;
using Windows.UI;

namespace SampleApp
{
    public class NColor
	{
		public static readonly string VIOLET = "#FF9C3FCD";
		public static readonly string BLUE = "#FF3c6bf0";
		public static readonly string GRAY = "#FFbdbdbd";
		public static readonly string YELLOW = "#FFfbcb26";
		public static readonly string PINK = "#FFff2084";
		public static readonly string MINT = "#FF27e0c8";
		public static readonly string RED = "#FFf93610";
		public static readonly string BLACK = "#FF181818";
		public static readonly string[] AllColor =
		{
			VIOLET, BLUE, GRAY, YELLOW, PINK, MINT, RED, BLACK
		};
		public static readonly string[] AllColorName =
		{
			"VIOLET", "BLUE", "GRAY", "YELLOW", "PINK", "MINT", "RED", "BLACK"
		};

		public static readonly Color[] AllRealColor =
		{
			Colors.Violet, Colors.Blue, Colors.Gray, Colors.Yellow, Colors.Pink, Windows.UI.Color.FromArgb(0xff, 144, 227, 199), Colors.Red, Colors.Black
		};

		public NColor()
		{
		}

		public NColor(int index)
		{
			if ( index >= 0 && index < AllColor.Length)
			{
				Name = AllColorName[index];
				RealColor = AllRealColor[index];
				SetColor(AllColor[index]);
			}
		}

		public void SetColor(string color)
		{
			Color = Int32.Parse(color.Replace("#", ""), System.Globalization.NumberStyles.HexNumber);
		}

		public string Name { get; set; }
		public int Color { get; set; }
		public Color RealColor { get; set; }
	}
}
