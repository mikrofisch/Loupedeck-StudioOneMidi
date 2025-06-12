using System.Timers;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Common;

namespace Loupedeck.StudioOneMidiPlugin
{
    public class DialStepsDetector : IDisposable
    {
        private readonly StudioOneMidiPlugin _plugin;
        private readonly HashSet<int> _uniqueValues = new();
        private bool _isActive = false;
        private System.Timers.Timer? _timeoutTimer;
        private const int _timeout = 3000; // 3 seconds

        public DialStepsDetector(StudioOneMidiPlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _plugin.ChannelValueChanged += OnChannelValueChanged;
        }

        public void Activate()
        {
            if (_isActive)
                return;

            _uniqueValues.Clear();
            _isActive = true;

            _timeoutTimer = new System.Timers.Timer(_timeout);
            _timeoutTimer.Elapsed += (s, e) => Deactivate();
            _timeoutTimer.AutoReset = false;
            _timeoutTimer.Start();
        }

        private void OnChannelValueChanged(object? sender, EventArgs e)
        {
            if (!_isActive) return;

            // Count unique integer values across all channels
            foreach (var channel in _plugin.channelData.Values)
            {
                int hash = HashCode.Combine(channel.ChannelID, channel.ValueStr ?? string.Empty);
                if (_uniqueValues.Add(hash))
                {
                    if (_uniqueValues.Count > 9) SendCount(_uniqueValues.Count - 8);
                }
            }
            if (_timeoutTimer != null)
            {
                _timeoutTimer.Interval = _timeout;
            }
        }

        private void SendCount(int count)
        {
            if (_plugin.ConfigMidiOut != null)
            {
                var noteEvent = new NoteOnEvent
                {
                    Channel = (FourBitNumber)15,
                    NoteNumber = (SevenBitNumber)0x12,
                    Velocity = (SevenBitNumber)Math.Min(count, 127)
                };
                _plugin.ConfigMidiOut.SendEvent(noteEvent);
            }
        }

        public void Deactivate()
        {
            if (!_isActive) return;

            _isActive = false;
            _timeoutTimer?.Stop();
            _timeoutTimer?.Dispose();
            _timeoutTimer = null;

            // Indicate deactivation to the app 
            if (_plugin.ConfigMidiOut != null)
            {
                var noteEvent = new NoteOnEvent
                {
                    Channel = (FourBitNumber)15,
                    NoteNumber = (SevenBitNumber)0x13,
                    Velocity = (SevenBitNumber)127
                };
                _plugin.ConfigMidiOut.SendEvent(noteEvent);
            }
        }

        public void Dispose()
        {
            Deactivate();
            _plugin.ChannelValueChanged -= OnChannelValueChanged;
        }
    }
}