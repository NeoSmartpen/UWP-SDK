using System.Collections.Generic;

namespace Neosmartpen.Net
{
	public sealed class PenProfileWriteValueEventArgs : PenProfileReceivedEventArgs
	{
		internal PenProfileWriteValueEventArgs()
		{
			Type = PenProfileType.WriteValue;
			Result = ResultType.Successs;
		}
		internal PenProfileWriteValueEventArgs(string profileName) : this()
		{
			ProfileName = profileName;
			Results = new List<WriteValueResult>();
		}

		public List<WriteValueResult> Results { get; internal set; }

		public class WriteValueResult
		{
			public int Status { get; internal set; }
			public string Key { get; internal set; }
		}
	}
}
