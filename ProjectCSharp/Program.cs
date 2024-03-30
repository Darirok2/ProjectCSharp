using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class Answer
{
    public string Text { get; set; }
    public bool IsCorrect { get; set; }
}

public class Question
{
    public string QuestionText { get; set; }
    public List<Answer> Answers { get; set; }
}

public class QuestionSection
{
    public int ID { get; set; }
    public string Theme { get; set; }
    public List<Question> Questions { get; set; }
}

public class User
{
    public string Nickname { get; set; }
    public string Password { get; set; }
    public DateTime DateOfBirth { get; set; }
    public bool IsAdmin { get; set; }

    public User(string nickname, string password, DateTime dateOfBirth, bool isAdmin)
    {
        Nickname = nickname;
        Password = password;
        DateOfBirth = dateOfBirth;
        IsAdmin = isAdmin;
    }

    public void ChangePassword(string newPassword)
    {
        Password = newPassword;
    }

    public void ChangeDateOfBirth(DateTime newDateOfBirth)
    {
        DateOfBirth = newDateOfBirth;
    }
}

public class Quiz
{
    public int LastID { get; set; }
    public Dictionary<string, List<QuestionSection>> QuestionsBySection { get; set; }
    public Dictionary<User, List<Question>> UserAnswers { get; set; }

    public Quiz()
    {
        LastID = 0;
        QuestionsBySection = new Dictionary<string, List<QuestionSection>>();
        UserAnswers = new Dictionary<User, List<Question>>();
    }

    public static Quiz LoadQuestionsFromJson(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new Quiz();
        }

        string json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<Quiz>(json);
    }

    public void SaveQuizzesToJson(string filePath)
    {
        string json = JsonConvert.SerializeObject(this);
        File.WriteAllText(filePath, json);
    }
}

public class QuizSystem
{
    private string usersJsonPath = "users.json";
    private string questionsJsonPath = "questions.json";

    public Quiz LoadQuestionsFromJson()
    {
        return Quiz.LoadQuestionsFromJson(questionsJsonPath);
    }

    public void SaveQuizzesToJson(Quiz quiz)
    {
        quiz.SaveQuizzesToJson(questionsJsonPath);
    }

    public void SaveUsersToJson(List<User> users)
    {
        string json = JsonConvert.SerializeObject(users);
        File.WriteAllText(usersJsonPath, json);
    }

    public List<User> LoadUsersFromJson()
    {
        if (!File.Exists(usersJsonPath))
        {
            return new List<User>();
        }

        string json = File.ReadAllText(usersJsonPath);
        return JsonConvert.DeserializeObject<List<User>>(json);
    }

    public User LogIn(string nickname, string password)
    {
        List<User> users = LoadUsersFromJson();
        User user = users.Find(u => u.Nickname == nickname && u.Password == password);
        return user;
    }

    public void AddUser(User newUser)
    {
        List<User> users = LoadUsersFromJson();
        users.Add(newUser);
        SaveUsersToJson(users);
    }

    public void AddQuestion(string section, QuestionSection questionSection)
    {
        Quiz quiz = LoadQuestionsFromJson();
        if (!quiz.QuestionsBySection.ContainsKey(section))
        {
            quiz.QuestionsBySection[section] = new List<QuestionSection>();
        }
        questionSection.ID = quiz.LastID + 1;
        quiz.QuestionsBySection[section].Add(questionSection);
        quiz.LastID++;
        SaveQuizzesToJson(quiz);
    }
}

public class UI
{
    private readonly QuizSystem quizSystem;

    public UI(QuizSystem quizSystem)
    {
        this.quizSystem = quizSystem;
    }

