using NModbus;
using ROLF.Views;
using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace ROLF.ViewModels
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        #region ==================== constants & fields ====================
        private const int MODBUS_TCP_PORT = 502;
        private const int MODBUS_SLAVE_ADDRESS = 0;
        private const int CANOPEN_SLAVE_ADDRESS = 1;
        private const int WTX_REGISTER_DATAWORD_COUNT = 10;
        private const int WTX_REGISTER_EXECUTION_COMMANDS = 0;

        private TcpClient _client;
        private IModbusMaster _master;
        private Timer _processDataTimer;
        private string _weight;
        private string _fillingResult;
        private string _ipaddress;
        private bool _gross = true;
        private string _coarseFlow;
        private string _fineFlow;
        private string _scaleStatus;
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        public INavigation Navigation { get; set; }
     
        public Command NavigateToNetwork { get; set; }

        public Command ConnectDevice { get; set; }

        public Command Tare { get; set; }

        public Command Zero { get; set; }

        public Command GrossNet { get; set; }

        public Command StartFilling { get; set; }

        public Command BreakFilling { get; set; }


        public MainPageViewModel(INavigation navigation)
        {
            Navigation = navigation;
            NavigateToNetwork = new Command(async () => await NavigateToNetworkSettingsPage());

            Weight = "--------";

            ConnectDevice = new Command(ConnectIP);
            Zero = new Command(ZeroDevice);
            Tare = new Command(TareDevice);
            GrossNet = new Command(GrossNetDevice);
            StartFilling = new Command(StartFillingDevice);
            BreakFilling = new Command(BreakFillingDevice);


            Application.Current.Properties["ipaddress"] = "172.19.103.100";
            Application.Current.SavePropertiesAsync();
            if (Application.Current.Properties.ContainsKey("ipaddress"))
            {
                IPAddress = Application.Current.Properties["ipaddress"] as string;
            }
            else
            {
                Application.Current.Properties["ipaddress"] = "172.19.103.100";
                Application.Current.SavePropertiesAsync();
            }
            _processDataTimer = new Timer(ProcessDataUpdateTick, null, Timeout.Infinite, Timeout.Infinite);

            ScaleStatus = "Connnecting...";
 
        }


        private string UIntToWeight(uint weight_d)
        {
            int result;
            result = (int)weight_d;
            return ((double)result / 10.0).ToString("0.0 ml");
        }


        private void ProcessDataUpdateTick(object info)
        {
            if ((_client != null) && _client.Connected)
            {
                ScaleStatus = "";
                _processDataTimer.Change(Timeout.Infinite, Timeout.Infinite);
                try
                {
                    ushort[] _data = _master.ReadHoldingRegisters(MODBUS_SLAVE_ADDRESS, (CANOPEN_SLAVE_ADDRESS * 10), WTX_REGISTER_DATAWORD_COUNT);
                    if (_data.Length > 4)
                    {
                        if ((_data[3] & 0x0080) > 0)
                        {
                            Weight = "--------";
                            ScaleStatus = "OVFL";
                        }
                        else
                        {
                            Weight = UIntToWeight(_data[1] + (uint)(_data[2] << 16));
                            ScaleStatus = "";
                        }
                        FillingResult = UIntToWeight(_data[4] + (uint)(_data[5] << 16));

                        if ((_data[3] & 0x0001) > 0)
                            CoarseFlow = "COARSE";
                        else
                            CoarseFlow = "";

                        if ((_data[3] & 0x0002) > 0)
                            _fineFlow = "FINE";
                        else
                            _fineFlow = "";

                        if ((_data[3] & 0x0004) > 0)
                            ScaleStatus = "READY";
                        else
                            ScaleStatus = "";

                        if ((_data[3] & 0x0040) > 0)
                            ScaleStatus = "ALARM";
                        else
                            ScaleStatus = "";
                    }
                    else
                        Weight = "Communication error";
                }
                catch (Exception ex)
                {
                    Weight = ex.Message;
                }
                finally
                {

                    _processDataTimer.Change(200, 200);
                }
            }
        }


        public String IPAddress
        {
            get { return _ipaddress; }
            set
            {
                _ipaddress = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IPAddress"));
            }
        }
               
        public string Weight
        {
            get
            {
                return _weight;
            }
            set
            {
                if (_weight != value)
                {
                    _weight = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Weight"));
                }
            }
        }

        public string CoarseFlow
        {
            get
            {
                return _coarseFlow;
            }
            set
            {
                if (_coarseFlow != value)
                {
                    _coarseFlow = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CoarseFlow"));
                }
            }
        }

        public string FineFlow
        {
            get
            {
                return _fineFlow;
            }
            set
            {
                if (_fineFlow != value)
                {
                    _fineFlow = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FineFlow"));
                }
            }
        }

        public string FillingResult
        {
            get { return _fillingResult; }
            set
            {
                _fillingResult = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("FillingResult"));
            }
        }


        public String ScaleStatus
        {
            get { return _scaleStatus; }
            set
            {
                _scaleStatus = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ScaleStatus"));
            }
        }


        private void ConnectIP()
        {
            if (_master != null)
            {
                _master.Dispose();
            }
            Connect();
        }

        public void Connect()
        {
            try
            {
                _client = new TcpClient(IPAddress, MODBUS_TCP_PORT);
                var factory = new ModbusFactory();
                _master = factory.CreateMaster(_client);
                if (_client.Connected)
                {
                    Application.Current.Properties["ipaddress"] = IPAddress;
                    Application.Current.SavePropertiesAsync();
                    _processDataTimer.Change(500, 200);
                }
            }
            catch (Exception)
            {
            }
        }

        public void Disconnect()
        {
            _client.Close();
        }

        /// <summary>
        /// Addressing Modbus/TCP am Gateway 1-GA-CACCET:
        /// CANopenAddress * 10 + 
        /// 0: (1 Reg) 1=New measured value
        /// 1: (2 Reg) Measured value
        /// 3: (1 Reg) Measured value status
        /// 4: (2 Reg) Filling result
        /// 6: (1 Reg) Filling result status
        /// 7: (1 Reg) 
        /// 8: (1 Reg) 
        /// 9: (1 Reg) Control word 
        ///     Bit 0 : Tare (0x01)
        ///     Bit 1 : Select Gross (0x02)
        ///     Bit 2´: Clear results (0x04)
        ///     Bit 3 : Start filler (0x08)
        ///     Bit 4 : Stop filler (0x10)
        ///     Bit 5 : Clear trigger (0x11)
        ///     Bit 6 : Zero (0x12)
        ///     Bit 7 : Clear peak (0x14)
        ///     Bit 10: Out 1 (0x21)
        ///     Bit 11: Out 2 (0x22)
        ///     Bit 12: Out 3 (0x24)
        ///     Bit 13: Out 4 (0x29)
        ///     Bit 14: Out 5 (0x30)
        ///     Bit 15: Out 6 (0x31)
        /// </summary>
        private void SendControlWordComand(ushort command)
        {
            ushort grosscomand = 0x0200;
            if (_gross)
                grosscomand = 0x0000;
            command |= grosscomand;

            _master.WriteMultipleRegisters(MODBUS_SLAVE_ADDRESS, CANOPEN_SLAVE_ADDRESS * 10 + 9, new ushort[] { command });
            Thread.Sleep(100);
            _master.WriteMultipleRegisters(MODBUS_SLAVE_ADDRESS, CANOPEN_SLAVE_ADDRESS * 10 + 9, new ushort[] { grosscomand });
        }
        
        public void TareDevice()
        {
            SendControlWordComand(0x0100);
        }

        public void ZeroDevice()
        {
            SendControlWordComand(0x1200);
        }

        public void GrossNetDevice()
        {
            if (_gross)
            {
                _master.WriteMultipleRegisters(MODBUS_SLAVE_ADDRESS, CANOPEN_SLAVE_ADDRESS * 10 + 9, new ushort[] { 0x0200 });
            }
            else
            {
                _master.WriteMultipleRegisters(MODBUS_SLAVE_ADDRESS, CANOPEN_SLAVE_ADDRESS * 10 + 9, new ushort[] { 0x0000 });
            }
        }

        public void StartFillingDevice()
        {
            SendControlWordComand(0x0800);
        }

        public void BreakFillingDevice()
        {
            SendControlWordComand(0x1000);
        }


        public async Task NavigateToNetworkSettingsPage()
        {
            Weight = "NETWORK";
            await Navigation.PushAsync(new NetworkSettingsPage());
        }
    }
}
