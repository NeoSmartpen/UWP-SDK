Neo smartpen SDK for Windows Universal Platform(UWP)
===================

## Universal Windows Platform(UWP) SDK 2.0 

UWP SDK for Windows 10. This open-source library allows you to integrate the Neo smartpen - Neo smartpen N2 and M1 - into your Windows 10 app.  

SDK was made to use Neo smartpen for Windows 10 (UWP). SDK library is built and published to [Nuget](https://www.nuget.org/packages/Neosmartpen.Net/)

## About Neo smartpen 

The Neo smartpen is designed to seamlessly integrate the real and digital worlds by transforming what you write on paper - everything from sketches and designs to business meeting notes - to your iOS, Android and Windows devices. It works in tandem with N notebooks, powered by NeoLAB Convergence’s patented Ncode™ technology and the accompanying application, Neo Notes. Find out more at www.neosmartpen.com
 - Tutorial video - https://goo.gl/MQaVwY
 
## About Ncode™: service development guide 

‘Natural Handwriting’ technology based on Ncode™(Microscopic data patterns containing various types of data) is a handwriting stroke recovery technology that digitizes paper coordinates obtained by optical pen devices such as Neo smartpen. The coordinates then can be used to store handwriting stroke information, analyzed to extract meaning based on user preferences and serve as the basis for many other types of services. 

Click the link below to view a beginners guide to Ncode technology. 
[https://github.com/NeoSmartpen/Documentations/blob/master/Ncode™ Service Development Getting Started Guide v1.01.pdf](https://github.com/NeoSmartpen/Documentations/blob/master/Ncode%E2%84%A2%20Service%20Development%20Getting%20Started%20Guide%20v1.01.pdf)


## Requirements

 - Visual Studio 2015
 - Install the Universal Windows Platform tools provided by Microsoft
 - Min version [10.0; build 10586]
 - need to MarkerMrtro.Unity.Ionic.Zlib library (Nuget Package)
 - Standard Bluetooth Dongles ( Bluetooth Specification Version 2.1 + EDR or later with Microsoft Bluetooth stack )

## Dependencies

 - MarkerMrtro.Unity.Ionic.Zlib

## Supported models

- Neo smartpen N2(F110, F120)
- Neo smartpen M1(F50)



## Getting Started Sample App

At first, download the zip file containing the current version. You can unzip the archive and open NeosmartpenSDK_UWP.sln in Visual Studio 2015.

[download](https://github.com/NeoSmartpen/UWP-SDK/archive/master.zip)


## Using SDK library


Add the NeosmartpenSDK dll to you project and the MarkerMetro.Unity.Ionic.Zlib dll in the Nuget Package
Let's getting started using the API

## Api References

SDK API references page : [References](https://neosmartpen.github.io/UWP-SDK/docs)

## Sample Code

#### Notice

SDK handle data and commucation with peer device in other thread. So if you want data get from pen to appear in UI,  than you have to execute in UI thread.

#### Create BluetoothPenClient and PenController instance
```cs
// create PenController instance.
// PenController control all pen event method
PenController _controller = new PenController();

// Create BluetoothPenClient instance. and bind PenController.
// BluetoothPenClient is implementation of bluetooth function.
BluetoothPenClient _client = new BluetoothPenClient(_controller);
```
#### Find Bluetooth Devices

You can find bluetooth device using below methods. And get **PenInformation** object that has bluetooth device information.

###### Find device
```cs
List<PenInformation> penList = await _client.FindDevices();
```
###### Using watcher

```cs
// bluetooth watcher event
_client.onAddPenController += MClient_onAddPenController;
_client.onRemovePenController += MClient_onRemovePenController;
_client.onStopSearch += MClient_onStopSearch;
_client.onUpdatePenController += MClient_onUpdatePenController;

// start watcher
_client.StartWatcher();

// Event that is called when a device is added by the watcher
private async void MClient_onAddPenController(BluetoothPenClient sender, PenInformation args)
{

}

// Event that is called when a device is updated 
private async void MClient_onUpdatePenController(BluetoothPenClient sender, PenUpdateInformation args)
{

}

// Event that is called when a device is removed
private async void MClient_onRemovePenController(BluetoothPenClient sender, PenUpdateInformation args)
{

}

// Event that is called when the watcher operation has been stopped
private async void MClient_onStopSearch(BluetoothPenClient sender, Windows.Devices.Bluetooth.BluetoothError args)
{

}
```
#### Connect with device

```cs
// penInfomation is PenInformation class object what can be obtained from find device method
bool result = await _client.Connect(penInfomation);
```

#### After Connection is established.

```cs
// add event in init method
_controller.Connected += MController_Connected;
_controller.PasswordRequested += MController_PasswordRequested;
_controller.Authenticated += MController_Authenticated;

// It is called when connection is established ( You cannot use function on your device without authentication )
private void MController_Connected(IPenClient sender, ConnectedEventArgs args)
{
	System.Diagnostics.Debug.WriteLine(String.Format("Mac : {0}\r\n\r\nName : {1}\r\n\r\nSubName : {2}\r\n\r\nFirmware Version : {3}\r\n\r\nProtocol Version : {4}", args.MacAddress, args.DeviceName, args.SubName, args.FirmwareVersion, args.ProtocolVersion));
}

// If your device is locked, it is called to input password.
private void MController_PasswordRequested(IPenClient sender, PasswordRequestedEventArgs args)
{
	System.Diagnostics.Debug.WriteLine($"Retry Count : {args.RetryCount}, ResetCount :  {args.ResetCount }");
    _controller.InputPassword(password);
}

// If your pen is not locked, or authentication is passed, it will be called.
// When it is called, You can use all function on your device.
private void MController_Authenticated(IPenClient sender, object args)
{

}
```

#### Handling a handwriting data from peer device
```cs
// add event in init method
_controller.DotReceived += MController_DotReceived;

// Identifier of note(paper) (it is consist of section and owner, note)
int section = 1;
int owner = 1;
int note = 102;

// Requests to set your note type.
_controller.AddAvailableNote(section, owner, note);

// event that is called when writing data is received
private void MController_DotReceived(IPenClient sender, DotReceivedEventArgs args)
{
// TODO : You should implements code using coordinate data.
}
```

#### Querying list of offline data in Smartpen's storage

```cs
// add event in init method
_controller.OfflineDataListReceived += MController_OfflineDataListReceived;


// Request offline data list
_controller.RequestOfflineDataList();

// Event method to receive offline data list
private void MController_OfflineDataListReceived(IPenClient sender, OfflineDataListReceivedEventArgs args)
{

}

```

#### Downloading offline data in Smartpen's storage
```cs
// add event in init method
_controller.OfflineDataDownloadStarted += MController_OfflineDataDownloadStarted;
_controller.OfflineStrokeReceived += MController_OfflineStrokeReceived;
_controller.OfflineDownloadFinished += MController_OfflineDownloadFinished;

// it is invoked when begins downloading offline data
private async void MController_OfflineDataDownloadStarted(IPenClient sender, object args)
{

}

// it is invoked when it obtained offline data ( it can be called several times )
private async void MController_OfflineStrokeReceived(IPenClient sender, OfflineStrokeReceivedEventArgs args)
{

}

// it is invoked when finished downloading
private async void MController_OfflineDownloadFinished(IPenClient sender, SimpleResultEventArgs args)
{

}
```

## LICENSE

Copyright©2017 by NeoLAB Convergence, Inc. All page content is property of NeoLAB Convergence Inc. <https://neolab.net> 

### GPL License v3 - for non-commercial purpose
    
For non-commercial use, follow the terms of the GNU General Public License. 

GPL License v3 is a free software: you can run, copy, redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation. 

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details. 

You should have received a copy of the GNU General Public License along with the program. If not, see <https://www.gnu.org/licenses/>. 


### 2. Commercial license - for commercial purpose 

For commercial use, it is not necessary or required to open up your source code. Technical support from NeoLAB Convergence, inc is available upon your request. 

Please contact our support team via email for the terms and conditions of this license. 

- Global: _global1@neolab.net
- Korea: _biz1@neolab.net



## Opensource Library

Please refer to the details of the open source library used below.

### 1. MarkerMetro.Unity.Ionic.Zlib (https://github.com/MarkerMetro/MarkerMetro.Unity.Ionic.Zlib/blob/master/LICENSE)

The MIT License (MIT)

Copyright (c) 2015 Marker Metro

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

---
**Feel free to leave any comment or feedback [here](https://github.com/NeoSmartpen/UWP-SDK/issues)**
