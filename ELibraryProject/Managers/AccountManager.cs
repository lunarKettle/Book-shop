﻿using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;
using System.Configuration;
using System.IO;
using System.Text.Json;

namespace ELibraryProject.Managers
{
    /// <summary>
    /// Класс, работающий с данными пользователя. Реализованы методы регистрации, входа в аккаунт
    /// и восстановление пароля
    /// </summary>
    internal static class AccountManager
    {
        static SqlConnection? sqlConnection;
        public static bool TryEnterToSystem(string login, string password, out bool isAdmin)
        {
            isAdmin = false;
            string connectionString = getConnectionString();
            sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();

            string sqlExpression = $"SELECT Login, Password, isAdmin FROM Users WHERE Login = @login AND Password = @password";

            SqlCommand command = new SqlCommand(sqlExpression, sqlConnection);
            command.Parameters.AddWithValue("@login", login);
            command.Parameters.AddWithValue("@password", password);
            SqlDataReader reader = command.ExecuteReader();
            // 1. Создаем строку запроса; 2. Отправляем запрос; 3-4. Настраеваем параметры;
            // 5. Создаем чтением запроса

            if (reader.Read()) // Если результат запроса не пустой
            {
                isAdmin = reader.GetBoolean(2);
                sqlConnection.Close();
                reader.Close(); ;
                return true;
            }
            else
            {
                sqlConnection.Close();
                reader.Close();
                return false;
            }
        }

        public static bool RegInSystem(string name, string secondName, string email, string login, string password, string passwordAgain,
         string codeWord, string tipToCodeWord, out string message)
        {
            if (checkForNulls(name, secondName, email, login, password, passwordAgain,
                codeWord, tipToCodeWord, out message))
            {
                return false;
            }

            string connectionString = getConnectionString();
            sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();

            string sqlExpression = $"SELECT Email FROM Users WHERE Email = @email";

            SqlCommand command = new SqlCommand(sqlExpression, sqlConnection);
            command.Parameters.AddWithValue("@email", email);
            SqlDataReader reader = command.ExecuteReader();


            if (reader.Read())
            {
                sqlConnection.Close();
                reader.Close();
                message = "Эта почта уже зарегистрирована";
                return false;
            }
            reader.Close();

            sqlExpression = $"SELECT Login FROM Users WHERE Login = @login";
            command = new SqlCommand(sqlExpression, sqlConnection);
            command.Parameters.AddWithValue("@login", login);
            reader = command.ExecuteReader();

            if (reader.Read())
            {
                sqlConnection.Close();
                reader.Close();
                message = "Этот логин уже занят";
                return false;
            }

            reader.Close();

            if (password != passwordAgain)
            {
                message = "Ваши пароли не совпадают";
                return false;
            }

            sqlExpression = "INSERT INTO Users (Login, Password, Email, FirstName, LastName, " +
                "CodeWord, CodeWordHint) " +
                "VALUES (@Login, @Password, @Email, @FirstName, @LastName, @CodeWord, @CodeWordHint)";
            command = new SqlCommand(sqlExpression, sqlConnection);
            command = new SqlCommand(sqlExpression, sqlConnection);
            command.Parameters.AddWithValue("@Login", login);
            command.Parameters.AddWithValue("@Password", password);
            command.Parameters.AddWithValue("@Email", email);
            command.Parameters.AddWithValue("@FirstName", name);
            command.Parameters.AddWithValue("@LastName", secondName);
            command.Parameters.AddWithValue("@CodeWord", codeWord);
            command.Parameters.AddWithValue("@CodeWordHint", tipToCodeWord);

            if (sqlConnection.State == ConnectionState.Open)
            {
                command.ExecuteNonQuery();
                sqlConnection.Close();
                return true;
                // Регистрация окончена, данные в БД
            }
            else
            {
                sqlConnection.Close();
                message = "Непредвиденная ошибка";
                return false;
            }


        }

        /// <summary>
        /// Метод вернёт true и в msg сообщение о первой пустой строке или вернёт false, если все строки не пустые
        /// </summary>
        static bool checkForNulls(string name, string secondName, string email, string login, string password, string passwordAgain,
         string codeWord, string tipToCodeWord, out string message)
        {
            if (name == "")
            {
                message = "Заполните поле имени";
                return true;
            }

            if (secondName == "")
            {
                message = "Заполните поле фамилии";
                return true;
            }

            if (email == "")
            {
                message = "Заполните поле электронной почты";
                return true;
            }

            if (login == "")
            {
                message = "Заполните поле логина";
                return true;
            }

            if (password == "")
            {
                message = "Заполните поле пароля";
                return true;
            }

            if (passwordAgain == "")
            {
                message = "Заполните поле повторения пароля";
                return true;
            }

            if (codeWord == "")
            {
                message = "Заполните поле кодового слова";
                return true;
            }

            if (name == "")
            {
                message = "Заполните поле вопроса к кодовому слову";
                return true;
            }

            message = "Nope";
            return false;
        }

        public static bool isUserExist(string login, out string? message)
        {

            string connectionString = getConnectionString();
            sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();

            // Запрос на сверку строки, котору вписал пользователь, с логином/почтой
            string sqlExpression = $"SELECT CodeWordHint FROM Users WHERE Email = @email OR Login = @login";

            SqlCommand command = new SqlCommand(sqlExpression, sqlConnection);
            command.Parameters.AddWithValue("@email", login);
            command.Parameters.AddWithValue("@login", login);
            SqlDataReader reader = command.ExecuteReader();


            if (reader.Read())
            {
                message = reader.GetValue(0).ToString();
                sqlConnection.Close();
                return true;
            }
            message = "Пользователь с такими данными не найден";
            return false;
        }

