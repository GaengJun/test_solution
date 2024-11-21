using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using Dapper;
using System.Configuration;
using System.Diagnostics;
using MES.Solution.Helpers;
using MES.Solution.Views;
using MES.Solution.Models;
using System.Linq;
using MES.Solution.Services;

namespace MES.Solution.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly string _connectionString;
        private string _username = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _autoLogin = false;
        private string _password;
        private RelayCommand _loginCommand;
        private RelayCommand _registerCommand;

        private readonly LogService _logService;//로그사용
        public ICommand LoginCommand
        {
            get => _loginCommand;
            private set => SetProperty(ref _loginCommand, (RelayCommand)value);
        }
        public ICommand RegisterCommand
        {
            get => _registerCommand;
            private set => SetProperty(ref _registerCommand, (RelayCommand)value);
        }

 
        public LoginViewModel()
        {
            // LogService 초기화
            _logService = new LogService();
            try
            {
                _connectionString = ConfigurationManager.ConnectionStrings["MESDatabase"].ConnectionString;
                RegisterCommand = new RelayCommand(ExecuteRegister);
                LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);

                // DB 연결 테스트
                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    Debug.WriteLine("DB 연결 성공");
                }

                // 자동 로그인 체크
                if (Properties.Settings.Default.AutoLogin)
                {
                    Username = Properties.Settings.Default.LastUsername;
                    AutoLogin = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"초기화 오류: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Initialization error: {ex.Message}");
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    (_loginCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (SetProperty(ref _password, value))
                {
                    (_loginCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool AutoLogin
        {
            get => _autoLogin;
            set => SetProperty(ref _autoLogin, value);
        }

        private async void ExecuteLogin()
        {
            try
            {
                // 연결 문자열 출력
                Debug.WriteLine($"Connecting with connection string: {_connectionString}");

                var loginWindow = Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault();
                if (loginWindow == null)
                {
                    ErrorMessage = "시스템 오류가 발생했습니다.";
                    return;
                }

                var passwordBox = loginWindow.FindName("PasswordBox") as PasswordBox;
                if (passwordBox == null)
                {
                    ErrorMessage = "로그인 정보를 가져올 수 없습니다.";
                    return;
                }
                string password = passwordBox.Password;

                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string userExistsQuery = "SELECT * FROM db_user WHERE username = @Username";
                    Debug.WriteLine($"Checking user existence: {Username}");

                    dynamic user = await connection.QueryFirstOrDefaultAsync(userExistsQuery, new { Username });

                    if (user == null)
                    {
                        Debug.WriteLine("User not found");
                        ErrorMessage = "사용자를 찾을 수 없습니다.";
                        return;
                    }

                    if (user.password_hash != password)
                    {
                        Debug.WriteLine("Invalid password");
                        ErrorMessage = "비밀번호가 일치하지 않습니다.";
                        return;
                    }

                    string roleQuery = "SELECT role_name FROM user_roles WHERE role_id = @RoleId";
                    string roleName = await connection.QueryFirstOrDefaultAsync<string>(roleQuery, new { RoleId = user.role_id });

                    Debug.WriteLine($"Login successful. Username: {user.username}, Role: {roleName}");

                    // 로그 저장: 로그인 성공 시 로그를 기록합니다.
                    string actionDetail = $"사용자: {user.username}, 역할: {roleName}";
                    await _logService.SaveLogAsync(user.user_id, "로그인", actionDetail);

                    await connection.ExecuteAsync(
                        "UPDATE db_user SET last_login = CURRENT_TIMESTAMP WHERE user_id = @UserId",
                        new { UserId = user.user_id }
                    );

                    Properties.Settings.Default.LastUsername = AutoLogin ? Username : string.Empty;
                    Properties.Settings.Default.AutoLogin = AutoLogin;
                    Properties.Settings.Default.Save();

                    App.CurrentUser = new CurrentUser
                    {
                        UserId = user.user_id,
                        Username = user.username,
                        Email = user.email,
                        RoleName = roleName,
                        LoggedInTime = DateTime.Now
                    };

                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    Application.Current.MainWindow.Close();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "로그인 중 오류가 발생했습니다.";
                Debug.WriteLine($"Login error: {ex.Message}");
            }
        }

        private void ExecuteRegister()
        {
            try
            {
                RegisterWindow registerWindow = new RegisterWindow();
                registerWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Register window error: {ex.Message}");
                MessageBox.Show("회원가입 창을 열 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearLoginData()
        {
            Password = string.Empty;
            Username = string.Empty;
            // 다른 데이터 초기화...
        }

        private DateTime? _lockoutUntil = null;

        private bool CanExecuteLogin()
        {
            if (_lockoutUntil.HasValue && DateTime.Now < _lockoutUntil.Value)
            {
                return false;
            }
            return !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);
        }


    }
}