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

namespace diplom.viewmodels
{
    public partial class UsersViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isLoading;

        public ObservableCollection<User> Users { get; } = new();
        public UserRole[] AvailableRoles { get; } = (UserRole[])Enum.GetValues(typeof(UserRole));

        public UsersViewModel()
        {
            LoadUsers();
        }

        [RelayCommand]
        private async Task ToggleUserStatus(User user)
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
                // Notify UI about property change on the specific user object
                // Since User might not implement ObservableObject, we refresh the list or the object
                LoadUsers(); 
            }
        }

        [RelayCommand]
        private async Task ChangeUserRole(User user)
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
                Users.Clear();
                foreach (var user in users)
                    Users.Add(user);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
