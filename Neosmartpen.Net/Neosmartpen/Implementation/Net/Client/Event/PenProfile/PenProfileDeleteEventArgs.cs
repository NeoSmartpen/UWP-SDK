namespace Neosmartpen.Net
{
	public sealed class PenProfileDeleteEventArgs : PenProfileReceivedEventArgs
	{
		internal PenProfileDeleteEventArgs()
		{
			Result = ResultType.Successs;
			Type = PenProfileType.Create;
		}
		internal PenProfileDeleteEventArgs(string profileName, int status) : this()
		{
			ProfileName = profileName;
			Status = status;
		}
	}
}
