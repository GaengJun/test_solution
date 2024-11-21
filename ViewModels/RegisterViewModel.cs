using System;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using Dapper;
using System.Configuration;
using System.Diagnostics;
using MES.Solution.Helpers;
using System.Windows.Controls;

namespace MES.Solution.ViewModels
{
    public class RegisterViewModel : ViewModelBase
    {
        private readonly string _connectionString;
        private static readonly Regex UsernamePattern = new Regex(@"^[a-zA-Z0-9_-]{4,20}$");
        private static readonly Regex EmailPattern = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        private static readonly Regex PasswordPattern = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,20}$");

        // 필드
        private string _username = string.Empty;
        private string _email = string.Empty;
        private string _selectedRole = "USER";
        private bool _isUsernameChecked;
        private bool _isEmailChecked;
        private string _password;
        private string _confirmPassword;
        private bool _canRegister;

        // 오류 메시지 필드
        private string _usernameError = string.Empty;
        private string _emailError = string.Empty;
        private string _passwordError = string.Empty;
        private string _confirmPasswordError = string.Empty;
        private string _permissionError = string.Empty;
        private string _generalError = string.Empty;

        public RegisterViewModel()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MESDatabase"].ConnectionString;
            RegisterCommand = new RelayCommand(ExecuteRegister, () => CanRegister);
            CheckUsernameCommand = new RelayCommand(ExecuteCheckUsername);
            CheckEmailCommand = new RelayCommand(ExecuteCheckEmail);
        }

        // Properties
        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    _isUsernameChecked = false;
                    ValidateUsername();
                    UpdateCanRegister();
                }
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                if (SetProperty(ref _email, value))
                {
                    _isEmailChecked = false;
                    ValidateEmail();
                    UpdateCanRegister();
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
                    ValidatePassword(value);
                    UpdateCanRegister();
                }
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                if (SetProperty(ref _confirmPassword, value))
                {
                    ValidateConfirmPassword(Password, value);
                    UpdateCanRegister();
                }
            }
        }

        public string SelectedRole
        {
            get => _selectedRole;
            set => SetProperty(ref _selectedRole, value);
        }

        public bool CanRegister
        {
            get => _canRegister;
            private set => SetProperty(ref _canRegister, value);
        }

        // 오류 메시지 Properties
        public string UsernameError
        {
            get => _usernameError;
            set => SetProperty(ref _usernameError, value);
        }

        public string EmailError
        {
            get => _emailError;
            set => SetProperty(ref _emailError, value);
        }

        public string PasswordError
        {
            get => _passwordError;
            set => SetProperty(ref _passwordError, value);
        }

        public string ConfirmPasswordError
        {
            get => _confirmPasswordError;
            set => SetProperty(ref _confirmPasswordError, value);
        }

        public string PermissionError
        {
            get => _permissionError;
            set => SetProperty(ref _permissionError, value);
        }

        public string GeneralError
        {
            get => _generalError;
            set => SetProperty(ref _generalError, value);
        }

        // Commands
        public ICommand RegisterCommand { get; }
        public ICommand CheckUsernameCommand { get; }
        public ICommand CheckEmailCommand { get; }

        private bool ValidateUsername()
        {
            if (string.IsNullOrEmpty(Username))
            {
                UsernameError = "아이디를 입력하세요.";
                return false;
            }

            if (!UsernamePattern.IsMatch(Username))
            {
                UsernameError = "아이디는 4~20자의 영문, 숫자, 특수문자(-_)만 사용 가능합니다.";
                return false;
            }

            if (!_isUsernameChecked)
            {
                UsernameError = "중복 확인이 필요합니다.";
                return false;
            }

            UsernameError = string.Empty;
            return true;
        }

        private bool ValidateEmail()
        {
            if (string.IsNullOrEmpty(Email))
            {
                EmailError = "이메일을 입력하세요.";
                return false;
            }

            if (!EmailPattern.IsMatch(Email))
            {
                EmailError = "올바른 이메일 형식이 아닙니다.";
                return false;
            }

            if (!_isEmailChecked)
            {
                EmailError = "중복 확인이 필요합니다.";
                return false;
            }

            EmailError = string.Empty;
            return true;
        }
        private bool ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                PasswordError = "비밀번호를 입력하세요.";
                return false;
            }

            if (!PasswordPattern.IsMatch(password))
            {
                PasswordError = "비밀번호는 8~20자의 영문 대/소문자, 숫자, 특수문자를 포함해야 합니다.";
                return false;
            }

            PasswordError = string.Empty;
            ValidateConfirmPassword(password, ConfirmPassword);
            return true;
        }

        private bool ValidateConfirmPassword(string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(confirmPassword))
            {
                ConfirmPasswordError = "비밀번호 확인을 입력하세요.";
                return false;
            }

            if (password != confirmPassword)
            {
                ConfirmPasswordError = "비밀번호가 일치하지 않습니다.";
                return false;
            }

            ConfirmPasswordError = string.Empty;
            return true;
        }

        private bool ValidatePermission()
        {
            if (string.IsNullOrEmpty(SelectedRole))
            {
                PermissionError = "권한을 선택하세요.";
                return false;
            }

            PermissionError = string.Empty;
            return true;
        }

        private void UpdateCanRegister()
        {
            bool isValid = !string.IsNullOrEmpty(Username)
                          && !string.IsNullOrEmpty(Email)
                          && !string.IsNullOrEmpty(Password)
                          && !string.IsNullOrEmpty(ConfirmPassword)
                          && string.IsNullOrEmpty(UsernameError)
                          && string.IsNullOrEmpty(EmailError)
                          && string.IsNullOrEmpty(PasswordError)
                          && string.IsNullOrEmpty(ConfirmPasswordError)
                          && _isUsernameChecked
                          && _isEmailChecked;

            CanRegister = isValid;
            var command = RegisterCommand as RelayCommand;
            command?.RaiseCanExecuteChanged();
        }

        private void ExecuteCheckUsername()
        {
            try
            {
                if (!UsernamePattern.IsMatch(Username))
                {
                    UsernameError = "아이디는 4~20자의 영문, 숫자, 특수문자(-_)만 사용 가능합니다.";
                    return;
                }

                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "SELECT COUNT(1) FROM db_user WHERE username = @Username";
                    int count = connection.ExecuteScalar<int>(sql, new { Username });

                    _isUsernameChecked = count == 0;
                    UsernameError = _isUsernameChecked ? "사용 가능한 아이디입니다." : "이미 사용중인 아이디입니다.";
                    UpdateCanRegister();
                }
            }
            catch (Exception ex)
            {
                UsernameError = "중복 확인 중 오류가 발생했습니다.";
                Debug.WriteLine($"Username check error: {ex.Message}");
                UpdateCanRegister();
            }
        }

        private void ExecuteCheckEmail()
        {
            try
            {
                if (!EmailPattern.IsMatch(Email))
                {
                    EmailError = "올바른 이메일 형식이 아닙니다.";
                    return;
                }

                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "SELECT COUNT(1) FROM db_user WHERE email = @Email";
                    int count = connection.ExecuteScalar<int>(sql, new { Email });

                    _isEmailChecked = count == 0;
                    EmailError = _isEmailChecked ? "사용 가능한 이메일입니다." : "이미 사용중인 이메일입니다.";
                    UpdateCanRegister();
                }
            }
            catch (Exception ex)
            {
                EmailError = "중복 확인 중 오류가 발생했습니다.";
                Debug.WriteLine($"Email check error: {ex.Message}");
                UpdateCanRegister();
            }
        }

        public static class PasswordBoxHelper
{
    public static readonly DependencyProperty IsEmptyProperty =
        DependencyProperty.RegisterAttached("IsEmpty", typeof(bool), typeof(PasswordBoxHelper), new PropertyMetadata(true));

