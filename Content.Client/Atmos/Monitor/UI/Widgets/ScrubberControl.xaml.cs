using Content.Shared.Atmos;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Atmos.Piping.Unary.Components;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.Atmos.Monitor.UI.Widgets
{
    [GenerateTypedNameReferences]
    public sealed partial class ScrubberControl : BoxContainer
    {
        public VentOrScrubberData Data { get => _data;
            set
            {
                _data = value;
                UpdateFromData();
            }
        }

        private VentOrScrubberData _data;

        private string _address;

        public event Action<string, IAtmosDeviceData>? ScrubberDataChanged;

        private CheckBox _enabled => CEnableDevice;
        private CollapsibleHeading _addressLabel => CAddress;
        private OptionButton _scrubberMode => CScrubberMode;
        private FloatSpinBox _transferRate => CTransferRate;
        private FloatSpinBox _targetPressure => CTargetPressure;

        public ScrubberControl(VentOrScrubberData data, string address)
        {
            RobustXamlLoader.Load(this);

            Name = address;

            _data = data;
            _address = address;

            _addressLabel.Title = Loc.GetString("air-alarm-ui-atmos-net-device-label", ("address", $"{address}"));

            _enabled.OnToggled += _ =>
            {
                _data.Enabled = _enabled.Pressed;
                ScrubberDataChanged?.Invoke(_address, _data);
            };

            _transferRate.OnValueChanged += _ =>
            {
                _data.VolumeRate = _transferRate.Value;
                ScrubberDataChanged?.Invoke(_address, _data);
            };
            _transferRate.IsValid += value => value >= 0;

            _targetPressure.OnValueChanged += _ =>
            {
                _data.TargetPressure = _targetPressure.Value;
                ScrubberDataChanged?.Invoke(_address, _data);
            };
            _targetPressure.IsValid += value => value >= 0;

            foreach (var value in Enum.GetValues<VentOrScrubberMode>())
                _scrubberMode.AddItem(Loc.GetString($"{value}"), (int) value);

            _scrubberMode.OnItemSelected += args =>
            {
                _scrubberMode.SelectId(args.Id);
                _data.Mode = (VentOrScrubberMode) args.Id;
                ScrubberDataChanged?.Invoke(_address, _data);
            };

            UpdateFromData();
        }

        private void UpdateFromData()
        {
            _enabled.Pressed = _data.Enabled;
            _transferRate.Value = _data.VolumeRate;
            _targetPressure.Value = _data.TargetPressure;
            _scrubberMode.SelectId((int) _data.Mode);
        }
    }
}
