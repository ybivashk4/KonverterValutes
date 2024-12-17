using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using RestSharp;

namespace KonverterValutes
{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MyViewModel();
        }
        public class MyViewModel : INotifyPropertyChanged
        {
            private string _entryText1;
            private string _entryText2;
            private DateTime _date = DateTime.Now;
            private string char_code_1 = "";
            private string char_code_2 = "";
            private CurrencyService _currencyServiece = new CurrencyService();
            public Dictionary<string, double> currencies { get; set; } =
                new Dictionary<string, double> ();
            public ObservableCollection<string> currencies_list { get; set; } =
                new ObservableCollection<string>();
            public ICommand LoadCurrenciesCommand { get; }
            public MyViewModel()
            {
                // Как запустить со старта 
                LoadCurrenciesAsync();
                char_code_1 = "";
                char_code_2 = "";
                LoadCurrenciesCommand = new Command(async () => await LoadCurrenciesAsync());
            }

            public async Task LoadCurrenciesAsync ()
            {
                var currencies_ = await _currencyServiece.GetCurrenciesAsync(_date);
                currencies.Clear();
                currencies_list.Clear();
                foreach (var currency in currencies_)
                {
                    currencies.Add(currency.Key, currency.Value);
                    currencies_list.Add(currency.Key);
                }
            }

            public string EntryText1
            {
                get => _entryText1;
                set
                {
                    if (_entryText1 == value) return;
                    _entryText1 = value;
                    if (value is string str && char_code_1 != ""  && char_code_2 != "" && double.TryParse(str, out double num))
                    {
                        _entryText2 = convert_to(num, char_code_1, char_code_2).ToString();
                    }
                    OnPropertyChanged(nameof(EntryText1));
                    OnPropertyChanged(nameof(EntryText2));
                }
            }

            public string EntryText2
            {
                get => _entryText2;
                set
                {
                    if (_entryText2 == value) return;
                    _entryText2 = value;
                    if (value is string str && char_code_1 != "" && char_code_2 != "" && double.TryParse(str, out double num))
                        _entryText1 = convert_to(num, char_code_2, char_code_1).ToString(); // func that convert
                    OnPropertyChanged(nameof(EntryText1));
                    OnPropertyChanged(nameof(EntryText2));
                }
            }

            public DateTime SelectedDate
            {
                get => _date;
                set
                {
                    if (_date == value) return;
                    _date = value;
                    LoadCurrenciesAsync();
                    OnPropertyChanged(nameof(SelectedDate));
                }

            }

            public event PropertyChangedEventHandler? PropertyChanged;
            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            private double convert_to(double num, string first_char, string second_char)
            {
                if (currencies.Count > 0 && first_char != null && second_char != null && first_char != "" && second_char != "")
                {
                    double k = currencies[first_char] / currencies[second_char];
                    return num  * k;
                }
                return num;
            }

            public string SelectedCurrency1 {
                get => char_code_1;
                set
                {
                    char_code_1 = value;
                    OnPropertyChanged(nameof(SelectedCurrency1));
                    // Не работает обновление при событии пикера
                    OnPropertyChanged(nameof(EntryText1));
                    OnPropertyChanged(nameof(EntryText2));
                }
            }

            public string SelectedCurrency2
            {
                get => char_code_2;
                set
                {
                    char_code_2 = value;
                    OnPropertyChanged(nameof(SelectedCurrency2));
                    // Не работает обновление при событии пикера
                    OnPropertyChanged(nameof(EntryText1));
                    OnPropertyChanged(nameof(EntryText2));
                }
            }

        }

    }
    

    public class CurrencyService
    {
        private string ApiUrl = "";
        private const string BaseUrl = "https://www.cbr-xml-daily.ru/";
        public async Task<Dictionary<string, double>> GetCurrenciesAsync(DateTime _date)
        {
            ApiUrl = BaseUrl + "/archive/" + _date.ToString("yyyy/MM/dd/").Replace(".", "/") + "/daily_json.js";
            var client = new RestClient(ApiUrl);
            var request = new RestRequest();

            var response = await client.GetAsync<CurrencyResponse>(request);
            while (response?.Valute == null)
            {
                _date = _date.AddDays(-1);
                ApiUrl = BaseUrl + "/archive/" + _date.ToString("yyyy/MM/dd/").Replace(".", "/") + "/daily_json.js";
                client = new RestClient(ApiUrl);
                response = await client.GetAsync<CurrencyResponse>(request);
                await Task.Delay(100);
            }
            if (response?.Valute !=null)
            {
                Dictionary<string, double>  res = new Dictionary<string, double>();
                var a = response.Valute.Keys.ToList();
                var b = response.Valute.Values.ToList();
                int n = a.Count;
                for (int i = 0; i < n;i++)
                {
                    res.Add(a[i], b[i].Value);
                }
                res.Add("Rub", 1);
                return res;
            }

            return new Dictionary<string, double>();
        }
    }

    // dateFormat - https://www.cbr-xml-daily.ru/daily_json.js
    // 
    public class CurrencyResponse
    {
        public Dictionary<string, CurrencyInfo> Valute { get; set; }
    }

    public class CurrencyInfo
    {
        public string ID { get; set; }
        public string NumCode { get; set; }
        public string CharCode { get; set; }
        public int Nominal { get; set; }
        public string Name { get; set; }
        public double Value { get; set; }
        public double Previous { get; set; }
    }

}
