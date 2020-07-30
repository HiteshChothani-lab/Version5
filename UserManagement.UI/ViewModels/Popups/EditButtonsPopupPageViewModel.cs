using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System;
using System.Threading.Tasks;
using System.Windows;
using UserManagement.Common.Constants;
using UserManagement.Common.Enums;
using UserManagement.Entity;
using UserManagement.Manager;
using UserManagement.UI.Events;
using UserManagement.UI.ItemModels;

namespace UserManagement.UI.ViewModels
{
    public class EditButtonsPopupPageViewModel : ViewModelBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IWindowsManager _windowsManager;

        public EditButtonsPopupPageViewModel(IRegionManager regionManager, IEventAggregator eventAggregator, IWindowsManager windowsManager) : base(regionManager)
        {
            _eventAggregator = eventAggregator;
            _windowsManager = windowsManager;

            this.CancelCommand = new DelegateCommand(() => ExecuteCancelCommand());
            this.SubmitCommand = new DelegateCommand(async () => await ExecuteSubmitCommand());
            this.ToggleButtonCommand = new DelegateCommand<string>((item) => ExecuteToggleButtonCommand(item));
        }

        private StoreUserEntity _selectedStoreUser;
        public StoreUserEntity SelectedStoreUser
        {
            get => _selectedStoreUser;
            set => SetProperty(ref _selectedStoreUser, value);
        }

        private bool IsSelectedStoreUser = false;

        private bool _isUserTypeMobile = true;
        public bool IsUserTypeMobile
        {
            get => _isUserTypeMobile;
            set => SetProperty(ref _isUserTypeMobile, value);
        }

        private bool _isUserTypeNonMobile;
        public bool IsUserTypeNonMobile
        {
            get => _isUserTypeNonMobile;
            set => SetProperty(ref _isUserTypeNonMobile, value);
        }

        private bool _button1;
        public bool Button1
        {
            get => _button1;
            set => SetProperty(ref _button1, value);
        }

        private bool _button2;
        public bool Button2
        {
            get => _button2;
            set => SetProperty(ref _button2, value);
        }

        private bool _button3;
        public bool Button3
        {
            get => _button3;
            set => SetProperty(ref _button3, value);
        }

        private bool _button4;
        public bool Button4
        {
            get => _button4;
            set => SetProperty(ref _button4, value);
        }

        private bool _button5;
        public bool Button5
        {
            get => _button5;
            set => SetProperty(ref _button5, value);
        }

        private bool _button6;
        public bool Button6
        {
            get => _button6;
            set => SetProperty(ref _button6, value);
        }

        private bool _button7;
        public bool Button7
        {
            get => _button7;
            set => SetProperty(ref _button7, value);
        }

        private bool _button8;
        public bool Button8
        {
            get => _button8;
            set => SetProperty(ref _button8, value);
        }

        private string _otherNumber;
        public string OtherNumber
        {
            get => _otherNumber;
            set
            {
                SetProperty(ref _otherNumber, value);

                if (!string.IsNullOrWhiteSpace(value))
                    ExecuteToggleButtonCommand("0");
            }
        }

        public string TableNumber { get; set; } = string.Empty;

        public DelegateCommand CancelCommand { get; private set; }
        public DelegateCommand SubmitCommand { get; private set; }
        public DelegateCommand<string> ToggleButtonCommand { get; private set; }

        private void ExecuteToggleButtonCommand(string parameter)
        {
            if (string.IsNullOrEmpty(parameter) && !int.TryParse(parameter, out _))
                return;

            TableNumber = parameter;

            if (!"0".Equals(parameter))
                OtherNumber = string.Empty;
            else
                TableNumber = string.Empty;

            switch (Convert.ToInt32(parameter))
            {
                case 1:
                    Button2 = Button3 = Button4 = Button5 = Button6 = Button7 = Button8 = false;
                    break;
                case 2:
                    Button1 = Button3 = Button4 = Button5 = Button6 = Button7 = Button8 = false;
                    break;
                case 3:
                    Button1 = Button2 = Button4 = Button5 = Button6 = Button7 = Button8 = false;
                    break;
                case 4:
                    Button1 = Button2 = Button3 = Button5 = Button6 = Button7 = Button8 = false;
                    break;
                case 5:
                    Button1 = Button2 = Button3 = Button4 = Button6 = Button7 = Button8 = false;
                    break;
                case 6:
                    Button1 = Button2 = Button3 = Button4 = Button5 = Button7 = Button8 = false;
                    break;
                case 7:
                    Button1 = Button2 = Button3 = Button4 = Button5 = Button6 = Button8 = false;
                    break;
                case 8:
                    Button1 = Button2 = Button3 = Button4 = Button5 = Button6 = Button7 = false;
                    break;
                default:
                    Button1 = Button2 = Button3 = Button4 = Button5 = Button6 = Button7 = Button8 = false;
                    break;
            }
        }

