using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Projekt
{
    class AudioPlayer : IDisposable
    {
        private WaveOutEvent _outputDevice;
        private AudioFileReader _audioFile;


        public void PlayBackgroundMusic(string relativePath, float volume = 0.5f)
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Audio file not found at: {fullPath}");
            }

            _outputDevice = new WaveOutEvent();
            _audioFile = new AudioFileReader(fullPath);
            _outputDevice.Init(_audioFile);
            _outputDevice.Volume = volume;
            _outputDevice.Play();
        }

        public void PlayBackgroundMusic2(string filePath, float volume = 0.5f)
        {
            Stop();

            _outputDevice = new WaveOutEvent();
            _audioFile = new AudioFileReader(filePath);

            _outputDevice.Init(_audioFile);
            _outputDevice.Volume = volume;
            _outputDevice.Play();

            // Loop the music
            _audioFile.Position = 0;
            _outputDevice.PlaybackStopped += (s, e) =>
            {
                if (_outputDevice != null)
                {
                    _audioFile.Position = 0;
                    _outputDevice.Play();
                }
            };
        }

        public void Stop()
        {
            _outputDevice?.Stop();
            _outputDevice?.Dispose();
            _audioFile?.Dispose();
        }

        public void SetVolume(float volume)
        {
            if (_outputDevice != null)
                _outputDevice.Volume = volume;
        }

        public void Dispose() => Stop();
    }
}
