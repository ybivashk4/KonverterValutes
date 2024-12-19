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
            private string charCode1 = "";
            private string charCode2 = "";
            private CurrencyService _currencyServiece = new CurrencyService();
            public Dictionary<string, double> currencies { get; set; } =
                new Dictionary<string, double> ();
            public ObservableCollection<string> currenciesList { get; set; } =
                new ObservableCollection<string>();
            public ICommand LoadCurrenciesCommand { get; }
            public MyViewModel()
            {
                // Как запустить со старта 
                LoadCurrenciesAsync();
                charCode1 = "";
                charCode2 = "";
                LoadCurrenciesCommand = new Command(async () => await LoadCurrenciesAsync());
            }

            public async Task LoadCurrenciesAsync ()
            {
                var currencies_ = await _currencyServiece.GetCurrenciesAsync(_date);
                var tempCode1 = charCode1;
                var tempCode2 = charCode2;

                currencies.Clear();
                currenciesList.Clear();
                foreach (var currency in currencies_)
                {
                    currencies.Add(currency.Key, currency.Value);
                    currenciesList.Add(currency.Key);
                }
                SelectedCurrency1 = tempCode1;
                SelectedCurrency2 = tempCode2;
            }

            public string EntryText1
            {
                get => _entryText1;
                set
                {
                    if (_entryText1 == value) return;
                    _entryText1 = value;
                    if (value is string str && charCode1 != ""  && charCode2 != "")
                    {
                        _entryText2 = ConvertTo(str, charCode1, charCode2).ToString();
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
                    if (value is string str && charCode1 != "" && charCode2 != "")
                        _entryText1 = ConvertTo(str, charCode2, charCode1).ToString(); // func that convert
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
                    SelectedCurrency2 = charCode2;

                    OnPropertyChanged(nameof(SelectedDate));
                    OnPropertyChanged(nameof(SelectedCurrency2));
                }

            }

            public event PropertyChangedEventHandler? PropertyChanged;
            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            private double ConvertTo(string inputString, string firstChar, string secodChar)
            {
                if (!double.TryParse(inputString, out double num))
                {
                    return 0;
                }

                if (currencies.Count > 0 && firstChar != null && secodChar != null && firstChar != "" && secodChar != "")
                {
                    double k = currencies[firstChar] / currencies[secodChar];
                    return num  * k;
                }
                return num;
            }

            public string SelectedCurrency1 {
                get => charCode1;
                set
                {
                    if (charCode1 != value)
                    {
                        charCode1 = value;
                        OnPropertyChanged(nameof(SelectedCurrency1));
                        EntryText1 = ConvertTo(EntryText2, charCode2, charCode1).ToString();
                        OnPropertyChanged(nameof(EntryText1));
                    }
                    
                }
            }

            public string SelectedCurrency2
            {
                get => charCode2;
                set
                {
                    if (charCode2 != value )
                    {
                        charCode2 = value;
                        OnPropertyChanged(nameof(SelectedCurrency2));
                        // Не работает обновление при событии пикера
                        EntryText2 = ConvertTo(EntryText1, charCode2, charCode1).ToString();
                        OnPropertyChanged(nameof(EntryText2));
                    }
                    
                    
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
                var keys = response.Valute.Keys.ToList();
                var values = response.Valute.Values.ToList();
                int count = keys.Count;
                for (int i = 0; i < count;i++)
                {
                    res.Add(keys[i], values[i].Value / values[i].Nominal);
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
