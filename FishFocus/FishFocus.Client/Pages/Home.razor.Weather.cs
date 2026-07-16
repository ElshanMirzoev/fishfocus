using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FishFocus.Client.Pages
{
    public partial class Home
    {
        private string GetContainerAnimationClass()
        {
            if (_isLeaving) return "scene-leave";
            if (isRunning) return "scene-enter";
            return "scene-hidden";
        }

        private string GetFinalShakeClass()
        {
            if (isRunning && secondsLeft > 0 && secondsLeft <= 5)
            {
                return "final-agony";
            }
            return "";
        }

        private async Task HandleMouseMove(MouseEventArgs e)
        {
            var now = DateTime.UtcNow;

            if ((now - _lastRippleTime).TotalMilliseconds > 130)
            {
                _lastRippleTime = now;
                await JS.InvokeVoidAsync("visualEffects.createRipple", ".water-part", e.ClientX, e.ClientY);
            }
        }

        private async Task RunThunderLoop()
        {
            var rnd = new Random();
            while (isThunderEnabled)
            {
                await Task.Delay(rnd.Next(10000, 20000));
                if (!isThunderEnabled) break;
                int count = rnd.Next(1, 4);
                for (int i = 0; i < count; i++)
                {
                    var bolt = new LightningData { Left = rnd.Next(10, 90), Top = rnd.Next(0, 20), Width = rnd.Next(40, 80), Height = rnd.Next(150, 250) };
                    _activeBolts.Add(bolt);
                    await InvokeAsync(StateHasChanged);
                    _ = Task.Delay(400).ContinueWith(async t => { _activeBolts.Remove(bolt); await InvokeAsync(StateHasChanged); });
                    await Task.Delay(rnd.Next(200, 500));
                }
            }
        }

        private async Task RunContinuousBirdsLoop()
        {
            var rnd = new Random();
            string[] birdClasses = { "bird-one", "bird-two", "bird-three", "bird-four" };

            while (true)
            {
                int count = rnd.Next(2, 5);
                int baseTop = rnd.Next(5, 25);
                bool flyRight = rnd.Next(0, 2) == 0;

                for (int i = 0; i < count; i++)
                {
                    var bird = new BirdData
                    {
                        BirdClass = birdClasses[rnd.Next(birdClasses.Length)],
                        Top = baseTop + rnd.Next(-3, 4),
                        Speed = 15.0 + rnd.NextDouble() * 5.0,
                        Delay = i * 0.7,
                        IsFlyingRight = flyRight
                    };

                    _activeBirds.Add(bird);

                    _ = Task.Delay((int)((bird.Speed + bird.Delay) * 1000) + 5000).ContinueWith(async t => {
                        _activeBirds.Remove(bird);
                        await InvokeAsync(StateHasChanged);
                    });
                }

                await InvokeAsync(StateHasChanged);
                await Task.Delay(rnd.Next(10000, 25000));
            }
        }

        private async Task PrepareRain()
        {
            var rnd = new Random();
            _rainDrops = Enumerable.Range(0, 80).Select(_ => new RainDropData
            {
                Left = rnd.NextDouble() * 100,
                Delay = rnd.NextDouble() * 2,
                Duration = 0.5 + rnd.NextDouble() * 0.3
            }).ToList();

            isRainInDom = true;
            StateHasChanged();

            await Task.Delay(100);
            isRainAnimationActive = true;
        }

        private void GenerateUniqueClouds()
        {
            var rnd = new Random();
            string[] cloudClasses = { "cloud-1", "cloud-2", "cloud-3" };
            _activeClouds.Clear();

            for (int i = 0; i < 6; i++)
            {
                int duration = rnd.Next(100, 200);
                _activeClouds.Add(new CloudData
                {
                    Class = cloudClasses[rnd.Next(cloudClasses.Length)],
                    DirectionClass = rnd.Next(0, 2) == 0 ? "drift-right" : "drift-left",
                    NegDelay = -rnd.NextDouble() * duration,
                    Duration = duration,
                    Top = rnd.Next(5, 40)
                });
            }
        }

        private async Task ToggleRain(bool value)
        {
            isRainEnabled = value;

            if (isRainEnabled)
            {
                if (_rainDrops == null || _rainDrops.Count == 0)
                {
                    var rnd = new Random();
                    _rainDrops = Enumerable.Range(0, 80).Select(_ => new RainDropData
                    {
                        Left = rnd.NextDouble() * 100,
                        Delay = rnd.NextDouble() * 2,
                        Duration = 0.5 + rnd.NextDouble() * 0.3
                    }).ToList();
                }

                isRainInDom = true;
                StateHasChanged();

                await Task.Delay(100);
                isRainAnimationActive = true;

                await JS.InvokeVoidAsync("audioInterop.play", "rain-audio");
                await UpdateRainVolume(rainVolume);
            }
            else
            {
                isRainAnimationActive = false;

                await Task.Delay(1500);
                if (!isRainEnabled)
                {
                    isRainInDom = false;
                    await JS.InvokeVoidAsync("audioInterop.pause", "rain-audio");
                }
            }

            await SaveUserSettings();
            StateHasChanged();
        }

        private async Task HandleLakeClick(MouseEventArgs e)
        {
            await JS.InvokeVoidAsync("console.log", "C# поймал клик в зоне воды");
            await JS.InvokeVoidAsync("visualEffects.createRipple", ".water-part", e.ClientX, e.ClientY);
        }

        private async Task TriggerSplash()
        {
            showSplash = true;
            StateHasChanged();

            await Task.Delay(600);

            showSplash = false;
            StateHasChanged();
        }

        private string GetFishingAnimationClass() => _animationState switch
        {
            FishingAnimationState.InitialDrop => "initial-drop",
            FishingAnimationState.PassiveFishing => "passive-fishing",
            FishingAnimationState.ActiveBite => "active-bite",
            FishingAnimationState.FinalRaise => "final-raise",
            _ => ""
        };
    }
}
