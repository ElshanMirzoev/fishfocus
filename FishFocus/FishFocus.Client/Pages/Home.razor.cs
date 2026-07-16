using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using FishFocus.Shared.Models;
using FishFocus.Shared.DTOs.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FishFocus.Client.Pages
{
    public partial class Home : ComponentBase, IDisposable
    {
        [Inject]
        protected HttpClient Http { get; set; } = default!;

        [Inject]
        protected ISnackbar Snackbar { get; set; } = default!;

        [Inject]
        protected IJSRuntime JS { get; set; } = default!;

        [Inject]
        protected IDialogService DialogService { get; set; } = default!;

        // Fields
        private bool _isLeaving = false;
        private bool _isRaiseAnimationActive = false;
        private bool isRadioEnabled = false;
        private int radioVolume = 50;
        private string currentTrackUrl = "";
        private int currentTrackIndex = 1;
        private bool isRadioInitialized = false;
        private bool isRainEnabled = false;
        private DateTime _lastRippleTime = DateTime.MinValue;
        private int rainVolume = 50;
        private bool playFog = false;
        private bool showDevTools = true;
        private bool showSplash = false;
        private bool isNightMode = false;
        private bool _isProcessingAuth = false;
        private int selectedMinutes = 30;
        private int secondsLeft = 1800;
        private bool isRunning = false;
        private double _progressValue = 0;
        private string _currentQuote = "Забросьте удочку и начните путь к спокойствию...";
        private bool _halfTimeNagSent = false;
        private PeriodicTimer? timer;
        private CancellationTokenSource cts = new();
        private FishCatchResult? catchResult;
        private int TotalScore = 0;
        private bool _isLoggedIn = false;
        private List<FishCatchResult> CatchHistory = new();
        private CancellationTokenSource? _adjustCts;
        private List<DiaryEntry> _diaryEntries = new();
        private bool _isAdjusting = false;
        private bool isRainInDom = false;
        private bool isRainAnimationActive = false;
        private List<RainDropData> _rainDrops = new();
        private bool _warning30Played = false;
        private bool _fourSecondsPlayed = false;
        private DateTime _lastSoundTime = DateTime.MinValue;
        private bool isThunderEnabled = false;
        private int thunderVolume = 50;
        private bool isLightningStrike = false;
        private List<LightningData> _activeBolts = new();
        private bool isBirdsEnabled = false;
        private int birdsVolume = 50;
        private bool _isAppReady = false;
        private bool _isFirstLoad = true;
        private List<BirdData> _activeBirds = new();
        private bool _isBirdsLoopRunning = false;
        private List<CloudData> _activeClouds = new();
        private FishingAnimationState _animationState = FishingAnimationState.None;

        private int displayHours = 0;
        private int displayMinutes = 30;
        private bool isHoursInputMode = false;
        private bool isMinutesInputMode = false;
        private bool isMouseDown = false;

        // Nested Classes
        private class RainDropData
        {
            public double Left { get; set; }
            public double Delay { get; set; }
            public double Duration { get; set; }
        }

        private class LightningData
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public int Left { get; set; }
            public int Top { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        private class BirdData
        {
            public Guid Id { get; set; } = Guid.NewGuid();
            public string BirdClass { get; set; } = "";
            public int Top { get; set; }
            public double Speed { get; set; }
            public double Delay { get; set; }
            public bool IsFlyingRight { get; set; }
        }

        private class CloudData
        {
            public string Class { get; set; } = "";
            public string DirectionClass { get; set; } = "";
            public double NegDelay { get; set; }
            public int Duration { get; set; }
            public int Top { get; set; }
        }

        private enum FishingAnimationState
        {
            None,
            InitialDrop,
            PassiveFishing,
            ActiveBite,
            FinalRaise
        }

        // Quotes
        private readonly string[] _quotes = new[]
        {
            "Терпение — это ключ к самому крупному улову.",
            "Труд рыбака незаметен, пока в сетях пусто.",
            "Рыбалка — это не про рыбу, это про спокойствие.",
            "Тишина — лучший друг рыбака.",
            "Даже самая маленькая рыбка лучше, чем пустое ведро.",
            "Настоящий рыбак видит не только рыбу, но и красоту момента.",
            "Рыбак рыбака видит издалека."
        };

        // Lifecycle Hooks
        protected override async Task OnInitializedAsync()
        {
            GenerateUniqueClouds();

            if (!isRadioInitialized)
            {
                currentTrackIndex = Random.Shared.Next(1, 11);
                currentTrackUrl = $"audio/radio/track{currentTrackIndex}.mp3";
            }

            await Task.CompletedTask;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                try
                {
                    await LoadUserSettings();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Settings load failed: {ex.Message}");
                }

                await Task.Delay(3000);
                _isAppReady = true;

                _ = RunContinuousBirdsLoop();

                await InvokeAsync(StateHasChanged);
            }
        }

        public void Dispose()
        {
            cts.Cancel();
            timer?.Dispose();
        }
    }
}
