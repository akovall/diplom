using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using diplom.Models;
using diplom.Models.enums;
using diplom.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Threading;

namespace diplom.viewmodels
{
    public partial class UsersViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isLoading;

        public ObservableCollection<UserDisplayItem> Users { get; } = new();
        public UserRole[] AvailableRoles { get; } = (UserRole[])Enum.GetValues(typeof(UserRole));

        private readonly DispatcherTimer _activityTimer;

        public UsersViewModel()
        {
            LoadUsers();

            _activityTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _activityTimer.Tick += (_, _) => RefreshActivityStates();
            _activityTimer.Start();
        }

        [RelayCommand]
        private async Task ToggleUserStatus(UserDisplayItem user)
        {
            if (user == null) return;
            
            bool success;
            if (user.IsActive)
                success = await ApiClient.Instance.PutAsync($"/api/users/{user.Id}/deactivate", null!);
            else
                success = await ApiClient.Instance.PutAsync($"/api/users/{user.Id}/activate", null!);

            if (success)
            {
                user.IsActive = !user.IsActive;
                await RefreshActivityFromApiAsync();
            }
        }

        [RelayCommand]
        private async Task ChangeUserRole(UserDisplayItem user)
        {
            if (user == null) return;

            // We assume the user.Role was already updated by the ComboBox binding
            // or we could pass the role as a separate parameter if needed.
            // Since User is a class, the ComboBox SelectedItem="{Binding Role}" 
            // will update the property even if it doesn't notify.
            
            var success = await ApiClient.Instance.PutAsync($"/api/users/{user.Id}/role", user.Role);
            if (success)
            {
                // Optionally reload to ensure consistency, 
                // but since it's a reference type it's already updated in the list.
                // LoadUsers(); 
            }
        }

        private async void LoadUsers()
        {
            IsLoading = true;
            try
            {
                var users = await ApiClient.Instance.GetAsync<List<User>>("/api/users") ?? new();
                var activity = await ApiClient.Instance.GetAsync<List<UserActivityDto>>("/api/users/activity") ?? new();
                var stateByUserId = activity.ToDictionary(a => a.UserId, a => a.State);

                Users.Clear();
                foreach (var user in users)
                {
                    var display = new UserDisplayItem(user);
                    display.ActivityState = stateByUserId.TryGetValue(display.Id, out var state)
                        ? state
                        : UserActivityState.Offline;
                    Users.Add(display);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void RefreshActivityStates()
        {
            if (Users.Count == 0)
                return;

            // Periodically refresh activity via API (presence + open time entry)
            _ = RefreshActivityFromApiAsync();
        }

        private async Task RefreshActivityFromApiAsync()
        {
            try
            {
                var activity = await ApiClient.Instance.GetAsync<List<UserActivityDto>>("/api/users/activity") ?? new();
                var stateByUserId = activity.ToDictionary(a => a.UserId, a => a.State);

                foreach (var user in Users)
                {
                    if (stateByUserId.TryGetValue(user.Id, out var state))
                        user.ActivityState = state;
                }
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch
            {
                // ignore transient failures
            }
        }
    }

    public sealed class UserActivityDto
    {
        public int UserId { get; set; }
        public UserActivityState State { get; set; }
    }

    public partial class UserDisplayItem : ObservableObject
    {
        private readonly User _user;

        public UserDisplayItem(User user)
        {
            _user = user;
        }

        public int Id => _user.Id;
        public string Username => _user.Username;
        public string FullName => _user.FullName;
        public string JobTitle => _user.JobTitle;

        public bool IsActive
        {
            get => _user.IsActive;
            set
            {
                if (_user.IsActive == value) return;
                _user.IsActive = value;
                OnPropertyChanged();
            }
        }

        public UserRole Role
        {
            get => _user.Role;
            set
            {
                if (_user.Role == value) return;
                _user.Role = value;
                OnPropertyChanged();
            }
        }

        [ObservableProperty]
        private UserActivityState _activityState;
    }
}