        private void ExecuteCancelCommand()
        {
            this.RegionNavigationService.Journal.Clear();
            _eventAggregator.GetEvent<EditButtonsSubmitEvent>().Publish(null);
            SetUnsetProperties();
        }

        private async Task ExecuteSubmitCommand()
        {
            if (string.IsNullOrWhiteSpace(TableNumber) && string.IsNullOrWhiteSpace(OtherNumber))
            {
                MessageBox.Show("Please choice a number or put number in the text box.", "Required.");
                return;
            }

            var reqEntity = new UpdateButtonsRequestEntity
            {
                Id = this.SelectedStoreUser.Id,
                UserId = Convert.ToInt32(this.SelectedStoreUser.UserId),
                SuperMasterId = Config.MasterStore.UserId,
                Action = this.IsSelectedStoreUser ? "update_buttons" : "update_buttons_archive"
            };

            reqEntity.Button1 = string.IsNullOrWhiteSpace(TableNumber) ? OtherNumber : TableNumber;

            var result = await _windowsManager.UpdateButtons(reqEntity);

            if (result.StatusCode == (int)GenericStatusValue.Success)
            {
                if (Convert.ToBoolean(result.Status))
                {
                    this.RegionNavigationService.Journal.Clear();
                    _eventAggregator.GetEvent<EditButtonsSubmitEvent>().Publish(new EditButtonsItemModel());
                    SetUnsetProperties();
                }
                else
                {
                    MessageBox.Show(result.Messagee, "Unsuccessful");
                }
            }
            else if (result.StatusCode == (int)GenericStatusValue.NoInternetConnection)
            {
                MessageBox.Show(MessageBoxMessage.NoInternetConnection, "Unsuccessful");
            }
            else if (result.StatusCode == (int)GenericStatusValue.HasErrorMessage)
            {
                MessageBox.Show(result.Message, "Unsuccessful");
            }
            else
            {
                MessageBox.Show(MessageBoxMessage.UnknownErorr, "Unsuccessful");
            }
        }

        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            SetUnsetProperties();

            if (navigationContext.Parameters[NavigationConstants.SelectedStoreUser] is StoreUserEntity selectedStoreUser)
                SelectedStoreUser = selectedStoreUser;

            if (navigationContext.Parameters[NavigationConstants.IsSelectedStoreUser] is bool isSelectedStoreUser)
                IsSelectedStoreUser = isSelectedStoreUser;

            if (!string.IsNullOrWhiteSpace(SelectedStoreUser.Btn1) && int.TryParse(SelectedStoreUser.Btn1, out int val))
                SelectButtons(SelectedStoreUser.Btn1);
        }

        private void SelectButtons(string val)
        {
            TableNumber = val;

            switch (Convert.ToInt32(val))
            {
                case 1:
                    Button1 = true;
                    break;
                case 2:
                    Button2 = true;
                    break;
                case 3:
                    Button3 = true;
                    break;
                case 4:
                    Button4 = true;
                    break;
                case 5:
                    Button5 = true;
                    break;
                case 6:
                    Button6 = true;
                    break;
                case 7:
                    Button7 = true;
                    break;
                case 8:
                    Button8 = true;
                    break;
                default:
                    OtherNumber = val;
                    break;
            }
        }

        private void SetUnsetProperties()
        {
            Button1 = Button2 = Button3 = Button4 = Button5 = Button6 = Button7 = Button8 = false;
            OtherNumber = TableNumber = string.Empty;
            this.IsUserTypeMobile = false;
            this.IsUserTypeNonMobile = false;
            this.IsSelectedStoreUser = false;
        }
    }
}
