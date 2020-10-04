using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UserManagement.Common.Constants;
using UserManagement.Common.Enums;
using UserManagement.Entity;
using UserManagement.Manager;
using UserManagement.Pushers.Events;
using UserManagement.UI.Converters;
using UserManagement.UI.Events;
using UserManagement.UI.ItemModels;
using UserManagement.UI.Views;

namespace UserManagement.UI.ViewModels
{
    public class MainPageViewModel : ViewModelBase
    {
        private readonly IEventAggregator _eventAggregator;
        private readonly IWindowsManager _windowsManager;
        private object locker = new object();

        public MainPageViewModel(IRegionManager regionManager, IEventAggregator eventAggregator, IWindowsManager windowsManager) : base(regionManager)
        {
            _eventAggregator = eventAggregator;
            _windowsManager = windowsManager;

            _eventAggregator.GetEvent<NonMobileUserUpdateEvent>().Subscribe(async (user) =>
            {
                _eventAggregator.GetEvent<PopupVisibilityEvent>().Publish(false);

                if (user != null)
                {
                    if (user.IsNewRecord)
                    {
                        this.NonMobileUser = user;
                        this.FirstName = user.FirstName;
                        this.LastName = user.LastName;
                    }
                    else
                    {
                        await GetData();
                    }
                }
            });

            _eventAggregator.GetEvent<NonMobileUserEditEvent>().Subscribe(async (user) =>
            {
                _eventAggregator.GetEvent<PopupVisibilityEvent>().Publish(false);

                if (user != null)
                {
                    await GetData();
                }

            });

            _eventAggregator.GetEvent<EditStoreUserSubmitEvent>().Subscribe(async (user) =>
            {
                _eventAggregator.GetEvent<PopupVisibilityEvent>().Publish(false);

                if (user != null)
                {
                    await GetData();
                }
            });

            _eventAggregator.GetEvent<MoveStoreUserSubmitEvent>().Subscribe(async (user) =>
                {
                    _eventAggregator.GetEvent<PopupVisibilityEvent>().Publish(false);

                    if (user != null)
                    {
                        await GetData();
                    }
                });

            _eventAggregator.GetEvent<MoveStoreUserToArchiveSubmitEvent>().Subscribe(async (user) =>
            {
                _eventAggregator.GetEvent<PopupVisibilityEvent>().Publish(false);

                if (user != null)
                {
                    await GetData();
                }
            });

            _eventAggregator.GetEvent<EditButtonsSubmitEvent>().Subscribe(async (user) =>
            {
                _eventAggregator.GetEvent<PopupVisibilityEvent>().Publish(false);

                if (user != null)
                {
                    await GetData();
                }
            });
            _eventAggregator.GetEvent<RegisterStoreUserSubmitEvent>().Subscribe(async (user) =>
            {
                _eventAggregator.GetEvent<PopupVisibilityEvent>().Publish(false);
                ResetFields();
                if (user != null)
                {
                    await GetData();
                }
            });

            this.NonMobileUserCommand = new DelegateCommand(() => ExecuteNonMobileUserCommand());
            this.AddUserCommand = new DelegateCommand(async () => await ExecuteAddUserCommand());
            this.DeleteStoreUserCommand = new DelegateCommand<StoreUserEntity>(async (user) => await ExecuteDeleteStoreUserCommand(user));
            this.DeleteArchiveUserCommand = new DelegateCommand<StoreUserEntity>(async (user) => await ExecuteDeleteArchiveUserCommand(user));
            this.EditStoreUserCommand = new DelegateCommand<StoreUserEntity>((user) => ExecuteEditStoreUserCommand(user));
            this.SetFlagCommand = new DelegateCommand<StoreUserEntity>((user) => ExecuteFlagCommand(user));
            this.EditNonMobileStoreUserCommand = new DelegateCommand<StoreUserEntity>((user) => ExecuteEditNonMobileStoreUserCommand(user));
            this.EditStoreButtonsCommand = new DelegateCommand<StoreUserEntity>((user) => ExecuteEditStoreButtonsCommand(user));
            this.EditArchiveButtonsCommand = new DelegateCommand<StoreUserEntity>((user) => ExecuteEditArchiveButtonsCommand(user));
            this.EditNonMobileArchiveStoreUserCommand = new DelegateCommand<StoreUserEntity>((user) => ExecuteEditNonMobileArchiveStoreUserCommand(user));
            this.MoveStoreUserCommand = new DelegateCommand<StoreUserEntity>((user) => ExecuteMoveStoreUserCommand(user));
            this.LogoutCommand = new DelegateCommand(() => ExecuteLogoutCommand());
            this.RefreshDataCommand = new DelegateCommand(async () => await ExecuteRefreshDataCommand());
            this.StoreIDCheckedCommand = new DelegateCommand<StoreUserEntity>(async (user) => await ExecuteStoreIDCheckedCommand(user));
            this.ArchiveIDCheckedCommand = new DelegateCommand<StoreUserEntity>(async (user) => await ExecuteArchiveIDCheckedCommand(user));
            this.UserDetailWindowCommand = new DelegateCommand<StoreUserEntity>((user) => ExecuteUserDetailWindowCommand(user));
            this.ToggleButtonCommand = new DelegateCommand<string>((item) => ExecuteToggleButtonCommand(item));
            this.DummyUserCommand = new DelegateCommand(() => ExecuteDummyUserCommand());

            #region Pusher Events Subscribe

            _eventAggregator.GetEvent<RefreshData>().Subscribe((model) =>
            {
                try
                {
                    lock (locker)
                    {
                        if (model.Action == PusherAction.Store)
                        {
                            if (model.EventName.Equals(PusherData.DeleteStoreUser))
                            {
                                Task.WaitAll(GetData());
                            }
                            else
                            {
                                Task.WhenAll(GetStoreUsers());
                            }
                        }
                        else if (model.Action == PusherAction.Archieve)
                        {
                            Task.WhenAll(GetArchieveStoreUsers());
                        }
                    }
                }
                catch (Exception) { }
            });

            #endregion
        }

