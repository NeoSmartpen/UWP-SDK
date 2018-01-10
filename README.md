Neo smartpen SDK for Windows Universal Platform(UWP)
===================

## What is Neo smartpen

Neo smartpen is a pen designed to capture your handwritten notes into our digital app with our Ncode notebooks.

You can see more information at http://neosmartpen.com


## Introduction

SDK was made to use Neo smartpen for Windows 10 (UWP). SDK library is built and published to [Nuget](https://www.nuget.org/packages/Neosmartpen.Net/)

This Repository is composed of SDK and sample app project. So you can use and test all SDK feature in sample app

Directory Structure:

 - NeosmartpenSDK_UWP\Neosmartpen.Net contains SDK project
 - NeosmartpenSDK_UWP\SampleApp contain Sample app project

You can see pen protocol information at [Link](https://github.com/NeoSmartpen/Documentations)



## Supported Neo smartpen

- F110 (N2)
- F50, F120


## Requirements

 - Visual Studio 2015
 - Install the Universal Windows Platform tools provided by Microsoft
 - Min version [10.0; build 10586]
 - need to MarkerMrtro.Unity.Ionic.Zlib library (Nuget Package)
 - Standard Bluetooth Dongles ( Bluetooth Specification Version 2.1 + EDR or later with Microsoft Bluetooth stack )

## Dependencies

 - MarkerMrtro.Unity.Ionic.Zlib

## Getting Started Sample App

At first, download the zip file containing the current version. You can unzip the archive and open NeosmartpenSDK_UWP.sln in Visual Studio 2015.

[download](https://github.com/NeoSmartpen/UWPSDK/archive/master.zip)


## Using SDK library


Add the NeosmartpenSDK dll to you project and the MarkerMetro.Unity.Ionic.Zlib dll in the Nuget Package
Let's getting started using the API

## Api References

SDK API references page : [References](https://neosmartpen.github.io/UWPSDK/docs)

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

### Ncode Coodinate Description

+ **Dot.X, Dot.Y**
Coordinates of our NCode cell.( NCode's cell size is 2.371mm )

+ **How to get millimeter unit from NCode unit**
Dot.X x 2.371 = millimeter unit

## Give Feedback


Please reports bugs or issues to [link](https://github.com/NeoSmartpen/UWPSDK/issues)


## Ncode™ SERVICE DEVELOPMENT GETTING STARTED GUIDE

<< https://github.com/NeoSmartpen/Documentations/blob/master/Ncode™ Service Development Getting Started Guide v1.01.pdf >>
 
## LICENSE

NeoSmartpen SDK is Copyright (c) 2017 NeoLAB Convergence, Inc.

We provide two types of license for Pen SDK.

### 1. GPL license v3
    
This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version. 
    
This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details. 
    
You should have received a copy of the GNU General Public License along with this program. 
    
If not, see <http://www.gnu.org/licenses/>.
    
*ps: normally, 3rd party developer can inquiry via the github issue column, but depending on the situation of internal, the answer may be delayed somewhat.



### 2. Commercial license

That does not require the source code open to be released, and technical support is available.

Please contact following to get more information:

- Korea: _dombiz@neolab.net
- Global: _globiz@neolab.net