        public static bool isCodeWordRight(string codeWord, string login)
        {
            string connectionString = getConnectionString();
            sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();

            // Запрос на сверку кодового слова
            string sqlExpression = "SELECT Password FROM Users WHERE (Email = @email OR Login = @login)" +
                " and CodeWord = @codeword";
            SqlCommand command = new SqlCommand(sqlExpression, sqlConnection);
            command.Parameters.AddWithValue("@login", login);
            command.Parameters.AddWithValue("@email", login);
            command.Parameters.AddWithValue("@codeWord", codeWord);
            SqlDataReader reader = command.ExecuteReader();
            if (reader.Read())
            {
                sqlConnection.Close();
                reader.Close();
                return true;
            }

            return false;
        }

        public static bool changePassword(string login, string password, string passwordAgain, out string msg)
        {
            if (password != passwordAgain)
            {
                msg = "Пароли не совпадают";
                return false;
            }

            string connectionString = getConnectionString();
            sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();

            // Запрос на смену(обновление) поля Password
            string sqlExpression = "UPDATE Users SET Password = @password WHERE Email = @email or Login = @login";
            SqlCommand command = new SqlCommand(sqlExpression, sqlConnection);
            command.Parameters.AddWithValue("@password", password);
            command.Parameters.AddWithValue("@email", login);
            command.Parameters.AddWithValue("@login", login);
            if (sqlConnection.State == ConnectionState.Open)
            {
                command.ExecuteNonQuery();
                sqlConnection.Close();
                msg = "Good";
                return true;
            }
            msg = "Неизвестная ошибка. Попробуйте ещё раз позже";
            return false;

        }

        async public static void tryToUseRecordedPassword(MainWindow mainWindow)
        {
            // создаю Пара/Ключ
            string login;
            KeyValuePair<string, string>? newPair;
            using (FileStream fs = new FileStream("user.json", FileMode.OpenOrCreate))
            {
                // пытаюсь читать с файла
                try
                {
                    newPair = await JsonSerializer.DeserializeAsync<KeyValuePair<string, string>>(fs);
                    login = newPair.Value.Key;

                    if (isHashEquals(login, newPair.Value.Value))
                    {
                        LoadAccount(login, mainWindow);
                    }
                }
                catch (Exception ex)
                {
                    // если была какая-то ошибка, то просто стираю файл дабы ошибки больше не было
                    fs.Close();
                    using (FileStream fs1 = new FileStream("user.json", FileMode.Truncate))
                    {

                    }
                    return;
                }

            }

        }

        private static bool isHashEquals(string login, string hash)
        {
            // Пока функция работает чисто на сравнивании строк, однако чуть позже нужно всё заменить на хэш
            string connectionString = getConnectionString();

            sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();

            string sqlExpression = "SELECT Password FROM Users WHERE Email = @email or Login = @login";
            SqlCommand command = new SqlCommand(sqlExpression, sqlConnection);
            command.Parameters.AddWithValue("@email", login);
            command.Parameters.AddWithValue("@login", login);

            SqlDataReader reader = command.ExecuteReader();

            if (reader.Read())
            {
                string passwordHash = reader["Password"].ToString();
                // после получения пароля сравнию этот пароль с тем, что лежит внутри json
                // без substring не работает т.к. строка всегда 32 в размере
                if (string.Compare(passwordHash.Substring(0, hash.Length), hash) == 0)
                {
                    sqlConnection.Close();
                    reader.Close();
                    return true;
                }
                sqlConnection.Close();
                reader.Close();

            }
            return false;
        }

        private static void LoadAccount(string login, MainWindow mainWindow)
        {
            // попытка загрузить аккаунт
            string question = "Войти в аккаунт " + login + "?";
            string caption = "Вход в систему";
            MessageBoxButton buttons = MessageBoxButton.YesNo;
            MessageBoxResult result = System.Windows.MessageBox.Show(question, caption, buttons);

            if (result is MessageBoxResult.Yes)
            {
                new UserWindow(login).Show();
                mainWindow.Close();
            }
        }

        public static void writeInfoToFile(string login, string password)
        {
            using (FileStream fs = new FileStream("user.json", FileMode.Truncate)) ;

            // запись в файл при правильном пароле
            using (FileStream fs = new FileStream("user.json", FileMode.OpenOrCreate))
            {
                KeyValuePair<string, string> pair = new KeyValuePair<string, string>(login, password);
                JsonSerializer.SerializeAsync(fs, pair);
                Console.WriteLine("Data has been saved to file");
            }
        }

        private static string getConnectionString()
        {
            string? connectionString = ConfigurationManager.ConnectionStrings["BookStoreDB"].ConnectionString;
            if (connectionString == null)
            {
                throw new InvalidOperationException("Connection string is null");
            }

            connectionString = connectionString
                .Replace("{AppDir}", AppDomain.CurrentDomain.BaseDirectory)
                .Replace("\\ELibraryProject\\bin\\Debug\\net8.0-windows\\", "");
            // 3 строки выше нужны т.к. у нас нет сервера и мы перекидываемся БД с одного ПК на другой
            // Потому путь получаем вот таким путём

            return connectionString;
        }

    }
}
