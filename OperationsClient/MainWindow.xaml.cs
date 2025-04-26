using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OperationsClient
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _http = new HttpClient();

        public MainWindow()
        {
            InitializeComponent();

            // טעינת קובץ ההגדרות appsettings.json
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var apiBaseUrl = config["ApiBaseUrl"];


            if (string.IsNullOrWhiteSpace(apiBaseUrl))
            {
                MessageBox.Show("שגיאה: לא נמצאה כתובת API בקובץ appsettings.json");
                Application.Current.Shutdown(); // סוגר את המערכת 
                return;
            }

            // אם עברנו את הבדיקה - apiBaseUrl בטוח לא ריק
            _http.BaseAddress = new Uri(apiBaseUrl);

            LoadOperations();
        }

        private async void LoadOperations()
        {
            try
            {
                // שליחת בקשה לשרת לקבלת כל הפעולות
                var response = await _http.GetAsync("operations/list");
                var content = await response.Content.ReadAsStringAsync();
                var list = JArray.Parse(content);
                OperationBox.ItemsSource = list;
                OperationBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("שגיאה בטעינת הפעולות: " + ex.Message);
            }
        }

        private async void OnCalculateClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var field1 = Field1Box.Text;
                var field2 = Field2Box.Text;
                var operation = OperationBox.SelectedItem?.ToString()
                    ;
                // שליחת בקשת חישוב לשרת
                var response = await _http.GetAsync($"operations/calculate?field1={field1}&field2={field2}&operation={operation}");
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    // ניסיון להוציא הודעת שגיאה מהשרת
                    string serverError = "שגיאה בבקשה לשרת.";

                    try
                    {
                        serverError = JObject.Parse(json)["title"]?.ToString() ?? serverError;
                    }
                    catch
                    {
                    }

                    ResultText.Text = $"{serverError}\nיש להכניס ערכים חוקיים בשדות."; 
                    return;
                }

                JObject resultData = JObject.Parse(json);
                string result = resultData["result"]?.ToString() ?? "לא ידוע";
                int countThisMonth = resultData["countThisMonth"]?.ToObject<int>() ?? 0;

                string output = $"🔢 תוצאה: {result}\n";
                output += $"📅 סה\"כ פעולות החודש: {countThisMonth}\n\n";

                var last3 = resultData["last3SameType"] as JArray; // שליפת 3 פעולות אחרונות

                if (last3 != null && last3.Count > 0)
                {
                    output += "🕒 3 פעולות אחרונות:\n"; 
                    foreach (var entry in last3)
                    {
                        var f1 = entry["field1"]?.ToString();
                        var f2 = entry["field2"]?.ToString();
                        var res = entry["result"]?.ToString();
                        var time = entry["executedAt"]?.ToString();
                        output += $"- {f1} {operation} {f2} = {res} ({time})\n";
                    }
                }
                else
                {
                    output += "אין פעולות קודמות מהסוג הזה.\n";
                }

                ResultText.Text = output;
            }
            catch (Exception ex)
            {
                ResultText.Text = "שגיאה: " + ex.Message;
            }
        }
    }
}
