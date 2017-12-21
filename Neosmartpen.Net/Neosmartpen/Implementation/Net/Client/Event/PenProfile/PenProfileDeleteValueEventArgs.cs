using System.Collections.Generic;

namespace Neosmartpen.Net
{
	public sealed class PenProfileDeleteValueEventArgs : PenProfileReceivedEventArgs
	{
		internal PenProfileDeleteValueEventArgs()
		{
			Type = PenProfileType.DeleteValue;
			Result = ResultType.Successs;
		}
		internal PenProfileDeleteValueEventArgs(string profileName) : this()
		{
			ProfileName = profileName;
			Results = new List<DeleteValueResult>();
		}

		public List<DeleteValueResult> Results { get; internal set; }

		public class DeleteValueResult
		{
			public string Key { get; internal set; }
			public int Status { get; internal set; }
		}
	}
}
