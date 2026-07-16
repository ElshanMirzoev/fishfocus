using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using FishFocus.Shared.Models;
using FishFocus.Client.Dialogs;
using System;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace FishFocus.Client.Pages
{
    public partial class Home
    {
        private string timeDisplay => $"{(secondsLeft / 60):D2}:{(secondsLeft % 60):D2}";

        private async Task StopTimer()
        {
            if (_isLeaving) return;

            cts.Cancel();
            timer?.Dispose();
            _animationState = FishingAnimationState.None;

            _isLeaving = true;
            isRunning = false;

            _animationState = FishingAnimationState.FinalRaise;
            StateHasChanged();

            await Task.Delay(250);

            _isLeaving = false;
            StateHasChanged();
        }

        private async Task HandleZoneClick(string type, int step)
        {
            if (isRunning) return;

            _isAdjusting = true;
            _adjustCts = new CancellationTokenSource();

            try
            {
                int currentDelay = 300;

                while (_isAdjusting && !_adjustCts.Token.IsCancellationRequested)
                {
                    ChangeValue(type, step);

                    var now = DateTime.Now;
                    if ((now - _lastSoundTime).TotalMilliseconds > 80)
                    {
                        _lastSoundTime = now;
                        _ = JS.InvokeVoidAsync("playTickSound");
                    }

                    StateHasChanged();

                    await Task.Delay(currentDelay, _adjustCts.Token);

                    if (currentDelay > 50)
                        currentDelay -= 40;
                }
            }
            catch (TaskCanceledException) { }
        }

        private void SyncSelectedMinutes()
        {
            if (displayHours >= 2)
            {
                displayHours = 2;
                displayMinutes = 0;
            }

            selectedMinutes = (displayHours * 60) + displayMinutes;
            if (!isRunning) secondsLeft = selectedMinutes * 60;

            _ = CheckAuthAndSave();

            StateHasChanged();
        }

        private async Task CheckAuthAndSave()
        {
            var token = await JS.InvokeAsync<string?>("localStorage.getItem", "jwt_token");
            if (!string.IsNullOrEmpty(token) && !_isProcessingAuth)
            {
                await SaveUserSettings();
            }
        }

        private async Task HandleMouseDown(MouseEventArgs e, string type)
        {
            if (isRunning || isHoursInputMode || isMinutesInputMode) return;

            bool isTopHalf = e.OffsetY < 40;
            int step = isTopHalf ? 1 : -1;

            _isAdjusting = true;
            _adjustCts = new CancellationTokenSource();

            try
            {
                int currentDelay = 300;

                while (_isAdjusting)
                {
                    ChangeValue(type, step);
                    StateHasChanged();

                    await Task.Delay(currentDelay, _adjustCts.Token);

                    if (currentDelay > 50) currentDelay -= 40;
                }
            }
            catch (TaskCanceledException) { }
        }

        private void HandleMouseUp()
        {
            _isAdjusting = false;
            _adjustCts?.Cancel();
        }

        private void HandleWheel(WheelEventArgs e, string type)
        {
            if (isRunning || !isMouseDown) return;

            int step = e.DeltaY < 0 ? 1 : -1;
            ChangeValue(type, step);
        }

        private void ChangeValue(string type, int step)
        {
            if (isHoursInputMode || isMinutesInputMode) return;

            if (type == "hours")
            {
                displayHours += step;
                if (displayHours > 2) displayHours = 0;
                if (displayHours < 0) displayHours = 2;

                if (displayHours == 2) displayMinutes = 0;
            }
            else
            {
                if (displayHours == 2) return;

                int remainder = displayMinutes % 5;

                if (remainder == 0)
                {
                    displayMinutes += (step * 5);
                }
                else
                {
                    if (step > 0)
                    {
                        displayMinutes += (5 - remainder);
                    }
                    else
                    {
                        displayMinutes -= remainder;
                    }
                }
                if (displayMinutes > 55) displayMinutes = 0;
                if (displayMinutes < 0) displayMinutes = 55;
            }

            SyncSelectedMinutes();
        }

        private async Task EnableInputMode(string type)
        {
            if (isRunning) return;
            HandleMouseUp();
            if (type == "hours") isHoursInputMode = true;
            else isMinutesInputMode = true;
            StateHasChanged();
            await Task.Delay(50);
            string elementId = type == "hours" ? "h-input-field" : "m-input-field";
            await JS.InvokeVoidAsync("audioInterop.focusElement", elementId);
        }

        private void ManualUpdate(ChangeEventArgs e, string type)
        {
            if (int.TryParse(e.Value?.ToString(), out int val))
            {
                if (type == "hours") displayHours = Math.Clamp(val, 0, 4);
                else displayMinutes = Math.Clamp(val, 0, 59);
            }
            isHoursInputMode = false;
            isMinutesInputMode = false;
            SyncSelectedMinutes();
        }

        private async Task StartTimer()
        {
            _isLeaving = false;
            _warning30Played = false;
            _fourSecondsPlayed = false;

            if (isRunning || selectedMinutes <= 0)
            {
                if (selectedMinutes <= 0) Snackbar.Add("Выберите время для рыбалки!", Severity.Warning);
                return;
            }

            isRunning = true;
            _progressValue = 0;

            int totalSeconds = selectedMinutes * 60;
            secondsLeft = totalSeconds;
            double thresholdSeconds = (double)totalSeconds * 0.10;

            _halfTimeNagSent = false;
            catchResult = null;
            _currentQuote = _quotes[Random.Shared.Next(_quotes.Length)];

            _animationState = FishingAnimationState.InitialDrop;
            StateHasChanged();

            await TriggerSplash();
            await Task.Delay(200);

            _animationState = FishingAnimationState.PassiveFishing;
            StateHasChanged();

            cts = new CancellationTokenSource();
            timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

            try
            {
                while (await timer.WaitForNextTickAsync(cts.Token) && secondsLeft > 0)
                {
                    secondsLeft--;
                    _progressValue = ((double)(totalSeconds - secondsLeft) / totalSeconds) * 100;

                    if (!_halfTimeNagSent && secondsLeft <= totalSeconds / 2)
                    {
                        Snackbar.Add("Рыба уже кружит рядом!", Severity.Info);
                        _halfTimeNagSent = true;
                    }

                    if (!_warning30Played && secondsLeft == 30)
                    {
                        _warning30Played = true;
                        await JS.InvokeVoidAsync("audioInterop.setVolume", "fish-sound1", 50);
                        await JS.InvokeVoidAsync("audioInterop.play", "fish-sound1");
                    }

                    if (!_fourSecondsPlayed && secondsLeft == 4)
                    {
                        _fourSecondsPlayed = true;
                        await JS.InvokeVoidAsync("audioInterop.setVolume", "fish-warning", 50);
                        await JS.InvokeVoidAsync("audioInterop.play", "fish-warning");
                    }

                    if (secondsLeft <= thresholdSeconds && _animationState == FishingAnimationState.PassiveFishing)
                    {
                        _animationState = FishingAnimationState.ActiveBite;
                    }

                    if (secondsLeft % 30 == 0 && secondsLeft > 0)
                    {
                        _currentQuote = _quotes[Random.Shared.Next(_quotes.Length)];
                    }

                    StateHasChanged();
                }

                if (secondsLeft == 0) await FinishSession();
            }
            catch (OperationCanceledException) { }
        }

        private async Task FinishSession()
        {
            _animationState = FishingAnimationState.FinalRaise;
            await TriggerSplash();
            await JS.InvokeVoidAsync("audioInterop.setVolume", "fish-sound2", 10);
            await JS.InvokeVoidAsync("audioInterop.play", "fish-sound2");

            isRunning = false;
            timer?.Dispose();
            cts.Cancel();

            _isLeaving = true;
            _animationState = FishingAnimationState.FinalRaise;

            await TriggerSplash();
            await JS.InvokeVoidAsync("audioInterop.setVolume", "fish-sound2", 10);
            await JS.InvokeVoidAsync("audioInterop.play", "fish-sound2");

            StateHasChanged();

            await Task.Delay(350);

            isRunning = false;
            _isLeaving = false;
            _animationState = FishingAnimationState.None;

            try
            {
                var response = await Http.GetFromJsonAsync<FishCatchResult>($"api/fishery/catch?minutes={selectedMinutes}");

                if (response != null)
                {
                    catchResult = response;

                    TotalScore += response.TotalPoints;
                    CatchHistory.Add(response);

                    var token = await JS.InvokeAsync<string?>("localStorage.getItem", "jwt_token");
                    if (!string.IsNullOrEmpty(token))
                    {
                        Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                        await Http.PostAsJsonAsync("api/profile/save-catch", response);
                    }

                    var parameters = new DialogParameters { ["CatchResult"] = response, ["IsNightMode"] = isNightMode };
                    var options = new DialogOptions { BackdropClick = false, CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };

                    var catchDialog = await DialogService.ShowAsync<CatchDialog>("Улов", parameters, options);
                    var result = await catchDialog.Result;

                    if (result is not null && !result.Canceled)
                    {
                        var noteParameters = new DialogParameters { ["IsNightMode"] = isNightMode };
                        var noteOptions = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true, BackdropClick = false };
                        var noteDialog = await DialogService.ShowAsync<NoteInputDialog>("Запись в дневник", noteParameters, noteOptions);
                        var noteResult = await noteDialog.Result;

                        if (noteResult is not null && !noteResult.Canceled)
                        {
                            string noteText = noteResult.Data as string ?? "";

                            var newNote = new DiaryEntry
                            {
                                CompletionTime = DateTime.UtcNow,
                                MinutesSpent = selectedMinutes,
                                FishName = response.Fish.Name,
                                Note = noteText
                            };

                            _diaryEntries.Add(newNote);

                            if (!string.IsNullOrEmpty(token))
                            {
                                var saveResponse = await Http.PostAsJsonAsync("api/profile/save-diary", newNote);
                                if (saveResponse.IsSuccessStatusCode)
                                {
                                    Console.WriteLine("--- [API] Заметка успешно улетела на сервер ---");
                                }
                                else
                                {
                                    Console.WriteLine($"--- [API] ОШИБКА сохранения заметки: {saveResponse.StatusCode} ---");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- [ERROR] Ошибка при завершении сессии: {ex.Message} ---");
            }

            _animationState = FishingAnimationState.None;
            StateHasChanged();
        }
    }
}