    public static bool GetIsEmpty(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsEmptyProperty);
    }

    public static void SetIsEmpty(DependencyObject obj, bool value)
    {
        obj.SetValue(IsEmptyProperty, value);
    }

    public static void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        PasswordBox passwordBox = sender as PasswordBox;
        if (passwordBox != null)
        {
            SetIsEmpty(passwordBox, string.IsNullOrEmpty(passwordBox.Password));
        }
    }
}
        private void ExecuteRegister()
        {
            try
            {
                var window = Application.Current.Windows[1] as Window;
                if (window == null)
                {
                    GeneralError = "시스템 오류가 발생했습니다.";
                    return;
                }

                var passwordBox = window.FindName("PasswordBox") as System.Windows.Controls.PasswordBox;
                var confirmPasswordBox = window.FindName("ConfirmPasswordBox") as System.Windows.Controls.PasswordBox;

                if (passwordBox == null || confirmPasswordBox == null)
                {
                    GeneralError = "시스템 오류가 발생했습니다.";
                    return;
                }

                if (!ValidateUsername() ||
                    !ValidateEmail() ||
                    !ValidatePassword(passwordBox.Password) ||
                    !ValidateConfirmPassword(passwordBox.Password, confirmPasswordBox.Password) ||
                    !ValidatePermission())
                {
                    return;
                }

                using (MySqlConnection connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    using (MySqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            string checkSql = "SELECT COUNT(1) FROM db_user WHERE username = @Username OR email = @Email";
                            int exists = connection.ExecuteScalar<int>(checkSql, new { Username, Email }, transaction);

                            if (exists > 0)
                            {
                                GeneralError = "아이디 또는 이메일이 이미 사용중입니다.";
                                transaction.Rollback();
                                return;
                            }

                            string sql = @"
                                INSERT INTO db_user (username, password_hash, email, role_id, created_at, is_active) 
                                VALUES (@Username, @Password, @Email, @RoleId, CURRENT_TIMESTAMP, 1)";

                            int roleId = SelectedRole == "ADMIN" ? 1 : 2;

                            connection.Execute(sql, new
                            {
                                Username,
                                Password = passwordBox.Password,
                                Email,
                                RoleId = roleId
                            }, transaction);

                            transaction.Commit();
                            MessageBox.Show("회원가입이 완료되었습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                            window.Close();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                GeneralError = "회원가입 중 오류가 발생했습니다.";
                Debug.WriteLine($"Registration error: {ex.Message}");
            }
        }
    }
}