using System.Media;
using System.Reflection;

namespace RestWorkTimer.UI;

internal static class AlertSound
{
    public static void Play(bool urgent)
    {
        var resourceName = urgent
            ? "RestWorkTimer.Assets.alert-30.wav"
            : "RestWorkTimer.Assets.alert-60.wav";
        var repeats = urgent ? 3 : 2;

        Task.Run(() =>
        {
            try
            {
                for (var i = 0; i < repeats; i++)
                {
                    using var stream = Assembly.GetExecutingAssembly()
                        .GetManifestResourceStream(resourceName);
                    if (stream is null)
                    {
                        SystemSounds.Exclamation.Play();
                        return;
                    }

                    using var player = new SoundPlayer(stream);
                    player.Load();
                    player.PlaySync();
                    Thread.Sleep(180);
                }
            }
            catch
            {
                SystemSounds.Exclamation.Play();
            }
        });
    }
}
