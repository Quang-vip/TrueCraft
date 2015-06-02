﻿using System;
using Xwt;
using Xwt.Drawing;
using System.Reflection;
using TrueCraft.Core;
using System.Net;
using System.IO;

namespace TrueCraft.Launcher.Views
{
    public class LoginView : VBox
    {
        public LauncherWindow Window { get; set; }

        public TextEntry UsernameText { get; set; }
        public PasswordEntry PasswordText { get; set; }
        public Button LogInButton { get; set; }
        public Button RegisterButton { get; set; }
        public ImageView TrueCraftLogoImage { get; set; }
        public Label ErrorLabel { get; set; }
        public CheckBox RememberCheckBox { get; set; }

        public LoginView(LauncherWindow window)
        {
            Window = window;
            this.MinWidth = 250;

            ErrorLabel = new Label("Username or password incorrect")
            {
                TextColor = Color.FromBytes(255, 0, 0),
                TextAlignment = Alignment.Center,
                Visible = false
            };
            UsernameText = new TextEntry();
            PasswordText = new PasswordEntry();
            LogInButton = new Button("Log In");
            RegisterButton = new Button("Register");
            RememberCheckBox = new CheckBox("Remember Me");
            UsernameText.Text = UserSettings.Local.Username;
            if (UserSettings.Local.AutoLogin)
            {
                PasswordText.Password = UserSettings.Local.Password;
                RememberCheckBox.Active = true;
            }

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TrueCraft.Launcher.Content.truecraft-logo.png"))
                TrueCraftLogoImage = new ImageView(Image.FromStream(stream));

            UsernameText.PlaceholderText = "Username";
            PasswordText.PlaceholderText = "Password";
            PasswordText.KeyReleased += (sender, e) =>
            {
                if (e.Key == Key.Return || e.Key == Key.NumPadEnter)
                    LogInButton_Clicked(sender, e);
            };
            UsernameText.KeyReleased += (sender, e) =>
            {
                if (e.Key == Key.Return || e.Key == Key.NumPadEnter)
                    LogInButton_Clicked(sender, e);
            };
            RegisterButton.Clicked += (sender, e) => Window.WebView.Url = "http://truecraft.io/register";
            LogInButton.Clicked += LogInButton_Clicked;

            this.PackStart(TrueCraftLogoImage);
            this.PackEnd(RegisterButton);
            this.PackEnd(LogInButton);
            this.PackEnd(RememberCheckBox);
            this.PackEnd(PasswordText);
            this.PackEnd(UsernameText);
            this.PackEnd(ErrorLabel);
        }

        private void DisableForm()
        {
            UsernameText.Sensitive = PasswordText.Sensitive = LogInButton.Sensitive = RegisterButton.Sensitive = false;
        }

        private void EnableForm()
        {
            UsernameText.Sensitive = PasswordText.Sensitive = LogInButton.Sensitive = RegisterButton.Sensitive = true;
        }

        private void LogInButton_Clicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(UsernameText.Text) || string.IsNullOrEmpty(PasswordText.Password))
            {
                ErrorLabel.Text = "Username and password are required";
                ErrorLabel.Visible = true;
                return;
            }
            ErrorLabel.Visible = false;
            DisableForm();

            Window.User.Username = UsernameText.Text;
            var request = WebRequest.CreateHttp(TrueCraftUser.AuthServer + "/api/login");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.BeginGetRequestStream(HandleLoginRequestReady, request);
        }

        private void HandleLoginRequestReady(IAsyncResult asyncResult)
        {
            var request = (HttpWebRequest)asyncResult.AsyncState;
            var requestStream = request.EndGetRequestStream(asyncResult);
            using (var writer = new StreamWriter(requestStream))
                writer.Write(string.Format("user={0}&password={1}&version=12", UsernameText.Text, PasswordText.Password));
            request.BeginGetResponse(HandleLoginResponse, request);
        }

        private void HandleLoginResponse(IAsyncResult asyncResult)
        {
            var request = (HttpWebRequest)asyncResult.AsyncState;
            var response = request.EndGetResponse(asyncResult);
            string session;
            using (var reader = new StreamReader(response.GetResponseStream()))
                session = reader.ReadToEnd();
            if (session.Contains(":"))
            {
                var parts = session.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                Window.User.Username = parts[2];
                Window.User.SessionId = parts[3];
                Application.Invoke(() =>
                {
                    EnableForm();
                    Window.MainContainer.Remove(this);
                    Window.MainContainer.PackEnd(Window.MainMenuView = new MainMenuView(Window));
                    UserSettings.Local.AutoLogin = RememberCheckBox.Active;
                    UserSettings.Local.Username = Window.User.Username;
                    if (UserSettings.Local.AutoLogin)
                        UserSettings.Local.Password = PasswordText.Password;
                    else
                        UserSettings.Local.Password = string.Empty;
                    UserSettings.Local.Save();
                });
            }
            else
            {
                Application.Invoke(() =>
                {
                    EnableForm();
                    ErrorLabel.Text = session;
                    ErrorLabel.Visible = true;
                });
            }
        }
    }
}