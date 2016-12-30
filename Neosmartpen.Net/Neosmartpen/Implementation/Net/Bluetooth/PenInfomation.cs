using System;
using Windows.Devices.Enumeration;

namespace Neosmartpen.Net.Bluetooth
{
    /// <summary>
    /// Represents Information of smartpen device
    /// </summary>
	public class PenInformation
	{
		internal PenInformation(DeviceInformation devInfo)
		{
			deviceInformation = devInfo;
		}

        /// <summary>
        /// Gets the device identifier
        /// </summary>
		public string Id
		{
			get
			{
				if (deviceInformation == null)
					return string.Empty;
				return deviceInformation.Id;
			}
		}

        /// <summary>
        /// Gets a common name of a device
        /// </summary>
		public string Name
		{
			get
			{
				if (deviceInformation == null)
					return string.Empty;
				return deviceInformation.Name;
			}
		}

        /// <summary>
        /// Gets the signal strength for the Bluetooth connection with the peer device
        /// </summary>
		public int Rssi { get; internal set; }
        // 이 아이만 따로 받는 이유는 LE와 아닌경우에 mac address의 차이가 있기 때문이다.

        /// <summary>
        /// Gets the device's mac address
        /// </summary>
        public string MacAddress { get; internal set; }
		public ulong BluetoothAddress
		{
			get
			{
				string temp = MacAddress.Replace(":", "");

				return Convert.ToUInt64(temp, 16);
			}
		}

		public void Update(PenUpdateInformation update)
		{
			if (deviceInformation != null)
			{
				deviceInformation.Update(update.deviceInformationUpdate);
			}
		}

		public override string ToString()
		{
			return "[" + Name + "] " + MacAddress;
		}

		internal DeviceInformation deviceInformation;
		internal ulong virtualMacAddress;
	}

	public class PenUpdateInformation
	{
		internal PenUpdateInformation(DeviceInformationUpdate update)
		{
			deviceInformationUpdate = update;
		}
		public string Id
		{
			get
			{
				if (deviceInformationUpdate == null)
					return string.Empty;
				return deviceInformationUpdate.Id;
			}
		}
		internal DeviceInformationUpdate deviceInformationUpdate;
	}
}