        private void ExecuteLogoutCommand()
        {
            _windowsManager.Logout();
            Application.Current.Shutdown();
        }

        private ObservableCollection<StoreUserEntity> _storeUsers = new ObservableCollection<StoreUserEntity>();
        public ObservableCollection<StoreUserEntity> StoreUsers
        {
            get => _storeUsers;
            set => SetProperty(ref _storeUsers, value);
        }

        private ObservableCollection<StoreUserEntity> _archieveStoreUsers = new ObservableCollection<StoreUserEntity>();
        public ObservableCollection<StoreUserEntity> ArchieveStoreUsers
        {
            get => _archieveStoreUsers;
            set => SetProperty(ref _archieveStoreUsers, value);
        }

        private string _firstName;
        public string FirstName
        {
            get => _firstName;
            set => SetProperty(ref _firstName, value);
        }

        private string _lastName;
        public string LastName
        {
            get => _lastName;
            set => SetProperty(ref _lastName, value);
        }

        private string _mobileNumber;
        public string MobileNumber
        {
            get => _mobileNumber;
            set => SetProperty(ref _mobileNumber, value);
        }

        private NonMobileUserItemModel _nonMobileUser;
        public NonMobileUserItemModel NonMobileUser
        {
            get => _nonMobileUser;
            set => SetProperty(ref _nonMobileUser, value);
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

        private bool _canTapAddCommand = true;
        public bool CanTapAddCommand
        {
            get => _canTapAddCommand;
            set => SetProperty(ref _canTapAddCommand, value);
        }

        private Visibility _loaderVisibility = Visibility.Collapsed;
        public Visibility LoaderVisibility
        {
            get => _loaderVisibility;
            set => SetProperty(ref _loaderVisibility, value);
        }

        private string _loaderMessage = string.Empty;
        public string LoaderMessage
        {
            get => _loaderMessage;
            set => SetProperty(ref _loaderMessage, value);
        }

        private int TotalStoreUsers
        {
            get
            {
                return StoreUsers == null || StoreUsers.Count <= 0 ? 0 : StoreUsers.Count;
            }
        }

        public string TableNumber { get; set; } = string.Empty;

        public bool IsTableVisible
        {
            get => Config.MasterStore.FacilityType.Equals("Restaurant");
        }

        private UserDetailsPage userDetailsPage;

        public DelegateCommand NonMobileUserCommand { get; private set; }
        public DelegateCommand AddUserCommand { get; private set; }
        public DelegateCommand<StoreUserEntity> DeleteStoreUserCommand { get; private set; }
        public DelegateCommand<StoreUserEntity> DeleteArchiveUserCommand { get; private set; }
        public DelegateCommand<StoreUserEntity> EditStoreUserCommand { get; private set; }
        public DelegateCommand<StoreUserEntity> SetFlagCommand { get; private set; }
        public DelegateCommand<StoreUserEntity> EditNonMobileStoreUserCommand { get; private set; }
        public DelegateCommand<StoreUserEntity> EditStoreButtonsCommand { get; private set; }
        public DelegateCommand<StoreUserEntity> EditArchiveButtonsCommand { get; private set; }
        public DelegateCommand<StoreUserEntity> EditNonMobileArchiveStoreUserCommand { get; private set; }
        public DelegateCommand<StoreUserEntity> MoveStoreUserCommand { get; private set; }
        public DelegateCommand LogoutCommand { get; private set; }
        public DelegateCommand RefreshDataCommand { get; private set; }
        public DelegateCommand<StoreUserEntity> StoreIDCheckedCommand { get; private set; }
        public DelegateCommand<StoreUserEntity> ArchiveIDCheckedCommand { get; private set; }
        public DelegateCommand<StoreUserEntity> UserDetailWindowCommand { get; private set; }
        public DelegateCommand<string> ToggleButtonCommand { get; private set; }
        public DelegateCommand DummyUserCommand { get; private set; }

        private void ExecuteDummyUserCommand()
        {
            this.MobileNumber = "0000000000";
        }

        private void ExecuteNonMobileUserCommand()
        {
            _eventAggregator.GetEvent<PopupVisibilityEvent>().Publish(true);
            var parameters = new NavigationParameters();
            this.RegionManager.RequestNavigate("PopupRegion", ViewNames.NonMobileUserPopup, parameters);
        }

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

        private async Task ExecuteAddUserCommand()
        {
            this.CanTapAddCommand = false;

            if (this.NonMobileUser == null)
            {
                if (string.IsNullOrEmpty(this.FirstName))
                {
                    MessageBox.Show("First Name is required.", "Required");
                    return;
                }
                else if (string.IsNullOrEmpty(this.LastName))
                {
                    MessageBox.Show("Last Name is required.", "Required");
                    return;
                }
                else if (string.IsNullOrEmpty(this.MobileNumber))
                {
                    MessageBox.Show("Mobile Number is required.", "Required");
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(TableNumber) && string.IsNullOrWhiteSpace(OtherNumber))
            {
                MessageBox.Show("Please choice a number or put number in the text box.", "Required.");
                return;
            }

            var reqEntity = new SaveUserDataRequestEntity
            {
                Action = "master",
                FirstName = this.FirstName,
                LastName = this.LastName,
                CountryCode = Config.MasterStore.CountryCode,
                MasterStoreId = Config.MasterStore.StoreId,
                SuperMasterId = Config.MasterStore.UserId
            };

            if ("0000000000".Equals(this.MobileNumber))
            {
                reqEntity.OrphanStatus = 1;
            }
            else
            {
                reqEntity.OrphanStatus = 0;
            }

            reqEntity.Mobile = this.MobileNumber;
            reqEntity.PostalCode = string.Empty;
            reqEntity.HomePhone = string.Empty;
            reqEntity.DeliverOrderStatus = TotalStoreUsers;
            //reqEntity.FillStatus = "0000000000".Equals(this.MobileNumber) ? 0 : 1;
            reqEntity.Button1 = string.IsNullOrWhiteSpace(TableNumber) ? OtherNumber : TableNumber;

            SetLoaderVisibility("Adding user...");
            var result = await _windowsManager.SaveUserData(reqEntity, false);
            if (result.StatusCode == (int)GenericStatusValue.Success)
            {
                SetLoaderVisibility();
                if (Convert.ToBoolean(result.Status))
                {
                    ResetFields();
                    if (this.StoreUsers.Count < 4)
                        await GetData();
                }
                else
                {
                    if (result.Messagee == "Mobile no doesnot exits!")
                    {
                        // ask to Register
                        var message = $"Mobile Number: {MobileNumber} not found. {Environment.NewLine} Do you want to register a new user?";
                        const string title = "Mobile Number Not Found";

                        var action = MessageBox.Show(message, title, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                        if (action == MessageBoxResult.OK)
                        {
                            ExecuteRegisterUserCommand(reqEntity);
                        }
                        else
                            MessageBox.Show(result.Messagee, "Unsuccessful");
                    }
                    else
                        MessageBox.Show(result.Messagee, "Unsuccessful");
                }
            }
            else if (result.StatusCode == (int)GenericStatusValue.NoInternetConnection)
            {
                SetLoaderVisibility();
                MessageBox.Show(MessageBoxMessage.NoInternetConnection, "Unsuccessful");
            }
            else if (result.StatusCode == (int)GenericStatusValue.HasErrorMessage)
            {
                SetLoaderVisibility();
                MessageBox.Show(result.Message, "Unsuccessful");
            }
            else
            {
                SetLoaderVisibility();
                MessageBox.Show(MessageBoxMessage.UnknownErorr, "Unsuccessful");
            }

            SetLoaderVisibility();
            this.CanTapAddCommand = true;
        }

        private void ExecuteRegisterUserCommand(SaveUserDataRequestEntity user)
        {
            _eventAggregator.GetEvent<PopupVisibilityEvent>().Publish(true);
            var parameters = new NavigationParameters
            {
                { NavigationConstants.SelectedStoreUser, user }
            };

            RegionManager.RequestNavigate
                ("PopupRegion", ViewNames.RegisterUserPopupPage, parameters);
        }

        private async Task ExecuteDeleteStoreUserCommand(StoreUserEntity parameter)
        {
            var dialogResult = MessageBox.Show("Are you want to delete a user?", "Delete store user", MessageBoxButton.YesNo);

            if (dialogResult == MessageBoxResult.Yes)
            {
                SetLoaderVisibility("Deleting user...");

                var result = await _windowsManager.DeleteStoreUser(new DeleteStoreUserRequestEntity()
                {
                    Id = parameter.Id,
                    MasterStoreId = parameter.MasterStoreId,
                    OrphanStatus = parameter.OrphanStatus,
                    UserId = parameter.UserId,
                    SuperMasterId = Config.MasterStore.UserId
                });

                if (result.StatusCode == (int)GenericStatusValue.Success)
                {
                    SetLoaderVisibility();
                    if (Convert.ToBoolean(result.Status))
                    {
                        ResetFields();
                        await GetData();
                    }
                    else
                    {
                        MessageBox.Show(result.Messagee, "Unsuccessful");
                    }
                }
                else if (result.StatusCode == (int)GenericStatusValue.NoInternetConnection)
                {
                    SetLoaderVisibility();
                    MessageBox.Show(MessageBoxMessage.NoInternetConnection, "Unsuccessful");
                }
                else if (result.StatusCode == (int)GenericStatusValue.HasErrorMessage)
                {
                    SetLoaderVisibility();
                    MessageBox.Show(result.Message, "Unsuccessful");
                }
                else
                {
                    SetLoaderVisibility();
                    MessageBox.Show(MessageBoxMessage.UnknownErorr, "Unsuccessful");
                }

                SetLoaderVisibility();
            }
        }

        private async Task ExecuteDeleteArchiveUserCommand(StoreUserEntity parameter)
        {
            if (parameter.OrphanStatus == "1")
            {
                if (parameter.IdrStatus == "0")
                {
                    if (parameter.RegisterType == "second")
                    {
                        var dialogResult = MessageBox.Show("Id has not been checked? (Select Yes if you want Id Checked)", "", MessageBoxButton.YesNo);
                        if (dialogResult == MessageBoxResult.Yes)
                        {
                            await ManageUser(parameter);
                        }
                    }
                    else
                    {
                        var dialogResult = MessageBox.Show("This person was not verified. If you delete them, they will be not be registered. Delete anyway?", "Delete", MessageBoxButton.YesNo);
                        if (dialogResult == MessageBoxResult.Yes)
                        {
                            await DeleteArchiveUser(parameter);
                        }
                    }
                }
                else
                {
                    var dialogResult = MessageBox.Show("Are you sure you want to delete?", "Delete", MessageBoxButton.YesNo);
                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        await DeleteArchiveUser(parameter);
                    }
                }
            }
            else
            {
                var dialogResult = MessageBox.Show("Are you sure you want to delete?", "Delete", MessageBoxButton.YesNo);
                if (dialogResult == MessageBoxResult.Yes)
                {
                    await DeleteArchiveUser(parameter);
                }
            }
        }

        private async Task ManageUser(StoreUserEntity parameter)
        {
            SetLoaderVisibility("Deleting Archieve User...");

            var deleteResult = await _windowsManager.ManageUser(new ManageUserRequestEntity()
            {
                Id = parameter.Id
            });

            if (deleteResult.StatusCode == (int)GenericStatusValue.Success)
            {
                SetLoaderVisibility();
                await GetData();
            }
            else if (deleteResult.StatusCode == (int)GenericStatusValue.NoInternetConnection)
            {
                SetLoaderVisibility();
                MessageBox.Show(MessageBoxMessage.NoInternetConnection, "Unsuccessful");
            }
            else if (deleteResult.StatusCode == (int)GenericStatusValue.HasErrorMessage)
            {
                SetLoaderVisibility();
                MessageBox.Show(deleteResult.Message, "Unsuccessful");
            }
            else
            {
                SetLoaderVisibility();
                MessageBox.Show(MessageBoxMessage.UnknownErorr, "Unsuccessful");
            }
        }

        private async Task DeleteArchiveUser(StoreUserEntity parameter)
        {
            SetLoaderVisibility("Deleting Archieve User...");

            var deleteResult = await _windowsManager.DeleteArchiveUser(new DeleteArchiveUserRequestEntity()
            {
                Id = parameter.Id,
                UserId = parameter.UserId,
                MasterStoreId = parameter.MasterStoreId,
                SuperMasterId = Config.MasterStore.UserId
            });

            if (deleteResult.StatusCode == (int)GenericStatusValue.Success)
            {
                SetLoaderVisibility();
                await GetData();
            }
            else if (deleteResult.StatusCode == (int)GenericStatusValue.NoInternetConnection)
            {
                SetLoaderVisibility();
                MessageBox.Show(MessageBoxMessage.NoInternetConnection, "Unsuccessful");
            }
            else if (deleteResult.StatusCode == (int)GenericStatusValue.HasErrorMessage)
            {
                SetLoaderVisibility();
                MessageBox.Show(deleteResult.Message, "Unsuccessful");
            }
            else
            {
                SetLoaderVisibility();
                MessageBox.Show(MessageBoxMessage.UnknownErorr, "Unsuccessful");
            }
        }

        private void ExecuteEditStoreUserCommand(StoreUserEntity user)
        {
            _eventAggregator.GetEvent<PopupVisibilityEvent>().Publish(true);
            var parameters = new NavigationParameters();
            parameters.Add(NavigationConstants.SelectedStoreUser, user);
            this.RegionManager.RequestNavigate("PopupRegion", ViewNames.EditUserPopupPage, parameters);
        }

        private async void ExecuteFlagCommand(StoreUserEntity user)
        {
            try
            {
                SetLoaderVisibility("Updating Flag...");

                var result = await _windowsManager.SetUnsetFlag(new SetUnsetFlagRequestEntity()
                {
                    Id = user.Id,
                    MasterStoreId = user.MasterStoreId,
                    RecentStatus = user.IsFlagSet ? 0 : 1
                });

                if (result.StatusCode == (int)GenericStatusValue.Success)
                {
                    if (Convert.ToBoolean(result.Status))
                    {
                        await GetStoreUsers();
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unsuccessful");
            }
            finally
            {
                SetLoaderVisibility();
            }
        }

        private void ExecuteEditNonMobileStoreUserCommand(StoreUserEntity user)
        {
            //if (user.OrphanStatus == "1")
            //{
            //    _eventAggregator.GetEvent<PopupVisibilityEvent>().Publish(true);
            //    var parameters = new NavigationParameters();
            //    parameters.Add(NavigationConstants.SelectedStoreUser, user);
            //    parameters.Add(NavigationConstants.Action, "update_non_mobile");
            //    this.RegionManager.RequestNavigate("PopupRegion", ViewNames.UpdateNonMobileUserPopupPage, parameters);
            //}
        }

        private void ExecuteEditStoreButtonsCommand(StoreUserEntity user)
        {
            _eventAggregator.GetEvent<PopupVisibilityEvent>().Publish(true);
            var parameters = new NavigationParameters();
            parameters.Add(NavigationConstants.SelectedStoreUser, user);
            parameters.Add(NavigationConstants.Action, "update_non_mobile");
            parameters.Add(NavigationConstants.IsSelectedStoreUser, true);
            this.RegionManager.RequestNavigate("PopupRegion", ViewNames.EditButtonsPopupPage, parameters);
        }

        private void ExecuteEditArchiveButtonsCommand(StoreUserEntity user)
        {
            _eventAggregator.GetEvent<PopupVisibilityEvent>().Publish(true);
            var parameters = new NavigationParameters();
            parameters.Add(NavigationConstants.SelectedStoreUser, user);
            parameters.Add(NavigationConstants.Action, "update_non_mobile");
            parameters.Add(NavigationConstants.IsSelectedStoreUser, false);
            this.RegionManager.RequestNavigate("PopupRegion", ViewNames.EditButtonsPopupPage, parameters);
        }

        private void ExecuteEditNonMobileArchiveStoreUserCommand(StoreUserEntity user)
        {
            //if (user.OrphanStatus == "1")
            //{
            //    _eventAggregator.GetEvent<PopupVisibilityEvent>().Publish(true);
            //    var parameters = new NavigationParameters();
            //    parameters.Add(NavigationConstants.SelectedStoreUser, user);
            //    parameters.Add(NavigationConstants.Action, "update_non_mobile_archive");
            //    this.RegionManager.RequestNavigate("PopupRegion", ViewNames.UpdateNonMobileUserPopupPage, parameters);
            //}
        }

        private void ExecuteUserDetailWindowCommand(StoreUserEntity user)
        {
            if (userDetailsPage != null)
            {
                userDetailsPage.Close();
                userDetailsPage = null;
            }

            userDetailsPage = new UserDetailsPage { DataContext = user };
            userDetailsPage.PostalCodeText.Text = user.IsZipCode ? "Zip :" : "Postal :";
            userDetailsPage.Show();
        }

        private void ExecuteMoveStoreUserCommand(StoreUserEntity user)
        {
            var reverseStoreUsers = this.StoreUsers.ToList();
            reverseStoreUsers.Reverse();

            if (reverseStoreUsers.Count >= 5)
            {
                var selectedIndex = reverseStoreUsers.IndexOf(user);

                if (selectedIndex > 3)
                {
                    var dialogResult = MessageBox.Show("Are you sure you want to move this entry's position?", "Moving user position", MessageBoxButton.YesNo);

                    if (dialogResult == MessageBoxResult.Yes)
                    {
                        _eventAggregator.GetEvent<PopupVisibilityEvent>().Publish(true);
                        var parameters = new NavigationParameters();
                        parameters.Add(NavigationConstants.StoreUsers, reverseStoreUsers.ToList());
                        parameters.Add(NavigationConstants.SelectedIndex, selectedIndex);
                        this.RegionManager.RequestNavigate("PopupRegion", ViewNames.MoveUserPopupPage, parameters);
                    }
                }
                else
                {
                    MessageBox.Show("Sorry you can't move once in here.");
                }
            }
            else
            {
                MessageBox.Show("Sorry you can't move once in here.");
            }
        }

        private async Task ExecuteStoreIDCheckedCommand(StoreUserEntity parameter)
        {
            var dialogResult = MessageBox.Show("Id has not been checked? (Select Yes if you want Id Checked)", "ID Required", MessageBoxButton.YesNo);

            if (dialogResult == MessageBoxResult.Yes)
            {
                SetLoaderVisibility("Updating IDR...");

                var idrResult = await _windowsManager.CheckIDRStoreUser(new ManageUserRequestEntity()
                {
                    Id = parameter.Id,
                    MasterStoreId = parameter.MasterStoreId,
                    SuperMasterId = Config.MasterStore.UserId
                });

                if (idrResult.StatusCode == (int)GenericStatusValue.Success)
                {
                    SetLoaderVisibility();
                    await GetData();
                }
                else if (idrResult.StatusCode == (int)GenericStatusValue.NoInternetConnection)
                {
                    SetLoaderVisibility();
                    MessageBox.Show(MessageBoxMessage.NoInternetConnection, "Unsuccessful");
                }
                else if (idrResult.StatusCode == (int)GenericStatusValue.HasErrorMessage)
                {
                    SetLoaderVisibility();
                    MessageBox.Show(idrResult.Message, "Unsuccessful");
                }
                else
                {
                    SetLoaderVisibility();
                    MessageBox.Show(MessageBoxMessage.UnknownErorr, "Unsuccessful");
                }

                SetLoaderVisibility();
            }
        }

        private async Task ExecuteArchiveIDCheckedCommand(StoreUserEntity parameter)
        {
            var dialogResult = MessageBox.Show("Id has not been checked? (Select Yes if you want Id Checked)", "ID Required", MessageBoxButton.YesNo);

            if (dialogResult == MessageBoxResult.Yes)
            {
                SetLoaderVisibility("Updating IDR...");

                var idrResult = await _windowsManager.CheckIDRArchiveUser(new ManageUserRequestEntity()
                {
                    Id = parameter.Id,
                    MasterStoreId = parameter.MasterStoreId,
                    SuperMasterId = Config.MasterStore.UserId
                });

                if (idrResult.StatusCode == (int)GenericStatusValue.Success)
                {
                    SetLoaderVisibility();
                    await GetData();
                }
                else if (idrResult.StatusCode == (int)GenericStatusValue.NoInternetConnection)
                {
                    SetLoaderVisibility();
                    MessageBox.Show(MessageBoxMessage.NoInternetConnection, "Unsuccessful");
                }
                else if (idrResult.StatusCode == (int)GenericStatusValue.HasErrorMessage)
                {
                    SetLoaderVisibility();
                    MessageBox.Show(idrResult.Message, "Unsuccessful");
                }
                else
                {
                    SetLoaderVisibility();
                    MessageBox.Show(MessageBoxMessage.UnknownErorr, "Unsuccessful");
                }

                SetLoaderVisibility();
            }
        }

        private void SetLoaderVisibility(string message = "")
        {
            this.LoaderMessage = message;

            if (string.IsNullOrEmpty(message))
            {
                this.LoaderVisibility = Visibility.Collapsed;
            }
            else
            {
                this.LoaderVisibility = Visibility.Visible;
            }
        }

        private async Task ExecuteRefreshDataCommand()
        {
            SetLoaderVisibility("Loading data...");
            ResetFields();
            await GetData();
        }

        private void ResetFields()
        {
            this.NonMobileUser = null;
            this.FirstName = string.Empty;
            this.LastName = string.Empty;
            this.MobileNumber = string.Empty;
            Button1 = Button2 = Button3 = Button4 = Button5 = Button6 = Button7 = Button8 = false;
            OtherNumber = TableNumber = string.Empty;
        }

        private async Task GetData()
        {
            this.LoaderVisibility = Visibility.Visible;
            await Task.WhenAll(GetStoreUsers());
            await Task.WhenAll(GetArchieveStoreUsers());
            this.LoaderVisibility = Visibility.Collapsed;
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);
            try
            {
                await GetData();
            }
            catch (Exception ex)
            {
                string path = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembl‌​y().Location);
                path += "exception.txt";
                using (var outputFile = new StreamWriter(path, false, Encoding.UTF8))
                {
                    outputFile.WriteLine(ex.StackTrace);
                }
            }
        }

        private async Task GetStoreUsers()
        {
            var result = await _windowsManager.GetStoreUsers(new GetStoreUsersRequestEntity()
            {
                StoreId = Config.MasterStore.StoreId,
                SuperMasterId = Config.MasterStore.UserId
            });

            this.StoreUsers = new ObservableCollection<StoreUserEntity>();

            if (result.StatusCode == (int)GenericStatusValue.Success)
            {
                if (Convert.ToBoolean(result.Status))
                {
                    result.Data = result.Data.Where(x => x != null && !string.IsNullOrEmpty(x.Id)).ToList();

                    if (result.Data.Count > 0)
                    {
                        //The bottom 4 rows of the table are yellow (like pouring of water).
                        result.Data.Skip(Math.Max(0, result.Data.Count() - 4)).ToList().ForEach(s => s.Column2RowColor = ColorNames.Yellow);

                        if (result.Data.Count > 4)
                        {
                            int takeSecontFour = result.Data.Count <= 8 ? result.Data.Count() - 4 : 4;

                            //And as you pour more water, rows 5, 6, 7, and 8 will be blue.
                            result.Data.Skip(Math.Max(0, result.Data.Count() - 8)).Take(takeSecontFour).ToList().ForEach(s => s.Column2RowColor = ColorNames.Blue);
                        }

                        //And as you pour more rows 9 and above all the way to infinity are green.
                        //NOTE: We have set the green color as a default color so no needed for code.
                    }

                    this.StoreUsers = new ObservableCollection<StoreUserEntity>(result.Data);
                }
            }
        }

        private async Task GetArchieveStoreUsers()
        {
            var result = await _windowsManager.GetArchieveStoreUsers(new GetStoreUsersRequestEntity()
            {
                StoreId = Config.MasterStore.StoreId,
                SuperMasterId = Config.MasterStore.UserId
            });

            this.ArchieveStoreUsers = new ObservableCollection<StoreUserEntity>();

            if (result.StatusCode == (int)GenericStatusValue.Success)
            {
                if (Convert.ToBoolean(result.Status))
                {
                    result.Data = result.Data.Where(x => x != null && !string.IsNullOrEmpty(x.Id)).ToList();
                    this.ArchieveStoreUsers = new ObservableCollection<StoreUserEntity>(result.Data);
                }
            }
        }
    }
}