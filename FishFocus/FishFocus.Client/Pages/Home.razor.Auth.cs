using Microsoft.JSInterop;
using MudBlazor;
using FishFocus.Shared.DTOs.Profile;
using FishFocus.Shared.Models;
using FishFocus.Client.Dialogs;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace FishFocus.Client.Pages
{
    public partial class Home
    {
        private async Task ToggleNightMode(bool value)
        {
            isNightMode = value;
            await SaveUserSettings();
        }

        private async Task ToggleFog(bool value)
        {
            playFog = value;
            await SaveUserSettings();
        }

        private async Task OpenLeaderboard()
        {
            var parameters = new DialogParameters { ["IsNightMode"] = isNightMode };
            var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            await DialogService.ShowAsync<LeaderboardDialog>("Таблица лидеров", parameters, options);
        }

        private async Task OpenDiary()
        {
            var parameters = new DialogParameters { ["Entries"] = _diaryEntries, ["IsNightMode"] = isNightMode };
            var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            await DialogService.ShowAsync<DiaryDialog>("Дневник успехов", parameters, options);
        }

        private async Task OpenCollection()
        {
            var parameters = new DialogParameters { ["CatchHistory"] = CatchHistory, ["IsNightMode"] = isNightMode };
            var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
            await DialogService.ShowAsync<FishCollectionDialog>("Моя коллекция", parameters, options);
        }

        private async Task OpenProfileOrLogin()
        {
            if (_isLoggedIn)
            {
                var username = await JS.InvokeAsync<string?>("localStorage.getItem", "username") ?? "Пользователь";
                var parameters = new DialogParameters
                {
                    ["Username"] = username,
                    ["Email"] = UserEmail,
                    ["TotalScore"] = TotalScore,
                    ["AvatarData"] = AvatarData,
                    ["CatchHistory"] = CatchHistory,
                    ["DiaryEntries"] = _diaryEntries,
                    ["IsNightMode"] = isNightMode
                };
                var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small, FullWidth = true };
                var dialog = await DialogService.ShowAsync<CabinetDialog>("Личный кабинет", parameters, options);
                var result = await dialog.Result;

                await LoadUserSettings();
            }
            else
            {
                await OpenLoginDialog();
            }
        }

        private async Task OpenLoginDialog()
        {
            var options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };
            var dialog = await DialogService.ShowAsync<LoginDialog>("Личный кабинет", options);
            var result = await dialog.Result;

            if (result is not null && !result.Canceled)
            {
                Console.WriteLine("--- [AUTH] Логин успешен. ВКЛЮЧАЮ БЛОКИРОВКУ. ---");
                _isProcessingAuth = true;

                await LoadUserSettings();
                await Task.Delay(800);

                _isProcessingAuth = false;
                Console.WriteLine("--- [AUTH] БЛОКИРОВКА СНЯТА. Теперь можно сохранять. ---");
                Snackbar.Add("Настройки синхронизированы!", Severity.Success);
            }
        }

        private async Task LoadUserSettings()
        {
            var token = await JS.InvokeAsync<string?>("localStorage.getItem", "jwt_token");
            if (string.IsNullOrEmpty(token))
            {
                _isLoggedIn = false;
                Console.WriteLine("--- [LOAD] Токен не найден, загрузка отменена ---");
                return;
            }
            Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                Console.WriteLine("--- [LOAD] Запрашиваю профиль с сервера... ---");
                var profile = await Http.GetFromJsonAsync<UserProfileDto>("api/profile");

                if (profile is null)
                {
                    Console.WriteLine("--- [LOAD] Ошибка: профиль пуст. ---");
                    return;
                }
                _isLoggedIn = true;
                UserEmail = profile.Email ?? "";
                Username = profile.Username ?? "Пользователь";
                AvatarData = profile.AvatarData;
                Console.WriteLine($"--- [LOAD] ДАННЫЕ С СЕРВЕРА: Таймер={profile.LastSelectedMinutes}, Очки={profile.TotalPoints} ---");
                TotalScore = profile.TotalPoints;

                isNightMode = profile.IsNightMode;
                playFog = profile.PlayFog;

                isRadioEnabled = profile.IsRadioEnabled;
                radioVolume = profile.RadioVolume;
                isRainEnabled = profile.IsRainEnabled;
                rainVolume = profile.RainVolume;

                isBirdsEnabled = profile.IsBirdsEnabled;
                isThunderEnabled = profile.IsThunderEnabled;
                birdsVolume = profile.BirdsVolume;
                thunderVolume = profile.ThunderVolume;

                StateHasChanged();

                if (isRadioEnabled)
                {
                    await Task.Delay(300);
                    await JS.InvokeVoidAsync("audioInterop.setVolume", "radio-audio", radioVolume);
                    if (!isRadioInitialized)
                    {
                        await JS.InvokeVoidAsync("audioInterop.forceRadioStart", "radio-audio", Random.Shared.Next(15, 70));
                        isRadioInitialized = true;
                    }
                    else { await JS.InvokeVoidAsync("audioInterop.play", "radio-audio"); }
                }

                if (isRainEnabled)
                {
                    await PrepareRain();
                    await JS.InvokeVoidAsync("audioInterop.play", "rain-audio");
                    await JS.InvokeVoidAsync("audioInterop.setVolume", "rain-audio", rainVolume);
                }

                if (isBirdsEnabled)
                {
                    await JS.InvokeVoidAsync("audioInterop.play", "birds-audio");
                    await JS.InvokeVoidAsync("audioInterop.setVolume", "birds-audio", birdsVolume);
                }

                if (isThunderEnabled)
                {
                    await JS.InvokeVoidAsync("audioInterop.play", "thunder-audio");
                    await JS.InvokeVoidAsync("audioInterop.setVolume", "thunder-audio", thunderVolume);
                    _ = RunThunderLoop();
                }

                if (profile.LastSelectedMinutes > 0)
                {
                    selectedMinutes = profile.LastSelectedMinutes;
                    displayHours = selectedMinutes / 60;
                    int rawMinutes = selectedMinutes % 60;
                    displayMinutes = (int)(Math.Round(rawMinutes / 5.0) * 5);
                    if (displayMinutes > 55) displayMinutes = 0;

                    if (!isRunning) secondsLeft = selectedMinutes * 60;

                    Console.WriteLine($"--- [LOAD] UI обновлен на: {displayHours}ч {displayMinutes}мин ---");
                }

                if (profile != null)
                {
                    CatchHistory = profile.CaughtFishes ?? new();
                    _diaryEntries = profile.DiaryEntries ?? new();
                    StateHasChanged();
                }
                else
                {
                    Console.WriteLine("--- [LOAD] В базе 0 минут, оставляем дефолт. ---");
                }
                StateHasChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- [LOAD] КРИТИЧЕСКАЯ ОШИБКА: {ex.Message} ---");
            }
        }

        private async Task SaveUserSettings(bool force = false)
        {
            if (_isProcessingAuth && !force)
            {
                Console.WriteLine("--- [SAVE] Блокировка: идёт вход/выход ---");
                return;
            }

            var token = await JS.InvokeAsync<string?>("localStorage.getItem", "jwt_token");
            if (string.IsNullOrEmpty(token)) return;
            if (!force && selectedMinutes < 1) return;
            Console.WriteLine($"--- [SAVE] Пытаюсь сохранить в базу: {selectedMinutes} мин. ---");

            Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var settings = new UpdateSettingsRequest
            {
                IsNightMode = isNightMode,
                PlayFog = playFog,
                IsRadioEnabled = isRadioEnabled,
                RadioVolume = radioVolume,
                IsRainEnabled = isRainEnabled,
                RainVolume = rainVolume,
                IsBirdsEnabled = isBirdsEnabled,
                BirdsVolume = birdsVolume,
                IsThunderEnabled = isThunderEnabled,
                ThunderVolume = thunderVolume,
                LastSelectedMinutes = selectedMinutes
            };

            try
            {
                var response = await Http.PutAsJsonAsync("api/profile/settings", settings);
                Console.WriteLine($"--- [SAVE] Отправлено: {selectedMinutes}. Код: {response.StatusCode} ---");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- [SAVE] ОШИБКА: {ex.Message} ---");
            }
        }

        private async Task Logout()
        {
            _isLoggedIn = false;
            Console.WriteLine("--- [AUTH] Начало выхода ---");
            await SaveUserSettings(force: true);

            _isProcessingAuth = true;

            await JS.InvokeVoidAsync("localStorage.removeItem", "jwt_token");
            Http.DefaultRequestHeaders.Authorization = null;

            selectedMinutes = 30;
            displayHours = 0;
            displayMinutes = 30;
            secondsLeft = 1800;

            TotalScore = 0;
            UserEmail = "";
            Username = "Пользователь";
            AvatarData = null;
            CatchHistory.Clear();
            _diaryEntries.Clear();

            isNightMode = false;
            playFog = false;

            isRainEnabled = false;
            isRainInDom = false;
            isRainAnimationActive = false;
            _rainDrops?.Clear();
            await JS.InvokeVoidAsync("audioInterop.pause", "rain-audio");

            isBirdsEnabled = false;
            await JS.InvokeVoidAsync("audioInterop.pause", "birds-audio");

            isThunderEnabled = false;
            _activeBolts?.Clear();
            await JS.InvokeVoidAsync("audioInterop.pause", "thunder-audio");

            isRadioEnabled = false;
            isRadioInitialized = false;
            await JS.InvokeVoidAsync("audioInterop.pause", "radio-audio");

            CatchHistory.Clear();
            _diaryEntries.Clear();

            await Task.Delay(500);
            _isProcessingAuth = false;
            Console.WriteLine("--- [AUTH] Выход завершен, блокировка снята ---");
            StateHasChanged();

            _isProcessingAuth = false;

            Snackbar.Add("Вы вышли. Все эффекты остановлены.", Severity.Info);
            StateHasChanged();
        }
    }
}
