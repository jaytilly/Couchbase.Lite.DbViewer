using Prism.Commands;
using Prism.Services.Dialogs;
using ReactiveUI;
using System;

namespace DbViewer.Dialogs
{
    public class GeneralMessageDialogViewModel : ReactiveObject, IDialogAware
    {
        public GeneralMessageDialogViewModel()
        {
            CloseCommand = new DelegateCommand(() => RequestClose?.Invoke(null));
        }

        public event Action<IDialogParameters> RequestClose;

        public DelegateCommand CloseCommand { get; }

        private string _mainBodyText;
        public string MainBodyText
        {
            get => _mainBodyText;
            set => this.RaiseAndSetIfChanged(ref _mainBodyText, value);
        }

        public bool CanCloseDialog() => true;

        public void OnDialogClosed()
        {
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey(DialogNames.MainMessageParam))
            {
                MainBodyText = parameters.GetValue<string>(DialogNames.MainMessageParam);
            }
        }
    }
}
