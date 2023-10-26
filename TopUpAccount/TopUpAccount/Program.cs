using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using System.Xml;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;

const string SECRET_KEY = "ADMIN";

while (true)
{
    Console.WriteLine("Start: ");
    string content = GetAllHistoryTransaction();
    Console.WriteLine("Content: " + content); 
    var historyTransactions = JsonConvert.DeserializeObject<TransactionResponseAPI>(content)?.ListHistoryTransaction;
    Console.WriteLine("HistoryTransactions: " + historyTransactions);
    if (historyTransactions != null)
    {
        //Gọi API GetAllTransactionID
        List<string> listTransactionID = GetAllTransactionID();
        Console.WriteLine("ListTransactionID: " + listTransactionID);
        foreach (var item in historyTransactions)
        {
            //Chỉ xét trường hợp giao dịch có nội dung hợp lệ (nội dung phải == UserID ứng dụng) và chưa có trong DB
            if (CheckValidContent(item.TransactionContent, out string username) && listTransactionID.Any(x => x == item.TransactionID) == false)
            {
                TransactionInsertRequest request = new TransactionInsertRequest()
                {
                    SecretKey = SECRET_KEY,
                    Money = float.Parse(item.Money),
                    TransactionContent = item.TransactionContent,
                    TransactionTime = ConvertSringToDateTime(item.TransactionTime, "dd/MM/yyyy"),
                    UserID = username
                };
                InsertTransaction(request);
                TopUpAccount(new AccountTopUpVM() { Username = username, AccountBalance = request.Money });
                Console.WriteLine("TopUpAccount: " + username + ", "+ request.Money);
            }
        }
    }
    Thread.Sleep(2000);
}


string GetAllHistoryTransaction()
{
    var contentAppsettings = File.ReadAllText("appsettings.json");
    var appSettings = JsonConvert.DeserializeObject<AppSettingsModel>(contentAppsettings);

    return File.ReadAllText(appSettings.FileHistoryPath);
}

List<string> GetAllTransactionID()
{
    var client = new HttpClient();
    var response = client.GetAsync("https://api.thanhtoan247.xyz/api/PRU221mControllers/GetAllTransactionID?SecretKey=" + SECRET_KEY).Result;
    var jsonString = response.Content.ReadAsStringAsync().Result;

    return JsonConvert.DeserializeObject<List<string>>(jsonString);
}

string InsertTransaction(TransactionInsertRequest requestData)
{
    var apiUrl = "https://api.thanhtoan247.xyz/api/PRU221mControllers/InsertTransaction";
    var client = new HttpClient();

    var requestContent = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

    //client.DefaultRequestHeaders.Accept.Clear();
    //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
    //client.DefaultRequestHeaders.Add("Authorization", "admin");

    var response = client.PostAsync(apiUrl, requestContent).Result;

    if (response.IsSuccessStatusCode)
    {
        var responseContent = response.Content.ReadAsStringAsync().Result;
        return responseContent;
    }
    else
    {
        throw new Exception($"Request failed: {response.StatusCode}");
    }
}

string TopUpAccount(AccountTopUpVM requestData)
{
    var apiUrl = "https://api.thanhtoan247.xyz/api/PRU221mControllers/TopUpAccount";
    var client = new HttpClient();

    var requestContent = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

    //client.DefaultRequestHeaders.Accept.Clear();
    //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
    //client.DefaultRequestHeaders.Add("Authorization", "admin");

    var response = client.PostAsync(apiUrl, requestContent).Result;

    if (response.IsSuccessStatusCode)
    {
        var responseContent = response.Content.ReadAsStringAsync().Result;
        return responseContent;
    }
    else
    {
        throw new Exception($"Request failed: {response.StatusCode}");
    }
}


bool CheckValidContent(string content, out string md5Hash)
{
    md5Hash = "";
    var match = Regex.Match(content, @"TT247.* |TT247.*");

    bool check = false;

    if (match.Success)
    {

        var contentValue = match.Value.Split("TT247")[1];

        md5Hash = contentValue;
        check = true;
    }
    return check;
}

DateTime ConvertSringToDateTime(string datetime, string pattern)
{
    DateTime result = DateTime.ParseExact(datetime, pattern, CultureInfo.InvariantCulture);
    return result;
}

public class TransactionInsertRequest
{
    public string SecretKey { get; set; }
    public string UserID { get; set; }
    public string TransactionContent { get; set; }
    public DateTime TransactionTime { get; set; }
    public float Money { get; set; }
}

public class TransactionResponseAPI
{
    public string LastUpdate { get; set; }
    public List<HistoryTransaction> ListHistoryTransaction { get; set; }
}

public class HistoryTransaction
{
    public string TransactionID { get; set; }
    public string TransactionTime { get; set; }
    public string TransactionContent { get; set; }
    public string Money { get; set; }
}

public class AppSettingsModel
{
    public string FileHistoryPath { get; set; }
}

public class AccountTopUpVM
{
    public string Username { get; set; }
    public float AccountBalance { get; set; }
}