using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace FishFocus.Client.Pages
{
    public partial class Home
    {
        private async Task ToggleRadio(bool value)
        {
            isRadioEnabled = value;

            if (isRadioEnabled)
            {
                if (string.IsNullOrEmpty(currentTrackUrl))
                {
                    currentTrackIndex = Random.Shared.Next(1, 11);
                    currentTrackUrl = $"audio/radio/track{currentTrackIndex}.mp3";
                    StateHasChanged();
                    await Task.Delay(300);
                }

                await JS.InvokeVoidAsync("audioInterop.setVolume", "radio-audio", radioVolume);

                if (!isRadioInitialized)
                {
                    double randomStartTime = Random.Shared.Next(15, 75);
                    await JS.InvokeVoidAsync("audioInterop.forceRadioStart", "radio-audio", randomStartTime);
                    isRadioInitialized = true;
                }
                else
                {
                    await JS.InvokeVoidAsync("audioInterop.play", "radio-audio");
                }
            }
            else
            {
                await JS.InvokeVoidAsync("audioInterop.pause", "radio-audio");
            }

            await SaveUserSettings();
        }

        private async Task ToggleBirds(bool value)
        {
            isBirdsEnabled = value;
            if (isBirdsEnabled)
            {
                await JS.InvokeVoidAsync("audioInterop.play", "birds-audio");
                await UpdateBirdsVolume(birdsVolume);
            }
            else
            {
                _ = FadeOutBirdsAudio();
            }
            await SaveUserSettings();
        }

        private async Task FadeOutBirdsAudio()
        {
            int currentVol = birdsVolume;
            while (currentVol > 0)
            {
                currentVol -= 5;
                if (currentVol < 0) currentVol = 0;
                await JS.InvokeVoidAsync("audioInterop.setVolume", "birds-audio", currentVol);
                await Task.Delay(150);
            }
            await JS.InvokeVoidAsync("audioInterop.pause", "birds-audio");
        }

        private async Task ToggleThunder(bool value)
        {
            isThunderEnabled = value;
            if (isThunderEnabled)
            {
                await JS.InvokeVoidAsync("audioInterop.play", "thunder-audio");
                await UpdateThunderVolume(thunderVolume);
                _ = RunThunderLoop();
            }
            else
            {
                await JS.InvokeVoidAsync("audioInterop.pause", "thunder-audio");
            }
            await SaveUserSettings();
        }

        private async Task UpdateBirdsVolume(int value)
        {
            birdsVolume = value;
            await JS.InvokeVoidAsync("audioInterop.setVolume", "birds-audio", value);
            await SaveUserSettings();
        }

        private async Task UpdateThunderVolume(int value)
        {
            thunderVolume = value;
            await JS.InvokeVoidAsync("audioInterop.setVolume", "thunder-audio", value);
            await SaveUserSettings();
        }

        private async Task UpdateRadioVolume(int value)
        {
            radioVolume = value;
            if (isRadioEnabled)
            {
                await JS.InvokeVoidAsync("audioInterop.setVolume", "radio-audio", radioVolume);
            }
            _ = SaveUserSettings();
        }

        private async Task PlayNextTrack()
        {
            currentTrackIndex = (currentTrackIndex >= 10) ? 1 : currentTrackIndex + 1;
            currentTrackUrl = $"audio/radio/track{currentTrackIndex}.mp3";

            StateHasChanged();
            await Task.Delay(200);

            await JS.InvokeVoidAsync("audioInterop.play", "radio-audio");
            await UpdateRadioVolume(radioVolume);
        }

        private async Task UpdateRainVolume(int value)
        {
            rainVolume = value;
            await JS.InvokeVoidAsync("audioInterop.setVolume", "rain-audio", rainVolume);
            await SaveUserSettings();
            StateHasChanged();
        }
    }
}
