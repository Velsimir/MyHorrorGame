using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Velsi.Typecraft.Text;

namespace Velsi.Typecraft.Samples.SimpleDialogue
{
    public sealed class SampleSwitchers : MonoBehaviour
    {
        private static readonly DeviceFamily[] Devices =
        {
            DeviceFamily.KeyboardMouse,
            DeviceFamily.Xbox,
            DeviceFamily.PlayStation,
            DeviceFamily.Switch
        };

        [SerializeField] private Button _locale;
        [SerializeField] private TMP_Text _localeLabel;
        [SerializeField] private Button _device;
        [SerializeField] private TMP_Text _deviceLabel;

        private void Start()
        {
            _locale.onClick.AddListener(NextLocale);
            _device.onClick.AddListener(NextDevice);
            Typecraft.Changed += UpdateLabels;
            UpdateLabels();
        }

        private void OnDestroy() => Typecraft.Changed -= UpdateLabels;

        private void NextLocale()
        {
            ITextSource source = Typecraft.Settings != null ? Typecraft.Settings.TextSource : null;
            List<string> locales = source != null ? source.Locales.ToList() : new List<string>();
            if (locales.Count == 0)
                return;

            int index = locales.IndexOf(Typecraft.Locale);
            source.SelectLocale(locales[(index + 1) % locales.Count]);
        }

        private void NextDevice()
        {
            int index = System.Array.IndexOf(Devices, Typecraft.Device);
            Typecraft.SetDevice(Devices[(index + 1) % Devices.Length]);
        }

        private void UpdateLabels()
        {
            _localeLabel.text = $"Язык / Language: {Typecraft.Locale}";
            _deviceLabel.text = $"Устройство / Device: {Typecraft.Device}";
        }
    }
}
