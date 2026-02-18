using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using diplom.Models;
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

        public UsersViewModel()
        {
            LoadUsers();
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
