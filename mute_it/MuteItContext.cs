using NAudio.CoreAudioApi;
using System;
using System.Drawing;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace mute_it
{
    class MuteItContext : ApplicationContext
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public static int MUTE_CODE = 123;
        public static int UNMUTE_CODE = 234;

        private NotifyIcon tbIcon;
        private HotkeyManager hkManager;

        private bool isMuted;

        private MMDeviceEnumerator deviceEnumerator;
        private NotificationClient notificationClient;
        private MMDevice primaryMicDevice;

        private static readonly string mediaFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\Media";
        private static readonly SoundPlayer speechOnPlayer = new SoundPlayer(Path.Combine(mediaFolder, "Speech On.wav"));
        private static readonly SoundPlayer speechOffPlayer = new SoundPlayer(Path.Combine(mediaFolder, "Speech Off.wav"));

        private static readonly Icon MicOffIcon = Properties.Resources.mic_off;
        private static readonly Icon MicOnIcon = Properties.Resources.mic_on;
        public MuteItContext()
        {
            isMuted = true;

            tbIcon = createIcon();
            hkManager = registerHotkeys();

            deviceEnumerator = new MMDeviceEnumerator();
            notificationClient = new NotificationClient(this);
            deviceEnumerator.RegisterEndpointNotificationCallback(notificationClient);

            updatePrimaryMicDevice();

            tbIcon.Visible = true;
        }

        public void updatePrimaryMicDevice()
        {
            if (primaryMicDevice != null)
            {
                primaryMicDevice.AudioEndpointVolume.OnVolumeNotification -= onVolumeNotification;
            }

            primaryMicDevice = getPrimaryMicDevice();
            if (primaryMicDevice != null)
            {
                primaryMicDevice.AudioEndpointVolume.Mute = isMuted;
                primaryMicDevice.AudioEndpointVolume.OnVolumeNotification += onVolumeNotification;
                tbIcon.Text = primaryMicDevice.DeviceFriendlyName;
            }
            else
            {
                isMuted = true;
                tbIcon.Text = "(None)";
            }

            updateMicStatus();
        }

        private void onVolumeNotification(AudioVolumeNotificationData data)
        {
            if (primaryMicDevice != null)
            {
                isMuted = primaryMicDevice.AudioEndpointVolume.Mute;
            }
            updateMicStatus();
        }

        private HotkeyManager registerHotkeys()
        {
            var manager = new HotkeyManager(this);

            RegisterHotKey(manager.Handle, MUTE_CODE, Constants.ALT + Constants.SHIFT, (int)Keys.P);
            RegisterHotKey(manager.Handle, UNMUTE_CODE, Constants.ALT + Constants.SHIFT, (int)Keys.O);

            return manager;
        }

        private NotifyIcon createIcon()
        {
            var exitMenuItem = new MenuItem("Exit", new EventHandler(handleExit));

            var icon = new NotifyIcon();
            icon.Icon = Properties.Resources.mic_off;
            icon.ContextMenu = new ContextMenu(new MenuItem[] {exitMenuItem});
            icon.DoubleClick += (source, e) => toggleMicStatus();

            return icon;
        }

        private void setMicMuteStatus(bool doMute)
        {
            if (primaryMicDevice != null)
            {
                primaryMicDevice.AudioEndpointVolume.Mute = doMute;
            }
        }
        
        public void muteMic()
        {
            setMicMuteStatus(true);
        }

        public void unmuteMic()
        {
            setMicMuteStatus(false);
        }

        private void toggleMicStatus()
        {
            if (primaryMicDevice != null)
            {
                primaryMicDevice.AudioEndpointVolume.Mute = !primaryMicDevice.AudioEndpointVolume.Mute;
            }
        }

        private void updateMicStatus()
        {
            //tbIcon.Icon = isMuted ? Properties.Resources.mic_off : Properties.Resources.mic_on;
            if (tbIcon.Icon != null && tbIcon.Icon != MicOffIcon && tbIcon.Icon != MicOnIcon)
            {
                tbIcon.Icon.Dispose();
            }
            tbIcon.Icon = isMuted ? MicOffIcon : MicOnIcon;
            //SystemSounds.Beep.Play();
            if (isMuted)
                speechOffPlayer.Play();
            else
                speechOnPlayer.Play();
        }

        private MMDevice getPrimaryMicDevice()
        {
            try
            {
                return deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
            }
            catch (COMException)
            {
                return null;
            }
        }

        private void disposeDevice()
        {
            if (primaryMicDevice != null)
            {
                primaryMicDevice.AudioEndpointVolume.OnVolumeNotification -= onVolumeNotification;
                primaryMicDevice.AudioEndpointVolume.Dispose();
                primaryMicDevice.Dispose();
                primaryMicDevice = null;
            }
        }

        private void handleExit(object sender, EventArgs e)
        {
            disposeDevice();
            deviceEnumerator.UnregisterEndpointNotificationCallback(notificationClient);
            deviceEnumerator.Dispose();
            speechOffPlayer.Dispose();
            speechOnPlayer.Dispose();
            tbIcon.Dispose();
            Dispose();
            Application.Exit();
        }
    }
}
