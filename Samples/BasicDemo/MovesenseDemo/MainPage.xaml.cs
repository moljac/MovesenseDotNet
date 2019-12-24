using MdsLibrary;
using Plugin.BluetoothLE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace MovesenseDemo
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();
		}

        IDisposable scan;
        public IAdapter BleAdapter => CrossBleAdapter.Current;

        private void OnClicked(object sender, EventArgs e)
        {
            if (BleAdapter.Status == AdapterStatus.PoweredOn)
            {
                DoScan();
            }
            else
            {
                BleAdapter.WhenStatusChanged().Subscribe(status =>
                {
                    if (status == AdapterStatus.PoweredOn)
                    {
                        DoScan();
                    }
                });
            }
        }

        private void DoScan()
        {
            bluetooth_devices = new List<IDevice>();
            StatusLabel.Text = "Scanning for devices...";
            scan = this.BleAdapter.Scan()
            .Subscribe(this.OnScanResult);
        }

        public void StopScanning()
        {
            this.scan?.Dispose();
        }

        List<IDevice> bluetooth_devices = null;
        async void OnScanResult(IScanResult result)
        {
            // Only interested in Movesense devices
            if (result.Device.Name != null)
            {
                if (result.Device.Name.StartsWith("Polar H"))
                {
                    StopScanning();
                    bluetooth_devices.Add(result.Device);
                    StatusLabel.Text = $"Scan found device {result.Device.Name}" + Environment.NewLine + StatusLabel.Text;
                }
                if (result.Device.Name.StartsWith("Movesense"))
                {
                    StopScanning();
                    bluetooth_devices.Add(result.Device);

                    var movesense = Plugin.Movesense.CrossMovesense.Current;

                    movesense.ConnectionListener.DeviceDisconnected += async (s, a) =>
                        {
                            await DisplayAlert("Disconnection", $"Device {a.Serial} disconnected", "OK");
                        };

                    // Now do the Mds connection
                    var sensor = result.Device;
                    StatusLabel.Text = $"Connecting to device {sensor.Name}" + Environment.NewLine + StatusLabel.Text;
                    var movesenseDevice = await movesense.ConnectMdsAsync(sensor.Uuid);

                    // Talk to the device
                    StatusLabel.Text = "Getting device info" + Environment.NewLine + StatusLabel.Text;
                    var info = await movesenseDevice.GetDeviceInfoAsync();

                    StatusLabel.Text = "Getting battery level" + Environment.NewLine + StatusLabel.Text;
                    var batt = await movesenseDevice.GetBatteryLevelAsync();

                    // Turn on the LED
                    StatusLabel.Text = "Turning on LED" + Environment.NewLine + StatusLabel.Text;
                    try
                    {
                        await movesenseDevice.SetLedStateAsync(0, true);
                    }
                    catch(System.Exception exc)
                    {
                        StatusLabel.Text = $"Turning ON LED error {exc.Message}" + Environment.NewLine + StatusLabel.Text;
                    }

                    await DisplayAlert(
                        "Success", 
                        $"Communicated with device {sensor.Name}, firmware version is: {info.DeviceInfo.Sw}, battery: {batt.ChargePercent}", 
                        "OK");

                    // Turn the LED off again
                    StatusLabel.Text = "Turning off LED" + Environment.NewLine + StatusLabel.Text;
                    try
                    {
                        await movesenseDevice.SetLedStateAsync(0, false);
                    }
                    catch (System.Exception exc)
                    {
                        StatusLabel.Text = $"Turning OFF LED error {exc.Message}"+ Environment.NewLine + StatusLabel.Text;
                    }

                    // Disconnect Mds
                    StatusLabel.Text = "Disconnecting" + StatusLabel.Text;
                    await movesenseDevice.DisconnectMdsAsync();
                    StatusLabel.Text = "Disconnected  + StatusLabel.Text";

                }
            }
        }
    }
}
