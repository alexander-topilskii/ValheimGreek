using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ValheimGreek
{
    /// <summary>
    /// Тот же выбор, что в Greek3 <c>speak.js</c>: язык el-GR, новая фраза обрывает предыдущую.
    /// На macOS говорит AVSpeechSynthesizer в процессе игры: <c>say</c> каждый раз стартует заново и опаздывает.
    /// </summary>
    public static class GreekSpeaker
    {
        private const uint Utf8 = 0x08000100;
        private const string Language = "el-GR";

        private static bool _ready;
        private static bool _warned;
        private static IntPtr _synthA;
        private static IntPtr _synthB;
        private static IntPtr _current;
        private static IntPtr _voice;
        private static IntPtr _selSpeak;
        private static IntPtr _selStop;
        private static IntPtr _selSetVoice;
        private static IntPtr _selSpeaking;
        private static Process? _sayProcess;
        private static string? _sayVoice;

        public static void Prepare()
        {
            if (_ready)
                return;
            _ready = true;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Warn("Greek speech is only available on macOS.");
                return;
            }

            try
            {
                dlopen("/System/Library/Frameworks/AVFoundation.framework/AVFoundation", 1);
                var synthClass = objc_getClass("AVSpeechSynthesizer");
                var voiceClass = objc_getClass("AVSpeechSynthesisVoice");
                if (synthClass == IntPtr.Zero || voiceClass == IntPtr.Zero)
                {
                    Warn("AVSpeechSynthesizer is unavailable. Falling back to say.");
                    CacheSayVoice();
                    return;
                }

                var alloc = sel_registerName("alloc");
                var init = sel_registerName("init");
                _synthA = Send(Send(synthClass, alloc), init);
                _synthB = Send(Send(synthClass, alloc), init);
                _current = _synthA;
                if (_synthA == IntPtr.Zero || _synthB == IntPtr.Zero)
                {
                    Warn("Could not create a speech synthesizer. Falling back to say.");
                    CacheSayVoice();
                    return;
                }

                var lang = Cf(Language);
                _voice = Send1(voiceClass, sel_registerName("voiceWithLanguage:"), lang);
                CFRelease(lang);
                if (_voice == IntPtr.Zero)
                {
                    Warn("No el-GR voice installed. Falling back to say.");
                    CacheSayVoice();
                    return;
                }

                Send(_voice, sel_registerName("retain"));
                _selSpeak = sel_registerName("speakUtterance:");
                _selStop = sel_registerName("stopSpeakingAtBoundary:");
                _selSetVoice = sel_registerName("setVoice:");
                _selSpeaking = sel_registerName("isSpeaking");
                Plugin.Log.LogInfo("Greek voice: " + Language);
            }
            catch (Exception ex)
            {
                Warn("Could not prepare Greek speech.");
                Plugin.Log.LogError(ex);
            }
        }

        public static void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;
            Prepare();
            if (_voice == IntPtr.Zero || _current == IntPtr.Zero)
            {
                SpeakWithSay(text);
                return;
            }

            try
            {
                var previous = _current;
                var next = previous == _synthA ? _synthB : _synthA;
                if (SendBool(previous, _selSpeaking) != 0)
                    SendStop(previous, _selStop, 0);
                _current = next;

                var cf = Cf(text);
                var utterance = Send1(objc_getClass("AVSpeechUtterance"), sel_registerName("speechUtteranceWithString:"), cf);
                CFRelease(cf);
                if (utterance == IntPtr.Zero)
                    return;
                Send1(utterance, _selSetVoice, _voice);
                Send1(next, _selSpeak, utterance);
            }
            catch (Exception ex)
            {
                Warn("Could not speak the answer.");
                Plugin.Log.LogError(ex);
            }
        }

        public static void Stop()
        {
            try
            {
                if (_selStop != IntPtr.Zero)
                {
                    if (_synthA != IntPtr.Zero && SendBool(_synthA, _selSpeaking) != 0)
                        SendStop(_synthA, _selStop, 0);
                    if (_synthB != IntPtr.Zero && SendBool(_synthB, _selSpeaking) != 0)
                        SendStop(_synthB, _selStop, 0);
                }
            }
            catch (Exception)
            {
            }

            StopSay();
        }

        private static void SpeakWithSay(string text)
        {
            if (string.IsNullOrEmpty(_sayVoice) || !File.Exists("/usr/bin/say"))
                return;
            StopSay();
            try
            {
                var start = new ProcessStartInfo
                {
                    FileName = "/usr/bin/say",
                    Arguments = "-v " + Quote(_sayVoice) + " -- " + Quote(text),
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                _sayProcess = Process.Start(start);
            }
            catch (Exception ex)
            {
                Warn("Could not speak the answer.");
                Plugin.Log.LogError(ex);
            }
        }

        private static void StopSay()
        {
            var process = _sayProcess;
            _sayProcess = null;
            if (process == null)
                return;
            try
            {
                if (!process.HasExited)
                    process.Kill();
            }
            catch (Exception)
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        private static void CacheSayVoice()
        {
            if (!File.Exists("/usr/bin/say"))
            {
                Warn("say was not found. Greek answers will not be spoken.");
                return;
            }

            try
            {
                var start = new ProcessStartInfo
                {
                    FileName = "/usr/bin/say",
                    Arguments = "-v ?",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var process = Process.Start(start))
                {
                    if (process == null)
                        return;
                    var listing = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(3000);
                    _sayVoice = PickGreekVoice(listing);
                }

                if (string.IsNullOrEmpty(_sayVoice))
                    Warn("No el-GR voice installed. Greek answers will not be spoken.");
            }
            catch (Exception ex)
            {
                Warn("Could not list speech voices.");
                Plugin.Log.LogError(ex);
            }
        }

        private static string? PickGreekVoice(string listing)
        {
            if (string.IsNullOrEmpty(listing))
                return null;
            var lines = listing.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.IndexOf("el_GR", StringComparison.OrdinalIgnoreCase) < 0 &&
                    line.IndexOf("el-GR", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                var name = line.Trim();
                var space = name.IndexOfAny(new[] { ' ', '\t' });
                if (space > 0)
                    name = name.Substring(0, space);
                return name.Length == 0 ? null : name;
            }

            return null;
        }

        private static IntPtr Cf(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var terminated = new byte[bytes.Length + 1];
            Buffer.BlockCopy(bytes, 0, terminated, 0, bytes.Length);
            return CFStringCreateWithCString(IntPtr.Zero, terminated, Utf8);
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "") + "\"";
        }

        private static void Warn(string message)
        {
            if (_warned)
                return;
            _warned = true;
            Plugin.Log.LogWarning(message);
        }

        [DllImport("/usr/lib/libSystem.dylib")]
        private static extern IntPtr dlopen(string path, int mode);

        [DllImport("/usr/lib/libobjc.A.dylib")]
        private static extern IntPtr objc_getClass(string name);

        [DllImport("/usr/lib/libobjc.A.dylib")]
        private static extern IntPtr sel_registerName(string name);

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr Send(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr Send1(IntPtr receiver, IntPtr selector, IntPtr arg);

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
        private static extern byte SendBool(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
        private static extern byte SendStop(IntPtr receiver, IntPtr selector, nint boundary);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern IntPtr CFStringCreateWithCString(IntPtr alloc, byte[] text, uint encoding);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRelease(IntPtr value);
    }
}