    public static void ShowError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: {message}");
        Console.ResetColor();
    }

    public static void ShowMessage(string message)
    {
        Console.WriteLine(message);
    }

    public static void ShowMenu()
    {
        Console.WriteLine("1. Пройти викторину");
        Console.WriteLine("2. Изменить пароль");
        Console.WriteLine("3. Изменить дату рождения");
        Console.WriteLine("4. Добавить вопрос");
        Console.WriteLine("5. Выйти");
    }

    public static void ShowLoginMenu()
    {
        Console.WriteLine("Выберите действие:");
        Console.WriteLine("1. Войти");
        Console.WriteLine("2. Зарегистрироваться");
    }

    public User RunLoginMenu()
    {
        int choice;
        do
        {
            ShowLoginMenu();
            int.TryParse(Console.ReadLine(), out choice);

            switch (choice)
            {
                case 1:
                    // Попытка входа пользователя
                    Console.Write("Введите ваш никнейм: ");
                    string nickname = Console.ReadLine();
                    Console.Write("Введите ваш пароль: ");
                    string password = Console.ReadLine();

                    User loggedInUser = quizSystem.LogIn(nickname, password);
                    if (loggedInUser != null)
                    {
                        Console.WriteLine($"Добро пожаловать, {loggedInUser.Nickname}!");
                        return loggedInUser;
                    }
                    else
                    {
                        Console.WriteLine("Неверный никнейм или пароль.");
                    }
                    break;
                case 2:
                    // Регистрация нового пользователя
                    Console.Write("Введите новый никнейм: ");
                    string newNickname = Console.ReadLine();
                    Console.Write("Введите пароль: ");
                    string newPassword = Console.ReadLine();
                    Console.Write("Введите дату рождения (гггг-мм-дд): ");
                    DateTime newDateOfBirth;
                    while (!DateTime.TryParse(Console.ReadLine(), out newDateOfBirth))
                    {
                        Console.WriteLine("Некорректный ввод. Пожалуйста, введите дату в правильном формате (гггг-мм-дд).");
                        Console.Write("Введите дату рождения (гггг-мм-дд): ");
                    }

                    User user = new User(newNickname, newPassword, newDateOfBirth, false);
                    quizSystem.AddUser(user);
                    Console.WriteLine("Пользователь успешно зарегистрирован.");
                    break;
                default:
                    Console.WriteLine("Некорректный ввод. Попробуйте снова.");
                    break;
            }
        } while (choice != 1 && choice != 2);

        return null;
    }

    private string ChooseTheme(string section)
    {
        Quiz quiz = quizSystem.LoadQuestionsFromJson();

        if (!quiz.QuestionsBySection.ContainsKey(section))
        {
            ShowError("Выбранный раздел не существует.");
            return null;
        }

        Console.WriteLine($"Доступные темы для раздела '{section}':");
        int themeIndex = 1;
        Dictionary<int, string> themeIndexes = new Dictionary<int, string>();
        foreach (var questionSection in quiz.QuestionsBySection[section])
        {
            Console.WriteLine($"{themeIndex}. {questionSection.Theme}");
            themeIndexes[themeIndex] = questionSection.Theme;
            themeIndex++;
        }

        int chosenThemeIndex;
        Console.Write($"Выберите тему для раздела '{section}': ");
        while (!int.TryParse(Console.ReadLine(), out chosenThemeIndex) || !themeIndexes.ContainsKey(chosenThemeIndex))
        {
            ShowError("Некорректный ввод. Пожалуйста, выберите номер темы из списка выше.");
            Console.Write($"Выберите тему для раздела '{section}': ");
        }

        return themeIndexes[chosenThemeIndex];
    }

    public void ShowResultForTheme(string section, string theme, int correctAnswersCount, int totalQuestionsCount)
    {
        Console.WriteLine($"Результаты для раздела '{section}' и темы '{theme}':");
        Console.WriteLine($"Вы ответили правильно на {correctAnswersCount} из {totalQuestionsCount} вопросов.");
    }

    public void StartQuiz(User user)
    {
        Quiz quiz = quizSystem.LoadQuestionsFromJson();

        if (quiz.QuestionsBySection.Count == 0)
        {
            Console.WriteLine("Нет доступных разделов для проведения викторины.");
            return;
        }

        Console.WriteLine("Доступные разделы викторины:");
        int sectionIndex = 1;
        Dictionary<int, string> sectionIndexes = new Dictionary<int, string>();
        foreach (var section in quiz.QuestionsBySection)
        {
            Console.WriteLine($"{sectionIndex}. {section.Key}");
            sectionIndexes[sectionIndex] = section.Key;
            sectionIndex++;
        }

        int chosenSectionIndex;
        Console.Write("Выберите раздел викторины: ");
        while (!int.TryParse(Console.ReadLine(), out chosenSectionIndex) || !sectionIndexes.ContainsKey(chosenSectionIndex))
        {
            ShowError("Некорректный ввод. Пожалуйста, выберите номер раздела из списка выше.");
            Console.Write("Выберите раздел викторины: ");
        }

        string chosenSection = sectionIndexes[chosenSectionIndex];

        string chosenTheme = ChooseTheme(chosenSection);
        if (chosenTheme == null)
        {
            return;
        }

        if (!quiz.QuestionsBySection.ContainsKey(chosenSection))
        {
            Console.WriteLine("Выбранный раздел не существует.");
            return;
        }

        List<QuestionSection> questionSections = quiz.QuestionsBySection[chosenSection].Where(qs => qs.Theme == chosenTheme).ToList();
        int correctAnswersCount = 0;

        foreach (QuestionSection section in questionSections)
        {
            Console.WriteLine($"Тема: {section.Theme}");
            foreach (Question question in section.Questions)
            {
                Console.WriteLine($"Вопрос: {question.QuestionText}");
                foreach (Answer answer in question.Answers)
                {
                    Console.WriteLine(answer.Text);
                }
                Console.Write("Введите номер правильного ответа: ");
                int chosenAnswerIndex;
                while (!int.TryParse(Console.ReadLine(), out chosenAnswerIndex) || chosenAnswerIndex < 1 || chosenAnswerIndex > question.Answers.Count)
                {
                    ShowError("Некорректный ввод. Пожалуйста, выберите номер правильного ответа из списка выше.");
                    Console.Write("Введите номер правильного ответа: ");
                }

                if (question.Answers[chosenAnswerIndex - 1].IsCorrect)
                {
                    Console.WriteLine("Верно!");
                    correctAnswersCount++;
                }
                else
                {
                    Console.WriteLine("Неверно!");
                }
            }
        }

        ShowResultForTheme(chosenSection, chosenTheme, correctAnswersCount, questionSections.SelectMany(qs => qs.Questions).Count());
    }

    public void ChangePassword(User user)
    {
        Console.Write("Введите новый пароль: ");
        string newPassword = Console.ReadLine();
        user.ChangePassword(newPassword);
        Console.WriteLine("Пароль успешно изменен.");
    }

    public void ChangeDateOfBirth(User user)
    {
        Console.Write("Введите новую дату рождения (гггг-мм-дд): ");
        DateTime newDateOfBirth;
        while (!DateTime.TryParse(Console.ReadLine(), out newDateOfBirth))
        {
            ShowError("Некорректный ввод. Пожалуйста, введите дату в правильном формате (гггг-мм-дд).");
            Console.Write("Введите новую дату рождения (гггг-мм-дд): ");
        }
        user.ChangeDateOfBirth(newDateOfBirth);
        Console.WriteLine("Дата рождения успешно изменена.");
    }

    public void AddQuestion(User user)
    {
        Console.Write("Введите раздел: ");
        string section = Console.ReadLine();
        Console.Write("Введите тему: ");
        string theme = Console.ReadLine();
        Console.Write("Введите текст вопроса: ");
        string questionText = Console.ReadLine();
        Console.Write("Введите количество ответов: ");
        int answerCount;
        while (!int.TryParse(Console.ReadLine(), out answerCount))
        {
            ShowError("Некорректный ввод. Пожалуйста, введите число.");
            Console.Write("Введите количество ответов: ");
        }
        List<Question> questions = new List<Question>();
        for (int i = 0; i < answerCount; i++)
        {
            Console.Write($"Введите текст ответа {i + 1}: ");
            string answerText = Console.ReadLine();
            Console.Write("Этот ответ верный? (true/false): ");
            bool isCorrect;
            while (!bool.TryParse(Console.ReadLine(), out isCorrect))
            {
                ShowError("Некорректный ввод. Пожалуйста, введите true или false.");
                Console.Write("Этот ответ верный?(true/false): ");
            }
            questions.Add(new Question { QuestionText = questionText, Answers = new List<Answer> { new Answer { Text = answerText, IsCorrect = isCorrect } } });
        }
        QuestionSection questionSection = new QuestionSection { Theme = theme, Questions = questions };
        quizSystem.AddQuestion(section, questionSection);
        Console.WriteLine("Вопрос успешно добавлен.");
    }
}

class Program
{
    static void Main(string[] args)
    {
        QuizSystem quizSystem = new QuizSystem();
        UI ui = new UI(quizSystem);

        User loggedInUser = null;

        while (true)
        {
            if (loggedInUser == null)
            {
                loggedInUser = ui.RunLoginMenu();
            }
            else
            {
                UI.ShowMenu();
                int choice;
                if (!int.TryParse(Console.ReadLine(), out choice))
                {
                    UI.ShowError("Некорректный ввод. Попробуйте снова.");
                    continue;
                }

                switch (choice)
                {
                    case 1:
                        // Прохождение викторины
                        ui.StartQuiz(loggedInUser);
                        break;
                    case 2:
                        // Изменение пароля
                        ui.ChangePassword(loggedInUser);
                        break;
                    case 3:
                        // Изменение даты рождения
                        ui.ChangeDateOfBirth(loggedInUser);
                        break;
                    case 4:
                        // Добавление вопроса
                        ui.AddQuestion(loggedInUser);
                        break;
                    case 5:
                        // Выход из системы
                        loggedInUser = null;
                        break;
                    default:
                        UI.ShowError("Некорректный ввод. Попробуйте снова.");
                        break;
                }
            }
        }
    }
}
