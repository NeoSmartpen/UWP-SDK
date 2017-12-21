using System.Collections.Generic;

namespace Neosmartpen.Net
{
	public sealed class PenProfileReadValueEventArgs : PenProfileReceivedEventArgs
	{
		internal PenProfileReadValueEventArgs()
		{
			Type = PenProfileType.ReadValue;
			Result = ResultType.Successs;
		}

		internal PenProfileReadValueEventArgs(string profileName):this()
		{
			ProfileName = profileName;
			Results = new List<ReadValueResult>();
		}

		public List<ReadValueResult> Results { get; internal set; }
		
		public class ReadValueResult
		{
			public int Status { get; internal set; }
			public string Key { get; internal set; }
			public byte[] Data { get; internal set; }
		}
	}
}
