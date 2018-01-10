using Neosmartpen.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace SampleApp
{
	public sealed partial class MainPage
	{
		private PenStatusReceivedEventArgs currentStatusArgs;
		private PenStatusReceivedEventArgs lastStatusArgs;
		private SimpleResultEventArgs lastStatusResultArgs;
		private async void ButtonPenStatusTest_Click(object sender, RoutedEventArgs e)
		{
			await Task.Factory.StartNew(() =>
			{
				isTest = true;
				_controller.PenStatusReceived += _controller_PenStatusReceived;
				_controller.PenColorChanged += _controller_PenColorChanged;
				_controller.BeepSoundChanged += _controller_BeepSoundChanged;
				_controller.AutoPowerOffTimeChanged += _controller_AutoPowerOffTimeChanged;
				_controller.AutoPowerOnChanged += _controller_AutoPowerOnChanged;

				_controller.RequestPenStatus();
				autoResetEvent.WaitOne();
				SaveCurrentStatus();

				OutputPrint($"##########################################");
				OutputPrint("Start Pen Status Change");

				TestStartV1();


				RollbackCurrentStatus();

				_controller.PenStatusReceived -= _controller_PenStatusReceived;
				_controller.PenColorChanged -= _controller_PenColorChanged;
				_controller.BeepSoundChanged -= _controller_BeepSoundChanged;
				_controller.AutoPowerOffTimeChanged -= _controller_AutoPowerOffTimeChanged;
				_controller.AutoPowerOnChanged -= _controller_AutoPowerOnChanged;
			});
		}

		private void _controller_AutoPowerOnChanged(IPenClient sender, SimpleResultEventArgs args)
		{
			lastStatusResultArgs = args;
			autoResetEvent.Set();
		}

		private void _controller_AutoPowerOffTimeChanged(IPenClient sender, SimpleResultEventArgs args)
		{
			lastStatusResultArgs = args;
			autoResetEvent.Set();
		}

		private void _controller_BeepSoundChanged(IPenClient sender, SimpleResultEventArgs args)
		{
			lastStatusResultArgs = args;
			autoResetEvent.Set();
		}

		private void _controller_PenColorChanged(IPenClient sender, SimpleResultEventArgs args)
		{
			lastStatusResultArgs = args;
			autoResetEvent.Set();
		}

		private void SaveCurrentStatus()
		{
			currentStatusArgs = lastStatusArgs;
		}

		private void RollbackCurrentStatus()
		{
			// 공통 
			if (_controller.Protocol == Protocols.V1)
			{
				_controller.SetColor(currentStatusArgs.PenColor);
			}
			_controller.SetAutoPowerOnEnable(currentStatusArgs.AutoPowerOn);
			_controller.SetBeepSoundEnable(currentStatusArgs.Beep);
			_controller.SetAutoPowerOffTime(currentStatusArgs.AutoShutdownTime);
		}

		private bool TestStartV1()
		{
			return TestStartV2();
		}
		private bool TestStartV2()
		{
			OutputPrint($"##########################################");
			OutputPrint("AutoPower OnOff Test");
			OutputPrint("");

			OutputPrint("AutoPower Off");
			_controller.SetAutoPowerOnEnable(false);
			autoResetEvent.WaitOne();
			if (lastStatusResultArgs.Result == false)
			{
				OutputPrint("Failed AutoPower off Result");
				return false;
			}
			_controller.RequestPenStatus();
			autoResetEvent.WaitOne();
			if (lastStatusArgs.AutoPowerOn == true)
			{
				OutputPrint("Failed AutoPower off setting");
				return false;
			}

			OutputPrint("AutoPower On");
			_controller.SetAutoPowerOnEnable(true);
			autoResetEvent.WaitOne();
			if (lastStatusResultArgs.Result == false)
			{
				OutputPrint("Failed AutoPower on Result");
				return false;
			}
			_controller.RequestPenStatus();
			autoResetEvent.WaitOne();
			if (lastStatusArgs.AutoPowerOn == false)
			{
				OutputPrint("Failed AutoPower on setting");
				return false;
			}

			OutputPrint($"##########################################");
			OutputPrint("Beep OnOff Test");
			OutputPrint("");

			OutputPrint("Beep Off");
			_controller.SetBeepSoundEnable(false);
			autoResetEvent.WaitOne();
			if (lastStatusResultArgs.Result == false)
			{
				OutputPrint("Failed beep off Result");
				return false;
			}
			_controller.RequestPenStatus();
			autoResetEvent.WaitOne();
			if (lastStatusArgs.Beep == true)
			{
				OutputPrint("Failed beep off setting");
				return false;
			}

			OutputPrint("Beep on");
			_controller.SetBeepSoundEnable(true);
			autoResetEvent.WaitOne();
			if (lastStatusResultArgs.Result == false)
			{
				OutputPrint("Failed beep on Result");
				return false;
			}
			_controller.RequestPenStatus();
			autoResetEvent.WaitOne();
			if (lastStatusArgs.Beep == false)
			{
				OutputPrint("Failed beep on setting");
				return false;
			}

			OutputPrint($"##########################################");
			OutputPrint("Auto shutdown time test");
			OutputPrint("");

			AutoShutdownTimeTest(5);
			AutoShutdownTimeTest(10);
			AutoShutdownTimeTest(100);
			AutoShutdownTimeTest(short.MaxValue);
			AutoShutdownTimeTest(-1);

			return true;
		}

		private bool AutoShutdownTimeTest(short time)
		{
			_controller.SetAutoPowerOffTime(time);
			autoResetEvent.WaitOne();
			if ( lastStatusResultArgs.Result == false)
			{
				OutputPrint($"Failed auto shutdown time : {time}");
				return false;
			}

			_controller.RequestPenStatus();
			autoResetEvent.WaitOne();
			if (lastStatusArgs.AutoShutdownTime != time)
			{
				OutputPrint($"Failed auto shutdown time set : {time}, status {lastStatusArgs.AutoShutdownTime}");
				return false;
			}

			return true;
		}

		private void _controller_PenStatusReceived(Neosmartpen.Net.IPenClient sender, Neosmartpen.Net.PenStatusReceivedEventArgs args)
		{
			lastStatusArgs = args;
			autoResetEvent.Set();
		}
	}
}
