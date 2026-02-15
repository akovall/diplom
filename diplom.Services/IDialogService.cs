namespace diplom.Services
{
    public interface IDialogService
    {
        void ShowError(string message, string title = "Error");
        void ShowWarning(string message, string title = "Warning");
        bool Confirm(string message, string title = "Confirm");
    }
}
