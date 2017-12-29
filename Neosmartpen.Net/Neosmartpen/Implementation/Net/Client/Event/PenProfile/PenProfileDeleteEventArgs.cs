namespace Neosmartpen.Net
{
	public sealed class PenProfileDeleteEventArgs : PenProfileReceivedEventArgs
	{
		internal PenProfileDeleteEventArgs()
		{
			Result = ResultType.Successs;
			Type = PenProfileType.Delete;
		}
		internal PenProfileDeleteEventArgs(string profileName, int status) : this()
		{
			ProfileName = profileName;
			Status = status;
		}
	}
}
